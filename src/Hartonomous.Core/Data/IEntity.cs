namespace Hartonomous.Core.Data;

/// <summary>
/// Marker interface for all database entities.
/// Provides type-safe entity discrimination and enables generic constraints.
/// </summary>
public interface IEntity
{
}

/// <summary>
/// Base interface for entities with a strongly-typed primary key.
/// </summary>
/// <typeparam name="TKey">The type of the entity's primary key.</typeparam>
public interface IEntity<TKey> : IEntity where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Gets or sets the primary key value.
    /// </summary>
    TKey Id { get; set; }
}

/// <summary>
/// Interface for entities that track creation and modification timestamps.
/// </summary>
public interface IAuditable
{
    /// <summary>
    /// Gets or sets the UTC timestamp when the entity was created.
    /// </summary>
    DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the entity was last modified.
    /// </summary>
    DateTime? ModifiedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who created the entity.
    /// </summary>
    string? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who last modified the entity.
    /// </summary>
    string? ModifiedBy { get; set; }
}

/// <summary>
/// Interface for entities that support soft deletion.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// Gets or sets a value indicating whether the entity is deleted.
    /// </summary>
    bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the entity was deleted.
    /// </summary>
    DateTime? DeletedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who deleted the entity.
    /// </summary>
    string? DeletedBy { get; set; }
}

/// <summary>
/// Interface for entities that support optimistic concurrency control.
/// </summary>
public interface IConcurrent
{
    /// <summary>
    /// Gets or sets the concurrency token (row version).
    /// </summary>
    byte[]? RowVersion { get; set; }
}

/// <summary>
/// Composite interface for entities with full auditing, soft delete, and concurrency support.
/// </summary>
/// <typeparam name="TKey">The type of the entity's primary key.</typeparam>
public interface IFullyAuditedEntity<TKey> : IEntity<TKey>, IAuditable, ISoftDeletable, IConcurrent
    where TKey : IEquatable<TKey>
{
}
