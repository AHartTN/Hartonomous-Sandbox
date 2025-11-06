using Hartonomous.Core.ValueObjects;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Provides ensemble inference capabilities across multiple models.
/// Aggregates predictions from multiple models to improve accuracy and robustness.
/// </summary>
public interface IEnsembleInferenceService
{
    /// <summary>
    /// Executes ensemble inference across multiple models using a stored procedure.
    /// Combines predictions from multiple models with optional weighting.
    /// </summary>
    /// <param name="inputData">Input payload or prompt sent to the ensemble.</param>
    /// <param name="modelIds">Identifiers of models participating in the ensemble.</param>
    /// <param name="weights">Optional weighting factors for each model (must match modelIds length).</param>
    /// <param name="cancellationToken">Token for cancelling database work.</param>
    /// <returns>Aggregate inference result with model contributions and confidence metrics.</returns>
    Task<EnsembleInferenceResult> EnsembleInferenceAsync(
        string inputData,
        IReadOnlyList<int> modelIds,
        IReadOnlyList<float>? weights = null,
        CancellationToken cancellationToken = default);
}
