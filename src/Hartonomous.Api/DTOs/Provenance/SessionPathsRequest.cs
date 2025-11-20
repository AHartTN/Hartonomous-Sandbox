using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Provenance;

/// <summary>
/// Request model for session paths queries with validation.
/// </summary>
public class SessionPathsRequest
{
    /// <summary>
    /// The session identifier.
    /// </summary>
    [Required]
    [Range(1, long.MaxValue, ErrorMessage = "Session ID must be greater than 0")]
    public long SessionId { get; set; }
}