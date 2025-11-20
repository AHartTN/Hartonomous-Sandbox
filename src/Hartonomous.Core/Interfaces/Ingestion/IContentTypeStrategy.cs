using Hartonomous.Core.Interfaces.Ingestion;

namespace Hartonomous.Core.Interfaces.Ingestion;

/// <summary>
/// Strategy for determining if an atomizer can handle specific content types.
/// </summary>
public interface IContentTypeStrategy
{
    /// <summary>
    /// Determines if this strategy can handle the specified content type and file extension.
    /// </summary>
    /// <param name="contentType">The MIME content type.</param>
    /// <param name="fileExtension">The file extension (without dot).</param>
    /// <returns>True if this strategy can handle the content, false otherwise.</returns>
    bool CanHandle(string contentType, string? fileExtension);

    /// <summary>
    /// Gets the priority of this strategy (higher values = higher priority).
    /// </summary>
    int Priority { get; }
}

/// <summary>
/// Registry for managing content type strategies following the Strategy pattern.
/// </summary>
public interface IContentTypeStrategyRegistry
{
    /// <summary>
    /// Registers a content type strategy.
    /// </summary>
    /// <param name="strategy">The strategy to register.</param>
    void RegisterStrategy(IContentTypeStrategy strategy);

    /// <summary>
    /// Finds the best strategy for handling the specified content type and file extension.
    /// </summary>
    /// <param name="contentType">The MIME content type.</param>
    /// <param name="fileExtension">The file extension (without dot).</param>
    /// <returns>The highest priority strategy that can handle the content, or null if none found.</returns>
    IContentTypeStrategy? FindStrategy(string contentType, string? fileExtension);

    /// <summary>
    /// Gets all registered strategies ordered by priority (highest first).
    /// </summary>
    /// <returns>Collection of registered strategies.</returns>
    IEnumerable<IContentTypeStrategy> GetAllStrategies();
}