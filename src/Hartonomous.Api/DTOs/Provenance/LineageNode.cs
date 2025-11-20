namespace Hartonomous.Api.DTOs.Provenance;

public record LineageNode
{
    public long AtomId { get; init; }
    public required LineageNodeType Type { get; init; }
    public int Depth { get; init; }
    public required string Label { get; init; }
    public double Confidence { get; init; }
    public required GeoJsonPoint Location { get; init; }
}
