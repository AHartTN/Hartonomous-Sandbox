namespace Hartonomous.Data.Entities;

/// <summary>
/// Interface for entities with audit fields (Created/Modified timestamps and user tracking).
/// Enables automatic audit logging via EF interceptors or triggers.
/// </summary>
public interface IAuditableEntity
{
    /// <summary>
    /// UTC timestamp when entity was created.
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    /// User/system that created the entity.
    /// </summary>
    string? CreatedBy { get; set; }

    /// <summary>
    /// UTC timestamp when entity was last modified.
    /// </summary>
    DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// User/system that last modified the entity.
    /// </summary>
    string? ModifiedBy { get; set; }
}
