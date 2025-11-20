using Hartonomous.Core.Interfaces.Ingestion;

namespace Hartonomous.Infrastructure.Services.Ingestion;

/// <summary>
/// Default implementation of IContentTypeStrategyRegistry.
/// Manages content type strategies and provides extensible content type detection.
/// </summary>
public class ContentTypeStrategyRegistry : IContentTypeStrategyRegistry
{
    private readonly List<IContentTypeStrategy> _strategies = new();
    private readonly object _lock = new();

    /// <summary>
    /// Registers a content type strategy.
    /// </summary>
    /// <param name="strategy">The strategy to register.</param>
    public void RegisterStrategy(IContentTypeStrategy strategy)
    {
        if (strategy == null)
            throw new ArgumentNullException(nameof(strategy));

        lock (_lock)
        {
            // Remove existing strategy with same type if present
            _strategies.RemoveAll(s => s.GetType() == strategy.GetType());
            _strategies.Add(strategy);
        }
    }

    /// <summary>
    /// Finds the best strategy for handling the specified content type and file extension.
    /// </summary>
    /// <param name="contentType">The MIME content type.</param>
    /// <param name="fileExtension">The file extension (without dot).</param>
    /// <returns>The highest priority strategy that can handle the content, or null if none found.</returns>
    public IContentTypeStrategy? FindStrategy(string contentType, string? fileExtension)
    {
        lock (_lock)
        {
            return _strategies
                .Where(s => s.CanHandle(contentType, fileExtension))
                .OrderByDescending(s => s.Priority)
                .FirstOrDefault();
        }
    }

    /// <summary>
    /// Gets all registered strategies ordered by priority (highest first).
    /// </summary>
    /// <returns>Collection of registered strategies.</returns>
    public IEnumerable<IContentTypeStrategy> GetAllStrategies()
    {
        lock (_lock)
        {
            return _strategies.OrderByDescending(s => s.Priority).ToList();
        }
    }
}