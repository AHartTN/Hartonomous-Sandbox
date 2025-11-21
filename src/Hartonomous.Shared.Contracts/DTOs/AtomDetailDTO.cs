namespace Hartonomous.Shared.Contracts.DTOs;

/// <summary>
/// Data transfer object for atom details with semantic relationships
/// </summary>
public class AtomDetailDTO
{
    public long AtomId { get; set; }
    public byte[] ContentHash { get; set; } = Array.Empty<byte>();
    public string AtomicValue { get; set; } = string.Empty;
    public string? CanonicalText { get; set; }
    public string Modality { get; set; } = string.Empty;
    public string? Subtype { get; set; }
    public string? ContentType { get; set; }
    public string? Metadata { get; set; }
    public int TenantId { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Parent atoms (atoms that this atom is derived from)
    /// </summary>
    public List<AtomRelationDTO> Parents { get; set; } = new();

    /// <summary>
    /// Child atoms (atoms derived from this atom)
    /// </summary>
    public List<AtomRelationDTO> Children { get; set; } = new();
}

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
