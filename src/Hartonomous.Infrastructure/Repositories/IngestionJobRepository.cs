using System;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Microsoft.EntityFrameworkCore;

namespace Hartonomous.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IIngestionJobRepository"/>.
/// </summary>
public class IngestionJobRepository : IIngestionJobRepository
{
    private readonly HartonomousDbContext _context;

    public IngestionJobRepository(HartonomousDbContext context)
    {
        _context = context;
    }

    public async Task<IngestionJob> StartJobAsync(IngestionJob job, CancellationToken cancellationToken = default)
    {
        _context.IngestionJobs.Add(job);
        await _context.SaveChangesAsync(cancellationToken);
        return job;
    }

    public async Task CompleteJobAsync(long jobId, string? status, CancellationToken cancellationToken = default)
    {
        var job = await _context.IngestionJobs.FirstOrDefaultAsync(j => j.IngestionJobId == jobId, cancellationToken);
        if (job is null)
        {
            return;
        }

        job.CompletedAt = DateTime.UtcNow;
        job.Status = status;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddJobAtomsAsync(IEnumerable<IngestionJobAtom> jobAtoms, CancellationToken cancellationToken = default)
    {
        await _context.IngestionJobAtoms.AddRangeAsync(jobAtoms, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
