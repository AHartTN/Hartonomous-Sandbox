namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Orchestrates ensemble inference across multiple models with voting strategies.
/// Manages model selection, parallel execution, and result aggregation.
/// </summary>
public interface IInferenceOrchestrator
{
    /// <summary>
    /// Executes inference using an ensemble of models with majority voting.
    /// </summary>
    /// <param name="modelIds">List of model IDs to include in the ensemble.</param>
    /// <param name="inputData">Input data for inference.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregated inference result with confidence score.</returns>
    Task<InferenceResult> ExecuteEnsembleAsync(
        IReadOnlyList<int> modelIds,
        object inputData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes inference using a single model.
    /// </summary>
    /// <param name="modelId">Model ID to use for inference.</param>
    /// <param name="inputData">Input data for inference.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Inference result from the specified model.</returns>
    Task<InferenceResult> ExecuteSingleModelAsync(
        int modelId,
        object inputData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available models for inference, optionally filtered by criteria.
    /// </summary>
    /// <param name="modality">Optional modality filter (text, image, audio, etc.).</param>
    /// <param name="minConfidence">Optional minimum confidence threshold.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of available model IDs.</returns>
    Task<IReadOnlyList<int>> GetAvailableModelsAsync(
        string? modality = null,
        double? minConfidence = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs semantic search for candidate atoms using a vector embedding.
    /// </summary>
    /// <param name="embedding">Vector embedding for semantic similarity search.</param>
    /// <param name="maxResults">Maximum number of results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matching atoms ranked by similarity.</returns>
    Task<List<Entities.Atom>> SemanticSearchAsync(
        float[] embedding,
        int maxResults = 100,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result from inference execution.
/// </summary>
public sealed class InferenceResult
{
    public required object Output { get; init; }
    public double Confidence { get; init; }
    public string? ModelId { get; init; }
    public TimeSpan Duration { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}
