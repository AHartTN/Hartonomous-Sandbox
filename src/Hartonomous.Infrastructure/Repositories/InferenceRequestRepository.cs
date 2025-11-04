using System.Linq.Expressions;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Repositories;

public class InferenceRequestRepository : EfRepository<InferenceRequest, long>, IInferenceRequestRepository
{
    public InferenceRequestRepository(HartonomousDbContext context, ILogger<InferenceRequestRepository> logger)
        : base(context, logger)
    {
    }

    protected override Expression<Func<InferenceRequest, long>> GetIdExpression() => x => x.InferenceId;

    protected override IQueryable<InferenceRequest> IncludeRelatedEntities(IQueryable<InferenceRequest> query)
    {
        return query.Include(x => x.Steps);
    }

    public new Task<InferenceRequest?> GetByIdAsync(long inferenceId, CancellationToken cancellationToken = default)
    {
        return IncludeRelatedEntities(DbSet.AsNoTracking())
            .FirstOrDefaultAsync(x => x.InferenceId == inferenceId, cancellationToken);
    }

    public async Task<IReadOnlyList<InferenceRequest>> GetByCorrelationIdAsync(
        string correlationId,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        return await IncludeRelatedEntities(DbSet.AsNoTracking())
            .Where(x => x.CorrelationId == correlationId)
            .OrderByDescending(x => x.RequestTimestamp)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InferenceRequest>> GetPendingAsync(int take = 100, CancellationToken cancellationToken = default)
    {
        return await IncludeRelatedEntities(DbSet.AsNoTracking())
            .Where(x => x.Status == "Pending" || x.Status == "InProgress")
            .OrderBy(x => x.RequestTimestamp)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public new async Task<InferenceRequest> AddAsync(InferenceRequest request, CancellationToken cancellationToken = default)
    {
        DbSet.Add(request);
        await Context.SaveChangesAsync(cancellationToken);
        return request;
    }

    public async Task UpdateStatusAsync(
        long inferenceId,
        string status,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        var request = await DbSet.FindAsync(new object[] { inferenceId }, cancellationToken);
        if (request != null)
        {
            request.Status = status;
            if (!string.IsNullOrEmpty(errorMessage))
            {
                request.OutputMetadata = errorMessage; // Store error in metadata
            }
            await Context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task UpdateResultAsync(
        long inferenceId,
        object result,
        double confidence,
        CancellationToken cancellationToken = default)
    {
        var request = await DbSet.FindAsync(new object[] { inferenceId }, cancellationToken);
        if (request != null)
        {
            request.OutputData = result.ToString();
            request.Confidence = confidence;
            request.Status = "Completed";
            await Context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<InferenceRequest>> GetHighConfidenceAsync(
        double minConfidence,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        return await IncludeRelatedEntities(DbSet.AsNoTracking())
            .Where(x => x.Confidence.HasValue && x.Confidence.Value >= minConfidence)
            .OrderByDescending(x => x.Confidence)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
