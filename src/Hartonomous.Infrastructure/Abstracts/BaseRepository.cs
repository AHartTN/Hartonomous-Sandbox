using Hartonomous.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Hartonomous.Infrastructure.Abstracts;

/// <summary>
/// Generic base repository providing common CRUD operations for entities.
/// Uses EF Core 10.0.0-rc.2.25502.107 with SQL Server 2025 VECTOR and JSON support.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TKey">The primary key type</typeparam>
/// <typeparam name="TDbContext">The DbContext type</typeparam>
public abstract class BaseRepository<TEntity, TKey, TDbContext> : IRepository<TEntity, TKey>
    where TEntity : class
    where TDbContext : DbContext
{
    protected readonly TDbContext Context;
    protected readonly ILogger Logger;
    protected readonly DbSet<TEntity> DbSet;

    protected BaseRepository(TDbContext context, ILogger logger)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        DbSet = context.Set<TEntity>();
    }

    /// <inheritdoc/>
    public virtual async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Getting {EntityType} by ID: {Id}", typeof(TEntity).Name, id);
        return await DbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Getting all {EntityType} entities", typeof(TEntity).Name);
        return await DbSet.ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<TKey> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Adding new {EntityType}", typeof(TEntity).Name);

        var entry = await DbSet.AddAsync(entity, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("{EntityType} added successfully", typeof(TEntity).Name);
        
        // Extract the key from the added entity
        var keyProperty = Context.Model.FindEntityType(typeof(TEntity))?
            .FindPrimaryKey()?.Properties.FirstOrDefault()?.PropertyInfo;
        
        if (keyProperty == null)
            throw new InvalidOperationException($"Cannot determine primary key for {typeof(TEntity).Name}");
            
        return (TKey)keyProperty.GetValue(entry.Entity)!;
    }

    /// <inheritdoc/>
    public virtual async Task<IEnumerable<TKey>> AddRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Adding {Count} {EntityType} entities in batch", entities.Count(), typeof(TEntity).Name);

        var entityList = entities.ToList();
        await DbSet.AddRangeAsync(entityList, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("{EntityType} entities added successfully", typeof(TEntity).Name);
        
        // Extract keys from added entities
        var keyProperty = Context.Model.FindEntityType(typeof(TEntity))?
            .FindPrimaryKey()?.Properties.FirstOrDefault()?.PropertyInfo;
        
        if (keyProperty == null)
            throw new InvalidOperationException($"Cannot determine primary key for {typeof(TEntity).Name}");
            
        return entityList.Select(e => (TKey)keyProperty.GetValue(e)!);
    }

    /// <inheritdoc/>
    public virtual async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Updating {EntityType}", typeof(TEntity).Name);

        DbSet.Update(entity);
        await Context.SaveChangesAsync(cancellationToken);

        Logger.LogInformation("{EntityType} updated successfully", typeof(TEntity).Name);
    }

    /// <inheritdoc/>
    public virtual async Task DeleteAsync(TKey id, CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Deleting {EntityType} with ID: {Id}", typeof(TEntity).Name, id);

        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            DbSet.Remove(entity);
            await Context.SaveChangesAsync(cancellationToken);
            Logger.LogInformation("{EntityType} deleted successfully", typeof(TEntity).Name);
        }
        else
        {
            Logger.LogWarning("{EntityType} not found for deletion: {Id}", typeof(TEntity).Name, id);
        }
    }

    /// <inheritdoc/>
    public virtual async Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync(new object[] { id }, cancellationToken) != null;
    }

    /// <inheritdoc/>
    public virtual async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.CountAsync(cancellationToken);
    }

    /// <summary>
    /// Executes a raw SQL query and returns entities.
    /// Use for operations EF Core cannot express (stored procedures, complex VECTOR operations).
    /// </summary>
    protected async Task<IEnumerable<TEntity>> ExecuteSqlQueryAsync(
        string sql,
        IEnumerable<object> parameters,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Executing raw SQL query for {EntityType}", typeof(TEntity).Name);
        return await DbSet.FromSqlRaw(sql, parameters.ToArray()).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Executes a stored procedure via EF Core FromSqlRaw.
    /// Preferred for inference operations that EF cannot express.
    /// </summary>
    protected async Task<int> ExecuteSqlCommandAsync(
        string sql,
        IEnumerable<object> parameters,
        CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Executing raw SQL command");
        return await Context.Database.ExecuteSqlRawAsync(sql, parameters.ToArray(), cancellationToken);
    }
}

/// <summary>
/// Base repository for entities with int primary keys.
/// Provides type-safe operations for common integer ID patterns.
/// </summary>
public abstract class BaseIntRepository<TEntity, TDbContext> : BaseRepository<TEntity, int, TDbContext>
    where TEntity : class
    where TDbContext : DbContext
{
    protected BaseIntRepository(TDbContext context, ILogger logger) : base(context, logger) { }
}

/// <summary>
/// Base repository for entities with long primary keys.
/// Provides type-safe operations for large ID spaces (embeddings, inference requests).
/// </summary>
public abstract class BaseLongRepository<TEntity, TDbContext> : BaseRepository<TEntity, long, TDbContext>
    where TEntity : class
    where TDbContext : DbContext
{
    protected BaseLongRepository(TDbContext context, ILogger logger) : base(context, logger) { }
}

/// <summary>
/// Base repository for entities with Guid primary keys.
/// Provides type-safe operations for globally unique identifiers.
/// </summary>
public abstract class BaseGuidRepository<TEntity, TDbContext> : BaseRepository<TEntity, Guid, TDbContext>
    where TEntity : class
    where TDbContext : DbContext
{
    protected BaseGuidRepository(TDbContext context, ILogger logger) : base(context, logger) { }
}