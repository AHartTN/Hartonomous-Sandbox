namespace Hartonomous.Core.Entities;

/// <summary>
/// Cross-reference catalog for all weights across dimension-specific tables.
/// Provides unified metadata and enables deduplication via content hashing.
/// </summary>
public class WeightCatalog
{
    public long CatalogId { get; set; }
    public long WeightId { get; set; }
    public int ModelId { get; set; }
    public int LayerIdx { get; set; }
    public string ComponentType { get; set; } = null!;
    public string? PositionMetadata { get; set; } // JSON
    public float? ImportanceScore { get; set; }
    public byte[] ContentHash { get; set; } = null!; // SHA256
    public DateTime CreatedDate { get; set; }

    // Navigation
    public virtual ModelArchitecture? Model { get; set; }
}
