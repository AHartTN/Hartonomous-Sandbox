using System.Collections.Generic;

namespace Hartonomous.Api.DTOs.Provenance;


public record GeoJsonFeature
{
    public required GeoJsonType Type { get; init; } // Feature
    public required object Geometry { get; init; } // GeoJsonPoint, GeoJsonLineString, etc.
    public required Dictionary<string, object> Properties { get; init; }
}
