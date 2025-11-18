using System.Data;
using Hartonomous.Api.DTOs.Models;
using Hartonomous.Core.Interfaces;
using Hartonomous.Shared.Contracts.Errors;
using Hartonomous.Shared.Contracts.Requests.Paging;
using Hartonomous.Shared.Contracts.Responses;
using Hartonomous.Shared.Contracts.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Hartonomous.Api.Controllers;

[Route("api/models")]
public sealed class ModelsController : ApiControllerBase
{
    private readonly IModelIngestionService _modelIngestionService;
    private readonly IIngestionStatisticsService _statisticsService;
    private readonly string _connectionString;
    private readonly ILogger<ModelsController> _logger;

    public ModelsController(
        IModelIngestionService modelIngestionService,
        IIngestionStatisticsService statisticsService,
        IConfiguration configuration,
        ILogger<ModelsController> logger)
    {
        _modelIngestionService = modelIngestionService ?? throw new ArgumentNullException(nameof(modelIngestionService));
        _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string not configured");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ModelSummary>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ModelSummary>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<PagedResult<ModelSummary>>>> GetAsync(
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        PagingOptions paging;

        try
        {
            paging = PagingOptions.Create(pageNumber, pageSize);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            var error = ErrorDetailFactory.Validation(ex.Message, ex.ParamName);
            return BadRequest(Failure<PagedResult<ModelSummary>>(new[] { error }));
        }

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Database does paging, sorting, counting in one efficient query
            await using var command = new SqlCommand(@"
                SELECT 
                    ModelId,
                    ModelName,
                    ModelType,
                    ParameterCount,
                    IngestionDate,
                    COUNT(*) OVER() AS TotalCount
                FROM dbo.Models
                ORDER BY IngestionDate DESC, ModelName
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY",
                connection);

            command.Parameters.AddWithValue("@Offset", (paging.PageNumber - 1) * paging.PageSize);
            command.Parameters.AddWithValue("@PageSize", paging.PageSize);

            var models = new List<ModelSummary>();
            int totalCount = 0;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                if (totalCount == 0)
                    totalCount = reader.GetInt32(reader.GetOrdinal("TotalCount"));

                models.Add(new ModelSummary
                {
                    ModelId = reader.GetInt32(0),
                    ModelName = reader.GetString(1),
                    ModelType = reader.IsDBNull(2) ? null : reader.GetString(2),
                    ParameterCount = reader.IsDBNull(3) ? null : reader.GetInt64(3),
                    IngestionDate = reader.IsDBNull(4) ? null : reader.GetDateTime(4)
                });
            }

            var result = new PagedResult<ModelSummary>(models, paging.PageNumber, paging.PageSize, totalCount);

            return Ok(Success(result, new Dictionary<string, object?>
            {
                ["returned"] = models.Count,
                ["total"] = totalCount
            }));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error retrieving models");
            return StatusCode(500, Failure<PagedResult<ModelSummary>>(new[] 
            { 
                ErrorDetailFactory.InternalServerError("DATABASE_ERROR", ex.Message) 
            }));
        }
    }

    [HttpGet("{modelId:int}")]
    [ProducesResponseType(typeof(ApiResponse<ModelDetail>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ModelDetail>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ModelDetail>>> GetByIdAsync(int modelId, CancellationToken cancellationToken)
    {
        if (modelId <= 0)
        {
            return BadRequest(Failure<ModelDetail>(new[] { ValidationError("Model identifier must be positive.", nameof(modelId)) }));
        }

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(@"
                SELECT 
                    m.ModelId, m.ModelName, m.ModelType, m.ParameterCount, 
                    m.IngestionDate, m.Architecture,
                    mm.SupportedTasks, mm.SupportedModalities,
                    (SELECT COUNT(*) FROM dbo.ModelLayers WHERE ModelId = m.ModelId) AS LayerCount
                FROM dbo.Models m
                LEFT JOIN dbo.ModelMetadata mm ON mm.ModelId = m.ModelId
                WHERE m.ModelId = @ModelId",
                connection);

            command.Parameters.AddWithValue("@ModelId", modelId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return NotFound(Failure<ModelDetail>(new[] 
                { 
                    ErrorDetailFactory.NotFound("model", modelId.ToString()) 
                }));
            }

            var detail = new ModelDetail
            {
                ModelId = reader.GetInt32(0),
                ModelName = reader.GetString(1),
                ModelType = reader.IsDBNull(2) ? null : reader.GetString(2),
                ParameterCount = reader.IsDBNull(3) ? null : reader.GetInt64(3),
                IngestionDate = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                Architecture = reader.IsDBNull(5) ? null : reader.GetString(5),
                SupportedTasks = reader.IsDBNull(6) ? null : reader.GetString(6),
                SupportedModalities = reader.IsDBNull(7) ? null : reader.GetString(7),
                LayerCount = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
                Layers = new List<ModelLayerInfo>() // Could load in second query if needed
            };

            return Ok(Success(detail, new Dictionary<string, object?>
            {
                ["layerCount"] = detail.LayerCount,
                ["hasMetadata"] = !string.IsNullOrEmpty(detail.SupportedTasks)
            }));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error retrieving model {ModelId}", modelId);
            return StatusCode(500, Failure<ModelDetail>(new[] 
            { 
                ErrorDetailFactory.InternalServerError("DATABASE_ERROR", ex.Message) 
            }));
        }
    }

    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<ModelStatsResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ModelStatsResponse>>> GetStatsAsync(CancellationToken cancellationToken)
    {
        var stats = await _statisticsService.GetStatsAsync(cancellationToken).ConfigureAwait(false);

        var response = new ModelStatsResponse(
            stats.TotalModels,
            stats.TotalParameters,
            (int)stats.TotalLayers,
            stats.ArchitectureBreakdown);

        return Ok(Success(response));
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<ModelIngestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ModelIngestResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ModelIngestResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ModelIngestResponse>>> IngestAsync(
        [FromForm] ModelIngestRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ModelFile is null || request.ModelFile.Length == 0)
        {
            return BadRequest(Failure<ModelIngestResponse>(new[] { MissingField(nameof(request.ModelFile)) }));
        }

        var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{Path.GetExtension(request.ModelFile.FileName)}");

        try
        {
            await using (var fileStream = System.IO.File.Create(tempFilePath))
            {
                await request.ModelFile.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
            }

            var modelId = await _modelIngestionService
                .IngestAsync(tempFilePath, request.ModelName, cancellationToken)
                .ConfigureAwait(false);

            // Query fresh model details
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(@"
                SELECT ModelId, ModelName, Architecture, ParameterCount, 
                       (SELECT COUNT(*) FROM dbo.ModelLayers WHERE ModelId = @ModelId) AS LayerCount
                FROM dbo.Models
                WHERE ModelId = @ModelId",
                connection);
            
            command.Parameters.AddWithValue("@ModelId", modelId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                _logger.LogWarning("Model {ModelId} could not be loaded after ingestion.", modelId);
                return StatusCode(500, Failure<ModelIngestResponse>(new[] 
                { 
                    ErrorDetailFactory.InternalServerError("MODEL_NOT_FOUND", "Model ingested but not retrievable") 
                }));
            }

            var response = new ModelIngestResponse(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? "unknown" : reader.GetString(2),
                reader.IsDBNull(3) ? 0 : reader.GetInt64(3),
                reader.IsDBNull(4) ? 0 : reader.GetInt32(4));

            return Ok(Success(response));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Model ingestion failed for uploaded file {FileName}", request.ModelFile.FileName);
            var error = ErrorDetailFactory.Validation(ex.Message, nameof(request.ModelFile));
            return BadRequest(Failure<ModelIngestResponse>(new[] { error }));
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning(ex, "Model file disappeared during ingestion for {FileName}", request.ModelFile.FileName);
            var error = ErrorDetailFactory.Validation("The uploaded model file could not be processed.", nameof(request.ModelFile));
            return BadRequest(Failure<ModelIngestResponse>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected failure ingesting model {FileName}", request.ModelFile.FileName);
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "An unexpected error occurred while ingesting the model.");
            return StatusCode(StatusCodes.Status500InternalServerError, Failure<ModelIngestResponse>(new[] { error }));
        }
        finally
        {
            TryDeleteTemp(tempFilePath);
        }
    }

    private static void TryDeleteTemp(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
        }
        catch
        {
            // Ignore cleanup failures.
        }
    }



    [HttpPost("{modelId:int}/distill")]
    [ProducesResponseType(typeof(ApiResponse<JobSubmittedResponse>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiResponse<JobSubmittedResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<JobSubmittedResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<JobSubmittedResponse>>> DistillModelAsync(
        int modelId,
        [FromBody] DTOs.Models.DistillationRequest request,
        CancellationToken cancellationToken)
    {
        if (modelId <= 0)
        {
            return BadRequest(Failure<JobSubmittedResponse>(new[] { ValidationError("Model identifier must be positive.", nameof(modelId)) }));
        }

        if (request is null || string.IsNullOrWhiteSpace(request.StudentName))
        {
            return BadRequest(Failure<JobSubmittedResponse>(new[] { ValidationError("StudentName is required.", nameof(request.StudentName)) }));
        }

        // Verify parent model exists
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        
        await using var checkCommand = new SqlCommand(
            "SELECT COUNT(*) FROM dbo.Models WHERE ModelId = @ModelId",
            connection);
        checkCommand.Parameters.AddWithValue("@ModelId", modelId);
        
        var exists = (int)(await checkCommand.ExecuteScalarAsync(cancellationToken) ?? 0) > 0;
        if (!exists)
        {
            return NotFound(Failure<JobSubmittedResponse>(new[] 
            { 
                ErrorDetailFactory.NotFound("parent model", modelId.ToString()) 
            }));
        }

        try
        {
            var inputData = System.Text.Json.JsonSerializer.Serialize(new
            {
                parentModelId = modelId,
                studentName = request.StudentName,
                layerIndices = request.LayerIndices,
                importanceThreshold = request.ImportanceThreshold
            });

            var job = new InferenceRequest
            {
                TaskType = "model_distillation",
                InputData = inputData,
                Status = "Pending",
                CorrelationId = Guid.NewGuid().ToString()
            };

            using var context = new HartonomousDbContext(new DbContextOptionsBuilder<HartonomousDbContext>()
                .UseSqlServer(_connectionString)
                .Options);

            context.InferenceRequests.Add(job);
            await context.SaveChangesAsync(cancellationToken);

            var response = new JobSubmittedResponse
            {
                JobId = job.InferenceId,
                Status = "Pending",
                StatusUrl = $"/api/inference/jobs/{job.InferenceId}"
            };

            return Accepted($"/api/inference/jobs/{job.InferenceId}", Success(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit distillation job for model {ModelId}", modelId);
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "Failed to submit distillation job");
            return StatusCode(StatusCodes.Status500InternalServerError, Failure<JobSubmittedResponse>(new[] { error }));
        }
    }

    [HttpGet("{modelId:int}/layers")]
    [ProducesResponseType(typeof(ApiResponse<List<DTOs.Models.LayerDetail>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<DTOs.Models.LayerDetail>>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<List<DTOs.Models.LayerDetail>>>> GetModelLayersAsync(
        int modelId,
        [FromQuery] double? minImportance,
        CancellationToken cancellationToken)
    {
        if (modelId <= 0)
        {
            return BadRequest(Failure<List<DTOs.Models.LayerDetail>>(new[] { ValidationError("Model identifier must be positive.", nameof(modelId)) }));
        }

        var model = await _modelRepository.GetByIdAsync(modelId, cancellationToken).ConfigureAwait(false);
        if (model is null)
        {
            var error = ErrorDetailFactory.NotFound("model", modelId.ToString());
            return NotFound(Failure<List<DTOs.Models.LayerDetail>>(new[] { error }));
        }

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var query = @"
                SELECT 
                    l.LayerId,
                    l.LayerIdx,
                    l.LayerName,
                    l.LayerType,
                    l.ParameterCount,
                    l.TensorShape,
                    l.TensorDtype,
                    l.CacheHitRate,
                    l.AvgComputeTimeMs,
                    COUNT(ta.TensorAtomId) AS TensorAtomCount,
                    AVG(ta.ImportanceScore) AS AvgImportance
                FROM dbo.ModelLayers l
                LEFT JOIN dbo.TensorAtoms ta ON ta.LayerId = l.LayerId
                WHERE l.ModelId = @ModelId
                GROUP BY l.LayerId, l.LayerIdx, l.LayerName, l.LayerType, l.ParameterCount, 
                         l.TensorShape, l.TensorDtype, l.CacheHitRate, l.AvgComputeTimeMs
                ORDER BY l.LayerIdx";

            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ModelId", modelId);

            var layers = new List<DTOs.Models.LayerDetail>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var avgImportance = reader.IsDBNull(10) ? (double?)null : (double)reader.GetFloat(10);
                
                if (minImportance.HasValue && avgImportance.HasValue && avgImportance.Value < minImportance.Value)
                {
                    continue;
                }

                layers.Add(new DTOs.Models.LayerDetail
                {
                    LayerId = reader.GetInt64(0),
                    LayerIdx = reader.GetInt32(1),
                    LayerName = reader.IsDBNull(2) ? null : reader.GetString(2),
                    LayerType = reader.IsDBNull(3) ? null : reader.GetString(3),
                    ParameterCount = reader.IsDBNull(4) ? null : reader.GetInt64(4),
                    TensorShape = reader.IsDBNull(5) ? null : reader.GetString(5),
                    TensorDtype = reader.IsDBNull(6) ? null : reader.GetString(6),
                    CacheHitRate = reader.IsDBNull(7) ? null : (double)reader.GetFloat(7),
                    AvgComputeTimeMs = reader.IsDBNull(8) ? null : (double)reader.GetFloat(8),
                    TensorAtomCount = reader.GetInt32(9),
                    AvgImportanceScore = avgImportance
                });
            }

            var metadata = new Dictionary<string, object?>
            {
                ["modelId"] = modelId,
                ["modelName"] = model.ModelName,
                ["totalLayers"] = layers.Count
            };

            return Ok(Success(layers, metadata));
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error retrieving layers for model {ModelId}", modelId);
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "Failed to retrieve model layers", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, Failure<List<DTOs.Models.LayerDetail>>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve layers for model {ModelId}", modelId);
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "An unexpected error occurred while retrieving layers.");
            return StatusCode(StatusCodes.Status500InternalServerError, Failure<List<DTOs.Models.LayerDetail>>(new[] { error }));
        }
    }
}
