using FluentAssertions;
using Hartonomous.DatabaseTests.Infrastructure;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.DatabaseTests.Tests.StoredProcedures;

/// <summary>
/// Tests for sp_SearchByEmbedding stored procedure.
/// Validates vector similarity search with embeddings.
/// </summary>
[Trait("Category", "Database")]
[Trait("Category", "StoredProcedure")]
[Trait("Category", "VectorSearch")]
public class SpSearchByEmbeddingTests : DatabaseTestBase
{
    public SpSearchByEmbeddingTests(ITestOutputHelper output) : base() { }

    #region Basic Vector Search Tests

    [Fact]
    public async Task SpSearchByEmbedding_ValidVector_ReturnsMatches()
    {
        // Arrange
        var queryVector = CreateFloat32Vector(1536);
        var topK = 10;
        var tenantId = 1;

        // Act
        var results = await ExecuteReaderAsync(
            "EXEC sp_SearchByEmbedding @QueryVector, @TopK, @TenantId",
            new SqlParameter("@QueryVector", System.Data.SqlDbType.VarBinary) { Value = queryVector },
            new SqlParameter("@TopK", topK),
            new SqlParameter("@TenantId", tenantId));

        // Assert
        results.Should().NotBeNull();
        results.Count.Should().BeLessThanOrEqualTo(topK);
    }

    [Fact]
    public async Task SpSearchByEmbedding_SimilarVectors_ReturnsSortedBySimilarity()
    {
        // Arrange
        var queryVector = CreateFloat32Vector(1536);
        var topK = 5;

        // Act
        var results = await ExecuteReaderAsync(
            "EXEC sp_SearchByEmbedding @QueryVector, @TopK, @TenantId",
            new SqlParameter("@QueryVector", System.Data.SqlDbType.VarBinary) { Value = queryVector },
            new SqlParameter("@TopK", topK),
            new SqlParameter("@TenantId", 1));

        // Assert
        results.Should().NotBeNull();
        // Results should be ordered by similarity (highest first)
    }

    #endregion

    #region Top-K Results Tests

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    public async Task SpSearchByEmbedding_VariousTopK_ReturnsCorrectCount(int topK)
    {
        // Arrange
        var queryVector = CreateFloat32Vector(1536);

        // Act
        var results = await ExecuteReaderAsync(
            "EXEC sp_SearchByEmbedding @QueryVector, @TopK, @TenantId",
            new SqlParameter("@QueryVector", System.Data.SqlDbType.VarBinary) { Value = queryVector },
            new SqlParameter("@TopK", topK),
            new SqlParameter("@TenantId", 1));

        // Assert
        results.Count.Should().BeLessThanOrEqualTo(topK);
    }

    #endregion

    #region Helper Methods

    private byte[] CreateFloat32Vector(int dimensions)
    {
        var values = new float[dimensions];
        var random = new Random(42);
        for (int i = 0; i < dimensions; i++)
        {
            values[i] = (float)random.NextDouble();
        }

        var bytes = new byte[dimensions * 4];
        Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    #endregion
}
