using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Shared.Contracts.Entities;
using Hartonomous.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Hartonomous.Data.Entities;

namespace Hartonomous.Infrastructure.Repositories;

/// <summary>
/// Base repository for deduplicated atomic entities that tracks reference counts and hash-based lookups.
/// Consolidates shared logic across atomic repositories while preserving EF Core integration.
/// </summary>
/// <typeparam name="TEntity">Entity type implementing <see cref="IReferenceTrackedEntity"/>.</typeparam>
/// <typeparam name="TKey">Primary key type used for identity updates.</typeparam>
/// <typeparam name="THash">Hash type used for deduplication lookups.</typeparam>
public abstract class AtomicReferenceRepository<TEntity, TKey, THash> : EfRepository<TEntity, TKey>
    where TEntity : class, IReferenceTrackedEntity
{
    protected AtomicReferenceRepository(HartonomousDbContext context, ILogger logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// Provides the expression selecting the hash column for the entity.
    /// Example: entity =&gt; entity.SampleHash
    /// </summary>
    protected abstract Expression<Func<TEntity, THash>> GetHashExpression();

    /// <summary>
    /// Provides the predicate used to match an entity by its identity key.
    /// Example: entity =&gt; entity.TokenId == key
    /// </summary>
    protected abstract Expression<Func<TEntity, bool>> BuildKeyPredicate(TKey key);

    /// <summary>
    /// Builds a predicate that matches an entity against the provided hash.
    /// </summary>
    protected virtual Expression<Func<TEntity, bool>> BuildHashPredicate(THash hash)
    {
        var hashExpression = GetHashExpression();
        var parameter = hashExpression.Parameters[0];
        var constant = Expression.Constant(hash, typeof(THash));
        var body = Expression.Equal(hashExpression.Body, constant);
        return Expression.Lambda<Func<TEntity, bool>>(body, parameter);
    }

    /// <summary>
    /// Retrieve an entity by its deduplicated hash value.
    /// </summary>
    public virtual Task<TEntity?> GetByHashAsync(THash hash, CancellationToken cancellationToken = default)
    {
        return DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(BuildHashPredicate(hash), cancellationToken);
    }

    /// <summary>
    /// Increment or decrement the reference count for the specified identity key.
    /// </summary>
    public virtual async Task UpdateReferenceCountAsync(TKey key, long delta = 1, CancellationToken cancellationToken = default)
    {
        if (delta == 0)
        {
            return;
        }

        await DbSet
            .Where(BuildKeyPredicate(key))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(entity => entity.ReferenceCount, entity => entity.ReferenceCount + delta)
                .SetProperty(entity => entity.LastReferenced, _ => DateTime.UtcNow),
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieve the current reference count for the specified identity key.
    /// </summary>
    public virtual async Task<long> GetReferenceCountAsync(TKey key, CancellationToken cancellationToken = default)
    {
        var count = await DbSet
            .AsNoTracking()
            .Where(BuildKeyPredicate(key))
            .Select(entity => entity.ReferenceCount)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return count;
    }
}
