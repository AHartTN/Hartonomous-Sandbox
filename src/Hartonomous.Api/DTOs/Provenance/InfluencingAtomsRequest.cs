using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Provenance;

/// <summary>
/// Request model for influencing atoms queries with validation.
/// </summary>
public class InfluencingAtomsRequest
{
    /// <summary>
    /// The result atom to analyze.
    /// </summary>
    [Required]
    [Range(1, long.MaxValue, ErrorMessage = "Atom ID must be greater than 0")]
    public long AtomId { get; set; }

    /// <summary>
    /// Minimum influence threshold (0.0-1.0, default: 0.1).
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "Influence threshold must be between 0.0 and 1.0")]
    public double MinInfluence { get; set; } = 0.1;
}