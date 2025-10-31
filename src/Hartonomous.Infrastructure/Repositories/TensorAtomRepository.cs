using System.Linq;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Microsoft.EntityFrameworkCore;

namespace Hartonomous.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ITensorAtomRepository"/>.
/// </summary>
public class TensorAtomRepository : ITensorAtomRepository
{
    private readonly HartonomousDbContext _context;

    public TensorAtomRepository(HartonomousDbContext context)
    {
        _context = context;
    }

    public async Task<TensorAtom?> GetByIdAsync(long tensorAtomId, CancellationToken cancellationToken = default)
    {
        return await _context.TensorAtoms
            .Include(t => t.Coefficients)
            .FirstOrDefaultAsync(t => t.TensorAtomId == tensorAtomId, cancellationToken);
    }

    public async Task<IReadOnlyList<TensorAtom>> GetByModelLayerAsync(int modelId, long? layerId, string? atomType, int take = 256, CancellationToken cancellationToken = default)
    {
        var query = _context.TensorAtoms.AsQueryable();

        query = query.Where(t => t.ModelId == modelId);

        if (layerId.HasValue)
        {
            query = query.Where(t => t.LayerId == layerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(atomType))
        {
            query = query.Where(t => t.AtomType == atomType);
        }

        return await query
            .OrderByDescending(t => t.ImportanceScore)
            .ThenByDescending(t => t.CreatedAt)
            .Take(take)
            .Include(t => t.Coefficients)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<TensorAtom> AddAsync(TensorAtom tensorAtom, CancellationToken cancellationToken = default)
    {
        _context.TensorAtoms.Add(tensorAtom);
        await _context.SaveChangesAsync(cancellationToken);
        return tensorAtom;
    }

    public async Task AddCoefficientsAsync(long tensorAtomId, IEnumerable<TensorAtomCoefficient> coefficients, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await _context.TensorAtomCoefficients
                .Where(c => c.TensorAtomId == tensorAtomId)
                .ExecuteDeleteAsync(cancellationToken);

            await _context.TensorAtomCoefficients.AddRangeAsync(coefficients, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
