using NetTopologySuite.Geometries;

namespace Hartonomous.Core.Interfaces.Base;

/// <summary>
/// Represents an entity that has a geographic location.
/// Enables spatial operations to depend only on location property without requiring the entire entity.
/// </summary>
public interface IHasLocation
{
    /// <summary>
    /// Gets the geographic location as a Point (uses NetTopologySuite for spatial types).
    /// </summary>
    Point Location { get; }

    /// <summary>
    /// Gets the latitude in decimal degrees.
    /// </summary>
    double Latitude => Location.Y;

    /// <summary>
    /// Gets the longitude in decimal degrees.
    /// </summary>
    double Longitude => Location.X;
}
