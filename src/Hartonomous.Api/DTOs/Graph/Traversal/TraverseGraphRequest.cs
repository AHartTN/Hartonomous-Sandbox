using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Graph.Traversal;

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
