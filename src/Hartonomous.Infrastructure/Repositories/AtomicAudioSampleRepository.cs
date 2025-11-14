using System;
using System.Linq.Expressions;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Microsoft.Extensions.Logging;
using Hartonomous.Data.Entities;

namespace Hartonomous.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAtomicAudioSampleRepository"/>.
/// Inherits base CRUD from EfRepository, adds hash-based deduplication and reference counting.
/// </summary>
public class AtomicAudioSampleRepository : AtomicReferenceRepository<AtomicAudioSample, byte[], byte[]>, IAtomicAudioSampleRepository
{
    public AtomicAudioSampleRepository(HartonomousDbContext context, ILogger<AtomicAudioSampleRepository> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// AtomicAudioSamples are identified by SampleHash property.
    /// </summary>
    protected override Expression<Func<AtomicAudioSample, byte[]>> GetIdExpression() => s => s.SampleHash;

    protected override Expression<Func<AtomicAudioSample, byte[]>> GetHashExpression() => s => s.SampleHash;

    protected override Expression<Func<AtomicAudioSample, bool>> BuildKeyPredicate(byte[] key) => sample => sample.SampleHash == key;

    // Domain-specific queries stay available via base implementations.
}
