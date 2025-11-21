using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Interfaces.Cognition;

/// <summary>
/// Cognitive activation service for advanced AI reasoning.
/// Provides neural activation patterns and cognitive state management.
/// </summary>
public interface ICognitiveService
{
    /// <summary>
    /// Perform cognitive activation on atom set.
    /// Calls sp_CognitiveActivation stored procedure.
    /// </summary>
    /// <param name="atomIds">Comma-separated atom IDs to activate</param>
    /// <param name="activationThreshold">Minimum activation strength (0.0-1.0)</param>
    /// <param name="spreadDepth">How far to spread activation (1-10)</param>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Activation result with affected atoms</returns>
    Task<CognitiveActivationResult> ActivateAsync(
        string atomIds,
        float activationThreshold = 0.3f,
        int spreadDepth = 3,
        int tenantId = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compute spatial projection for atom embeddings.
    /// Calls sp_ComputeSpatialProjection stored procedure.
    /// </summary>
    /// <param name="atomId">Atom to project</param>
    /// <param name="dimensions">Target dimensionality (2 or 3 for visualization)</param>
    /// <param name="method">Projection method (PCA, TSNE, UMAP)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Spatial coordinates</returns>
    Task<SpatialProjectionResult> ProjectAsync(
        long atomId,
        int dimensions = 3,
        string method = "PCA",
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of cognitive activation.
/// </summary>
/// <param name="ActivatedCount">Number of atoms activated</param>
/// <param name="TotalActivation">Sum of activation strengths</param>
/// <param name="ProcessingTimeMs">Processing duration</param>
public record CognitiveActivationResult(
    int ActivatedCount,
    float TotalActivation,
    int ProcessingTimeMs);

/// <summary>
/// Result of spatial projection.
/// </summary>
/// <param name="AtomId">Projected atom ID</param>
/// <param name="Coordinates">Spatial coordinates (X, Y, Z)</param>
/// <param name="Method">Projection method used</param>
public record SpatialProjectionResult(
    long AtomId,
    float[] Coordinates,
    string Method);
