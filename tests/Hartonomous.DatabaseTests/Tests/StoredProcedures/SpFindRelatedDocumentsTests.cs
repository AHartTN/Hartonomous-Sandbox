using FluentAssertions;
using Hartonomous.DatabaseTests.Infrastructure;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.DatabaseTests.Tests.StoredProcedures;

/// <summary>
/// Tests for sp_FindRelatedDocuments stored procedure.
/// Validates semantic similarity search and related document discovery.
/// </summary>
[Trait("Category", "Database")]
[Trait("Category", "StoredProcedure")]
[Trait("Category", "Search")]
public class SpFindRelatedDocumentsTests : DatabaseTestBase
{
    public SpFindRelatedDocumentsTests(ITestOutputHelper output) : base() { }

    #region Basic Search Tests

    [Fact]
    public async Task SpFindRelatedDocuments_ValidAtomId_ReturnsRelated()
    {
        // Arrange
        var sourceAtomId = 1;
        var threshold = 0.7f;
        var maxResults = 10;

        // Act
        var related = await ExecuteReaderAsync(
            "EXEC sp_FindRelatedDocuments @SourceAtomId, @SimilarityThreshold, @MaxResults",
            new SqlParameter("@SourceAtomId", sourceAtomId),
            new SqlParameter("@SimilarityThreshold", threshold),
            new SqlParameter("@MaxResults", maxResults));

        // Assert
        related.Should().NotBeNull();
        related.Count.Should().BeLessThanOrEqualTo(maxResults);
    }

    [Fact]
    public async Task SpFindRelatedDocuments_HighThreshold_ReturnsFewerResults()
    {
        // Arrange
        var atomId = 1;

        // Act
        var lowThreshold = await ExecuteReaderAsync(
            "EXEC sp_FindRelatedDocuments @SourceAtomId, @SimilarityThreshold, @MaxResults",
            new SqlParameter("@SourceAtomId", atomId),
            new SqlParameter("@SimilarityThreshold", 0.5f),
            new SqlParameter("@MaxResults", 100));

        var highThreshold = await ExecuteReaderAsync(
            "EXEC sp_FindRelatedDocuments @SourceAtomId, @SimilarityThreshold, @MaxResults",
            new SqlParameter("@SourceAtomId", atomId),
            new SqlParameter("@SimilarityThreshold", 0.9f),
            new SqlParameter("@MaxResults", 100));

        // Assert
        highThreshold.Count.Should().BeLessThanOrEqualTo(lowThreshold.Count);
    }

    #endregion

    #region Result Limit Tests

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    public async Task SpFindRelatedDocuments_MaxResults_RespectsLimit(int maxResults)
    {
        // Arrange
        var atomId = 1;

        // Act
        var results = await ExecuteReaderAsync(
            "EXEC sp_FindRelatedDocuments @SourceAtomId, @SimilarityThreshold, @MaxResults",
            new SqlParameter("@SourceAtomId", atomId),
            new SqlParameter("@SimilarityThreshold", 0.5f),
            new SqlParameter("@MaxResults", maxResults));

        // Assert
        results.Count.Should().BeLessThanOrEqualTo(maxResults);
    }

    #endregion

    #region Tenant Isolation Tests

    [Fact]
    public async Task SpFindRelatedDocuments_DifferentTenants_IsolatesResults()
    {
        // Arrange
        var atomIdTenant1 = 1;
        var atomIdTenant2 = 1000; // Assumed different tenant

        // Act
        var tenant1Results = await ExecuteReaderAsync(
            "EXEC sp_FindRelatedDocuments @SourceAtomId, @SimilarityThreshold, @MaxResults",
            new SqlParameter("@SourceAtomId", atomIdTenant1),
            new SqlParameter("@SimilarityThreshold", 0.5f),
            new SqlParameter("@MaxResults", 10));

        var tenant2Results = await ExecuteReaderAsync(
            "EXEC sp_FindRelatedDocuments @SourceAtomId, @SimilarityThreshold, @MaxResults",
            new SqlParameter("@SourceAtomId", atomIdTenant2),
            new SqlParameter("@SimilarityThreshold", 0.5f),
            new SqlParameter("@MaxResults", 10));

        // Assert - Results should be isolated
        tenant1Results.Should().NotBeEquivalentTo(tenant2Results);
    }

    #endregion
}
