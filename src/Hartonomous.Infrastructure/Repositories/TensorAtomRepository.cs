using System.Linq;
using System.Linq.Expressions;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ITensorAtomRepository"/>.
/// Inherits base CRUD from EfRepository, adds specialized tensor operations.
/// </summary>
public class TensorAtomRepository : EfRepository<TensorAtom, long>, ITensorAtomRepository
{
    public TensorAtomRepository(HartonomousDbContext context, ILogger<TensorAtomRepository> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// TensorAtoms are identified by TensorAtomId property.
    /// </summary>
    protected override Expression<Func<TensorAtom, long>> GetIdExpression() => t => t.TensorAtomId;

    /// <summary>
    /// Include coefficients for complete tensor atom queries.
    /// </summary>
    protected override IQueryable<TensorAtom> IncludeRelatedEntities(IQueryable<TensorAtom> query)
    {
        return query.Include(t => t.Coefficients);
    }

    // Domain-specific queries

    public async Task<IReadOnlyList<TensorAtom>> GetByModelLayerAsync(int modelId, long? layerId, string? atomType, int take = 256, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsQueryable();

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

    /// <summary>
    /// Replaces all coefficients for a tensor atom in a transaction.
    /// Uses ExecuteDelete for efficient bulk deletion.
    /// </summary>
    public async Task AddCoefficientsAsync(long tensorAtomId, IEnumerable<TensorAtomCoefficient> coefficients, CancellationToken cancellationToken = default)
    {
        await using var transaction = await Context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Efficient bulk delete using ExecuteDeleteAsync
            await Context.TensorAtomCoefficients
                .Where(c => c.TensorAtomId == tensorAtomId)
                .ExecuteDeleteAsync(cancellationToken);

            await Context.TensorAtomCoefficients.AddRangeAsync(coefficients, cancellationToken);
            await Context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
