using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Interfaces.Reconstruction;

/// <summary>
/// Service for reconstruction operations.
/// </summary>
public interface IReconstructionService
{
    /// <summary>
    /// Reconstructs text from atom.
    /// Calls sp_ReconstructText stored procedure.
    /// </summary>
    Task<string> ReconstructTextAsync(
        long atomId,
        int tenantId = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reconstructs image from atom.
    /// Calls sp_ReconstructImage stored procedure.
    /// </summary>
    Task<byte[]> ReconstructImageAsync(
        long atomId,
        int tenantId = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds images by dominant color.
    /// Calls sp_FindImagesByColor stored procedure.
    /// </summary>
    Task<IEnumerable<ImageResult>> FindImagesByColorAsync(
        string colorHex,
        int tolerance = 10,
        int topK = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds model weights in value range.
    /// Calls sp_FindWeightsByValueRange stored procedure.
    /// </summary>
    Task<IEnumerable<WeightResult>> FindWeightsByRangeAsync(
        float minValue,
        float maxValue,
        int modelId,
        CancellationToken cancellationToken = default);
}

public record ImageResult(
    long AtomId,
    string DominantColor,
    float ColorDistance);

public record WeightResult(
    int LayerIndex,
    string LayerName,
    long WeightIndex,
    float Value);
