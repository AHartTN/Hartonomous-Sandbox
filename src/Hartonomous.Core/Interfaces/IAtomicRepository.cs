using Hartonomous.Core.Entities;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Generic repository interface for deduplicated atomic entities with reference counting.
/// Consolidates common operations across AtomicPixel, AtomicAudioSample, and AtomicTextToken repositories.
/// </summary>
/// <typeparam name="TEntity">Entity type implementing IReferenceTrackedEntity.</typeparam>
/// <typeparam name="TKey">Primary key type for identity-based operations.</typeparam>
public interface IAtomicRepository<TEntity, TKey> 
    where TEntity : class, IReferenceTrackedEntity
{
    /// <summary>
    /// Retrieve an entity by its content hash for deduplication purposes.
    /// </summary>
    Task<TEntity?> GetByHashAsync(byte[] hash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new atomic entity to the repository.
    /// </summary>
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update the reference count for an atomic entity, tracking how many times it's referenced.
    /// </summary>
    Task UpdateReferenceCountAsync(TKey key, long delta = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the current reference count for an atomic entity.
    /// </summary>
    Task<long> GetReferenceCountAsync(TKey key, CancellationToken cancellationToken = default);
}
