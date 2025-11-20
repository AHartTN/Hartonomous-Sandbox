using System.Collections.Generic;

namespace Hartonomous.Api.DTOs.Provenance;


public record GeoJsonLineString
{
    public required GeoJsonType Type { get; init; } // LineString
    public required List<double[]> Coordinates { get; init; } // [[lon, lat], [lon, lat], ...]
}
