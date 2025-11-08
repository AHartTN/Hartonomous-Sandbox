using Hartonomous.Infrastructure.Data;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Shared;
using Hartonomous.Core.Utilities;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using System.Data.SqlTypes;

using Hartonomous.Infrastructure.Repositories.EfCore.Models;

namespace Hartonomous.Infrastructure.Repositories.EfCore;

/// <summary>
/// Repository for vector search operations using EF Core
/// Replaces sp_SpatialVectorSearch, sp_TemporalVectorSearch, sp_HybridSearch, sp_MultiModelEnsemble
/// </summary>
public class VectorSearchRepository : IVectorSearchRepository
{
    private readonly HartonomousDbContext _context;

    public VectorSearchRepository(HartonomousDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Performs spatial pre-filtering + exact k-NN search
    /// </summary>
    public async Task<IReadOnlyList<VectorSearchResult>> SpatialVectorSearchAsync(
        byte[] queryVector,
        Geometry? spatialCenter = null,
        double? radiusMeters = null,
        int topK = 10,
        int tenantId = 0,
        double minSimilarity = 0.0)
    {
        IQueryable<AtomEmbedding> query = _context.AtomEmbeddings
            .Include(e => e.Atom);

        // Spatial pre-filtering using SpatialGeometry
        if (spatialCenter != null && radiusMeters.HasValue)
        {
            query = query.Where(e =>
                e.SpatialGeometry != null &&
                spatialCenter.Distance(e.SpatialGeometry) <= radiusMeters.Value);
        }

        // Get candidates (limit for performance)
        var candidates = await query
            .Take(100000) // Safety limit
            .Select(e => new
            {
                e.AtomEmbeddingId,
                e.AtomId,
                e.EmbeddingVector,
                SpatialDistance = spatialCenter != null && e.SpatialGeometry != null
                    ? spatialCenter.Distance(e.SpatialGeometry)
                    : 0.0
            })
            .ToListAsync();

        // Compute similarities and rank
        var results = candidates
            .Select(c =>
            {
                double similarity = 0.0;
                if (c.EmbeddingVector.HasValue)
                {
                    var vector = VectorUtility.Materialize(c.EmbeddingVector.Value, c.EmbeddingVector.Value.Length);
                    similarity = 1.0 - VectorUtility.ComputeCosineDistance(vector, queryVector.Select(b => (float)b).ToArray());
                }
                return new VectorSearchResult
                {
                    AtomId = c.AtomId,
                    Similarity = similarity,
                    SpatialDistance = c.SpatialDistance
                };
            })
            .Where(r => r.Similarity >= minSimilarity)
            .OrderByDescending(r => r.Similarity)
            .Take(topK)
            .ToList();

        // Load atom details for results
        var atomIds = results.Select(r => r.AtomId).ToList();
        var atoms = await _context.Atoms
            .Where(a => atomIds.Contains(a.AtomId))
            .ToDictionaryAsync(a => a.AtomId);

        foreach (var result in results)
        {
            if (atoms.TryGetValue(result.AtomId, out var atom))
            {
                result.ContentHash = atom.ContentHash;
                result.ContentType = atom.Subtype ?? atom.Modality;
                result.CreatedUtc = atom.CreatedAt;
            }
        }

        return results;
    }

    /// <summary>
    /// Performs point-in-time semantic search using temporal tables
    /// </summary>
    public async Task<IReadOnlyList<VectorSearchResult>> TemporalVectorSearchAsync(
        byte[] queryVector,
        DateTime asOfDate,
        int topK = 10,
        int tenantId = 0)
    {
        // Note: EF Core temporal table support is limited
        // This would need custom SQL or raw SQL execution
        // For now, return empty results with a comment
        throw new NotImplementedException(
            "Temporal vector search requires custom SQL implementation. " +
            "Consider using raw SQL with FOR SYSTEM_TIME AS OF clause.");
    }

    /// <summary>
    /// Performs hybrid search combining full-text, vector, and spatial ranking
    /// </summary>
    public async Task<IReadOnlyList<HybridSearchResult>> HybridSearchAsync(
        byte[] queryVector,
        string? keywords = null,
        Geometry? spatialRegion = null,
        int topK = 10,
        double vectorWeight = 0.5,
        double keywordWeight = 0.3,
        double spatialWeight = 0.2,
        int tenantId = 0)
    {
        // Validate weights
        var totalWeight = vectorWeight + keywordWeight + spatialWeight;
        if (Math.Abs(totalWeight - 1.0) > 0.01)
        {
            throw new ArgumentException("Weights must sum to 1.0");
        }

        var atoms = await _context.Atoms
            .Select(a => new
            {
                a.AtomId,
                a.ContentHash,
                a.Subtype,
                a.Modality,
                a.CreatedAt
            })
            .ToListAsync();

        var atomIds = atoms.Select(a => a.AtomId).ToList();

        // Get embeddings for vector scores
        var embeddings = await _context.AtomEmbeddings
            .Where(e => atomIds.Contains(e.AtomId))
            .ToDictionaryAsync(e => e.AtomId, e => e.EmbeddingVector);

        // Compute scores for each atom
        var results = new List<HybridSearchResult>();
        foreach (var atom in atoms)
        {
            double vectorScore = 0.0;
            double keywordScore = 0.0;
            double spatialScore = 0.0;

            // Vector score
            if (embeddings.TryGetValue(atom.AtomId, out var embedding) && embedding.HasValue)
            {
                var vector = VectorUtility.Materialize(embedding.Value, embedding.Value.Length);
                vectorScore = 1.0 - VectorUtility.ComputeCosineDistance(vector, queryVector.Select(b => (float)b).ToArray());
            }

            // Keyword score (simplified - would need full-text search)
            if (!string.IsNullOrEmpty(keywords))
            {
                // This is a placeholder - actual implementation would use CONTAINSTABLE
                // or EF Core full-text search capabilities
                keywordScore = atom.Subtype?.Contains(keywords, StringComparison.OrdinalIgnoreCase) == true ? 0.5 : 0.0;
            }

            // Spatial score
            if (spatialRegion != null)
            {
                // This would need spatial data from atoms table
                // Placeholder implementation
                spatialScore = 0.0;
            }

            var combinedScore = (vectorScore * vectorWeight) +
                               (keywordScore * keywordWeight) +
                               (spatialScore * spatialWeight);

            results.Add(new HybridSearchResult
            {
                AtomId = atom.AtomId,
                VectorScore = vectorScore,
                KeywordScore = keywordScore,
                SpatialScore = spatialScore,
                CombinedScore = combinedScore,
                ContentHash = atom.ContentHash,
                ContentType = atom.Subtype ?? atom.Modality,
                CreatedUtc = atom.CreatedAt
            });
        }

        return results
            .Where(r => r.CombinedScore > 0)
            .OrderByDescending(r => r.CombinedScore)
            .Take(topK)
            .ToList();
    }

    /// <summary>
    /// Performs ensemble search blending results from multiple models
    /// </summary>
    public async Task<IReadOnlyList<EnsembleSearchResult>> MultiModelEnsembleSearchAsync(
        byte[] queryVector1, byte[] queryVector2, byte[] queryVector3,
        int model1Id, int model2Id, int model3Id,
        double model1Weight = 0.4, double model2Weight = 0.35, double model3Weight = 0.25,
        int topK = 10, int tenantId = 0)
    {
        var atoms = await _context.Atoms
            .Select(a => new { a.AtomId, a.ContentHash, a.Subtype, a.Modality })
            .ToListAsync();

        var atomIds = atoms.Select(a => a.AtomId).ToList();

        // Get embeddings for each model
        var model1Embeddings = await _context.AtomEmbeddings
            .Where(e => atomIds.Contains(e.AtomId) && e.ModelId == model1Id)
            .ToDictionaryAsync(e => e.AtomId, e => e.EmbeddingVector);

        var model2Embeddings = await _context.AtomEmbeddings
            .Where(e => atomIds.Contains(e.AtomId) && e.ModelId == model2Id)
            .ToDictionaryAsync(e => e.AtomId, e => e.EmbeddingVector);

        var model3Embeddings = await _context.AtomEmbeddings
            .Where(e => atomIds.Contains(e.AtomId) && e.ModelId == model3Id)
            .ToDictionaryAsync(e => e.AtomId, e => e.EmbeddingVector);

        // Compute ensemble scores
        var results = new List<EnsembleSearchResult>();
        foreach (var atom in atoms)
        {
            double model1Score = model1Embeddings.TryGetValue(atom.AtomId, out var emb1) && emb1.HasValue
                ? 1.0 - VectorUtility.ComputeCosineDistance(
                    VectorUtility.Materialize(emb1.Value, emb1.Value.Length),
                    queryVector1.Select(b => (float)b).ToArray())
                : 0.0;

            double model2Score = model2Embeddings.TryGetValue(atom.AtomId, out var emb2) && emb2.HasValue
                ? 1.0 - VectorUtility.ComputeCosineDistance(
                    VectorUtility.Materialize(emb2.Value, emb2.Value.Length),
                    queryVector2.Select(b => (float)b).ToArray())
                : 0.0;

            double model3Score = model3Embeddings.TryGetValue(atom.AtomId, out var emb3) && emb3.HasValue
                ? 1.0 - VectorUtility.ComputeCosineDistance(
                    VectorUtility.Materialize(emb3.Value, emb3.Value.Length),
                    queryVector3.Select(b => (float)b).ToArray())
                : 0.0;

            var ensembleScore = (model1Score * model1Weight) +
                               (model2Score * model2Weight) +
                               (model3Score * model3Weight);

            results.Add(new EnsembleSearchResult
            {
                AtomId = atom.AtomId,
                Model1Score = model1Score,
                Model2Score = model2Score,
                Model3Score = model3Score,
                EnsembleScore = ensembleScore,
                ContentHash = atom.ContentHash,
                ContentType = atom.Subtype ?? atom.Modality
            });
        }

        return results
            .OrderByDescending(r => r.EnsembleScore)
            .Take(topK)
            .ToList();
    }
}

/// <summary>
/// Vector distance calculations (placeholder - would use SQL Server VECTOR_DISTANCE)
/// </summary>
public static class VectorDistance
{
    public static double Cosine(byte[] a, byte[] b)
    {
        // Placeholder implementation
        // In real implementation, this would delegate to SQL Server VECTOR_DISTANCE function
        // or use a native library for vector operations
        if (a.Length != b.Length) return 0.0;

        double dotProduct = 0.0;
        double normA = 0.0;
        double normB = 0.0;

        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        normA = Math.Sqrt(normA);
        normB = Math.Sqrt(normB);

        return normA == 0.0 || normB == 0.0 ? 0.0 : dotProduct / (normA * normB);
    }
}

