namespace Hartonomous.Core.Interfaces.Base;

/// <summary>
/// Represents an entity that tracks creation and modification timestamps.
/// Enables audit trail functionality without requiring the entire entity.
/// </summary>
public interface IHasTimestamps
{
    /// <summary>
    /// Gets the timestamp when the entity was created.
    /// </summary>
    DateTime CreatedAt { get; }

    /// <summary>
    /// Gets the timestamp when the entity was last modified.
    /// </summary>
    DateTime? ModifiedAt { get; }
}
