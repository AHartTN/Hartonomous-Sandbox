using System.Linq;
using System.Linq.Expressions;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Hartonomous.Data.Entities;

namespace Hartonomous.Infrastructure.Repositories;

/// <summary>
/// Repository for managing inference request history and analytics.
/// </summary>
public sealed class InferenceRepository : EfRepository<InferenceRequest, long>, IInferenceRepository
{
    public InferenceRepository(HartonomousDbContext context, ILogger<InferenceRepository> logger)
        : base(context, logger)
    {
    }

    protected override Expression<Func<InferenceRequest, long>> GetIdExpression()
        => request => request.InferenceId;

    protected override IQueryable<InferenceRequest> IncludeRelatedEntities(IQueryable<InferenceRequest> query)
    {
        return query.Include(r => r.InferenceSteps);
    }

    public async Task<IEnumerable<InferenceRequest>> GetRecentAsync(int count = 100, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .OrderByDescending(r => r.RequestTimestamp)
            .Take(count)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<InferenceRequest>> GetByTaskTypeAsync(string taskType, int count = 100, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskType);

        return await DbSet
            .AsNoTracking()
            .Where(r => r.TaskType == taskType)
            .OrderByDescending(r => r.RequestTimestamp)
            .Take(count)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<double> GetAverageDurationAsync(string? taskType = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().Where(r => r.TotalDurationMs.HasValue);
        if (!string.IsNullOrWhiteSpace(taskType))
        {
            query = query.Where(r => r.TaskType == taskType);
        }

        return await query.AnyAsync(cancellationToken).ConfigureAwait(false)
            ? await query.AverageAsync(r => r.TotalDurationMs!.Value, cancellationToken).ConfigureAwait(false)
            : 0d;
    }
}
