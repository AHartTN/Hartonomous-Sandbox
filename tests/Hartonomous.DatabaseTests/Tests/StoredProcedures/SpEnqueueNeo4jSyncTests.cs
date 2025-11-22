using FluentAssertions;
using Hartonomous.DatabaseTests.Infrastructure;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.DatabaseTests.Tests.StoredProcedures;

/// <summary>
/// Tests for sp_EnqueueNeo4jSync stored procedure.
/// Validates graph database synchronization job creation.
/// </summary>
[Trait("Category", "Database")]
[Trait("Category", "StoredProcedure")]
[Trait("Category", "Neo4j")]
public class SpEnqueueNeo4jSyncTests : DatabaseTestBase
{
    public SpEnqueueNeo4jSyncTests(ITestOutputHelper output) : base() { }

    #region Basic Sync Tests

    [Fact]
    public async Task SpEnqueueNeo4jSync_CreateOperation_EnqueuesJob()
    {
        // Arrange
        var entityType = "Atom";
        var entityId = 123;
        var syncType = "CREATE";

        // Act
        var jobId = await ExecuteScalarAsync<Guid>(
            "EXEC sp_EnqueueNeo4jSync @EntityType, @EntityId, @SyncType",
            new SqlParameter("@EntityType", entityType),
            new SqlParameter("@EntityId", entityId),
            new SqlParameter("@SyncType", syncType));

        // Assert
        jobId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SpEnqueueNeo4jSync_UpdateOperation_EnqueuesJob()
    {
        // Arrange
        var entityType = "Atom";
        var entityId = 456;
        var syncType = "UPDATE";

        // Act
        var jobId = await ExecuteScalarAsync<Guid>(
            "EXEC sp_EnqueueNeo4jSync @EntityType, @EntityId, @SyncType",
            new SqlParameter("@EntityType", entityType),
            new SqlParameter("@EntityId", entityId),
            new SqlParameter("@SyncType", syncType));

        // Assert
        jobId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SpEnqueueNeo4jSync_DeleteOperation_EnqueuesJob()
    {
        // Arrange
        var entityType = "Atom";
        var entityId = 789;
        var syncType = "DELETE";

        // Act
        var jobId = await ExecuteScalarAsync<Guid>(
            "EXEC sp_EnqueueNeo4jSync @EntityType, @EntityId, @SyncType",
            new SqlParameter("@EntityType", entityType),
            new SqlParameter("@EntityId", entityId),
            new SqlParameter("@SyncType", syncType));

        // Assert
        jobId.Should().NotBeEmpty();
    }

    #endregion

    #region Entity Type Tests

    [Theory]
    [InlineData("Atom")]
    [InlineData("Composition")]
    [InlineData("ProvenanceLink")]
    public async Task SpEnqueueNeo4jSync_VariousEntityTypes_EnqueuesCorrectly(string entityType)
    {
        // Act
        var jobId = await ExecuteScalarAsync<Guid>(
            "EXEC sp_EnqueueNeo4jSync @EntityType, @EntityId, @SyncType",
            new SqlParameter("@EntityType", entityType),
            new SqlParameter("@EntityId", 100),
            new SqlParameter("@SyncType", "CREATE"));

        // Assert
        jobId.Should().NotBeEmpty();
    }

    #endregion
}
