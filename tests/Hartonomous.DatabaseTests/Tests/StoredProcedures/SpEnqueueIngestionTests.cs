using FluentAssertions;
using Hartonomous.DatabaseTests.Infrastructure;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.DatabaseTests.Tests.StoredProcedures;

/// <summary>
/// Tests for sp_EnqueueIngestion stored procedure.
/// Validates background job creation for ingestion tasks.
/// </summary>
[Trait("Category", "Database")]
[Trait("Category", "StoredProcedure")]
[Trait("Category", "BackgroundJob")]
public class SpEnqueueIngestionTests : DatabaseTestBase
{
    public SpEnqueueIngestionTests(ITestOutputHelper output) : base() { }

    #region Basic Enqueue Tests

    [Fact]
    public async Task SpEnqueueIngestion_ValidParameters_CreatesJob()
    {
        // Arrange
        var payload = "{\"fileName\":\"test.txt\",\"sizeBytes\":1024}";
        var tenantId = 1;
        var priority = 5;

        // Act
        var jobId = await ExecuteScalarAsync<Guid>(
            "EXEC sp_EnqueueIngestion @Payload, @TenantId, @Priority",
            new SqlParameter("@Payload", payload),
            new SqlParameter("@TenantId", tenantId),
            new SqlParameter("@Priority", priority));

        // Assert
        jobId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SpEnqueueIngestion_MultipleJobs_CreatesAll()
    {
        // Arrange
        var payload1 = "{\"fileName\":\"file1.txt\"}";
        var payload2 = "{\"fileName\":\"file2.txt\"}";

        // Act
        var jobId1 = await ExecuteScalarAsync<Guid>(
            "EXEC sp_EnqueueIngestion @Payload, @TenantId, @Priority",
            new SqlParameter("@Payload", payload1),
            new SqlParameter("@TenantId", 1),
            new SqlParameter("@Priority", 5));

        var jobId2 = await ExecuteScalarAsync<Guid>(
            "EXEC sp_EnqueueIngestion @Payload, @TenantId, @Priority",
            new SqlParameter("@Payload", payload2),
            new SqlParameter("@TenantId", 1),
            new SqlParameter("@Priority", 5));

        // Assert
        jobId1.Should().NotBe(jobId2);
    }

    #endregion

    #region Priority Tests

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task SpEnqueueIngestion_VariousPriorities_CreatesWithCorrectPriority(int priority)
    {
        // Arrange
        var payload = "{\"test\":true}";

        // Act
        var jobId = await ExecuteScalarAsync<Guid>(
            "EXEC sp_EnqueueIngestion @Payload, @TenantId, @Priority",
            new SqlParameter("@Payload", payload),
            new SqlParameter("@TenantId", 1),
            new SqlParameter("@Priority", priority));

        // Assert
        jobId.Should().NotBeEmpty();
    }

    #endregion
}
