using Hartonomous.Core.Entities;
using Hartonomous.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Infrastructure.Repositories;

public class AtomicAudioSampleRepository : IAtomicAudioSampleRepository
{
    private readonly HartonomousDbContext _context;
    private readonly ILogger<AtomicAudioSampleRepository> _logger;

    public AtomicAudioSampleRepository(HartonomousDbContext context, ILogger<AtomicAudioSampleRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AtomicAudioSample?> GetByHashAsync(byte[] sampleHash, CancellationToken cancellationToken = default)
    {
        return await _context.AtomicAudioSamples.FindAsync(new object[] { sampleHash }, cancellationToken);
    }

    public async Task<AtomicAudioSample> AddAsync(AtomicAudioSample sample, CancellationToken cancellationToken = default)
    {
        _context.AtomicAudioSamples.Add(sample);
        await _context.SaveChangesAsync(cancellationToken);
        return sample;
    }

    public async Task UpdateReferenceCountAsync(byte[] sampleHash, CancellationToken cancellationToken = default)
    {
        var sample = await GetByHashAsync(sampleHash, cancellationToken);
        if (sample != null)
        {
            sample.ReferenceCount++;
            sample.LastReferenced = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<long> GetReferenceCountAsync(byte[] sampleHash, CancellationToken cancellationToken = default)
    {
        var sample = await GetByHashAsync(sampleHash, cancellationToken);
        return sample?.ReferenceCount ?? 0;
    }
}