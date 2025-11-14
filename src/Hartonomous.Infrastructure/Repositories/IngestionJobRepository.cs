using System;
using System.Linq.Expressions;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Hartonomous.Data.Entities;

namespace Hartonomous.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IIngestionJobRepository"/>.
/// Inherits base CRUD from EfRepository, adds job lifecycle operations.
/// </summary>
public class IngestionJobRepository : EfRepository<IngestionJob, long>, IIngestionJobRepository
{
    public IngestionJobRepository(HartonomousDbContext context, ILogger<IngestionJobRepository> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// IngestionJobs are identified by IngestionJobId property.
    /// </summary>
    protected override Expression<Func<IngestionJob, long>> GetIdExpression() => j => j.IngestionJobId;

    // Domain-specific operations

    public async Task<IngestionJob> StartJobAsync(IngestionJob job, CancellationToken cancellationToken = default)
    {
        return await AddAsync(job, cancellationToken);
    }

    /// <summary>
    /// Efficiently complete a job using ExecuteUpdateAsync.
    /// Avoids loading the entity into memory.
    /// </summary>
    public async Task CompleteJobAsync(long jobId, string? status, CancellationToken cancellationToken = default)
    {
        await DbSet
            .Where(j => j.IngestionJobId == jobId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(j => j.CompletedAt, DateTime.UtcNow)
                .SetProperty(j => j.Status, status),
                cancellationToken);
    }

    public async Task AddJobAtomsAsync(IEnumerable<IngestionJobAtom> jobAtoms, CancellationToken cancellationToken = default)
    {
        await Context.IngestionJobAtoms.AddRangeAsync(jobAtoms, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
    }
}
