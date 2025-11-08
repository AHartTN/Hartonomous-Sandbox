namespace Hartonomous.Api.DTOs.Graph.Query;

public class FindRelatedAtomsResponse
{
    public long SourceAtomId { get; set; }
    public required List<RelatedAtomEntry> RelatedAtoms { get; set; }
    public int TotalPaths { get; set; }
}
