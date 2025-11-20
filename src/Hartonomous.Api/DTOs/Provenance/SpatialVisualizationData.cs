using System.Collections.Generic;

namespace Hartonomous.Api.DTOs.Provenance;

/// <summary>
/// GeoJSON FeatureCollection for spatial visualization.
/// </summary>

public record SpatialVisualizationData
{
    public required GeoJsonType Type { get; init; } // FeatureCollection
    public required List<GeoJsonFeature> Features { get; init; }
    public double[]? BoundingBox { get; init; }
}
