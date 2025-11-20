namespace Hartonomous.Api.DTOs.Provenance;

public record GeoJsonPoint
{
    public required GeoJsonType Type { get; init; } // Point
    public required double[] Coordinates { get; init; } // [longitude, latitude]
}
