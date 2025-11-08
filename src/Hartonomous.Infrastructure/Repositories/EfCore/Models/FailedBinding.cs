namespace Hartonomous.Infrastructure.Repositories.EfCore.Models;

/// <summary>
/// A concept that failed to bind
/// </summary>
public class FailedBinding
{
    /// <summary>
    /// The concept that failed to bind
    /// </summary>
    public DiscoveredConcept Concept { get; set; } = null!;

    /// <summary>
    /// Reason for binding failure
    /// </summary>
    public string FailureReason { get; set; } = string.Empty;
}
