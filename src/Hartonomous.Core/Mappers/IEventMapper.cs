namespace Hartonomous.Core.Mappers;

/// <summary>
/// Interface for mapping source data to target event types.
/// Provides abstraction for event conversion logic.
/// Uses contravariance (in) for source and covariance (out) for target to enable flexible type hierarchies.
/// </summary>
/// <typeparam name="TSource">The source data type (contravariant)</typeparam>
/// <typeparam name="TTarget">The target event type (covariant)</typeparam>
public interface IEventMapper<in TSource, out TTarget>
{
    /// <summary>
    /// Maps a single source item to target event.
    /// </summary>
    TTarget Map(TSource source);
}

/// <summary>
/// Non-variance version for scenarios requiring both input and output of same type.
/// Used when MapMany needs to return IEnumerable of exact type.
/// </summary>
public interface IEventMapperBidirectional<TSource, TTarget>
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
