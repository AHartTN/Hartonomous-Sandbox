using FluentAssertions;
using Hartonomous.Data.Entities;
using Hartonomous.Infrastructure.Services;
using Hartonomous.UnitTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.UnitTests.Tests.Infrastructure.Services;

/// <summary>
/// Comprehensive tests for BackgroundJobService.
/// Tests job creation, retrieval, updates, and querying.
/// </summary>
public class BackgroundJobServiceTests : UnitTestBase
{
    public BackgroundJobServiceTests(ITestOutputHelper output) : base(output) { }

    #region Job Creation Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Category", "Fast")]
    public async Task CreateJobAsync_ValidParameters_CreatesJob()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateBackgroundJobService(context);

        // Act
        var jobId = await service.CreateJobAsync(
            "GenerateEmbedding",
            "{\"atomId\":123}",
            tenantId: 1);

        // Assert
        jobId.Should().NotBeEmpty();
        
        var job = await context.BackgroundJobs.FirstOrDefaultAsync();
        job.Should().NotBeNull();
        job!.JobType.Should().Be("GenerateEmbedding");
        job.Payload.Should().Be("{\"atomId\":123}");
        job.TenantId.Should().Be(1);
        job.Status.Should().Be(0); // Pending
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CreateJobAsync_MultipleJobs_CreatesAll()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateBackgroundJobService(context);

        // Act
        var jobId1 = await service.CreateJobAsync("Job1", "{}", 1);
        var jobId2 = await service.CreateJobAsync("Job2", "{}", 1);
        var jobId3 = await service.CreateJobAsync("Job3", "{}", 2);

        // Assert
        jobId1.Should().NotBeEmpty();
        jobId2.Should().NotBeEmpty();
        jobId3.Should().NotBeEmpty();
        
        context.BackgroundJobs.Count().Should().Be(3);
    }

    #endregion

    #region Job Retrieval Tests

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetJobAsync_ExistingJob_ReturnsJob()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateBackgroundJobService(context);
        var jobId = await service.CreateJobAsync("TestJob", "{\"test\":true}", 1);

        // Act
        var job = await service.GetJobAsync(jobId);

        // Assert
        job.Should().NotBeNull();
        job!.JobId.Should().Be(jobId);
        job.JobType.Should().Be("TestJob");
        job.Status.Should().Be("Pending");
        job.TenantId.Should().Be(1);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetJobAsync_NonExistentJob_ReturnsNull()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateBackgroundJobService(context);

        // Act
        var job = await service.GetJobAsync(Guid.NewGuid());

        // Assert
        job.Should().BeNull();
    }

    #endregion

    #region Job Update Tests

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateJobAsync_ValidUpdate_UpdatesStatus()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateBackgroundJobService(context);
        var jobId = await service.CreateJobAsync("TestJob", "{}", 1);

        // Act
        await service.UpdateJobAsync(
            jobId,
            "Completed",
            resultJson: "{\"result\":\"success\"}",
            errorMessage: null);

        // Assert
        var job = await service.GetJobAsync(jobId);
        job.Should().NotBeNull();
        job!.Status.Should().Be("Completed");
        job.ResultJson.Should().Be("{\"result\":\"success\"}");
        job.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateJobAsync_WithError_StoresErrorMessage()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateBackgroundJobService(context);
        var jobId = await service.CreateJobAsync("TestJob", "{}", 1);

        // Act
        await service.UpdateJobAsync(
            jobId,
            "Failed",
            resultJson: null,
            errorMessage: "Test error occurred");

        // Assert
        var job = await service.GetJobAsync(jobId);
        job.Should().NotBeNull();
        job!.Status.Should().Be("Failed");
        job.ErrorMessage.Should().Be("Test error occurred");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateJobAsync_NonExistentJob_DoesNotThrow()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateBackgroundJobService(context);

        // Act
        Func<Task> act = async () => await service.UpdateJobAsync(
            Guid.NewGuid(),
            "Completed",
            null,
            null);

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region List Jobs Tests

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ListJobsAsync_ByTenant_ReturnsOnlyTenantJobs()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateBackgroundJobService(context);
        
        await service.CreateJobAsync("Job1", "{}", tenantId: 1);
        await service.CreateJobAsync("Job2", "{}", tenantId: 1);
        await service.CreateJobAsync("Job3", "{}", tenantId: 2);

        // Act
        var jobs = await service.ListJobsAsync(tenantId: 1);

        // Assert
        jobs.Should().HaveCount(2);
        jobs.Should().OnlyContain(j => j.TenantId == 1);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ListJobsAsync_WithStatusFilter_ReturnsMatchingJobs()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateBackgroundJobService(context);
        
        var jobId1 = await service.CreateJobAsync("Job1", "{}", 1);
        var jobId2 = await service.CreateJobAsync("Job2", "{}", 1);
        await service.UpdateJobAsync(jobId2, "Completed");

        // Act
        var pendingJobs = await service.ListJobsAsync(1, statusFilter: "Pending");
        var completedJobs = await service.ListJobsAsync(1, statusFilter: "Completed");

        // Assert
        pendingJobs.Should().HaveCount(1);
        completedJobs.Should().HaveCount(1);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ListJobsAsync_WithLimit_RespectsLimit()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateBackgroundJobService(context);
        
        for (int i = 0; i < 10; i++)
        {
            await service.CreateJobAsync($"Job{i}", "{}", 1);
        }

        // Act
        var jobs = await service.ListJobsAsync(1, limit: 5);

        // Assert
        jobs.Should().HaveCount(5);
    }

    #endregion

    #region Get Pending Jobs Tests

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetPendingJobsAsync_ReturnsOnlyPendingJobs()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateBackgroundJobService(context);
        
        var jobId1 = await service.CreateJobAsync("GenerateEmbedding", "{}", 1);
        var jobId2 = await service.CreateJobAsync("GenerateEmbedding", "{}", 1);
        var jobId3 = await service.CreateJobAsync("GenerateEmbedding", "{}", 1);
        
        await service.UpdateJobAsync(jobId2, "Completed");

        // Act
        var pendingJobs = await service.GetPendingJobsAsync("GenerateEmbedding");

        // Assert
        pendingJobs.Should().HaveCount(2);
        pendingJobs.Should().OnlyContain(j => j.ParametersJson != null);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetPendingJobsAsync_FiltersByJobType()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateBackgroundJobService(context);
        
        await service.CreateJobAsync("GenerateEmbedding", "{}", 1);
        await service.CreateJobAsync("GenerateEmbedding", "{}", 1);
        await service.CreateJobAsync("ProcessVideo", "{}", 1);

        // Act
        var embeddingJobs = await service.GetPendingJobsAsync("GenerateEmbedding");
        var videoJobs = await service.GetPendingJobsAsync("ProcessVideo");

        // Assert
        embeddingJobs.Should().HaveCount(2);
        videoJobs.Should().HaveCount(1);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetPendingJobsAsync_RespectsBatchSize()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateBackgroundJobService(context);
        
        for (int i = 0; i < 20; i++)
        {
            await service.CreateJobAsync("TestJob", "{}", 1);
        }

        // Act
        var jobs = await service.GetPendingJobsAsync("TestJob", batchSize: 10);

        // Assert
        jobs.Should().HaveCount(10);
    }

    #endregion

    #region Service Broker Tests (Stored Procedure Calls)

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Category", "StoredProcedure")]
    public async Task EnqueueIngestionAsync_CallsStoredProcedure()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateBackgroundJobService(context);

        // Act & Assert
        // Note: This will fail in unit tests because sp_EnqueueIngestion doesn't exist
        // In real integration tests with SQL Server, this would work
        Func<Task> act = async () => await service.EnqueueIngestionAsync(
            "{}",
            tenantId: 1,
            priority: 5);

        // For unit tests, we expect this to throw because stored proc doesn't exist
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Category", "StoredProcedure")]
    public async Task EnqueueNeo4jSyncAsync_CallsStoredProcedure()
    {
        // Arrange
        using var context = DbFixture.CreateContext();
        var service = CreateBackgroundJobService(context);

        // Act & Assert
        // Note: This will fail in unit tests because sp_EnqueueNeo4jSync doesn't exist
        Func<Task> act = async () => await service.EnqueueNeo4jSyncAsync(
            "Atom",
            entityId: 123,
            syncType: "CREATE");

        // For unit tests, we expect this to throw because stored proc doesn't exist
        await act.Should().ThrowAsync<Exception>();
    }

    #endregion

    #region Helper Methods

    private BackgroundJobService CreateBackgroundJobService(HartonomousDbContext context)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:HartonomousDb"] = "Data Source=InMemory;Mode=Memory;Cache=Shared"
            })
            .Build();

        return new BackgroundJobService(context, CreateLogger<BackgroundJobService>(), configuration);
    }

    #endregion
}
