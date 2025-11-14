using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Hartonomous.Data.Entities;

namespace Hartonomous.Infrastructure.Services.Jobs;

public sealed class InferenceJobProcessor
{
    private readonly HartonomousDbContext _context;
    private readonly IInferenceService _inferenceService;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<InferenceJobProcessor> _logger;

    public InferenceJobProcessor(
        HartonomousDbContext context,
        IInferenceService inferenceService,
        IEmbeddingService embeddingService,
        ILogger<InferenceJobProcessor> logger)
    {
        _context = context;
        _inferenceService = inferenceService;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task<bool> ProcessJobAsync(long inferenceId, CancellationToken cancellationToken)
    {
        var job = await _context.InferenceRequests
            .FirstOrDefaultAsync(r => r.InferenceId == inferenceId, cancellationToken);

        if (job == null)
        {
            _logger.LogWarning("Inference job {InferenceId} not found", inferenceId);
            return false;
        }

        if (job.Status != "Pending")
        {
            _logger.LogWarning("Inference job {InferenceId} is not in Pending status: {Status}", inferenceId, job.Status);
            return false;
        }

        job.Status = "InProgress";
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            switch (job.TaskType?.ToLowerInvariant())
            {
                case "text_generation":
                case "text-generation":
                    await ProcessTextGenerationAsync(job, cancellationToken);
                    break;

                case "ensemble":
                case "ensemble_inference":
                    await ProcessEnsembleAsync(job, cancellationToken);
                    break;

                case "distillation":
                case "model_distillation":
                    await ProcessDistillationAsync(job, cancellationToken);
                    break;

                default:
                    throw new InvalidOperationException($"Unknown task type: {job.TaskType}");
            }

            job.Status = "Completed";
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Inference job {InferenceId} failed", inferenceId);
            job.Status = "Failed";
            job.OutputMetadata = System.Text.Json.JsonSerializer.Serialize(new { error = ex.Message });
            await _context.SaveChangesAsync(cancellationToken);
            return false;
        }
    }

    private async Task ProcessTextGenerationAsync(InferenceRequest job, CancellationToken cancellationToken)
    {
        var input = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(job.InputData ?? "{}");
        if (input == null || !input.TryGetValue("prompt", out var promptElem) || promptElem.ValueKind != System.Text.Json.JsonValueKind.String)
        {
            throw new InvalidOperationException("Text generation requires 'prompt' in InputData");
        }

        var prompt = promptElem.GetString() ?? throw new InvalidOperationException("Prompt cannot be null");
        
        var maxTokens = input.TryGetValue("maxTokens", out var maxTokensElem) && maxTokensElem.ValueKind == System.Text.Json.JsonValueKind.Number
            ? maxTokensElem.GetInt32()
            : 128;
            
        var temperature = input.TryGetValue("temperature", out var tempElem) && tempElem.ValueKind == System.Text.Json.JsonValueKind.Number
            ? (float)tempElem.GetDouble()
            : 0.7f;

        var promptEmbedding = await _embeddingService.EmbedTextAsync(prompt, cancellationToken);
        var result = await _inferenceService.GenerateViaSpatialAsync(promptEmbedding, maxTokens, temperature, cancellationToken);

        job.OutputData = System.Text.Json.JsonSerializer.Serialize(new
        {
            generatedText = result.GeneratedText,
            tokenCount = result.TokenCount,
            averageConfidence = result.AverageConfidence
        });
        job.Confidence = result.AverageConfidence;
        job.TotalDurationMs = 0;
    }

    private async Task ProcessEnsembleAsync(InferenceRequest job, CancellationToken cancellationToken)
    {
        var input = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(job.InputData ?? "{}");
        if (input == null || !input.TryGetValue("inputData", out var inputDataElem) || inputDataElem.ValueKind != System.Text.Json.JsonValueKind.String)
        {
            throw new InvalidOperationException("Ensemble requires 'inputData' in InputData");
        }

        var inputData = inputDataElem.GetString() ?? throw new InvalidOperationException("Input data cannot be null");

        var modelIds = new List<int>();
        if (input.TryGetValue("modelIds", out var modelIdsElem) && modelIdsElem.ValueKind == System.Text.Json.JsonValueKind.String)
        {
            var modelIdsJson = modelIdsElem.GetString();
            if (!string.IsNullOrEmpty(modelIdsJson))
            {
                var parsed = System.Text.Json.JsonSerializer.Deserialize<List<int>>(modelIdsJson);
                if (parsed != null)
                    modelIds = parsed;
            }
        }

        if (modelIds.Count == 0)
        {
            throw new InvalidOperationException("Ensemble requires at least one model ID");
        }

        var result = await _inferenceService.EnsembleInferenceAsync(inputData, modelIds, null, cancellationToken);

        job.OutputData = result.OutputData;
        job.Confidence = result.ConfidenceScore;
        job.TotalDurationMs = 0;
    }

    private async Task ProcessDistillationAsync(InferenceRequest job, CancellationToken cancellationToken)
    {
        var input = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(job.InputData ?? "{}");
        if (input == null || !input.TryGetValue("parentModelId", out var parentModelIdElem) || parentModelIdElem.ValueKind != System.Text.Json.JsonValueKind.Number)
        {
            throw new InvalidOperationException("Distillation requires 'parentModelId' in InputData");
        }

        var parentModelId = parentModelIdElem.GetInt32();

        if (!input.TryGetValue("studentName", out var studentNameElem) || studentNameElem.ValueKind != System.Text.Json.JsonValueKind.String)
        {
            throw new InvalidOperationException("Distillation requires 'studentName' in InputData");
        }

        var studentName = studentNameElem.GetString() ?? throw new InvalidOperationException("Student name cannot be null");

        var importanceThreshold = input.TryGetValue("importanceThreshold", out var thresholdElem) && thresholdElem.ValueKind == System.Text.Json.JsonValueKind.Number
            ? thresholdElem.GetDouble()
            : 0.05;

        List<int>? layerIndices = null;
        if (input.TryGetValue("layerIndices", out var layerIndicesElem))
        {
            layerIndices = System.Text.Json.JsonSerializer.Deserialize<List<int>>(layerIndicesElem.GetRawText());
        }

        var connection = _context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        using var command = connection.CreateCommand();
        command.CommandText = "dbo.sp_ExtractStudentModel";
        command.CommandType = System.Data.CommandType.StoredProcedure;
        command.CommandTimeout = 600;

        var parentParam = command.CreateParameter();
        parentParam.ParameterName = "@ParentModelId";
        parentParam.Value = parentModelId;
        command.Parameters.Add(parentParam);

        var layerParam = command.CreateParameter();
        layerParam.ParameterName = "@layer_subset";
        layerParam.Value = layerIndices != null && layerIndices.Count > 0 
            ? string.Join(",", layerIndices) 
            : DBNull.Value;
        command.Parameters.Add(layerParam);

        var thresholdParam = command.CreateParameter();
        thresholdParam.ParameterName = "@importance_threshold";
        thresholdParam.Value = importanceThreshold;
        command.Parameters.Add(thresholdParam);

        var nameParam = command.CreateParameter();
        nameParam.ParameterName = "@NewModelName";
        nameParam.Value = studentName;
        command.Parameters.Add(nameParam);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            var studentModelId = reader.GetInt32(0);
            var originalAtoms = reader.GetInt64(2);
            var studentAtoms = reader.GetInt64(3);
            var compressionRatio = reader.IsDBNull(4) ? 0.0 : reader.GetDouble(4);

            job.OutputData = System.Text.Json.JsonSerializer.Serialize(new
            {
                studentModelId,
                studentName,
                parentModelId,
                originalTensorAtoms = originalAtoms,
                studentTensorAtoms = studentAtoms,
                compressionRatio,
                retentionPercent = compressionRatio
            });
            job.Confidence = compressionRatio;
        }
        else
        {
            throw new InvalidOperationException("Distillation procedure returned no results");
        }
    }
}
