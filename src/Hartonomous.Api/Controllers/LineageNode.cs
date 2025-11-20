namespace Hartonomous.Api.Controllers;

public record LineageNode
{
    public long AtomId { get; init; }
    public required string Type { get; init; }
    public int Depth { get; init; }
    public required string Label { get; init; }
    public double Confidence { get; init; }
    public required GeoJsonPoint Location { get; init; }
}
