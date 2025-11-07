using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hartonomous.Core.Entities;

/// <summary>
/// SQL Graph edge table connecting AtomGraphNodes
/// Defines typed relationships between atoms
/// </summary>
[Table("AtomGraphEdges", Schema = "graph")]
public class AtomGraphEdge
{
    [Key]
    public long EdgeId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column(TypeName = "nvarchar(50)")]
    public string EdgeType { get; set; } = string.Empty; // 'DerivedFrom', 'ComponentOf', 'SimilarTo', etc.

    [Required]
    [Column(TypeName = "float")]
    [Range(0.0, 1.0)]
    public double Weight { get; set; } = 1.0;

    [Column(TypeName = "nvarchar(max)")]
    public string? Metadata { get; set; } // JSON: {"confidence": 0.95, "method": "cosine_similarity"}

    [Column(TypeName = "datetime2")]
    public DateTime? ValidFrom { get; set; }

    [Column(TypeName = "datetime2")]
    public DateTime? ValidTo { get; set; }

    [Required]
    [Column(TypeName = "datetime2")]
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    // SQL Graph requires explicit FROM and TO node references
    // These will be configured in the entity configuration
    public virtual AtomGraphNode? FromNode { get; set; }
    public virtual AtomGraphNode? ToNode { get; set; }
}