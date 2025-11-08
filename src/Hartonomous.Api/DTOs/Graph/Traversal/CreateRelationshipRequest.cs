using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Graph.Traversal;

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
