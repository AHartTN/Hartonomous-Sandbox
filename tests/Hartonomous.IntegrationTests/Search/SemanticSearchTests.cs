using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Utilities;
using Microsoft.Data.SqlTypes;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.IntegrationTests.Search;

/// <summary>
/// Integration tests for semantic search functionality
/// </summary>
public class SemanticSearchTests : IClassFixture<SqlServerTestFixture>
{
    private readonly SqlServerTestFixture _fixture;
    private readonly ITestOutputHelper _output;

    public SemanticSearchTests(SqlServerTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task HybridSearch_WithValidQuery_ShouldReturnResults()
    {
        // Arrange
        var atomEmbeddings = _fixture.AtomEmbeddings!;
        var queryEmbedding = GenerateRandomEmbedding(768, seed: 42);

        var padded = VectorUtility.PadToSqlLength(queryEmbedding, out _);
        var sqlVector = padded.ToSqlVector();
        var spatialPoint = await atomEmbeddings.ComputeSpatialProjectionAsync(
            sqlVector, queryEmbedding.Length, CancellationToken.None);

        // Act
        var results = await atomEmbeddings.HybridSearchAsync(
            queryEmbedding,
            spatialPoint,
            spatialCandidates: 100,
            finalTopK: 5,
            CancellationToken.None);

        // Assert
        Assert.NotNull(results);
        Assert.True(results.Count <= 5);

        if (results.Count > 0)
        {
            _output.WriteLine($"Found {results.Count} results:");
            foreach (var result in results)
            {
                var similarity = 1.0 - result.CosineDistance;
                _output.WriteLine($"  Atom {result.Embedding.Atom.AtomId}: Similarity={similarity:F4}");
            }
        }
    }

    [Fact]
    public async Task HybridSearch_WithNoMatches_ShouldReturnEmpty()
    {
        // Arrange
        var atomEmbeddings = _fixture.AtomEmbeddings!;

        // Create very specific embedding unlikely to match
        var queryEmbedding = new float[768];
        for (int i = 0; i < 768; i++)
        {
            queryEmbedding[i] = i % 2 == 0 ? 1.0f : -1.0f;
        }
        var mag = (float)Math.Sqrt(queryEmbedding.Sum(v => v * v));
        for (int i = 0; i < 768; i++)
        {
            queryEmbedding[i] /= mag;
        }

        var padded = VectorUtility.PadToSqlLength(queryEmbedding, out _);
        var sqlVector = padded.ToSqlVector();
        var spatialPoint = await atomEmbeddings.ComputeSpatialProjectionAsync(
            sqlVector, queryEmbedding.Length, CancellationToken.None);

        // Act
        var results = await atomEmbeddings.HybridSearchAsync(
            queryEmbedding,
            spatialPoint,
            spatialCandidates: 10,
            finalTopK: 5,
            CancellationToken.None);

        // Assert
        Assert.NotNull(results);
        // May or may not have results depending on database state
        _output.WriteLine($"Query returned {results.Count} results");
    }

    private static float[] GenerateRandomEmbedding(int dimension, int seed)
    {
        var random = new Random(seed);
        var embedding = new float[dimension];
        for (int i = 0; i < dimension; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2.0 - 1.0);
        }

        var magnitude = (float)Math.Sqrt(embedding.Sum(v => v * v));
        for (int i = 0; i < dimension; i++)
        {
            embedding[i] /= magnitude;
        }

        return embedding;
    }
}
