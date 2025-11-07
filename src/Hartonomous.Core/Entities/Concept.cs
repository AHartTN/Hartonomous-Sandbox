using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Discovered concepts from unsupervised learning
/// Stored in provenance schema for lineage tracking
/// </summary>
[Table("Concepts", Schema = "provenance")]
public class Concept
{
    [Key]
    public long ConceptId { get; set; }

    [Required]
    [MaxLength(200)]
    [Column(TypeName = "nvarchar(200)")]
    public string ConceptName { get; set; } = string.Empty;

    [Column(TypeName = "nvarchar(max)")]
    public string? Description { get; set; }

    [Required]
    [Column(TypeName = "varbinary(max)")]
    public byte[] CentroidVector { get; set; } = Array.Empty<byte>();

    [Required]
    public int VectorDimension { get; set; }

    public int MemberCount { get; set; } = 0;

    [Column(TypeName = "float")]
    public double? CoherenceScore { get; set; }

    [Column(TypeName = "float")]
    public double? SeparationScore { get; set; }

    [Required]
    [MaxLength(100)]
    [Column(TypeName = "nvarchar(100)")]
    public string DiscoveryMethod { get; set; } = string.Empty; // kmeans, dbscan, etc.

    [Required]
    public int ModelId { get; set; }

    [Required]
    [Column(TypeName = "datetime2")]
    public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "datetime2")]
    public DateTime? LastUpdatedAt { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    // Navigation property
    [ForeignKey(nameof(ModelId))]
    public virtual Model? Model { get; set; }
}