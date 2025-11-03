using System.Linq;
using System.Linq.Expressions;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hartonomous.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAtomRelationRepository"/>.
/// Inherits base CRUD from EfRepository, adds relationship-specific queries.
/// </summary>
public class AtomRelationRepository : EfRepository<AtomRelation, long>, IAtomRelationRepository
{
    private readonly IAtomGraphWriter _graphWriter;
    private readonly IOptionsMonitor<AtomGraphOptions> _graphOptions;

    public AtomRelationRepository(
        HartonomousDbContext context,
        ILogger<AtomRelationRepository> logger,
        IAtomGraphWriter graphWriter,
        IOptionsMonitor<AtomGraphOptions> graphOptions)
        : base(context, logger)
    {
        _graphWriter = graphWriter ?? throw new ArgumentNullException(nameof(graphWriter));
        _graphOptions = graphOptions ?? throw new ArgumentNullException(nameof(graphOptions));
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

    public override async Task<AtomRelation> AddAsync(AtomRelation entity, CancellationToken cancellationToken = default)
    {
        var relation = await base.AddAsync(entity, cancellationToken).ConfigureAwait(false);

        if (_graphOptions.CurrentValue.EnableSqlGraphWrites)
        {
            await _graphWriter.UpsertRelationAsync(relation, cancellationToken).ConfigureAwait(false);
        }

        return relation;
    }
}
