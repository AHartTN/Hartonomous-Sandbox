using FluentAssertions;
using Hartonomous.DatabaseTests.Infrastructure;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.DatabaseTests.Tests.StoredProcedures;

/// <summary>
/// Tests for sp_LinkProvenance stored procedure.
/// Validates provenance chain creation between atoms and sources.
/// </summary>
[Trait("Category", "Database")]
[Trait("Category", "StoredProcedure")]
[Trait("Category", "Provenance")]
public class SpLinkProvenanceTests : DatabaseTestBase
{
    public SpLinkProvenanceTests(ITestOutputHelper output) : base() { }

    #region Basic Linking Tests

    [Fact]
    public async Task SpLinkProvenance_ValidAtoms_CreatesLink()
    {
        // Arrange
        var sourceAtomId = 1;
        var derivedAtomId = 2;

        // Act
        await ExecuteNonQueryAsync(
            "EXEC sp_LinkProvenance @SourceAtomId, @DerivedAtomId, @ProvenanceType",
            new SqlParameter("@SourceAtomId", sourceAtomId),
            new SqlParameter("@DerivedAtomId", derivedAtomId),
            new SqlParameter("@ProvenanceType", "derived_from"));

        // Assert - Link should exist
        var count = await ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM ProvenanceLinks WHERE SourceAtomId = @SourceAtomId AND DerivedAtomId = @DerivedAtomId",
            new SqlParameter("@SourceAtomId", sourceAtomId),
            new SqlParameter("@DerivedAtomId", derivedAtomId));

        count.Should().Be(1);
    }

    [Fact]
    public async Task SpLinkProvenance_ChainedLinks_CreatesHierarchy()
    {
        // Arrange
        var atom1 = 1;
        var atom2 = 2;
        var atom3 = 3;

        // Act - Create chain: atom1 ? atom2 ? atom3
        await ExecuteNonQueryAsync(
            "EXEC sp_LinkProvenance @SourceAtomId, @DerivedAtomId, @ProvenanceType",
            new SqlParameter("@SourceAtomId", atom1),
            new SqlParameter("@DerivedAtomId", atom2),
            new SqlParameter("@ProvenanceType", "derived_from"));

        await ExecuteNonQueryAsync(
            "EXEC sp_LinkProvenance @SourceAtomId, @DerivedAtomId, @ProvenanceType",
            new SqlParameter("@SourceAtomId", atom2),
            new SqlParameter("@DerivedAtomId", atom3),
            new SqlParameter("@ProvenanceType", "derived_from"));

        // Assert
        var count = await ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM ProvenanceLinks WHERE SourceAtomId IN (1, 2)");

        count.Should().Be(2);
    }

    #endregion

    #region Duplicate Prevention Tests

    [Fact]
    public async Task SpLinkProvenance_DuplicateLink_Idempotent()
    {
        // Arrange
        var sourceAtomId = 1;
        var derivedAtomId = 2;

        // Act - Insert same link twice
        await ExecuteNonQueryAsync(
            "EXEC sp_LinkProvenance @SourceAtomId, @DerivedAtomId, @ProvenanceType",
            new SqlParameter("@SourceAtomId", sourceAtomId),
            new SqlParameter("@DerivedAtomId", derivedAtomId),
            new SqlParameter("@ProvenanceType", "derived_from"));

        await ExecuteNonQueryAsync(
            "EXEC sp_LinkProvenance @SourceAtomId, @DerivedAtomId, @ProvenanceType",
            new SqlParameter("@SourceAtomId", sourceAtomId),
            new SqlParameter("@DerivedAtomId", derivedAtomId),
            new SqlParameter("@ProvenanceType", "derived_from"));

        // Assert - Should only have one link
        var count = await ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM ProvenanceLinks WHERE SourceAtomId = @SourceAtomId",
            new SqlParameter("@SourceAtomId", sourceAtomId));

        count.Should().Be(1);
    }

    #endregion
}
