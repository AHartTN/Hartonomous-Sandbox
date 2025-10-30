using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Abstracts;

/// <summary>
/// Abstract base class for all services in the system.
/// Provides common service functionality and logging.
/// </summary>
public abstract class BaseService : IService
{
    protected readonly ILogger Logger;

    protected BaseService(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public abstract string ServiceName { get; }

    /// <inheritdoc/>
    public virtual async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Initializing service: {ServiceName}", ServiceName);
        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public virtual async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        // Default implementation - override in derived classes for specific health checks
        return await Task.FromResult(true);
    }
}

/// <summary>
/// Abstract base class for services that need configuration.
/// Combines IService with IConfigurable.
/// </summary>
/// <typeparam name="TConfig">The configuration type</typeparam>
public abstract class BaseConfigurableService<TConfig> : BaseService, IConfigurable<TConfig>
    where TConfig : class
{
    protected TConfig Configuration { get; private set; }

    protected BaseConfigurableService(ILogger logger, TConfig configuration)
        : base(logger)
    {
        Configure(configuration);
    }

    /// <inheritdoc/>
    public void Configure(TConfig config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        var validation = ValidateConfiguration(config);
        if (!validation.IsValid)
        {
            throw new ArgumentException(
                $"Invalid configuration: {string.Join(", ", validation.Errors)}",
                nameof(config));
        }

        Configuration = config;
        Logger.LogInformation("Configuration updated for service: {ServiceName}", ServiceName);
    }

    /// <inheritdoc/>
    public TConfig GetConfiguration() => Configuration;

    /// <inheritdoc/>
    public virtual ValidationResult ValidateConfiguration(TConfig config)
    {
        // Default implementation - override for specific validation
        return new ValidationResult { IsValid = true };
    }
}

/// <summary>
/// Abstract base class for processors.
/// Provides common processing functionality and error handling.
/// </summary>
/// <typeparam name="TInput">The input type</typeparam>
/// <typeparam name="TOutput">The output type</typeparam>
public abstract class BaseProcessor<TInput, TOutput> : BaseService, IProcessor<TInput, TOutput>
{
    protected BaseProcessor(ILogger logger) : base(logger) { }

    /// <inheritdoc/>
    public abstract Task<TOutput> ProcessAsync(TInput input, CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public virtual async Task<IEnumerable<TOutput>> ProcessBatchAsync(IEnumerable<TInput> inputs, CancellationToken cancellationToken = default)
    {
        var results = new List<TOutput>();

        foreach (var input in inputs)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var result = await ProcessAsync(input, cancellationToken);
                results.Add(result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to process input: {Input}", input);
                // Continue processing other items
            }
        }

        return results;
    }

    /// <inheritdoc/>
    public abstract bool CanProcess(TInput input);
}

/// <summary>
/// Abstract base class for validators.
/// Provides common validation functionality.
/// </summary>
/// <typeparam name="T">The type to validate</typeparam>
public abstract class BaseValidator<T> : IValidator<T>
{
    /// <inheritdoc/>
    public abstract ValidationResult Validate(T obj);

    /// <inheritdoc/>
    public virtual IEnumerable<ValidationResult> ValidateBatch(IEnumerable<T> objects)
    {
        return objects.Select(Validate);
    }

    /// <summary>
    /// Helper method to create a validation error result.
    /// </summary>
    protected static ValidationResult Error(params string[] errors)
    {
        return new ValidationResult
        {
            IsValid = false,
            Errors = errors
        };
    }

    /// <summary>
    /// Helper method to create a validation warning result.
    /// </summary>
    protected static ValidationResult Warning(params string[] warnings)
    {
        return new ValidationResult
        {
            IsValid = true,
            Warnings = warnings
        };
    }

    /// <summary>
    /// Helper method to create a successful validation result.
    /// </summary>
    protected static ValidationResult Success()
    {
        return new ValidationResult { IsValid = true };
    }
}

/// <summary>
/// Abstract base class for factories.
/// Provides common factory functionality and registration.
/// </summary>
/// <typeparam name="TKey">The key type</typeparam>
/// <typeparam name="TResult">The result type</typeparam>
public abstract class BaseFactory<TKey, TResult> : IFactory<TKey, TResult>
{
    protected readonly Dictionary<TKey, Func<TResult>> Registrations = new();

    /// <inheritdoc/>
    public virtual TResult Create(TKey key)
    {
        if (!Registrations.TryGetValue(key, out var factory))
        {
            throw new ArgumentException($"No registration found for key: {key}", nameof(key));
        }

        return factory();
    }

    /// <inheritdoc/>
    public virtual bool CanCreate(TKey key)
    {
        return Registrations.ContainsKey(key);
    }

    /// <inheritdoc/>
    public virtual IEnumerable<TKey> GetSupportedKeys()
    {
        return Registrations.Keys;
    }

    /// <summary>
    /// Register a factory function for a key.
    /// </summary>
    /// <param name="key">The key to register</param>
    /// <param name="factory">The factory function</param>
    protected void Register(TKey key, Func<TResult> factory)
    {
        Registrations[key] = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <summary>
    /// Register a type that can be instantiated for a key.
    /// </summary>
    /// <param name="key">The key to register</param>
    /// <param name="type">The type to instantiate</param>
    protected void RegisterType(TKey key, Type type)
    {
        Register(key, () => (TResult)Activator.CreateInstance(type));
    }
}