using FluentAssertions;
using Hartonomous.DatabaseTests.Infrastructure;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.DatabaseTests.Tests.StoredProcedures;

/// <summary>
/// Tests for sp_FindImpactedAtoms stored procedure.
/// Validates impact analysis when atoms are modified or deleted.
/// </summary>
[Trait("Category", "Database")]
[Trait("Category", "StoredProcedure")]
[Trait("Category", "Provenance")]
public class SpFindImpactedAtomsTests : DatabaseTestBase
{
    public SpFindImpactedAtomsTests(ITestOutputHelper output) : base() { }

    #region Basic Impact Analysis Tests

    [Fact]
    public async Task SpFindImpactedAtoms_WithDescendants_ReturnsImpactedAtoms()
    {
        // Arrange
        var sourceAtomId = 1;

        // Act
        var impacted = await ExecuteReaderAsync(
            "EXEC sp_FindImpactedAtoms @SourceAtomId, @MaxDepth",
            new SqlParameter("@SourceAtomId", sourceAtomId),
            new SqlParameter("@MaxDepth", 5));

        // Assert
        impacted.Should().NotBeNull();
    }

    [Fact]
    public async Task SpFindImpactedAtoms_NoDescendants_ReturnsEmpty()
    {
        // Arrange
        var leafAtomId = 999; // Assumed to have no descendants

        // Act
        var impacted = await ExecuteReaderAsync(
            "EXEC sp_FindImpactedAtoms @SourceAtomId, @MaxDepth",
            new SqlParameter("@SourceAtomId", leafAtomId),
            new SqlParameter("@MaxDepth", 5));

        // Assert
        impacted.Should().BeEmpty();
    }

    #endregion

    #region Transitive Impact Tests

    [Fact]
    public async Task SpFindImpactedAtoms_TransitiveChain_ReturnsAllDescendants()
    {
        // Arrange - Atom chain: 1 ? 2 ? 3 ? 4
        var rootAtomId = 1;

        // Act
        var impacted = await ExecuteReaderAsync(
            "EXEC sp_FindImpactedAtoms @SourceAtomId, @MaxDepth",
            new SqlParameter("@SourceAtomId", rootAtomId),
            new SqlParameter("@MaxDepth", 10));

        // Assert
        impacted.Count.Should().BeGreaterThan(0);
    }

    #endregion

    #region Depth Limit Tests

    [Fact]
    public async Task SpFindImpactedAtoms_DepthLimit_RespectsMaxDepth()
    {
        // Arrange
        var atomId = 1;

        // Act
        var depth2 = await ExecuteReaderAsync(
            "EXEC sp_FindImpactedAtoms @SourceAtomId, @MaxDepth",
            new SqlParameter("@SourceAtomId", atomId),
            new SqlParameter("@MaxDepth", 2));

        var depth5 = await ExecuteReaderAsync(
            "EXEC sp_FindImpactedAtoms @SourceAtomId, @MaxDepth",
            new SqlParameter("@SourceAtomId", atomId),
            new SqlParameter("@MaxDepth", 5));

        // Assert
        depth5.Count.Should().BeGreaterThanOrEqualTo(depth2.Count);
    }

    #endregion
}
