using Hartonomous.Core.Interfaces;
using Hartonomous.Infrastructure.Ingestion;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.IntegrationTests.Ingestion;

/// <summary>
/// Integration tests for embedding ingestion and deduplication
/// </summary>
public class EmbeddingIngestionTests : IClassFixture<SqlServerTestFixture>
{
    private readonly SqlServerTestFixture _fixture;
    private readonly ITestOutputHelper _output;

    public EmbeddingIngestionTests(SqlServerTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task IngestEmbedding_FirstTime_ShouldNotBeDuplicate()
    {
        // Arrange
        var embeddingService = _fixture.EmbeddingIngestion!;
        var embedding = GenerateRandomEmbedding(768, seed: 42);
        var text = $"Unique test sentence {Guid.NewGuid()}";

        // Act
        var result = await embeddingService.IngestEmbeddingAsync(
            text, "test", embedding, null, CancellationToken.None);

        // Assert
        Assert.False(result.WasDuplicate);
        Assert.True(result.EmbeddingId > 0);
    }

    [Fact]
    public async Task IngestEmbedding_SameContentHash_ShouldDetectDuplicate()
    {
        // Arrange
        var embeddingService = _fixture.EmbeddingIngestion!;
        var embedding = GenerateRandomEmbedding(768, seed: 123);
        var text = $"Duplicate test {Guid.NewGuid()}";

        // Act - Insert first time
        var result1 = await embeddingService.IngestEmbeddingAsync(
            text, "test", embedding, null, CancellationToken.None);

        // Act - Insert same text again
        var result2 = await embeddingService.IngestEmbeddingAsync(
            text, "test", embedding, null, CancellationToken.None);

        // Assert
        Assert.False(result1.WasDuplicate);
        Assert.True(result2.WasDuplicate);
        Assert.Contains("hash", result2.DuplicateReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task IngestEmbedding_SimilarEmbedding_ShouldDetectSemanticDuplicate()
    {
        // Arrange
        var embeddingService = _fixture.EmbeddingIngestion!;
        var embedding1 = GenerateRandomEmbedding(768, seed: 456);
        var text1 = $"Original sentence {Guid.NewGuid()}";

        // Create similar embedding (95% similarity)
        var embedding2 = embedding1.Select(v => v * 0.95f).ToArray();
        var mag = (float)Math.Sqrt(embedding2.Sum(v => v * v));
        embedding2 = embedding2.Select(v => v / mag).ToArray();
        var text2 = $"Similar sentence {Guid.NewGuid()}";

        // Act
        var result1 = await embeddingService.IngestEmbeddingAsync(
            text1, "test", embedding1, null, CancellationToken.None);

        var result2 = await embeddingService.IngestEmbeddingAsync(
            text2, "test", embedding2, null, CancellationToken.None);

        // Assert
        Assert.False(result1.WasDuplicate);
        Assert.True(result2.WasDuplicate);
        Assert.Contains("semantic", result2.DuplicateReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task IngestSampleEmbeddings_BatchIngestion_ShouldTrackDeduplication()
    {
        // Arrange
        var embeddingService = _fixture.EmbeddingIngestion!;
        var count = 50;
        var random = new Random(42);
        int newCount = 0;
        int duplicateCount = 0;

        // Act
        for (int i = 0; i < count; i++)
        {
            var embedding = GenerateRandomEmbedding(768, random);
            var text = $"Sample sentence {i} - {Guid.NewGuid()}";

            var result = await embeddingService.IngestEmbeddingAsync(
                text, "batch-test", embedding, null, CancellationToken.None);

            if (result.WasDuplicate)
                duplicateCount++;
            else
                newCount++;
        }

        // Assert
        Assert.Equal(count, newCount + duplicateCount);
        _output.WriteLine($"Batch ingestion: {newCount} new, {duplicateCount} duplicates");
    }

    private static float[] GenerateRandomEmbedding(int dimension, int? seed = null)
    {
        var random = seed.HasValue ? new Random(seed.Value) : new Random();
        return GenerateRandomEmbedding(dimension, random);
    }

    private static float[] GenerateRandomEmbedding(int dimension, Random random)
    {
        var embedding = new float[dimension];
        for (int i = 0; i < dimension; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2.0 - 1.0);
        }

        // Normalize to unit length
        var magnitude = (float)Math.Sqrt(embedding.Sum(v => v * v));
        for (int i = 0; i < dimension; i++)
        {
            embedding[i] /= magnitude;
        }

        return embedding;
    }
}
