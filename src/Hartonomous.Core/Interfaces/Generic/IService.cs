namespace Hartonomous.Core.Interfaces.Generic;

public interface IService
{
    /// <summary>
    /// Gets the name of the service for logging and identification.
    /// </summary>
    string ServiceName { get; }

    /// <summary>
    /// Initialize the service with any required setup.
    /// Called once during service registration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the initialization</returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the service is healthy and operational.
    /// Used for health checks and monitoring.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if service is healthy</returns>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Generic repository interface for CRUD operations.
/// Provides a consistent pattern for all entity repositories.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TKey">The primary key type</typeparam>
