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

public class GraphStatsResponse
{
    public long TotalNodes { get; set; }
    public long TotalRelationships { get; set; }
    public required List<string> Modalities { get; set; }
    public required Dictionary<string, long> ModalityCounts { get; set; }
    public required List<string> RelationshipTypes { get; set; }
    public required Dictionary<string, long> RelationshipTypeCounts { get; set; }
    public double Density { get; set; }
    public long? ConnectedComponents { get; set; }
    public Dictionary<string, object>? ComponentDistribution { get; set; }
    public double AverageDegree { get; set; }
}

public class RelationshipAnalysisRequest
{
    public string? ModalityFilter { get; set; }
    public int? TopRelationships { get; set; } = 20;
}

public class RelationshipAnalysisResponse
{
    public string? ModalityFilter { get; set; }
    public required List<RelationshipStats> RelationshipStats { get; set; }
    public List<CrossModalityStats>? CrossModalityStats { get; set; }
    public long TotalRelationshipsAnalyzed { get; set; }
}

public class RelationshipStats
{
    public required string RelationshipType { get; set; }
    public long Count { get; set; }
    public double? AverageWeight { get; set; }
    public double? MinWeight { get; set; }
    public double? MaxWeight { get; set; }
    public double? WeightStdDev { get; set; }
    public required List<string> SourceModalities { get; set; }
    public required List<string> TargetModalities { get; set; }
}

public class CrossModalityStats
{
    public required string SourceModality { get; set; }
    public required string TargetModality { get; set; }
    public required string RelationshipType { get; set; }
    public long Count { get; set; }
}

public class CentralityAnalysisRequest
{
    public string Algorithm { get; set; } = "degree";
    public int? TopNodes { get; set; } = 100;
}

public class CentralityAnalysisResponse
{
    public required string Algorithm { get; set; }
    public required List<CentralityScore> CentralityScores { get; set; }
    public int TotalNodesAnalyzed { get; set; }
}

public class CentralityScore
{
    public long AtomId { get; set; }
    public double Score { get; set; }
    public int Rank { get; set; }
    public string? Modality { get; set; }
    public string? CanonicalText { get; set; }
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

// ============================================================================
// SQL Server Graph DTOs (AS NODE / AS EDGE syntax)
// ============================================================================

/// <summary>
/// Request to create a node in SQL Server graph (graph.AtomGraphNodes)
/// </summary>
public class SqlGraphCreateNodeRequest
{
    [Required]
    public long AtomId { get; set; }
    
    [Required]
    public required string NodeType { get; set; } // 'Atom', 'Model', 'Concept', etc.
    
    public Dictionary<string, object>? Metadata { get; set; }
    
    public float? EmbeddingX { get; set; }
    public float? EmbeddingY { get; set; }
    public float? EmbeddingZ { get; set; }
}

/// <summary>
/// Response from creating a SQL Server graph node
/// </summary>
public class SqlGraphCreateNodeResponse
{
    public long NodeId { get; set; }
    public long AtomId { get; set; }
    public required string NodeType { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Request to create an edge in SQL Server graph (graph.AtomGraphEdges)
/// </summary>
public class SqlGraphCreateEdgeRequest
{
    [Required]
    public long FromNodeId { get; set; }
    
    [Required]
    public long ToNodeId { get; set; }
    
    [Required]
    public required string EdgeType { get; set; } // 'DerivedFrom', 'ComponentOf', 'SimilarTo', etc.
    
    [Range(0.0, 1.0)]
    public double Weight { get; set; } = 1.0;
    
    public Dictionary<string, object>? Metadata { get; set; }
    
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}

/// <summary>
/// Response from creating a SQL Server graph edge
/// </summary>
public class SqlGraphCreateEdgeResponse
{
    public long EdgeId { get; set; }
    public long FromNodeId { get; set; }
    public long ToNodeId { get; set; }
    public required string EdgeType { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Request to traverse SQL Server graph using MATCH syntax
/// </summary>
public class SqlGraphTraverseRequest
{
    [Required]
    public long StartAtomId { get; set; }
    
    public long? EndAtomId { get; set; }
    
    [Range(1, 5)]
    public int MaxDepth { get; set; } = 3;
    
    public string? EdgeTypeFilter { get; set; } // Optional: 'DerivedFrom', 'SimilarTo', etc.
    
    public string Direction { get; set; } = "outbound"; // outbound, inbound, both
}

/// <summary>
/// Response from SQL Server graph traversal
/// </summary>
public class SqlGraphTraverseResponse
{
    public long StartAtomId { get; set; }
    public long? EndAtomId { get; set; }
    public required List<SqlGraphPathEntry> Paths { get; set; }
    public int TotalPathsFound { get; set; }
    public int ExecutionTimeMs { get; set; }
}

/// <summary>
/// Single path in SQL Server graph traversal result
/// </summary>
public class SqlGraphPathEntry
{
    public required List<long> NodeIds { get; set; }
    public required List<long> AtomIds { get; set; }
    public required List<string> EdgeTypes { get; set; }
    public int PathLength { get; set; }
    public double TotalWeight { get; set; }
}

/// <summary>
/// Request to find shortest path in SQL Server graph using SHORTEST_PATH
/// </summary>
public class SqlGraphShortestPathRequest
{
    [Required]
    public long StartAtomId { get; set; }
    
    [Required]
    public long EndAtomId { get; set; }
    
    public string? EdgeTypeFilter { get; set; }
}

/// <summary>
/// Response from shortest path query
/// </summary>
public class SqlGraphShortestPathResponse
{
    public long StartAtomId { get; set; }
    public long EndAtomId { get; set; }
    public SqlGraphPathEntry? ShortestPath { get; set; }
    public bool PathFound { get; set; }
    public int ExecutionTimeMs { get; set; }
}
