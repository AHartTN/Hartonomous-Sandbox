using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Repositories;

/// <summary>
/// Generic EF Core repository base class implementing common CRUD operations.
/// Eliminates code duplication across all entity repositories.
/// Derived repositories can focus on domain-specific queries.
/// </summary>
/// <typeparam name="TEntity">Entity type (must be a class)</typeparam>
/// <typeparam name="TKey">Primary key type</typeparam>
public abstract class EfRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class
{
    protected readonly HartonomousDbContext Context;
    protected readonly DbSet<TEntity> DbSet;
    protected readonly ILogger Logger;

    protected EfRepository(HartonomousDbContext context, ILogger logger)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        DbSet = context.Set<TEntity>();
    }

    /// <summary>
    /// Override to provide entity ID extraction logic.
    /// Example: entity => entity.Id
    /// </summary>
    protected abstract Expression<Func<TEntity, TKey>> GetIdExpression();

    /// <summary>
    /// Override to include related entities in queries.
    /// Example: query => query.Include(e => e.RelatedEntity)
    /// </summary>
    protected virtual IQueryable<TEntity> IncludeRelatedEntities(IQueryable<TEntity> query)
    {
        return query;
    }

    public virtual async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var idExpression = GetIdExpression();
        var parameter = idExpression.Parameters[0];
        var equalExpression = Expression.Equal(idExpression.Body, Expression.Constant(id));
        var lambda = Expression.Lambda<Func<TEntity, bool>>(equalExpression, parameter);

        return await IncludeRelatedEntities(DbSet.AsNoTracking())
            .FirstOrDefaultAsync(lambda, cancellationToken);
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await IncludeRelatedEntities(DbSet.AsNoTracking())
            .ToListAsync(cancellationToken);
    }

    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        DbSet.Add(entity);
        await Context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public virtual async Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        var entityList = entities.ToList();
        DbSet.AddRange(entityList);
        await Context.SaveChangesAsync(cancellationToken);
        return entityList;
    }

    public virtual async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        Context.Entry(entity).State = EntityState.Modified;
        await Context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task DeleteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            DbSet.Remove(entity);
            await Context.SaveChangesAsync(cancellationToken);
        }
    }

    public virtual async Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var idExpression = GetIdExpression();
        var parameter = idExpression.Parameters[0];
        var equalExpression = Expression.Equal(idExpression.Body, Expression.Constant(id));
        var lambda = Expression.Lambda<Func<TEntity, bool>>(equalExpression, parameter);

        return await DbSet.AnyAsync(lambda, cancellationToken);
    }

    public virtual async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.CountAsync(cancellationToken);
    }

    /// <summary>
    /// Advanced querying: Find entities matching a predicate.
    /// </summary>
    protected async Task<IEnumerable<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await IncludeRelatedEntities(DbSet.AsNoTracking())
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Advanced querying: Find with ordering.
    /// </summary>
    protected async Task<IEnumerable<TEntity>> FindAsync<TOrderKey>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TOrderKey>> orderBy,
        bool ascending = true,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().Where(predicate);
        query = ascending 
            ? query.OrderBy(orderBy) 
            : query.OrderByDescending(orderBy);
        
        return await IncludeRelatedEntities(query).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Advanced querying: Paginated results.
    /// </summary>
    protected async Task<IEnumerable<TEntity>> GetPagedAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        return await IncludeRelatedEntities(DbSet.AsNoTracking())
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
