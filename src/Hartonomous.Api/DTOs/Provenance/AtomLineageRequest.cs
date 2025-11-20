using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Provenance;

/// <summary>
/// Request model for atom lineage queries with validation.
/// </summary>
public class AtomLineageRequest
{
    /// <summary>
    /// The unique identifier of the atom.
    /// </summary>
    [Required]
    [Range(1, long.MaxValue, ErrorMessage = "Atom ID must be greater than 0")]
    public long AtomId { get; set; }

    /// <summary>
    /// Maximum graph traversal depth (default: 5).
    /// </summary>
    [Range(1, 20, ErrorMessage = "Max depth must be between 1 and 20")]
    public int? MaxDepth { get; set; } = 5;
}