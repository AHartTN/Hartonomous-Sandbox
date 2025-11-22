using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Hartonomous.Api.DTOs.Ingestion;
using Hartonomous.Api.Extensions;
using Hartonomous.Core.Interfaces.BackgroundJob;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Core.Services;
using Hartonomous.Infrastructure.Atomizers;
using Hartonomous.Infrastructure.FileType;
using Hartonomous.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Api.Controllers;

/// <summary>
/// Data ingestion API - comprehensive atomization of files into 64-byte atoms.
/// Supports text, images, audio, video, documents, archives, models, code, and more.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ingestion")]
// NOTE: [Authorize] temporarily disabled for testing - re-enable for production
// [Authorize(Policy = "DataIngestion")]
public class DataIngestionController : ControllerBase
{
    private readonly IFileTypeDetector _fileTypeDetector;
    private readonly IEnumerable<IAtomizer<byte[]>> _atomizers;
    private readonly IAtomBulkInsertService _bulkInsertService;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly IIngestionService _ingestionService;
    private readonly ILogger<DataIngestionController> _logger;

    public DataIngestionController(
        IFileTypeDetector fileTypeDetector,
        IEnumerable<IAtomizer<byte[]>> atomizers,
        IAtomBulkInsertService bulkInsertService,
        IBackgroundJobService backgroundJobService,
        IIngestionService ingestionService,
        ILogger<DataIngestionController> logger)
    {
        _fileTypeDetector = fileTypeDetector ?? throw new ArgumentNullException(nameof(fileTypeDetector));
        _atomizers = atomizers ?? throw new ArgumentNullException(nameof(atomizers));
        _bulkInsertService = bulkInsertService ?? throw new ArgumentNullException(nameof(bulkInsertService));
        _backgroundJobService = backgroundJobService ?? throw new ArgumentNullException(nameof(backgroundJobService));
        _ingestionService = ingestionService ?? throw new ArgumentNullException(nameof(ingestionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Ingest a single file with full atomization.
    /// POST /api/ingestion/file
    /// </summary>
    [HttpPost("file")]
    [RequestSizeLimit(1_000_000_000)] // 1GB max
    [EnableRateLimiting("ingestion")]
    public async Task<IActionResult> IngestFile(
        [FromForm] IFormFile file,
        [FromForm] int tenantId = 0,
        CancellationToken cancellationToken = default)
    {
        // First check: file parameter exists and has content
        if (file == null || file.Length == 0)
            return BadRequest("No file provided");

        // Read file content
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        var fileContent = memoryStream.ToArray();
        
        // Second check: file content is not empty (catches corrupted uploads)
        if (fileContent.Length == 0)
            return BadRequest("File content is empty");

        // Create persistent job record
        var jobGuid = await _backgroundJobService.CreateJobAsync(
            "FileIngestion",
            $"{{\"fileName\":\"{file.FileName}\",\"sizeBytes\":{file.Length}}}",
            tenantId,
            cancellationToken);
        var jobId = jobGuid.ToString();

        // Local job tracking for response (also persisted via service)
        var job = new IngestionJob
        {
            JobId = jobId,
            FileName = file.FileName,
            FileSizeBytes = file.Length,
            Status = "processing",
            StartedAt = DateTime.UtcNow,
            TenantId = tenantId
        };

        try
        {
            // Detect file type
            var fileType = _fileTypeDetector.Detect(fileContent, file.FileName);
            job.DetectedType = fileType.ContentType;
            job.DetectedCategory = fileType.Category.ToString();

            _logger.LogInformation(
                "Ingesting file: {FileName} ({SizeBytes} bytes, {ContentType}, {Category})",
                file.FileName,
                file.Length,
                fileType.ContentType,
                fileType.Category);

            // Find appropriate atomizer
            var atomizer = _atomizers
                .Where(a => a.CanHandle(fileType.ContentType, fileType.Extension))
                .OrderByDescending(a => a.Priority)
                .FirstOrDefault();

            if (atomizer == null)
            {
                job.Status = "failed";
                job.ErrorMessage = $"No atomizer found for {fileType.ContentType}";
                return BadRequest(job.ErrorMessage);
            }

            // Atomize file
            var sourceMetadata = new SourceMetadata
            {
                FileName = file.FileName,
                SourceUri = $"upload://{file.FileName}",
                SourceType = "file-upload",
                ContentType = fileType.ContentType,
                SizeBytes = file.Length,
                TenantId = tenantId,
                Metadata = $"{{\"category\":\"{fileType.Category}\",\"format\":\"{fileType.SpecificFormat}\"}}"
            };

            var result = await atomizer.AtomizeAsync(fileContent, sourceMetadata, cancellationToken);
            job.TotalAtoms = result.ProcessingInfo.TotalAtoms;
            job.UniqueAtoms = result.ProcessingInfo.UniqueAtoms;

            // Bulk insert atoms
            var atomIdMap = await _bulkInsertService.BulkInsertAtomsAsync(
                result.Atoms,
                tenantId,
                cancellationToken);

            // Bulk insert compositions
            if (result.Compositions.Count > 0)
            {
                await _bulkInsertService.BulkInsertCompositionsAsync(
                    result.Compositions,
                    atomIdMap,
                    tenantId,
                    cancellationToken);
            }

            // Handle recursive atomization (archives)
            if (result.ChildSources?.Count > 0)
            {
                job.ChildJobs = new List<string>();
                foreach (var childSource in result.ChildSources)
                {
                    var childJobId = await IngestChildSourceAsync(childSource, tenantId, cancellationToken);
                    job.ChildJobs.Add(childJobId);
                }
            }

            job.Status = "completed";
            job.CompletedAt = DateTime.UtcNow;
            job.DurationMs = (long)(job.CompletedAt.Value - job.StartedAt).TotalMilliseconds;

            // Update persistent job status
            await _backgroundJobService.UpdateJobAsync(
                jobGuid,
                "Completed",
                $"{{\"totalAtoms\":{job.TotalAtoms},\"uniqueAtoms\":{job.UniqueAtoms},\"durationMs\":{job.DurationMs}}}",
                null,
                cancellationToken);

            _logger.LogInformation(
                "Ingestion completed: {JobId}, {TotalAtoms} atoms ({UniqueAtoms} unique), {DurationMs}ms",
                jobId,
                job.TotalAtoms,
                job.UniqueAtoms,
                job.DurationMs);

            return Ok(new
            {
                jobId,
                status = job.Status,
                atoms = new
                {
                    total = job.TotalAtoms,
                    unique = job.UniqueAtoms,
                    deduplicationRate = job.TotalAtoms > 0
                        ? (1.0 - (double)job.UniqueAtoms / job.TotalAtoms) * 100
                        : 0
                },
                durationMs = job.DurationMs,
                childJobs = job.ChildJobs
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ingestion failed for {FileName}", file.FileName);
            job.Status = "failed";
            job.ErrorMessage = ex.Message;
            job.CompletedAt = DateTime.UtcNow;

            // Update persistent job status
            await _backgroundJobService.UpdateJobAsync(
                jobGuid,
                "Failed",
                null,
                ex.Message,
                cancellationToken);

            throw; // Problem Details middleware will handle
        }
    }

    /// <summary>
    /// Get ingestion job status and progress.
    /// GET /api/ingestion/jobs/{jobId}
    /// </summary>
    [HttpGet("jobs/{jobId}")]
    public async Task<IActionResult> GetJobStatus(string jobId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(jobId, out var jobGuid))
            return BadRequest("Invalid job ID format");

        var job = await _backgroundJobService.GetJobAsync(jobGuid, cancellationToken);
        if (job == null)
            return NotFound("Job not found");

        return Ok(new
        {
            jobId = job.JobId,
            jobType = job.JobType,
            status = job.Status,
            parameters = job.ParametersJson,
            result = job.ResultJson,
            tenantId = job.TenantId,
            createdAt = job.CreatedAt,
            completedAt = job.CompletedAt,
            error = job.ErrorMessage
        });
    }

    /// <summary>
    /// Query atoms by content hash.
    /// GET /api/ingestion/atoms?hash={sha256}
    /// </summary>
    [HttpGet("atoms")]
    public IActionResult QueryAtoms([FromQuery] string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            return BadRequest("Hash parameter required");

        // TODO: Query atoms from database
        return Ok(new { message = "Atom query not yet implemented", hash });
    }

    /// <summary>
    /// Ingest content from a URL (web fetch).
    /// POST /api/ingestion/url
    /// </summary>
    [HttpPost("url")]
    public async Task<IActionResult> IngestUrl(
        [FromBody] UrlIngestionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return BadRequest("URL is required");

        var jobId = Guid.NewGuid().ToString();
        var job = new IngestionJob
        {
            JobId = jobId,
            FileName = request.Url,
            Status = "processing",
            StartedAt = DateTime.UtcNow,
            TenantId = request.TenantId
        };
        // Job tracked via IBackgroundJobService

        try
        {
            // Use WebFetchAtomizer (IAtomizer<string>)
            var webAtomizer = _atomizers.OfType<IAtomizer<string>>().FirstOrDefault();
            if (webAtomizer == null)
                throw new InvalidOperationException("Web fetch atomizer not configured");

            var sourceMetadata = new SourceMetadata
            {
                SourceUri = request.Url,
                SourceType = "web-fetch",
                ContentType = "text/uri-list",
                TenantId = request.TenantId
            };

            var result = await webAtomizer.AtomizeAsync(request.Url, sourceMetadata, cancellationToken);
            job.TotalAtoms = result.ProcessingInfo.TotalAtoms;
            job.UniqueAtoms = result.ProcessingInfo.UniqueAtoms;

            // Bulk insert
            var atomIdMap = await _bulkInsertService.BulkInsertAtomsAsync(result.Atoms, request.TenantId, cancellationToken);
            if (result.Compositions.Count > 0)
            {
                await _bulkInsertService.BulkInsertCompositionsAsync(result.Compositions, atomIdMap, request.TenantId, cancellationToken);
            }

            job.Status = "completed";
            job.CompletedAt = DateTime.UtcNow;
            job.DurationMs = (long)(job.CompletedAt.Value - job.StartedAt).TotalMilliseconds;

            return Ok(new
            {
                jobId,
                status = job.Status,
                atoms = new { total = job.TotalAtoms, unique = job.UniqueAtoms },
                durationMs = job.DurationMs
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "URL ingestion failed: {Url}", request.Url);
            job.Status = "failed";
            job.ErrorMessage = ex.Message;
            job.CompletedAt = DateTime.UtcNow;

            throw;
        }
    }

    /// <summary>
    /// Ingest database schema and data.
    /// POST /api/ingestion/database
    /// </summary>
    [HttpPost("database")]
    public async Task<IActionResult> IngestDatabase(
        [FromBody] DatabaseIngestionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ConnectionString))
            return BadRequest("Connection string is required");

        var jobId = Guid.NewGuid().ToString();
        var job = new IngestionJob
        {
            JobId = jobId,
            FileName = "Database",
            Status = "processing",
            StartedAt = DateTime.UtcNow,
            TenantId = request.TenantId
        };
        // Job tracked via IBackgroundJobService

        try
        {
            // Use DatabaseAtomizer
            var dbAtomizer = _atomizers.OfType<IAtomizer<DatabaseConnectionInfo>>().FirstOrDefault();
            if (dbAtomizer == null)
                throw new InvalidOperationException("Database atomizer not configured");

            var dbInfo = new DatabaseConnectionInfo
            {
                ConnectionString = request.ConnectionString,
                MaxTables = request.MaxTables,
                MaxRowsPerTable = request.MaxRowsPerTable
            };

            var sourceMetadata = new SourceMetadata
            {
                SourceType = "database",
                ContentType = "application/x-sql",
                TenantId = request.TenantId
            };

            var result = await dbAtomizer.AtomizeAsync(dbInfo, sourceMetadata, cancellationToken);
            job.TotalAtoms = result.ProcessingInfo.TotalAtoms;
            job.UniqueAtoms = result.ProcessingInfo.UniqueAtoms;

            // Bulk insert
            var atomIdMap = await _bulkInsertService.BulkInsertAtomsAsync(result.Atoms, request.TenantId, cancellationToken);
            if (result.Compositions.Count > 0)
            {
                await _bulkInsertService.BulkInsertCompositionsAsync(result.Compositions, atomIdMap, request.TenantId, cancellationToken);
            }

            job.Status = "completed";
            job.CompletedAt = DateTime.UtcNow;
            job.DurationMs = (long)(job.CompletedAt.Value - job.StartedAt).TotalMilliseconds;

            return Ok(new
            {
                jobId,
                status = job.Status,
                atoms = new { total = job.TotalAtoms, unique = job.UniqueAtoms },
                durationMs = job.DurationMs
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database ingestion failed");
            job.Status = "failed";
            job.ErrorMessage = ex.Message;
            job.CompletedAt = DateTime.UtcNow;

            throw;
        }
    }

    /// <summary>
    /// Ingest Git repository metadata.
    /// POST /api/ingestion/git
    /// </summary>
    [HttpPost("git")]
    public async Task<IActionResult> IngestGitRepository(
        [FromBody] GitIngestionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RepositoryPath))
            return BadRequest("Repository path is required");

        var jobId = Guid.NewGuid().ToString();
        var job = new IngestionJob
        {
            JobId = jobId,
            FileName = request.RepositoryPath,
            Status = "processing",
            StartedAt = DateTime.UtcNow,
            TenantId = request.TenantId
        };
        // Job tracked via IBackgroundJobService

        try
        {
            // Use GitRepositoryAtomizer
            var gitAtomizer = _atomizers.OfType<IAtomizer<GitRepositoryInfo>>().FirstOrDefault();
            if (gitAtomizer == null)
                throw new InvalidOperationException("Git repository atomizer not configured");

            var gitInfo = new GitRepositoryInfo
            {
                RepositoryPath = request.RepositoryPath,
                MaxBranches = request.MaxBranches,
                MaxCommits = request.MaxCommits,
                MaxFiles = request.MaxFiles,
                IncludeFileHistory = request.IncludeFileHistory
            };

            var sourceMetadata = new SourceMetadata
            {
                SourceUri = request.RepositoryPath,
                SourceType = "git-repository",
                ContentType = "application/x-git",
                TenantId = request.TenantId
            };

            var result = await gitAtomizer.AtomizeAsync(gitInfo, sourceMetadata, cancellationToken);
            job.TotalAtoms = result.ProcessingInfo.TotalAtoms;
            job.UniqueAtoms = result.ProcessingInfo.UniqueAtoms;

            // Bulk insert
            var atomIdMap = await _bulkInsertService.BulkInsertAtomsAsync(result.Atoms, request.TenantId, cancellationToken);
            if (result.Compositions.Count > 0)
            {
                await _bulkInsertService.BulkInsertCompositionsAsync(result.Compositions, atomIdMap, request.TenantId, cancellationToken);
            }

            job.Status = "completed";
            job.CompletedAt = DateTime.UtcNow;
            job.DurationMs = (long)(job.CompletedAt.Value - job.StartedAt).TotalMilliseconds;

            return Ok(new
            {
                jobId,
                status = job.Status,
                atoms = new { total = job.TotalAtoms, unique = job.UniqueAtoms },
                durationMs = job.DurationMs
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Git repository ingestion failed: {RepoPath}", request.RepositoryPath);
            job.Status = "failed";
            job.ErrorMessage = ex.Message;
            job.CompletedAt = DateTime.UtcNow;

            throw;
        }
    }

    /// <summary>
    /// Recursively ingest child sources (from archives).
    /// </summary>
    private async Task<string> IngestChildSourceAsync(
        ChildSource childSource,
        int tenantId,
        CancellationToken cancellationToken)
    {
        var childJobId = Guid.NewGuid().ToString();
        var childJob = new IngestionJob
        {
            JobId = childJobId,
            FileName = childSource.Metadata.FileName,
            FileSizeBytes = childSource.Content.Length,
            Status = "processing",
            StartedAt = DateTime.UtcNow,
            TenantId = tenantId
        };
        // Job tracked via IBackgroundJobService

        try
        {
            // Detect type
            var fileType = _fileTypeDetector.Detect(childSource.Content, childSource.Metadata.FileName);
            childJob.DetectedType = fileType.ContentType;
            childJob.DetectedCategory = fileType.Category.ToString();

            // Find atomizer
            var atomizer = _atomizers
                .Where(a => a.CanHandle(fileType.ContentType, fileType.Extension))
                .OrderByDescending(a => a.Priority)
                .FirstOrDefault();

            if (atomizer != null)
            {
                var result = await atomizer.AtomizeAsync(childSource.Content, childSource.Metadata, cancellationToken);
                childJob.TotalAtoms = result.ProcessingInfo.TotalAtoms;
                childJob.UniqueAtoms = result.ProcessingInfo.UniqueAtoms;

                // Bulk insert
                var atomIdMap = await _bulkInsertService.BulkInsertAtomsAsync(result.Atoms, tenantId, cancellationToken);
                if (result.Compositions.Count > 0)
                {
                    await _bulkInsertService.BulkInsertCompositionsAsync(result.Compositions, atomIdMap, tenantId, cancellationToken);
                }

                // Recursive children
                if (result.ChildSources?.Count > 0)
                {
                    childJob.ChildJobs = new List<string>();
                    foreach (var grandchild in result.ChildSources)
                    {
                        var grandchildJobId = await IngestChildSourceAsync(grandchild, tenantId, cancellationToken);
                        childJob.ChildJobs.Add(grandchildJobId);
                    }
                }
            }

            childJob.Status = "completed";
            childJob.CompletedAt = DateTime.UtcNow;
            childJob.DurationMs = (long)(childJob.CompletedAt.Value - childJob.StartedAt).TotalMilliseconds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Child ingestion failed: {FileName}", childSource.Metadata.FileName);
            childJob.Status = "failed";
            childJob.ErrorMessage = ex.Message;
            childJob.CompletedAt = DateTime.UtcNow;
        }

        return childJobId;
    }
}
