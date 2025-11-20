using Hartonomous.Api.DTOs.Provenance;

namespace Hartonomous.Api.DTOs.Reasoning;


public record ReasoningPath
{
    public required string PathId { get; init; }
    public required PathStatus Status { get; init; }
    public double Confidence { get; init; }
    public int Steps { get; init; }
    public string? Reason { get; init; }
    public required List<GeoJsonPoint> Waypoints { get; init; }
}
