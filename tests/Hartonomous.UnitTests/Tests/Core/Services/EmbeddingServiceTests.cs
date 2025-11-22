using FluentAssertions;
using Hartonomous.Core.Services;
using Hartonomous.UnitTests.Infrastructure;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.UnitTests.Tests.Core.Services;

/// <summary>
/// Tests for EmbeddingService - embedding generation and vector operations.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Service")]
public class EmbeddingServiceTests : UnitTestBase
{
    public EmbeddingServiceTests(ITestOutputHelper output) : base(output) { }

    #region Generate Embedding Tests

    [Fact]
    public async Task GenerateEmbeddingAsync_ValidText_ReturnsVector()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateEmbeddingService(context);
        var text = "This is a test sentence for embedding generation.";

        // Act
        var embedding = await service.GenerateEmbeddingAsync(text, tenantId: 1);

        // Assert
        embedding.Should().NotBeNull();
        embedding.Should().HaveCount(1536); // OpenAI embedding size
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_EmptyText_ThrowsException()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateEmbeddingService(context);

        // Act
        Func<Task> act = async () => await service.GenerateEmbeddingAsync("", tenantId: 1);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_NullText_ThrowsException()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateEmbeddingService(context);

        // Act
        Func<Task> act = async () => await service.GenerateEmbeddingAsync(null, tenantId: 1);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region Batch Embedding Tests

    [Fact]
    public async Task GenerateBatchEmbeddingsAsync_MultipleTexts_ReturnsVectors()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateEmbeddingService(context);
        var texts = new[] { "First sentence", "Second sentence", "Third sentence" };

        // Act
        var embeddings = await service.GenerateBatchEmbeddingsAsync(texts, tenantId: 1);

        // Assert
        embeddings.Should().HaveCount(3);
        embeddings.Should().OnlyContain(e => e.Length == 1536);
    }

    #endregion

    #region Similarity Tests

    [Fact]
    public async Task CalculateSimilarityAsync_SameText_ReturnsHighScore()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateEmbeddingService(context);
        var embedding1 = await service.GenerateEmbeddingAsync("test", tenantId: 1);
        var embedding2 = await service.GenerateEmbeddingAsync("test", tenantId: 1);

        // Act
        var similarity = service.CalculateCosineSimilarity(embedding1, embedding2);

        // Assert
        similarity.Should().BeGreaterThan(0.99f);
    }

    [Fact]
    public async Task CalculateSimilarityAsync_DifferentText_ReturnsLowScore()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateEmbeddingService(context);
        var embedding1 = await service.GenerateEmbeddingAsync("dog", tenantId: 1);
        var embedding2 = await service.GenerateEmbeddingAsync("car", tenantId: 1);

        // Act
        var similarity = service.CalculateCosineSimilarity(embedding1, embedding2);

        // Assert
        similarity.Should().BeLessThan(0.7f);
    }

    #endregion

    #region Helper Methods

    private IEmbeddingService CreateEmbeddingService(HartonomousDbContext context)
    {
        return new EmbeddingService(
            context,
            CreateLogger<EmbeddingService>());
    }

    #endregion
}
