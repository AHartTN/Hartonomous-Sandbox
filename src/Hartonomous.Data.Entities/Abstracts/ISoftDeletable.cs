namespace Hartonomous.Data.Entities;

/// <summary>
/// Interface for soft-deletable entities (logical delete with timestamp).
/// Enables global query filters to exclude deleted records by default.
/// Crash prevention: Preserves data integrity and enables recovery.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// UTC timestamp when entity was soft-deleted. Null if not deleted.
    /// </summary>
    DateTime? DeletedAt { get; set; }

    /// <summary>
    /// User/system that deleted the entity.
    /// </summary>
    string? DeletedBy { get; set; }

    /// <summary>
    /// Indicates if the entity is deleted (computed from DeletedAt).
    /// </summary>
    bool IsDeleted => DeletedAt.HasValue;
}
