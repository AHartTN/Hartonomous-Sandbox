using FluentAssertions;
using Hartonomous.DatabaseTests.Infrastructure;
using Microsoft.Data.SqlClient;
using System.Data;
using Xunit;
using Xunit.Abstracts;

namespace Hartonomous.DatabaseTests.Tests.StoredProcedures;

/// <summary>
/// Tests for sp_ProjectTo3D stored procedure.
/// Validates embedding dimensionality reduction and spatial projection.
/// </summary>
[Trait("Category", "Database")]
[Trait("Category", "StoredProcedure")]
[Trait("Category", "Spatial")]
public class SpProjectTo3DTests : DatabaseTestBase
{
    public SpProjectTo3DTests(ITestOutputHelper output) : base() { }

    #region Basic Projection Tests

    [Fact]
    public async Task SpProjectTo3D_ValidEmbedding_ReturnsGeometry()
    {
        // Arrange
        var embedding = CreateFloat32Vector(Enumerable.Range(0, 1536)
            .Select(i => (float)Math.Sin(i * 0.1))
            .ToArray());

        // Act
        var geometry = await ExecuteScalarAsync<string>(
            "SELECT dbo.fn_ProjectTo3D(@embedding).ToString()",
            new SqlParameter("@embedding", SqlDbType.VarBinary) { Value = embedding });

        // Assert
        geometry.Should().NotBeNullOrEmpty();
        geometry.Should().StartWith("POINT");
    }

    [Fact]
    public async Task SpProjectTo3D_DifferentEmbeddings_ProducesDifferentPoints()
    {
        // Arrange
        var embedding1 = CreateFloat32Vector(Enumerable.Range(0, 1536).Select(i => (float)i).ToArray());
        var embedding2 = CreateFloat32Vector(Enumerable.Range(0, 1536).Select(i => (float)(i * 2)).ToArray());

        // Act
        var point1 = await ExecuteScalarAsync<string>(
            "SELECT dbo.fn_ProjectTo3D(@embedding).ToString()",
            new SqlParameter("@embedding", SqlDbType.VarBinary) { Value = embedding1 });

        var point2 = await ExecuteScalarAsync<string>(
            "SELECT dbo.fn_ProjectTo3D(@embedding).ToString()",
            new SqlParameter("@embedding", SqlDbType.VarBinary) { Value = embedding2 });

        // Assert
        point1.Should().NotBe(point2);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task SpProjectTo3D_NullEmbedding_ReturnsNull()
    {
        // Act
        var result = await ExecuteScalarAsync<string>(
            "SELECT dbo.fn_ProjectTo3D(NULL)");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Helper Methods

    private byte[] CreateFloat32Vector(float[] values)
    {
        var bytes = new byte[values.Length * 4];
        Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    #endregion
}
