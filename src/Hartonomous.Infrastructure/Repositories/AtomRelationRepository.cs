using System.Linq;
using System.Linq.Expressions;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAtomRelationRepository"/>.
/// Inherits base CRUD from EfRepository, adds relationship-specific queries.
/// </summary>
public class AtomRelationRepository : EfRepository<AtomRelation, long>, IAtomRelationRepository
{
    public AtomRelationRepository(HartonomousDbContext context, ILogger<AtomRelationRepository> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// AtomRelations are identified by AtomRelationId property.
    /// </summary>
    protected override Expression<Func<AtomRelation, long>> GetIdExpression() => r => r.AtomRelationId;

    // Domain-specific queries

    public async Task<IReadOnlyList<AtomRelation>> GetRelationsForAtomAsync(long atomId, int take = 256, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(r => r.SourceAtomId == atomId || r.TargetAtomId == atomId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(take)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
