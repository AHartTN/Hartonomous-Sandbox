using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Utilities;
using Microsoft.Data.SqlTypes;

namespace ModelIngestion;

/// <summary>
/// Service for executing semantic search queries
/// </summary>
public class QueryService
{
    private readonly IAtomEmbeddingRepository _atomEmbeddings;
    private readonly ILogger<QueryService> _logger;

    public QueryService(
        IAtomEmbeddingRepository atomEmbeddings,
        ILogger<QueryService> logger)
    {
        _atomEmbeddings = atomEmbeddings ?? throw new ArgumentNullException(nameof(atomEmbeddings));
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
        var padded = VectorUtility.PadToSqlLength(queryEmbedding, out _);
        var sqlVector = new SqlVector<float>(padded);
        var spatialPoint = await _atomEmbeddings
            .ComputeSpatialProjectionAsync(sqlVector, queryEmbedding.Length, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Running hybrid AtomEmbeddings search (spatial + cosine)...");
        var hybridResults = await _atomEmbeddings
            .HybridSearchAsync(queryEmbedding, spatialPoint, spatialCandidates: 256, finalTopK: 5, cancellationToken)
            .ConfigureAwait(false);

        if (hybridResults.Count == 0)
        {
            _logger.LogWarning("No atom embeddings matched the query.");
        }
        else
        {
            _logger.LogInformation("Top {Count} matches:", hybridResults.Count);
            foreach (var entry in hybridResults)
            {
                var similarity = 1d - entry.CosineDistance;
                var atom = entry.Embedding.Atom;
                var preview = atom.CanonicalText;
                if (!string.IsNullOrEmpty(preview) && preview.Length > 80)
                {
                    preview = preview[..77] + "...";
                }

                _logger.LogInformation(
                    "  [Atom {AtomId} | Embedding {EmbeddingId}] Modality={Modality} Similarity={Sim:F4} Text={Preview}",
                    atom.AtomId,
                    entry.Embedding.AtomEmbeddingId,
                    atom.Modality,
                    similarity,
                    preview ?? "<no-text>");
            }
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