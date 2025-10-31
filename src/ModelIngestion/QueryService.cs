using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Hartonomous.Core.Interfaces;

namespace ModelIngestion;

/// <summary>
/// Service for executing semantic search queries
/// </summary>
public class QueryService
{
    private readonly IEmbeddingRepository _embeddings;
    private readonly ILogger<QueryService> _logger;

    public QueryService(
        IEmbeddingRepository embeddings,
        ILogger<QueryService> logger)
    {
        _embeddings = embeddings ?? throw new ArgumentNullException(nameof(embeddings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Execute semantic search query
    /// </summary>
    public async Task ExecuteSemanticQueryAsync(string queryText, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing semantic query: '{Query}'", queryText);

        // Generate query embedding (in production, use actual embedding model)
        var random = new Random(queryText.GetHashCode());
        var queryEmbedding = GenerateRandomEmbedding(random, 768);

        // Execute exact search
        _logger.LogInformation("Running exact VECTOR search...");
        var exactResults = await _embeddings.ExactSearchAsync(queryEmbedding, topK: 5);

        _logger.LogInformation("Top 5 exact matches:");
        foreach (var result in exactResults)
        {
            _logger.LogInformation("  [{Id}] Distance: {Dist:F4} | {Text}",
                result.EmbeddingId, result.Distance,
                result.SourceText.Length > 80 ? result.SourceText.Substring(0, 77) + "..." : result.SourceText);
        }

        // Execute approximate spatial search
        _logger.LogInformation("Computing spatial projection...");
        var spatial3D = await _embeddings.ComputeSpatialProjectionAsync(queryEmbedding);

        _logger.LogInformation("Running approximate spatial search...");
        var approxResults = await _embeddings.HybridSearchAsync(queryEmbedding, spatial3D[0], spatial3D[1], spatial3D[2], spatialCandidates: 100, finalTopK: 5);

        _logger.LogInformation("Top 5 approximate matches:");
        foreach (var result in approxResults)
        {
            _logger.LogInformation("  [{Id}] Distance: {Dist:F4} | {Text}",
                result.EmbeddingId, result.Distance,
                result.SourceText.Length > 80 ? result.SourceText.Substring(0, 77) + "..." : result.SourceText);
        }

        _logger.LogInformation("✓ Query complete");
    }

    private float[] GenerateRandomEmbedding(Random random, int dimension)
    {
        var embedding = new float[dimension];
        for (int i = 0; i < dimension; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2.0 - 1.0); // Range: -1 to 1
        }

        // Normalize to unit length (cosine similarity requirement)
        var magnitude = (float)Math.Sqrt(embedding.Sum(v => v * v));
        for (int i = 0; i < dimension; i++)
        {
            embedding[i] /= magnitude;
        }

        return embedding;
    }
}