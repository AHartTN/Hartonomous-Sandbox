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

    public List<AtomRelationDTO> Parents { get; set; } = new();
    public List<AtomRelationDTO> Children { get; set; } = new();
}
