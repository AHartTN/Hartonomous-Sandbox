namespace Hartonomous.Core.Mappers;

/// <summary>
/// Interface for mapping source data to target event types.
/// Provides abstraction for event conversion logic.
/// </summary>
/// <typeparam name="TSource">The source data type</typeparam>
/// <typeparam name="TTarget">The target event type</typeparam>
public interface IEventMapper<TSource, TTarget>
{
    /// <summary>
    /// Maps a single source item to target event.
    /// </summary>
    TTarget Map(TSource source);

    /// <summary>
    /// Maps multiple source items to target events.
    /// </summary>
    IEnumerable<TTarget> MapMany(IEnumerable<TSource> sources);
}
