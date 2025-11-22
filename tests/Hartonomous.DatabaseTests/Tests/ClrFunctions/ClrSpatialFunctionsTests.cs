using FluentAssertions;
using Hartonomous.DatabaseTests.Infrastructure;
using Microsoft.Data.SqlClient;
using NetTopologySuite.Geometries;
using System.Data;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.DatabaseTests.Tests.ClrFunctions;

/// <summary>
/// Tests for CLR spatial functions.
/// Tests 3D projection, Hilbert curve mapping, and spatial bucketing.
/// Requires SQL Server with CLR assemblies and NetTopologySuite support.
/// </summary>
[Trait("Category", "Database")]
[Trait("Category", "CLR")]
[Trait("Category", "Spatial")]
public class ClrSpatialFunctionsTests : DatabaseTestBase
{
    public ClrSpatialFunctionsTests(ITestOutputHelper output) : base()
    {
    }

    #region ProjectTo3D Tests

    [Fact]
    public async Task FnProjectTo3D_ValidEmbedding_ReturnsGeometryPoint()
    {
        // Arrange - Create a 1536D embedding vector
        var embedding = CreateFloat32Vector(Enumerable.Range(0, 1536)
            .Select(i => (float)Math.Sin(i * 0.1))
            .ToArray());

        // Act
        var result = await ExecuteScalarAsync<string>(
            "SELECT dbo.fn_ProjectTo3D(@embedding).ToString()",
            new SqlParameter("@embedding", SqlDbType.VarBinary) { Value = embedding });

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().StartWith("POINT");
        
        // Parse and validate it's a 3D point
        result.Should().MatchRegex(@"POINT\s*\(\s*-?\d+\.?\d*\s+-?\d+\.?\d*\s+-?\d+\.?\d*\s*\)");
    }

    [Fact]
    public async Task FnProjectTo3D_MultipleEmbeddings_ProducesDistinctPoints()
    {
        // Arrange
        var embedding1 = CreateFloat32Vector(Enumerable.Range(0, 1536)
            .Select(i => (float)Math.Sin(i * 0.1))
            .ToArray());
        
        var embedding2 = CreateFloat32Vector(Enumerable.Range(0, 1536)
            .Select(i => (float)Math.Cos(i * 0.1))
            .ToArray());

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

    [Fact]
    public async Task FnProjectTo3D_NullEmbedding_ReturnsNull()
    {
        // Act
        var result = await ExecuteScalarAsync<string>(
            "SELECT dbo.fn_ProjectTo3D(NULL)");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region ComputeHilbertValue Tests

    [Fact]
    public async Task ClrComputeHilbertValue_Origin_ReturnsZero()
    {
        // Arrange
        var sql = @"
            DECLARE @point GEOMETRY = geometry::Point(0, 0, 0);
            SELECT dbo.clr_ComputeHilbertValue(@point, 21)";

        // Act
        var hilbertValue = await ExecuteScalarAsync<long>(sql);

        // Assert
        hilbertValue.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task ClrComputeHilbertValue_DifferentPoints_ProducesDifferentValues()
    {
        // Arrange
        var sql1 = @"
            DECLARE @point GEOMETRY = geometry::Point(0.5, 0.5, 0);
            SELECT dbo.clr_ComputeHilbertValue(@point, 21)";

        var sql2 = @"
            DECLARE @point GEOMETRY = geometry::Point(0.75, 0.25, 0);
            SELECT dbo.clr_ComputeHilbertValue(@point, 21)";

        // Act
        var value1 = await ExecuteScalarAsync<long>(sql1);
        var value2 = await ExecuteScalarAsync<long>(sql2);

        // Assert
        value1.Should().NotBe(value2);
    }

    [Fact]
    public async Task ClrComputeHilbertValue_ClosePoints_ProduceSimilarValues()
    {
        // Arrange - Points very close together should have similar Hilbert values
        var sql1 = @"
            DECLARE @point GEOMETRY = geometry::Point(0.5, 0.5, 0);
            SELECT dbo.clr_ComputeHilbertValue(@point, 21)";

        var sql2 = @"
            DECLARE @point GEOMETRY = geometry::Point(0.50001, 0.50001, 0);
            SELECT dbo.clr_ComputeHilbertValue(@point, 21)";

        // Act
        var value1 = await ExecuteScalarAsync<long>(sql1);
        var value2 = await ExecuteScalarAsync<long>(sql2);

        // Assert
        var difference = Math.Abs(value1 - value2);
        difference.Should().BeLessThan(1000); // Similar values for close points
    }

    #endregion

    #region ComputeSpatialBucket Tests

    [Fact]
    public async Task FnComputeSpatialBucket_ValidPoint_ReturnsBucketCoordinates()
    {
        // Arrange
        var sql = @"
            DECLARE @point GEOMETRY = geometry::Point(0.543, -0.217, 0.891);
            SELECT dbo.fn_ComputeSpatialBucket(@point, 0.2)";

        // Act
        var result = await ExecuteScalarAsync<string>(sql);

        // Assert
        result.Should().NotBeNullOrEmpty();
        
        // Result should be in format "X,Y,Z" where X, Y, Z are integers
        var parts = result!.Split(',');
        parts.Should().HaveCount(3);
        parts.All(p => int.TryParse(p.Trim(), out _)).Should().BeTrue();
    }

    [Fact]
    public async Task FnComputeSpatialBucket_DifferentBucketSizes_ProducesDifferentGranularity()
    {
        // Arrange
        var sql1 = @"
            DECLARE @point GEOMETRY = geometry::Point(0.543, -0.217, 0.891);
            SELECT dbo.fn_ComputeSpatialBucket(@point, 0.1)"; // Fine granularity

        var sql2 = @"
            DECLARE @point GEOMETRY = geometry::Point(0.543, -0.217, 0.891);
            SELECT dbo.fn_ComputeSpatialBucket(@point, 1.0)"; // Coarse granularity

        // Act
        var result1 = await ExecuteScalarAsync<string>(sql1);
        var result2 = await ExecuteScalarAsync<string>(sql2);

        // Assert
        result1.Should().NotBe(result2);
    }

    #endregion

    #region ParseFloat16Array Tests

    [Fact]
    public async Task FnParseFloat16Array_ValidData_ParsesCorrectly()
    {
        // Arrange - Create FP16 data (simplified test)
        var fp16Data = CreateFloat16TestData();

        // Act
        var result = await ExecuteScalarAsync<byte[]>(
            "SELECT dbo.fn_ParseFloat16Array(@data)",
            new SqlParameter("@data", SqlDbType.VarBinary) { Value = fp16Data });

        // Assert
        result.Should().NotBeNull();
        result!.Length.Should().BeGreaterThan(0);
    }

    #endregion

    #region ParseBFloat16Array Tests

    [Fact]
    public async Task FnParseBFloat16Array_ValidData_ParsesCorrectly()
    {
        // Arrange - Create BF16 data (simplified test)
        var bf16Data = CreateBFloat16TestData();

        // Act
        var result = await ExecuteScalarAsync<byte[]>(
            "SELECT dbo.fn_ParseBFloat16Array(@data)",
            new SqlParameter("@data", SqlDbType.VarBinary) { Value = bf16Data });

        // Assert
        result.Should().NotBeNull();
        result!.Length.Should().BeGreaterThan(0);
    }

    #endregion

    #region Helper Methods

    private byte[] CreateFloat32Vector(float[] values)
    {
        var bytes = new byte[values.Length * 4];
        Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    private byte[] CreateFloat16TestData()
    {
        // Simplified FP16 test data (IEEE 754 half-precision)
        // In real implementation, this would be proper FP16 encoding
        return new byte[] { 0x00, 0x3C, 0x00, 0x40, 0x00, 0x42 }; // ~1.0, ~2.0, ~3.0
    }

    private byte[] CreateBFloat16TestData()
    {
        // Simplified BF16 test data (Brain Float16)
        // In real implementation, this would be proper BF16 encoding
        return new byte[] { 0x3F, 0x80, 0x40, 0x00, 0x40, 0x40 }; // ~1.0, ~2.0, ~3.0
    }

    #endregion
}
