using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Graph;

public class GraphQueryRequest
{
    [Required]
    public required string CypherQuery { get; set; }
    
    public Dictionary<string, object>? Parameters { get; set; }
    
    [Range(1, 10000)]
    public int Limit { get; set; } = 100;
}

public class GraphQueryResponse
{
    public required List<Dictionary<string, object>> Results { get; set; }
    public int ResultCount { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public string? Query { get; set; }
}

public class FindRelatedAtomsRequest
{
    [Required]
    public long AtomId { get; set; }
    
    public string? RelationshipType { get; set; } // derives_from, similar_to, co_occurs_with, etc.
    
    [Range(1, 3)]
    public int MaxDepth { get; set; } = 1;
    
    [Range(1, 1000)]
    public int Limit { get; set; } = 50;
    
    public double? MinSimilarity { get; set; }
}

public class FindRelatedAtomsResponse
{
    public long SourceAtomId { get; set; }
    public required List<RelatedAtomEntry> RelatedAtoms { get; set; }
    public int TotalPaths { get; set; }
}

public class RelatedAtomEntry
{
    public long AtomId { get; set; }
    public required string Modality { get; set; }
    public string? CanonicalText { get; set; }
    public required string RelationshipType { get; set; }
    public double? Similarity { get; set; }
    public int Depth { get; set; }
    public required List<string> PathDescription { get; set; }
}

public class TraverseGraphRequest
{
    [Required]
    public long StartAtomId { get; set; }
    
    public long? EndAtomId { get; set; }
    
    public required List<string> AllowedRelationships { get; set; }
    
    [Range(1, 5)]
    public int MaxDepth { get; set; } = 3;
    
    public string TraversalStrategy { get; set; } = "shortest_path"; // shortest_path, all_paths, widest_path
}

public class TraverseGraphResponse
{
    public long StartAtomId { get; set; }
    public long? EndAtomId { get; set; }
    public required List<GraphPath> Paths { get; set; }
    public int TotalPathsFound { get; set; }
}

public class GraphPath
{
    public required List<GraphNode> Nodes { get; set; }
    public required List<GraphRelationship> Relationships { get; set; }
    public int PathLength { get; set; }
    public double? TotalWeight { get; set; }
}

public class GraphNode
{
    public long AtomId { get; set; }
    public required string Modality { get; set; }
    public string? CanonicalText { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
}

public class GraphRelationship
{
    public required string Type { get; set; }
    public long FromAtomId { get; set; }
    public long ToAtomId { get; set; }
    public double? Weight { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
}

public class ExploreConceptRequest
{
    [Required]
    public required string ConceptText { get; set; }
    
    public int? ModelId { get; set; }
    
    [Range(1, 1000)]
    public int TopK { get; set; } = 20;
    
    public double? MinSimilarity { get; set; } = 0.7;
}

public class ExploreConceptResponse
{
    public required string ConceptText { get; set; }
    public required List<ConceptNode> Nodes { get; set; }
    public required List<ConceptRelationship> Relationships { get; set; }
    public required Dictionary<string, int> ModalityBreakdown { get; set; }
}

public class ConceptNode
{
    public long AtomId { get; set; }
    public required string Modality { get; set; }
    public string? CanonicalText { get; set; }
    public double Similarity { get; set; }
    public int ConnectionCount { get; set; }
}

public class ConceptRelationship
{
    public long FromAtomId { get; set; }
    public long ToAtomId { get; set; }
    public required string Type { get; set; }
    public double? Strength { get; set; }
}

public class GetGraphStatsResponse
{
    public long TotalNodes { get; set; }
    public long TotalRelationships { get; set; }
    public required Dictionary<string, long> NodesByModality { get; set; }
    public required Dictionary<string, long> RelationshipsByType { get; set; }
    public double AverageDegree { get; set; }
    public int MaxDegree { get; set; }
    public long IsolatedNodes { get; set; }
}

public class CreateRelationshipRequest
{
    [Required]
    public long FromAtomId { get; set; }
    
    [Required]
    public long ToAtomId { get; set; }
    
    [Required]
    public required string RelationshipType { get; set; }
    
    public double? Weight { get; set; }
    
    public Dictionary<string, object>? Properties { get; set; }
}

public class CreateRelationshipResponse
{
    public long FromAtomId { get; set; }
    public long ToAtomId { get; set; }
    public required string RelationshipType { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
}
