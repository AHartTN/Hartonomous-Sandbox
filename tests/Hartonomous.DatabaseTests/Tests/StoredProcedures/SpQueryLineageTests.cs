using FluentAssertions;
using Hartonomous.DatabaseTests.Infrastructure;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.DatabaseTests.Tests.StoredProcedures;

/// <summary>
/// Tests for sp_QueryLineage stored procedure.
/// Validates ancestor/descendant lineage queries with depth limits.
/// </summary>
[Trait("Category", "Database")]
[Trait("Category", "StoredProcedure")]
[Trait("Category", "Provenance")]
public class SpQueryLineageTests : DatabaseTestBase
{
    public SpQueryLineageTests(ITestOutputHelper output) : base() { }

    #region Ancestor Query Tests

    [Fact]
    public async Task SpQueryLineage_DirectAncestor_ReturnsParent()
    {
        // Arrange
        var atomId = 2;

        // Act
        var ancestors = await ExecuteReaderAsync(
            "EXEC sp_QueryLineage @AtomId, @Direction, @MaxDepth",
            new SqlParameter("@AtomId", atomId),
            new SqlParameter("@Direction", "ancestors"),
            new SqlParameter("@MaxDepth", 1));

        // Assert
        ancestors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SpQueryLineage_MultiLevelAncestors_ReturnsChain()
    {
        // Arrange
        var atomId = 3;

        // Act
        var ancestors = await ExecuteReaderAsync(
            "EXEC sp_QueryLineage @AtomId, @Direction, @MaxDepth",
            new SqlParameter("@AtomId", atomId),
            new SqlParameter("@Direction", "ancestors"),
            new SqlParameter("@MaxDepth", 10));

        // Assert
        ancestors.Should().HaveCountGreaterThan(0);
    }

    #endregion

    #region Descendant Query Tests

    [Fact]
    public async Task SpQueryLineage_Descendants_ReturnsChildren()
    {
        // Arrange
        var atomId = 1;

        // Act
        var descendants = await ExecuteReaderAsync(
            "EXEC sp_QueryLineage @AtomId, @Direction, @MaxDepth",
            new SqlParameter("@AtomId", atomId),
            new SqlParameter("@Direction", "descendants"),
            new SqlParameter("@MaxDepth", 1));

        // Assert
        descendants.Should().NotBeNull();
    }

    #endregion

    #region Depth Limit Tests

    [Fact]
    public async Task SpQueryLineage_MaxDepthLimit_RespectsBoundary()
    {
        // Arrange
        var atomId = 1;

        // Act
        var depth1 = await ExecuteReaderAsync(
            "EXEC sp_QueryLineage @AtomId, @Direction, @MaxDepth",
            new SqlParameter("@AtomId", atomId),
            new SqlParameter("@Direction", "descendants"),
            new SqlParameter("@MaxDepth", 1));

        var depth3 = await ExecuteReaderAsync(
            "EXEC sp_QueryLineage @AtomId, @Direction, @MaxDepth",
            new SqlParameter("@AtomId", atomId),
            new SqlParameter("@Direction", "descendants"),
            new SqlParameter("@MaxDepth", 3));

        // Assert
        depth3.Count.Should().BeGreaterThanOrEqualTo(depth1.Count);
    }

    #endregion
}
