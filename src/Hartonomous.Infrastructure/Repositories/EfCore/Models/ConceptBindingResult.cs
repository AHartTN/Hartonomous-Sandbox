namespace Hartonomous.Infrastructure.Repositories.EfCore.Models;

/// <summary>
/// Result of concept binding operation
/// </summary>
public class ConceptBindingResult
{
    /// <summary>
    /// Unique ID for this binding session
    /// </summary>
    public Guid BindingId { get; set; }

    /// <summary>
    /// Successfully bound concepts
    /// </summary>
    public IReadOnlyList<BoundConcept> BoundConcepts { get; set; } = Array.Empty<BoundConcept>();

    /// <summary>
    /// Concepts that failed to bind
    /// </summary>
    public IReadOnlyList<FailedBinding> FailedBindings { get; set; } = Array.Empty<FailedBinding>();

    /// <summary>
    /// New relationships created
    /// </summary>
    public int RelationshipsCreated { get; set; }

    /// <summary>
    /// Timestamp of binding operation
    /// </summary>
    public DateTime Timestamp { get; set; }
}
