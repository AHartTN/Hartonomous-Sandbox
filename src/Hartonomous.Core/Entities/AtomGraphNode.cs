using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hartonomous.Core.Entities;

/// <summary>
/// SQL Graph node table for Atom relationships
/// Represents atoms as nodes in a graph structure
/// </summary>
[Table("AtomGraphNodes", Schema = "graph")]
public class AtomGraphNode
{
    [Key]
    public long NodeId { get; set; }

    [Required]
    public long AtomId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column(TypeName = "nvarchar(100)")]
    public string NodeType { get; set; } = string.Empty; // Atom, Concept, Model, etc.

    [MaxLength(500)]
    [Column(TypeName = "nvarchar(500)")]
    public string? NodeLabel { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? Properties { get; set; } // JSON properties

    [Required]
    [Column(TypeName = "datetime2")]
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "datetime2")]
    public DateTime? UpdatedUtc { get; set; }

    // Navigation property to the actual Atom
    [ForeignKey(nameof(AtomId))]
    public virtual Atom? Atom { get; set; }

    // Graph navigation properties (SQL Graph specific)
    public virtual ICollection<AtomGraphEdge> OutgoingEdges { get; set; } = new List<AtomGraphEdge>();
    public virtual ICollection<AtomGraphEdge> IncomingEdges { get; set; } = new List<AtomGraphEdge>();
}