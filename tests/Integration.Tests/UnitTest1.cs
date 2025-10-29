using Hartonomous.Core.Entities;
using Hartonomous.Data;
using Hartonomous.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Integration.Tests;

public class EmbeddingRepositoryIntegrationTests : IDisposable
{
    private readonly HartonomousDbContext _context;
    private readonly EmbeddingRepository _repository;
    private readonly ILogger<EmbeddingRepository> _logger;

    public EmbeddingRepositoryIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<HartonomousDbContext>()
            .UseSqlServer("Server=localhost;Database=Hartonomous;Trusted_Connection=True;TrustServerCertificate=True;", 
                sqlOptions => sqlOptions.UseNetTopologySuite())
            .Options;

        _context = new HartonomousDbContext(options);
        _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<EmbeddingRepository>();
        _repository = new EmbeddingRepository(_context, _logger);
    }

    [Fact]
    public async Task ComputeSpatialProjectionAsync_ReturnsValidCoordinates()
    {
        // Arrange
        var testVector = new float[768];
        for (int i = 0; i < 768; i++)
        {
            testVector[i] = (float)i / 768.0f; // Simple gradient vector
        }

        // Act
        var result = await _repository.ComputeSpatialProjectionAsync(testVector);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Length);
        Assert.All(result, coord => Assert.True(coord >= 0)); // Distances should be non-negative
    }

    [Fact]
    public async Task CheckDuplicateBySimilarityAsync_NoMatch_ReturnsNull()
    {
        // Arrange - Use a truly unique vector pattern that's very different from existing test data
        var uniqueVector = new float[768];
        for (int i = 0; i < 768; i++)
        {
            uniqueVector[i] = (i % 2 == 0) ? 1000.0f : -500.0f; // Alternating large positive/negative values
        }

        // Act
        var result = await _repository.CheckDuplicateBySimilarityAsync(uniqueVector, 0.95);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AddAndRetrieveEmbedding_WorksCorrectly()
    {
        // Arrange
        var testVector = new float[768];
        for (int i = 0; i < 768; i++)
        {
            testVector[i] = (float)i / 768.0f;
        }

        var embedding = new Embedding
        {
            SourceText = "Test embedding for integration test",
            SourceType = "test",
            EmbeddingFull = new Microsoft.Data.SqlTypes.SqlVector<float>(testVector),
            EmbeddingModel = "test-model",
            Dimension = 768,
            ContentHash = Guid.NewGuid().ToString(),
            AccessCount = 1
        };

        // Act
        var added = await _repository.AddAsync(embedding);
        var retrieved = await _repository.GetByIdAsync(added.EmbeddingId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(added.EmbeddingId, retrieved.EmbeddingId);
        Assert.Equal("Test embedding for integration test", retrieved.SourceText);

        // Cleanup
        await _repository.DeleteAsync(added.EmbeddingId);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
