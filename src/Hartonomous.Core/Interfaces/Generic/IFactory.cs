namespace Hartonomous.Core.Interfaces.Generic;

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
