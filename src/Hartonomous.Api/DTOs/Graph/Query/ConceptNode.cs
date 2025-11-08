namespace Hartonomous.Api.DTOs.Graph.Query;

public class ConceptNode
{
    public long AtomId { get; set; }
    public required string Modality { get; set; }
    public string? CanonicalText { get; set; }
    public double Similarity { get; set; }
    public int ConnectionCount { get; set; }
}
