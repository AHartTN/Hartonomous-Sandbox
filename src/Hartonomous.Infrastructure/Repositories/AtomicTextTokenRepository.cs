using System;
using System.Linq.Expressions;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAtomicTextTokenRepository"/>.
/// Inherits base CRUD from EfRepository, adds hash-based deduplication logic.
/// </summary>
public class AtomicTextTokenRepository : EfRepository<AtomicTextToken, long>, IAtomicTextTokenRepository
{
    public AtomicTextTokenRepository(HartonomousDbContext context, ILogger<AtomicTextTokenRepository> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// AtomicTextTokens are identified by TokenId property.
    /// </summary>
    protected override Expression<Func<AtomicTextToken, long>> GetIdExpression() => t => t.TokenId;

    // Domain-specific queries

    public async Task<AtomicTextToken?> GetByHashAsync(byte[] tokenHash, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    /// <summary>
    /// Efficiently increment reference count using ExecuteUpdateAsync.
    /// Avoids loading the entity into memory.
    /// </summary>
    public async Task UpdateReferenceCountAsync(long tokenId, CancellationToken cancellationToken = default)
    {
        await DbSet
            .Where(t => t.TokenId == tokenId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.ReferenceCount, t => t.ReferenceCount + 1)
                .SetProperty(t => t.LastReferenced, DateTime.UtcNow),
                cancellationToken);
    }

    public async Task<long> GetReferenceCountAsync(long tokenId, CancellationToken cancellationToken = default)
    {
        var token = await DbSet
            .Where(t => t.TokenId == tokenId)
            .Select(t => new { t.ReferenceCount })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
        
        return token?.ReferenceCount ?? 0;
    }
}