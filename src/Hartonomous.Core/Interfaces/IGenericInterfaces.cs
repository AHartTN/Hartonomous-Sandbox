using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Generic base interface for all services in the system.
/// Provides common service patterns and lifecycle management.
/// </summary>
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
public interface IFactory<TKey, TResult>
{
    /// <summary>
    /// Create an instance based on the provided key.
    /// </summary>
    /// <param name="key">The key identifying what to create</param>
    /// <returns>The created instance</returns>
    TResult Create(TKey key);

    /// <summary>
    /// Check if the factory can create an instance for the given key.
    /// </summary>
    /// <param name="key">The key to check</param>
    /// <returns>True if the key is supported</returns>
    bool CanCreate(TKey key);

    /// <summary>
    /// Get all supported keys for this factory.
    /// </summary>
    IEnumerable<TKey> GetSupportedKeys();
}

/// <summary>
/// Generic processor interface for processing items.
/// Provides a consistent pattern for processing pipelines.
/// </summary>
/// <typeparam name="TInput">The input type to process</typeparam>
/// <typeparam name="TOutput">The output type after processing</typeparam>
public interface IProcessor<TInput, TOutput>
{
    /// <summary>
    /// Process a single input item.
    /// </summary>
    /// <param name="input">The input to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The processed output</returns>
    Task<TOutput> ProcessAsync(TInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Process multiple input items in batch.
    /// </summary>
    /// <param name="inputs">The inputs to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The processed outputs</returns>
    Task<IEnumerable<TOutput>> ProcessBatchAsync(IEnumerable<TInput> inputs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate that the input can be processed.
    /// </summary>
    /// <param name="input">The input to validate</param>
    /// <returns>True if the input is valid for processing</returns>
    bool CanProcess(TInput input);
}

/// <summary>
/// Generic validator interface for validating objects.
/// Provides a consistent pattern for validation logic.
/// </summary>
/// <typeparam name="T">The type to validate</typeparam>
public interface IValidator<T>
{
    /// <summary>
    /// Validate a single object.
    /// </summary>
    /// <param name="obj">The object to validate</param>
    /// <returns>Validation result</returns>
    ValidationResult Validate(T obj);

    /// <summary>
    /// Validate multiple objects.
    /// </summary>
    /// <param name="objects">The objects to validate</param>
    /// <returns>Collection of validation results</returns>
    IEnumerable<ValidationResult> ValidateBatch(IEnumerable<T> objects);
}

/// <summary>
/// Result of a validation operation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets or sets whether the validation passed.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the validation errors, if any.
    /// </summary>
    public IEnumerable<string> Errors { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets any warnings from validation.
    /// </summary>
    public IEnumerable<string> Warnings { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Generic configuration interface for services.
/// Provides a consistent pattern for configuration management.
/// </summary>
/// <typeparam name="TConfig">The configuration type</typeparam>
public interface IConfigurable<TConfig> where TConfig : class
{
    /// <summary>
    /// Configure the service with the provided configuration.
    /// </summary>
    /// <param name="config">The configuration to apply</param>
    void Configure(TConfig config);

    /// <summary>
    /// Get the current configuration.
    /// </summary>
    /// <returns>The current configuration</returns>
    TConfig GetConfiguration();

    /// <summary>
    /// Validate the configuration.
    /// </summary>
    /// <param name="config">The configuration to validate</param>
    /// <returns>Validation result</returns>
    ValidationResult ValidateConfiguration(TConfig config);
}