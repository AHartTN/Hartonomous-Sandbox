namespace Hartonomous.Api.DTOs.Provenance;

public class RelatedResource
{
    public string ResourceId { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public int Distance { get; set; }
}
