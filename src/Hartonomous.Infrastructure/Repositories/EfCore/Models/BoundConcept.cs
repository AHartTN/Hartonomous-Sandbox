namespace Hartonomous.Infrastructure.Repositories.EfCore.Models;

/// <summary>
/// A successfully bound concept
/// </summary>
public class BoundConcept
{
    /// <summary>
    /// The discovered concept
    /// </summary>
    public DiscoveredConcept Concept { get; set; } = null!;

    /// <summary>
    /// ID of the created or updated concept entity
    /// </summary>
    public long ConceptEntityId { get; set; }

    /// <summary>
    /// Relationships established
    /// </summary>
    public IReadOnlyList<string> Relationships { get; set; } = Array.Empty<string>();
}
