namespace Hartonomous.Data.Entities;

/// <summary>
/// **OPTIONAL** abstract base class for custom entities that need audit/soft-delete patterns.
/// NOT FOR SCAFFOLDED ENTITIES - use for hand-written domain entities only.
/// </summary>
/// <typeparam name="TKey">Primary key type</typeparam>
/// <remarks>
/// WHEN TO USE:
/// - Custom business entities (not from database)
/// - Entities you control that need audit trails
/// - Domain models (not data models)
/// 
/// WHEN NOT TO USE:
/// - Temporal tables (SQL Server tracks changes automatically)
/// - In-memory tables
/// - Scaffolded entities from existing database
/// 
/// SCAFFOLDED ENTITIES: Implement schema as-is. Add auditing via SQL triggers, temporal tables, or CDC.
/// </remarks>
public abstract class EntityBase<TKey> : IEntity<TKey>, IAuditableEntity, ISoftDeletable, IConcurrencyToken
    where TKey : IEquatable<TKey>
{
    /// <inheritdoc />
    public virtual TKey Id { get; set; } = default!;

    /// <inheritdoc />
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <inheritdoc />
    public string? CreatedBy { get; set; }

    /// <inheritdoc />
    public DateTime? ModifiedAt { get; set; }

    /// <inheritdoc />
    public string? ModifiedBy { get; set; }

    /// <inheritdoc />
    public DateTime? DeletedAt { get; set; }

    /// <inheritdoc />
    public string? DeletedBy { get; set; }

    /// <inheritdoc />
    public byte[]? RowVersion { get; set; }

    /// <inheritdoc />
    public bool IsDeleted => DeletedAt.HasValue;

    /// <summary>
    /// Marks entity as deleted (soft delete).
    /// </summary>
    /// <param name="deletedBy">User/system performing deletion</param>
    public virtual void Delete(string? deletedBy = null)
    {
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    /// <summary>
    /// Restores a soft-deleted entity.
    /// </summary>
    public virtual void Restore()
    {
        DeletedAt = null;
        DeletedBy = null;
    }
}
