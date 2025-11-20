namespace Hartonomous.Core.Interfaces.Ingestion;

/// <summary>
/// Spatial position in 3D+M space.
/// </summary>
public class SpatialPosition
{
    /// <summary>
    /// X coordinate (e.g., column, pixel X, tensor X).
    /// </summary>
    public required double X { get; init; }

    /// <summary>
    /// Y coordinate (e.g., row, pixel Y, tensor Y).
    /// </summary>
    public required double Y { get; init; }

    /// <summary>
    /// Z coordinate (e.g., depth, layer, tensor Z).
    /// </summary>
    public double Z { get; init; }

    /// <summary>
    /// M coordinate (measure - e.g., time, importance, confidence).
    /// </summary>
    public double? M { get; init; }

    /// <summary>
    /// Convert to SQL Server GEOMETRY Well-Known Text.
    /// </summary>
    public string ToWkt()
    {
        if (M.HasValue)
            return $"POINT ({X} {Y} {Z} {M.Value})";
        return $"POINT ({X} {Y} {Z})";
    }
}
