using System;
using System.Linq.Expressions;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAtomicAudioSampleRepository"/>.
/// Inherits base CRUD from EfRepository, adds hash-based deduplication and reference counting.
/// </summary>
public class AtomicAudioSampleRepository : EfRepository<AtomicAudioSample, byte[]>, IAtomicAudioSampleRepository
{
    public AtomicAudioSampleRepository(HartonomousDbContext context, ILogger<AtomicAudioSampleRepository> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// AtomicAudioSamples are identified by SampleHash property.
    /// </summary>
    protected override Expression<Func<AtomicAudioSample, byte[]>> GetIdExpression() => s => s.SampleHash;

    // Domain-specific queries

    public async Task<AtomicAudioSample?> GetByHashAsync(byte[] sampleHash, CancellationToken cancellationToken = default)
    {
        return await GetByIdAsync(sampleHash, cancellationToken);
    }

    /// <summary>
    /// Efficiently increment reference count using ExecuteUpdateAsync.
    /// Avoids loading the entity into memory.
    /// </summary>
    public async Task UpdateReferenceCountAsync(byte[] sampleHash, CancellationToken cancellationToken = default)
    {
        await DbSet
            .Where(s => s.SampleHash == sampleHash)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.ReferenceCount, s => s.ReferenceCount + 1)
                .SetProperty(s => s.LastReferenced, DateTime.UtcNow),
                cancellationToken);
    }

    public async Task<long> GetReferenceCountAsync(byte[] sampleHash, CancellationToken cancellationToken = default)
    {
        var sample = await DbSet
            .Where(s => s.SampleHash == sampleHash)
            .Select(s => new { s.ReferenceCount })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
        
        return sample?.ReferenceCount ?? 0;
    }
}