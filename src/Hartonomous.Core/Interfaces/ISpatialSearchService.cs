using Hartonomous.Core.Models;
using NetTopologySuite.Geometries;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Provides spatial vector search capabilities using SQL Server geometry functions.
/// Projects high-dimensional embeddings into 3D spatial coordinates for approximate nearest neighbor search.
/// </summary>
public interface ISpatialSearchService
{
    /// <summary>
    /// Performs an approximate spatial search by projecting embeddings into vector space geometry.
    /// Uses SQL Server spatial indexes (STDistance) for fast approximate nearest neighbor retrieval.
    /// </summary>
    /// <param name="queryVector">Vector used to compute the spatial projection.</param>
    /// <param name="topK">Number of nearest neighbors to return.</param>
    /// <param name="cancellationToken">Token that can cancel processing.</param>
    /// <returns>Ranked list of embeddings surfaced by the spatial search.</returns>
    Task<IReadOnlyList<AtomEmbeddingSearchResult>> SpatialSearchAsync(
        float[] queryVector,
        int topK = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes spatial search using a pre-computed spatial point (3D projection).
    /// Direct low-level method for when projection is already available.
    /// </summary>
    /// <param name="spatialPoint">3D point representing the projected embedding in SQL geometry space.</param>
    /// <param name="topK">Total results requested.</param>
    /// <param name="cancellationToken">Token to cancel command execution.</param>
    /// <returns>List of search results produced by the stored procedure.</returns>
    Task<IReadOnlyList<AtomEmbeddingSearchResult>> ExecuteSpatialSearchAsync(
        Point spatialPoint,
        int topK,
        CancellationToken cancellationToken);
}
