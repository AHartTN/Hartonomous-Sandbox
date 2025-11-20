namespace Hartonomous.Api.DTOs.Provenance;

public record InfluenceStatistics
{
    public int TotalInfluencingAtoms { get; init; }
    public int DirectInfluences { get; init; }
    public int IndirectInfluences { get; init; }
    public double AverageInfluenceWeight { get; init; }
    public double MaxInfluenceWeight { get; init; }
    public required string SpatialRadius { get; init; }
}
