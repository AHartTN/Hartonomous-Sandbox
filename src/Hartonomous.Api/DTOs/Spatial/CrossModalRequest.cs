namespace Hartonomous.Api.DTOs.Spatial;

public sealed class CrossModalRequest
{
    public string? TextQuery { get; set; }
    public SpatialCoordinate? SpatialQuery { get; set; }
    public string? ModalityFilter { get; set; }
    public int? TopK { get; set; }
}
