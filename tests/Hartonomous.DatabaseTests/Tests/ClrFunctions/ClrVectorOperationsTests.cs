using FluentAssertions;
using Hartonomous.DatabaseTests.Infrastructure;
using Microsoft.Data.SqlClient;
using System.Data;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.DatabaseTests.Tests.ClrFunctions;

/// <summary>
/// Tests for CLR vector operations functions.
/// Requires SQL Server with CLR assemblies deployed.
/// Tests cosine similarity, dot product, Euclidean distance, and vector normalization.
/// </summary>
[Trait("Category", "Database")]
[Trait("Category", "CLR")]
public class ClrVectorOperationsTests : DatabaseTestBase
{
    public ClrVectorOperationsTests(ITestOutputHelper output) : base()
    {
        // Fixture is injected via base class
    }

    #region Cosine Similarity Tests

    [Fact]
    public async Task ClrCosineSimilarity_IdenticalVectors_ReturnsOne()
    {
        // Arrange
        var vector = CreateFloat32Vector(new float[] { 1.0f, 0.5f, 0.25f });

        // Act
        var similarity = await ExecuteScalarAsync<float>(
            "SELECT dbo.clr_CosineSimilarity(@vec1, @vec2)",
            new SqlParameter("@vec1", SqlDbType.VarBinary) { Value = vector },
            new SqlParameter("@vec2", SqlDbType.VarBinary) { Value = vector });

        // Assert
        similarity.Should().BeApproximately(1.0f, 0.0001f);
    }

    [Fact]
    public async Task ClrCosineSimilarity_OrthogonalVectors_ReturnsZero()
    {
        // Arrange
        var vec1 = CreateFloat32Vector(new float[] { 1.0f, 0.0f, 0.0f });
        var vec2 = CreateFloat32Vector(new float[] { 0.0f, 1.0f, 0.0f });

        // Act
        var similarity = await ExecuteScalarAsync<float>(
            "SELECT dbo.clr_CosineSimilarity(@vec1, @vec2)",
            new SqlParameter("@vec1", SqlDbType.VarBinary) { Value = vec1 },
            new SqlParameter("@vec2", SqlDbType.VarBinary) { Value = vec2 });

        // Assert
        similarity.Should().BeApproximately(0.0f, 0.0001f);
    }

    [Fact]
    public async Task ClrCosineSimilarity_OppositeVectors_ReturnsNegativeOne()
    {
        // Arrange
        var vec1 = CreateFloat32Vector(new float[] { 1.0f, 1.0f, 1.0f });
        var vec2 = CreateFloat32Vector(new float[] { -1.0f, -1.0f, -1.0f });

        // Act
        var similarity = await ExecuteScalarAsync<float>(
            "SELECT dbo.clr_CosineSimilarity(@vec1, @vec2)",
            new SqlParameter("@vec1", SqlDbType.VarBinary) { Value = vec1 },
            new SqlParameter("@vec2", SqlDbType.VarBinary) { Value = vec2 });

        // Assert
        similarity.Should().BeApproximately(-1.0f, 0.0001f);
    }

    [Fact]
    public async Task ClrCosineSimilarity_LargeVectors_CalculatesCorrectly()
    {
        // Arrange - 1536D vectors (typical for embeddings)
        var vec1 = CreateFloat32Vector(Enumerable.Range(0, 1536).Select(i => (float)i / 1536).ToArray());
        var vec2 = CreateFloat32Vector(Enumerable.Range(0, 1536).Select(i => (float)(1536 - i) / 1536).ToArray());

        // Act
        var similarity = await ExecuteScalarAsync<float>(
            "SELECT dbo.clr_CosineSimilarity(@vec1, @vec2)",
            new SqlParameter("@vec1", SqlDbType.VarBinary) { Value = vec1 },
            new SqlParameter("@vec2", SqlDbType.VarBinary) { Value = vec2 });

        // Assert
        similarity.Should().BeInRange(-1.0f, 1.0f);
    }

    #endregion

    #region Dot Product Tests

    [Fact]
    public async Task ClrDotProduct_SimpleVectors_CalculatesCorrectly()
    {
        // Arrange
        var vec1 = CreateFloat32Vector(new float[] { 1.0f, 2.0f, 3.0f });
        var vec2 = CreateFloat32Vector(new float[] { 4.0f, 5.0f, 6.0f });
        // Expected: 1*4 + 2*5 + 3*6 = 4 + 10 + 18 = 32

        // Act
        var dotProduct = await ExecuteScalarAsync<float>(
            "SELECT dbo.clr_DotProduct(@vec1, @vec2)",
            new SqlParameter("@vec1", SqlDbType.VarBinary) { Value = vec1 },
            new SqlParameter("@vec2", SqlDbType.VarBinary) { Value = vec2 });

        // Assert
        dotProduct.Should().BeApproximately(32.0f, 0.0001f);
    }

    [Fact]
    public async Task ClrDotProduct_ZeroVector_ReturnsZero()
    {
        // Arrange
        var vec1 = CreateFloat32Vector(new float[] { 1.0f, 2.0f, 3.0f });
        var vec2 = CreateFloat32Vector(new float[] { 0.0f, 0.0f, 0.0f });

        // Act
        var dotProduct = await ExecuteScalarAsync<float>(
            "SELECT dbo.clr_DotProduct(@vec1, @vec2)",
            new SqlParameter("@vec1", SqlDbType.VarBinary) { Value = vec1 },
            new SqlParameter("@vec2", SqlDbType.VarBinary) { Value = vec2 });

        // Assert
        dotProduct.Should().BeApproximately(0.0f, 0.0001f);
    }

    #endregion

    #region Euclidean Distance Tests

    [Fact]
    public async Task ClrEuclideanDistance_IdenticalVectors_ReturnsZero()
    {
        // Arrange
        var vector = CreateFloat32Vector(new float[] { 1.0f, 2.0f, 3.0f });

        // Act
        var distance = await ExecuteScalarAsync<float>(
            "SELECT dbo.clr_EuclideanDistance(@vec1, @vec2)",
            new SqlParameter("@vec1", SqlDbType.VarBinary) { Value = vector },
            new SqlParameter("@vec2", SqlDbType.VarBinary) { Value = vector });

        // Assert
        distance.Should().BeApproximately(0.0f, 0.0001f);
    }

    [Fact]
    public async Task ClrEuclideanDistance_SimpleVectors_CalculatesCorrectly()
    {
        // Arrange
        var vec1 = CreateFloat32Vector(new float[] { 0.0f, 0.0f, 0.0f });
        var vec2 = CreateFloat32Vector(new float[] { 3.0f, 4.0f, 0.0f });
        // Expected: sqrt(3^2 + 4^2 + 0^2) = sqrt(25) = 5.0

        // Act
        var distance = await ExecuteScalarAsync<float>(
            "SELECT dbo.clr_EuclideanDistance(@vec1, @vec2)",
            new SqlParameter("@vec1", SqlDbType.VarBinary) { Value = vec1 },
            new SqlParameter("@vec2", SqlDbType.VarBinary) { Value = vec2 });

        // Assert
        distance.Should().BeApproximately(5.0f, 0.0001f);
    }

    #endregion

    #region Normalize Vector Tests

    [Fact]
    public async Task ClrNormalizeVector_NonNormalizedVector_ReturnsUnitLength()
    {
        // Arrange
        var vector = CreateFloat32Vector(new float[] { 3.0f, 4.0f, 0.0f });

        // Act
        var normalized = await ExecuteScalarAsync<byte[]>(
            "SELECT dbo.clr_NormalizeVector(@vec)",
            new SqlParameter("@vec", SqlDbType.VarBinary) { Value = vector });

        // Assert
        normalized.Should().NotBeNull();
        
        // Parse normalized vector and check magnitude
        var normalizedFloats = ParseFloat32Vector(normalized!);
        var magnitude = Math.Sqrt(normalizedFloats.Sum(x => x * x));
        magnitude.Should().BeApproximately(1.0, 0.0001);
    }

    [Fact]
    public async Task ClrNormalizeVector_ZeroVector_ReturnsZeroVector()
    {
        // Arrange
        var vector = CreateFloat32Vector(new float[] { 0.0f, 0.0f, 0.0f });

        // Act
        var normalized = await ExecuteScalarAsync<byte[]>(
            "SELECT dbo.clr_NormalizeVector(@vec)",
            new SqlParameter("@vec", SqlDbType.VarBinary) { Value = vector });

        // Assert
        normalized.Should().NotBeNull();
        var normalizedFloats = ParseFloat32Vector(normalized!);
        normalizedFloats.Should().AllSatisfy(x => x.Should().Be(0.0f));
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a VARBINARY representation of float32 vector.
    /// </summary>
    private byte[] CreateFloat32Vector(float[] values)
    {
        var bytes = new byte[values.Length * 4];
        Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    /// <summary>
    /// Parses a VARBINARY into float32 array.
    /// </summary>
    private float[] ParseFloat32Vector(byte[] bytes)
    {
        var floats = new float[bytes.Length / 4];
        Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
        return floats;
    }

    #endregion
}
