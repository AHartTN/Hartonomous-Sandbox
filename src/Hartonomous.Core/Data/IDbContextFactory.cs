namespace Hartonomous.Core.Data;

/// <summary>
/// Factory interface for creating database context instances.
/// Supports multi-tenancy and connection string resolution.
/// </summary>
/// <typeparam name="TContext">The database context type.</typeparam>
public interface IDbContextFactory<TContext> where TContext : class
{
    /// <summary>
    /// Creates a new database context instance.
    /// </summary>
    /// <returns>A new database context instance.</returns>
    TContext CreateDbContext();

    /// <summary>
    /// Creates a new database context instance asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A new database context instance.</returns>
    Task<TContext> CreateDbContextAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Extended factory interface with connection string override capability.
/// </summary>
/// <typeparam name="TContext">The database context type.</typeparam>
public interface IDbContextFactoryWithConnectionString<TContext> : IDbContextFactory<TContext>
    where TContext : class
{
    /// <summary>
    /// Creates a new database context instance with a specific connection string.
    /// </summary>
    /// <param name="connectionString">The connection string to use.</param>
    /// <returns>A new database context instance.</returns>
    TContext CreateDbContext(string connectionString);
}
