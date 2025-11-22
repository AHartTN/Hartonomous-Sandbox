namespace Hartonomous.Shared.Contracts.DTOs;

/// <summary>
/// Data transfer object for atom relationships
/// </summary>
public class AtomRelationDTO
{
    public long RelatedAtomId { get; set; }
    public string RelationType { get; set; } = string.Empty;
    public double? SemanticSimilarity { get; set; }
    public string? RelatedAtomicValue { get; set; }
    public string? RelatedModality { get; set; }
}
