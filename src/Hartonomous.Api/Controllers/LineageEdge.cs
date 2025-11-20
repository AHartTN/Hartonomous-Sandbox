namespace Hartonomous.Api.Controllers;

public record LineageEdge
{
    public long From { get; init; }
    public long To { get; init; }
    public required string Type { get; init; }
    public double Weight { get; init; }
    public required string Label { get; init; }
}
