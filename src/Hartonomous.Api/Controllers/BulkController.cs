using Hartonomous.Api.Common;
using Hartonomous.Api.DTOs.Bulk;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Hartonomous.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class BulkController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<BulkController> _logger;
    private readonly string _connectionString;

    public BulkController(
        IConfiguration configuration,
        ILogger<BulkController> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Database connection string not found");
    }

    [HttpPost("ingest")]
    [ProducesResponseType(typeof(ApiResponse<BulkIngestResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> BulkIngest(
        [FromBody] BulkIngestRequest request,
        CancellationToken cancellationToken)
    {
        if (request?.Items == null || request.Items.Count == 0)
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "At least one item is required"));
        }

        if (request.Items.Count > 10000)
        {
            return BadRequest(ApiResponse<object>.Fail("TOO_MANY_ITEMS", "Maximum 10,000 items per bulk request"));
        }

        try
        {
            var jobId = Guid.NewGuid().ToString("N");
            var createdAt = DateTime.UtcNow;

            _logger.LogInformation("Creating bulk ingestion job {JobId} with {ItemCount} items",
                jobId, request.Items.Count);

            // Create job record
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var createJobQuery = @"
                INSERT INTO BulkJobs (JobId, JobType, Status, TotalItems, ProcessedItems, FailedItems, CallbackUrl, CreatedAt, Metadata)
                VALUES (@JobId, 'INGEST', 'PENDING', @TotalItems, 0, 0, @CallbackUrl, @CreatedAt, @Metadata)";

            await using (var command = new SqlCommand(createJobQuery, connection))
            {
                command.Parameters.AddWithValue("@JobId", jobId);
                command.Parameters.AddWithValue("@TotalItems", request.Items.Count);
                command.Parameters.AddWithValue("@CallbackUrl", request.CallbackUrl ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@CreatedAt", createdAt);
                command.Parameters.AddWithValue("@Metadata", request.Metadata != null 
                    ? System.Text.Json.JsonSerializer.Serialize(request.Metadata) 
                    : (object)DBNull.Value);

                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            // Insert job items for processing
            var insertItemsQuery = @"
                INSERT INTO BulkJobItems (JobId, ItemIndex, Modality, CanonicalText, BinaryData, ContentUrl, Metadata, Status)
                VALUES (@JobId, @ItemIndex, @Modality, @CanonicalText, @BinaryData, @ContentUrl, @Metadata, 'PENDING')";

            for (int i = 0; i < request.Items.Count; i++)
            {
                var item = request.Items[i];

                await using var command = new SqlCommand(insertItemsQuery, connection);
                command.Parameters.AddWithValue("@JobId", jobId);
                command.Parameters.AddWithValue("@ItemIndex", i);
                command.Parameters.AddWithValue("@Modality", item.Modality);
                command.Parameters.AddWithValue("@CanonicalText", item.CanonicalText ?? (object)DBNull.Value);
                
                // Convert base64 to binary if provided
                byte[]? binaryData = null;
                if (!string.IsNullOrWhiteSpace(item.BinaryDataBase64))
                {
                    try
                    {
                        binaryData = Convert.FromBase64String(item.BinaryDataBase64);
                    }
                    catch (FormatException)
                    {
                        _logger.LogWarning("Invalid base64 data for item {Index} in job {JobId}", i, jobId);
                    }
                }
                
                command.Parameters.AddWithValue("@BinaryData", binaryData ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ContentUrl", item.ContentUrl ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Metadata", item.Metadata != null 
                    ? System.Text.Json.JsonSerializer.Serialize(item.Metadata) 
                    : (object)DBNull.Value);

                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            // Queue job for processing if async
            if (request.ProcessAsync)
            {
                // TODO: Send message to Azure Service Bus / Event Hub for background processing
                _logger.LogInformation("Job {JobId} queued for async processing", jobId);
            }
            else
            {
                // Synchronous processing (for small batches)
                await ProcessBulkJobAsync(jobId, cancellationToken).ConfigureAwait(false);
            }

            var statusUrl = $"{Request.Scheme}://{Request.Host}/api/v1/bulk/status/{jobId}";

            return Ok(ApiResponse<BulkIngestResponse>.Ok(new BulkIngestResponse
            {
                JobId = jobId,
                Status = request.ProcessAsync ? "PENDING" : "PROCESSING",
                TotalItems = request.Items.Count,
                ProcessedItems = 0,
                FailedItems = 0,
                CreatedAt = createdAt,
                CallbackUrl = request.CallbackUrl,
                StatusUrl = statusUrl
            }));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error creating bulk job");
            return StatusCode(500, ApiResponse<object>.Fail("DATABASE_ERROR", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create bulk ingestion job");
            return StatusCode(500, ApiResponse<object>.Fail("JOB_CREATION_FAILED", ex.Message));
        }
    }

    [HttpGet("status/{jobId}")]
    [ProducesResponseType(typeof(ApiResponse<BulkJobStatusResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetJobStatus(
        string jobId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "JobId is required"));
        }

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            // Get job metadata
            var jobQuery = @"
                SELECT JobId, Status, TotalItems, ProcessedItems, FailedItems, CreatedAt, StartedAt, CompletedAt, ErrorMessage
                FROM BulkJobs
                WHERE JobId = @JobId";

            BulkJobStatusResponse? response = null;

            await using (var command = new SqlCommand(jobQuery, connection))
            {
                command.Parameters.AddWithValue("@JobId", jobId);

                await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    return NotFound(ApiResponse<object>.Fail("JOB_NOT_FOUND", $"Job {jobId} not found"));
                }

                var totalItems = reader.GetInt32(2);
                var processedItems = reader.GetInt32(3);
                var failedItems = reader.GetInt32(4);
                var successItems = processedItems - failedItems;
                var createdAt = reader.GetDateTime(5);
                var startedAt = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6);
                var completedAt = reader.IsDBNull(7) ? (DateTime?)null : reader.GetDateTime(7);

                TimeSpan? totalDuration = null;
                TimeSpan? estimatedRemaining = null;

                if (completedAt.HasValue && startedAt.HasValue)
                {
                    totalDuration = completedAt.Value - startedAt.Value;
                }
                else if (startedAt.HasValue && processedItems > 0)
                {
                    var elapsed = DateTime.UtcNow - startedAt.Value;
                    var avgTimePerItem = elapsed.TotalSeconds / processedItems;
                    var remainingItems = totalItems - processedItems;
                    estimatedRemaining = TimeSpan.FromSeconds(avgTimePerItem * remainingItems);
                }

                response = new BulkJobStatusResponse
                {
                    JobId = reader.GetString(0),
                    Status = reader.GetString(1),
                    TotalItems = totalItems,
                    ProcessedItems = processedItems,
                    SuccessItems = successItems,
                    FailedItems = failedItems,
                    ProgressPercentage = totalItems > 0 ? (double)processedItems / totalItems * 100 : 0,
                    CreatedAt = createdAt,
                    StartedAt = startedAt,
                    CompletedAt = completedAt,
                    TotalDuration = totalDuration,
                    EstimatedTimeRemaining = estimatedRemaining,
                    Results = new List<BulkJobItemResult>(),
                    ErrorMessage = reader.IsDBNull(8) ? null : reader.GetString(8)
                };
            }

            // Get item results
            var itemsQuery = @"
                SELECT ItemIndex, Status, AtomId, ContentHash, IsDuplicate, ErrorMessage, ProcessingTimeMs
                FROM BulkJobItems
                WHERE JobId = @JobId
                ORDER BY ItemIndex";

            await using (var command = new SqlCommand(itemsQuery, connection))
            {
                command.Parameters.AddWithValue("@JobId", jobId);

                await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    response.Results.Add(new BulkJobItemResult
                    {
                        ItemIndex = reader.GetInt32(0),
                        Status = reader.GetString(1),
                        AtomId = reader.IsDBNull(2) ? null : reader.GetInt64(2),
                        ContentHash = reader.IsDBNull(3) ? null : reader.GetString(3),
                        IsDuplicate = !reader.IsDBNull(4) && reader.GetBoolean(4),
                        ErrorMessage = reader.IsDBNull(5) ? null : reader.GetString(5),
                        ProcessingTime = reader.IsDBNull(6) ? null : TimeSpan.FromMilliseconds(reader.GetInt32(6))
                    });
                }
            }

            return Ok(ApiResponse<BulkJobStatusResponse>.Ok(response));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error retrieving job status");
            return StatusCode(500, ApiResponse<object>.Fail("DATABASE_ERROR", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve job status");
            return StatusCode(500, ApiResponse<object>.Fail("STATUS_FAILED", ex.Message));
        }
    }

    [HttpPost("cancel")]
    [ProducesResponseType(typeof(ApiResponse<CancelBulkJobResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> CancelJob(
        [FromBody] CancelBulkJobRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.JobId))
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "JobId is required"));
        }

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var updateQuery = @"
                UPDATE BulkJobs
                SET Status = 'CANCELLED',
                    CompletedAt = @CancelledAt,
                    ErrorMessage = @Reason
                OUTPUT DELETED.Status, DELETED.ProcessedItems
                WHERE JobId = @JobId AND Status IN ('PENDING', 'PROCESSING')";

            await using var command = new SqlCommand(updateQuery, connection);
            command.Parameters.AddWithValue("@JobId", request.JobId);
            command.Parameters.AddWithValue("@CancelledAt", DateTime.UtcNow);
            command.Parameters.AddWithValue("@Reason", request.Reason ?? "Cancelled by user");

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                return NotFound(ApiResponse<object>.Fail("JOB_NOT_FOUND", 
                    $"Job {request.JobId} not found or already completed/cancelled"));
            }

            var previousStatus = reader.GetString(0);
            var itemsProcessed = reader.GetInt32(1);

            _logger.LogInformation("Cancelled job {JobId} (was {PreviousStatus})", request.JobId, previousStatus);

            return Ok(ApiResponse<CancelBulkJobResponse>.Ok(new CancelBulkJobResponse
            {
                JobId = request.JobId,
                Success = true,
                Status = "CANCELLED",
                ItemsProcessedBeforeCancellation = itemsProcessed,
                Message = "Job cancelled successfully"
            }));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error cancelling job");
            return StatusCode(500, ApiResponse<object>.Fail("DATABASE_ERROR", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel job");
            return StatusCode(500, ApiResponse<object>.Fail("CANCEL_FAILED", ex.Message));
        }
    }

    [HttpPost("upload")]
    [ProducesResponseType(typeof(ApiResponse<BulkUploadResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [RequestSizeLimit(1_073_741_824)] // 1GB max
    public async Task<IActionResult> BulkUpload(
        [FromForm] BulkUploadRequest request,
        [FromForm] List<IFormFile> files,
        CancellationToken cancellationToken)
    {
        if (request == null || files == null || files.Count == 0)
        {
            return BadRequest(ApiResponse<object>.Fail("INVALID_REQUEST", "At least one file is required"));
        }

        if (files.Count > 1000)
        {
            return BadRequest(ApiResponse<object>.Fail("TOO_MANY_FILES", "Maximum 1,000 files per upload"));
        }

        try
        {
            var jobId = Guid.NewGuid().ToString("N");
            long totalBytes = files.Sum(f => f.Length);

            _logger.LogInformation("Creating bulk upload job {JobId} with {FileCount} files ({TotalMB} MB)",
                jobId, files.Count, totalBytes / 1024.0 / 1024.0);

            // Create job and queue items
            var items = new List<BulkContentItem>();

            foreach (var file in files)
            {
                await using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
                var bytes = memoryStream.ToArray();

                items.Add(new BulkContentItem
                {
                    Modality = request.Modality,
                    CanonicalText = request.ExtractMetadata ? file.FileName : null,
                    BinaryDataBase64 = Convert.ToBase64String(bytes),
                    Metadata = new Dictionary<string, object>
                    {
                        ["fileName"] = file.FileName,
                        ["contentType"] = file.ContentType,
                        ["fileSize"] = file.Length
                    }
                });
            }

            // Reuse BulkIngest logic
            var ingestRequest = new BulkIngestRequest
            {
                Items = items,
                ModelId = request.ModelId,
                ProcessAsync = true,
                Metadata = request.GlobalMetadata
            };

            var ingestResult = await BulkIngest(ingestRequest, cancellationToken);

            if (ingestResult is OkObjectResult okResult)
            {
                return Ok(ApiResponse<BulkUploadResponse>.Ok(new BulkUploadResponse
                {
                    JobId = jobId,
                    FilesReceived = files.Count,
                    TotalBytes = totalBytes,
                    Status = "PENDING",
                    CreatedAt = DateTime.UtcNow
                }));
            }

            return StatusCode(500, ApiResponse<object>.Fail("UPLOAD_FAILED", "Failed to queue upload job"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk upload failed");
            return StatusCode(500, ApiResponse<object>.Fail("UPLOAD_FAILED", ex.Message));
        }
    }

    [HttpGet("jobs")]
    [ProducesResponseType(typeof(ApiResponse<ListBulkJobsResponse>), 200)]
    public async Task<IActionResult> ListJobs(
        [FromQuery] ListBulkJobsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var whereClause = new List<string>();
            if (!string.IsNullOrWhiteSpace(request.Status))
                whereClause.Add("Status = @Status");
            if (request.StartDate.HasValue)
                whereClause.Add("CreatedAt >= @StartDate");
            if (request.EndDate.HasValue)
                whereClause.Add("CreatedAt <= @EndDate");

            var whereSQL = whereClause.Count > 0 ? "WHERE " + string.Join(" AND ", whereClause) : "";

            var query = $@"
                SELECT JobId, Status, TotalItems, ProcessedItems, CreatedAt, CompletedAt
                FROM BulkJobs
                {whereSQL}
                ORDER BY CreatedAt DESC
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY;

                SELECT COUNT(*) FROM BulkJobs {whereSQL};";

            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Offset", (request.PageNumber - 1) * request.PageSize);
            command.Parameters.AddWithValue("@PageSize", request.PageSize);
            if (!string.IsNullOrWhiteSpace(request.Status))
                command.Parameters.AddWithValue("@Status", request.Status);
            if (request.StartDate.HasValue)
                command.Parameters.AddWithValue("@StartDate", request.StartDate.Value);
            if (request.EndDate.HasValue)
                command.Parameters.AddWithValue("@EndDate", request.EndDate.Value);

            var jobs = new List<BulkJobSummary>();
            int totalCount = 0;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var totalItems = reader.GetInt32(2);
                var processedItems = reader.GetInt32(3);
                var createdAt = reader.GetDateTime(4);
                var completedAt = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5);

                jobs.Add(new BulkJobSummary
                {
                    JobId = reader.GetString(0),
                    Status = reader.GetString(1),
                    TotalItems = totalItems,
                    ProcessedItems = processedItems,
                    ProgressPercentage = totalItems > 0 ? (double)processedItems / totalItems * 100 : 0,
                    CreatedAt = createdAt,
                    CompletedAt = completedAt,
                    Duration = completedAt.HasValue ? completedAt.Value - createdAt : null
                });
            }

            if (await reader.NextResultAsync(cancellationToken).ConfigureAwait(false) &&
                await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                totalCount = reader.GetInt32(0);
            }

            return Ok(ApiResponse<ListBulkJobsResponse>.Ok(new ListBulkJobsResponse
            {
                Jobs = jobs,
                TotalJobs = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            }, new ApiMetadata
            {
                TotalCount = totalCount,
                Page = request.PageNumber,
                PageSize = request.PageSize,
                Extra = new Dictionary<string, object>
                {
                    ["totalPages"] = (int)Math.Ceiling((double)totalCount / request.PageSize)
                }
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list bulk jobs");
            return StatusCode(500, ApiResponse<object>.Fail("LIST_FAILED", ex.Message));
        }
    }

    private async Task ProcessBulkJobAsync(string jobId, CancellationToken cancellationToken)
    {
        // This would be implemented as a background worker/service
        // For now, just update status to PROCESSING
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var updateQuery = "UPDATE BulkJobs SET Status = 'PROCESSING', StartedAt = @StartedAt WHERE JobId = @JobId";
        await using var command = new SqlCommand(updateQuery, connection);
        command.Parameters.AddWithValue("@JobId", jobId);
        command.Parameters.AddWithValue("@StartedAt", DateTime.UtcNow);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        // TODO: Process items in batches
        // TODO: Update progress in BulkJobs and BulkJobItems tables
        // TODO: Call callback URL when complete
    }
}
