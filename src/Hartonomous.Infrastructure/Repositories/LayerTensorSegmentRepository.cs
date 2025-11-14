using System.Linq.Expressions;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Hartonomous.Data.Entities;

namespace Hartonomous.Infrastructure.Repositories;

/// <summary>
/// Repository for persisted tensor segments supporting FILESTREAM-backed storage.
/// </summary>
public class LayerTensorSegmentRepository : EfRepository<LayerTensorSegment, long>, ILayerTensorSegmentRepository
{
    public LayerTensorSegmentRepository(HartonomousDbContext context, ILogger<LayerTensorSegmentRepository> logger)
        : base(context, logger)
    {
    }

    protected override Expression<Func<LayerTensorSegment, long>> GetIdExpression()
        => segment => segment.LayerTensorSegmentId;

    protected override IQueryable<LayerTensorSegment> IncludeRelatedEntities(IQueryable<LayerTensorSegment> query)
        => query.Include(s => s.Layer);

    public async Task<IReadOnlyList<LayerTensorSegment>> GetByLayerAsync(long layerId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(s => s.LayerId == layerId)
            .OrderBy(s => s.SegmentOrdinal)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task BulkInsertAsync(IEnumerable<LayerTensorSegment> segments, CancellationToken cancellationToken = default)
    {
        await DbSet.AddRangeAsync(segments, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteByLayerAsync(long layerId, CancellationToken cancellationToken = default)
    {
        var existing = await DbSet
            .Where(s => s.LayerId == layerId)
            .ToListAsync(cancellationToken);

        if (existing.Count == 0)
        {
            return;
        }

        DbSet.RemoveRange(existing);
        await Context.SaveChangesAsync(cancellationToken);
    }
}
