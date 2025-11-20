namespace Hartonomous.Core.Interfaces.Base;

/// <summary>
/// Represents an entity that has a unique identifier.
/// Enables methods to depend only on ID property without requiring the entire entity.
/// </summary>
/// <typeparam name="TId">The type of the identifier (e.g., int, long, Guid).</typeparam>
public interface IHasIdentifier<out TId>
{
    /// <summary>
    /// Gets the unique identifier for the entity.
    /// </summary>
    TId Id { get; }
}
