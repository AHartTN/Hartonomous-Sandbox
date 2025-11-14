using Hartonomous.Core.Entities;
using Hartonomous.Core.Enums;
using Hartonomous.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Hartonomous.Data.Entities;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// Adapter that implements IInferenceOrchestrator by delegating to IInferenceService.
/// </summary>
public sealed class InferenceOrchestratorAdapter : IInferenceOrchestrator
{
    private readonly IInferenceService _inferenceService;
    private readonly IModelRepository _modelRepository;
    private readonly ILogger<InferenceOrchestratorAdapter> _logger;

    public InferenceOrchestratorAdapter(
        IInferenceService inferenceService,
        IModelRepository modelRepository,
        ILogger<InferenceOrchestratorAdapter> logger)
    {
        _inferenceService = inferenceService ?? throw new ArgumentNullException(nameof(inferenceService));
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<InferenceResult> ExecuteEnsembleAsync(
        IReadOnlyList<int> modelIds,
        object inputData,
        CancellationToken cancellationToken = default)
    {
        var inputJson = inputData is string str ? str : System.Text.Json.JsonSerializer.Serialize(inputData);

        var result = await _inferenceService.EnsembleInferenceAsync(
            inputJson,
            modelIds,
            weights: null,
            cancellationToken);

        return new InferenceResult
        {
            Output = result.OutputData,
            Confidence = result.ConfidenceScore,
            ModelId = string.Join(",", modelIds),
            Duration = TimeSpan.Zero,
            Metadata = new Dictionary<string, object>
            {
                ["InferenceId"] = result.InferenceId,
                ["ModelContributions"] = result.ModelContributions,
                ["CompletedTimestamp"] = result.CompletedTimestamp
            }
        };
    }

    public async Task<InferenceResult> ExecuteSingleModelAsync(
        int modelId,
        object inputData,
        CancellationToken cancellationToken = default)
    {
        var inputJson = inputData is string str ? str : System.Text.Json.JsonSerializer.Serialize(inputData);

        var result = await _inferenceService.EnsembleInferenceAsync(
            inputJson,
            new[] { modelId },
            weights: null,
            cancellationToken);

        return new InferenceResult
        {
            Output = result.OutputData,
            Confidence = result.ConfidenceScore,
            ModelId = modelId.ToString(),
            Duration = TimeSpan.Zero,
            Metadata = new Dictionary<string, object>
            {
                ["InferenceId"] = result.InferenceId,
                ["ModelContributions"] = result.ModelContributions,
                ["CompletedTimestamp"] = result.CompletedTimestamp
            }
        };
    }

    public async Task<IReadOnlyList<int>> GetAvailableModelsAsync(
        string? modality = null,
        double? minConfidence = null,
        CancellationToken cancellationToken = default)
    {
        var models = await _modelRepository.GetActiveModelsAsync(cancellationToken);

        if (!string.IsNullOrEmpty(modality))
        {
            models = models.Where(m =>
                m.ModelMetadatum != null &&
                m.ModelMetadatum.SupportedModalities != null &&
                m.ModelMetadatum.SupportedModalities.Contains(modality, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        return models.Select(m => m.ModelId).ToList();
    }

    public async Task<List<Atom>> SemanticSearchAsync(
        float[] embedding,
        int maxResults = 100,
        CancellationToken cancellationToken = default)
    {
        // Delegate to IInferenceService.SemanticSearchAsync
        var searchResults = await _inferenceService.SemanticSearchAsync(
            embedding,
            maxResults,
            cancellationToken);

        // Extract Atom entities from search results
        return searchResults
            .Select(result => result.Embedding.Atom)
            .Where(atom => atom != null)
            .ToList()!;
    }
}
