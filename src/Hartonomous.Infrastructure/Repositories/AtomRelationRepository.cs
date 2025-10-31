using System.Linq;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Microsoft.EntityFrameworkCore;

namespace Hartonomous.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAtomRelationRepository"/>.
/// </summary>
public class AtomRelationRepository : IAtomRelationRepository
{
    private readonly HartonomousDbContext _context;

    public AtomRelationRepository(HartonomousDbContext context)
    {
        _context = context;
    }

    public async Task<AtomRelation?> GetByIdAsync(long relationId, CancellationToken cancellationToken = default)
    {
        return await _context.AtomRelations
            .FirstOrDefaultAsync(r => r.AtomRelationId == relationId, cancellationToken);
    }

    public async Task<IReadOnlyList<AtomRelation>> GetRelationsForAtomAsync(long atomId, int take = 256, CancellationToken cancellationToken = default)
    {
        return await _context.AtomRelations
            .Where(r => r.SourceAtomId == atomId || r.TargetAtomId == atomId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(take)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<AtomRelation> AddAsync(AtomRelation relation, CancellationToken cancellationToken = default)
    {
        _context.AtomRelations.Add(relation);
        await _context.SaveChangesAsync(cancellationToken);
        return relation;
    }
}
