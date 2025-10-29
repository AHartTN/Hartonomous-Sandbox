using Hartonomous.Core.Entities;

namespace Hartonomous.Infrastructure.Repositories;

/// <summary>
/// Repository interface for AtomicAudioSample entity operations
/// </summary>
public interface IAtomicAudioSampleRepository
{
    Task<AtomicAudioSample?> GetByHashAsync(byte[] sampleHash, CancellationToken cancellationToken = default);
    Task<AtomicAudioSample> AddAsync(AtomicAudioSample sample, CancellationToken cancellationToken = default);
    Task UpdateReferenceCountAsync(byte[] sampleHash, CancellationToken cancellationToken = default);
    Task<long> GetReferenceCountAsync(byte[] sampleHash, CancellationToken cancellationToken = default);
}