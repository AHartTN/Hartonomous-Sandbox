namespace Hartonomous.Core.Interfaces.Generic;

public interface IRepository<TEntity, TKey> where TEntity : class
{
    /// <summary>
    /// Get entity by its primary key.
    /// </summary>
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all entities.
    /// </summary>
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new entity.
    /// Returns the entity with any generated values (like auto-increment IDs).
    /// </summary>
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add multiple entities in batch.
    /// Returns the entities with any generated values.
    /// </summary>
    Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing entity.
    /// </summary>
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an entity by its primary key.
    /// </summary>
    Task DeleteAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if an entity exists by its primary key.
    /// </summary>
    Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the total count of entities.
    /// </summary>
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Generic factory interface for creating objects.
/// Provides a consistent pattern for factory implementations.
/// </summary>
/// <typeparam name="TKey">The key type used to identify what to create</typeparam>
/// <typeparam name="TResult">The result type to create</typeparam>
