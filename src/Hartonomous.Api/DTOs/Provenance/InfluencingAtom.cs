namespace Hartonomous.Api.DTOs.Provenance;

public record InfluencingAtom
{
    public long AtomId { get; init; }
    public double InfluenceWeight { get; init; }
    public required InfluenceType InfluenceType { get; init; }
    public required string Label { get; init; }
    public required GeoJsonPoint Location { get; init; }
    public double Distance { get; init; }
}
