using NetTopologySuite.Geometries;

namespace Hartonomous.Core.Interfaces.SpatialSearch;

/// <summary>
/// Represents a service that performs spatial search operations on atomized data.
/// Uses SQL CLR functions for efficient spatial indexing and nearest neighbor queries.
/// </summary>
public interface ISpatialSearchService
{
    /// <summary>
    /// Finds the nearest atoms to a given location within a specified radius.
    /// </summary>
    /// <param name="location">The center point for the search.</param>
    /// <param name="radiusMeters">The search radius in meters.</param>
    /// <param name="maxResults">Maximum number of results to return.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Collection of atoms ordered by distance from the center point.</returns>
    Task<IEnumerable<SpatialAtom>> FindNearestAtomsAsync(
        Point location,
        double radiusMeters,
        int maxResults = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs k-nearest neighbor search for atoms near a given location.
    /// </summary>
    /// <param name="location">The center point for the search.</param>
    /// <param name="k">Number of nearest neighbors to find.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The k nearest atoms ordered by distance.</returns>
    Task<IEnumerable<SpatialAtom>> FindKNearestAtomsAsync(
        Point location,
        int k,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Projects atoms onto landmark coordinates for dimensional reduction.
    /// Uses SQL CLR landmark projection functions.
    /// </summary>
    /// <param name="atomIds">The IDs of atoms to project.</param>
    /// <param name="landmarkIds">The IDs of landmark atoms to project onto.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Projected coordinates for each atom.</returns>
    Task<IEnumerable<LandmarkProjection>> ProjectOntoLandmarksAsync(
        IEnumerable<long> atomIds,
        IEnumerable<long> landmarkIds,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an atom with spatial information for search results.
/// </summary>
public sealed class SpatialAtom
{
    /// <summary>
    /// Gets or sets the atom ID.
    /// </summary>
    public long AtomId { get; set; }

    /// <summary>
    /// Gets or sets the geographic location.
    /// </summary>
    public required Point Location { get; set; }

    /// <summary>
    /// Gets or sets the distance in meters from the search center point.
    /// </summary>
    public double DistanceMeters { get; set; }

    /// <summary>
    /// Gets or sets the atom data as JSON.
    /// </summary>
    public string? AtomData { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the atom was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Represents the result of projecting an atom onto landmark coordinates.
/// </summary>
public sealed class LandmarkProjection
{
    /// <summary>
    /// Gets or sets the atom ID being projected.
    /// </summary>
    public long AtomId { get; set; }

    /// <summary>
    /// Gets or sets the projected coordinates (one per landmark).
    /// </summary>
    public required double[] Coordinates { get; set; }

    /// <summary>
    /// Gets or sets the landmark IDs used for projection.
    /// </summary>
    public required long[] LandmarkIds { get; set; }
}
