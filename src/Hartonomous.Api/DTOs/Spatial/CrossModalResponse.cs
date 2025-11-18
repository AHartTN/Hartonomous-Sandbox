namespace Hartonomous.Api.DTOs.Spatial;

public sealed class CrossModalResponse
{
    public required List<CrossModalResult> Results { get; set; }
    public int TotalResults { get; set; }
    public string? SourceModality { get; set; }
    public string? TargetModality { get; set; }
}
