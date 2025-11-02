using System;
using System.Linq.Expressions;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAtomicPixelRepository"/>.
/// Inherits base CRUD from EfRepository, adds hash-based deduplication and reference counting.
/// </summary>
public class AtomicPixelRepository : EfRepository<AtomicPixel, byte[]>, IAtomicPixelRepository
{
    public AtomicPixelRepository(HartonomousDbContext context, ILogger<AtomicPixelRepository> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// AtomicPixels are identified by PixelHash property.
    /// </summary>
    protected override Expression<Func<AtomicPixel, byte[]>> GetIdExpression() => p => p.PixelHash;

    // Domain-specific queries

    public async Task<AtomicPixel?> GetByHashAsync(byte[] pixelHash, CancellationToken cancellationToken = default)
    {
        return await GetByIdAsync(pixelHash, cancellationToken);
    }

    /// <summary>
    /// Efficiently increment reference count using ExecuteUpdateAsync.
    /// Avoids loading the entity into memory.
    /// </summary>
    public async Task UpdateReferenceCountAsync(byte[] pixelHash, CancellationToken cancellationToken = default)
    {
        await DbSet
            .Where(p => p.PixelHash == pixelHash)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.ReferenceCount, p => p.ReferenceCount + 1)
                .SetProperty(p => p.LastReferenced, DateTime.UtcNow),
                cancellationToken);
    }

    public async Task<long> GetReferenceCountAsync(byte[] pixelHash, CancellationToken cancellationToken = default)
    {
        var pixel = await DbSet
            .Where(p => p.PixelHash == pixelHash)
            .Select(p => new { p.ReferenceCount })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
        
        return pixel?.ReferenceCount ?? 0;
    }
}