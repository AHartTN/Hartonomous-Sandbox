using Hartonomous.Core.Interfaces.BackgroundJob;
using Moq;

namespace Hartonomous.UnitTests.Infrastructure.Builders;

/// <summary>
/// Fluent builder for creating mock IBackgroundJobService in tests.
/// Simplifies job service mocking with sensible defaults.
/// </summary>
public class MockBackgroundJobServiceBuilder
{
    private readonly List<(Guid JobId, string JobType, string ParametersJson, int TenantId)> _createdJobs = new();
    private readonly Dictionary<Guid, BackgroundJobInfo> _jobs = new();
    private bool _throwOnCreate;
    private bool _throwOnGet;
    private bool _throwOnUpdate;

    /// <summary>
    /// Configures the service to throw an exception when CreateJobAsync is called.
    /// </summary>
    public MockBackgroundJobServiceBuilder ThrowOnCreate()
    {
        _throwOnCreate = true;
        return this;
    }

    /// <summary>
    /// Configures the service to throw an exception when GetJobAsync is called.
    /// </summary>
    public MockBackgroundJobServiceBuilder ThrowOnGet()
    {
        _throwOnGet = true;
        return this;
    }

    /// <summary>
    /// Configures the service to throw an exception when UpdateJobAsync is called.
    /// </summary>
    public MockBackgroundJobServiceBuilder ThrowOnUpdate()
    {
        _throwOnUpdate = true;
        return this;
    }

    /// <summary>
    /// Pre-seeds a job in the mock service.
    /// </summary>
    public MockBackgroundJobServiceBuilder WithExistingJob(
        Guid jobId,
        string jobType,
        string status = "Pending",
        int tenantId = 1)
    {
        _jobs[jobId] = new BackgroundJobInfo(
            jobId,
            jobType,
            status,
            "{}",
            null,
            null,
            tenantId,
            DateTime.UtcNow,
            null);
        return this;
    }

    /// <summary>
    /// Builds the mock IBackgroundJobService with configured behavior.
    /// </summary>
    public IBackgroundJobService Build()
    {
        var mock = new Mock<IBackgroundJobService>();

        // CreateJobAsync
        mock.Setup(x => x.CreateJobAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string jobType, string parametersJson, int tenantId, CancellationToken ct) =>
            {
                if (_throwOnCreate)
                    throw new InvalidOperationException("Mock configured to throw on CreateJobAsync");

                var jobId = Guid.NewGuid();
                _createdJobs.Add((jobId, jobType, parametersJson, tenantId));

                var jobInfo = new BackgroundJobInfo(
                    jobId,
                    jobType,
                    "Pending",
                    parametersJson,
                    null,
                    null,
                    tenantId,
                    DateTime.UtcNow,
                    null);
                _jobs[jobId] = jobInfo;

                return jobId;
            });

        // GetJobAsync
        mock.Setup(x => x.GetJobAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid jobId, CancellationToken ct) =>
            {
                if (_throwOnGet)
                    throw new InvalidOperationException("Mock configured to throw on GetJobAsync");

                return _jobs.TryGetValue(jobId, out var job) ? job : null;
            });

        // UpdateJobAsync
        mock.Setup(x => x.UpdateJobAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns((Guid jobId, string status, string? resultJson, string? errorMessage, CancellationToken ct) =>
            {
                if (_throwOnUpdate)
                    throw new InvalidOperationException("Mock configured to throw on UpdateJobAsync");

                if (_jobs.TryGetValue(jobId, out var existingJob))
                {
                    _jobs[jobId] = existingJob with
                    {
                        Status = status,
                        ResultJson = resultJson,
                        ErrorMessage = errorMessage,
                        CompletedAt = DateTime.UtcNow
                    };
                }

                return Task.CompletedTask;
            });

        // ListJobsAsync
        mock.Setup(x => x.ListJobsAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((int tenantId, string? statusFilter, int limit, CancellationToken ct) =>
            {
                var query = _jobs.Values.Where(j => j.TenantId == tenantId);

                if (!string.IsNullOrEmpty(statusFilter))
                    query = query.Where(j => j.Status == statusFilter);

                return query.Take(limit).ToList();
            });

        // GetPendingJobsAsync
        mock.Setup(x => x.GetPendingJobsAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string jobType, int batchSize, CancellationToken ct) =>
            {
                return _jobs.Values
                    .Where(j => j.JobType == jobType && j.Status == "Pending")
                    .Take(batchSize)
                    .Select(j => (j.JobId, j.ParametersJson ?? "{}"))
                    .ToList();
            });

        return mock.Object;
    }

    /// <summary>
    /// Gets all jobs created through this mock (for verification).
    /// </summary>
    public IReadOnlyList<(Guid JobId, string JobType, string ParametersJson, int TenantId)> GetCreatedJobs()
    {
        return _createdJobs.AsReadOnly();
    }
}
