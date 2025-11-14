using Hartonomous.Data.Entities;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Repository interface for operations over atomic audio samples backed by the deduplicated atom substrate.
/// </summary>
public interface IAtomicAudioSampleRepository
{
    Task<AtomicAudioSample?> GetByHashAsync(byte[] sampleHash, CancellationToken cancellationToken = default);
    Task<AtomicAudioSample> AddAsync(AtomicAudioSample sample, CancellationToken cancellationToken = default);
    Task UpdateReferenceCountAsync(byte[] sampleHash, long delta = 1, CancellationToken cancellationToken = default);
    Task<long> GetReferenceCountAsync(byte[] sampleHash, CancellationToken cancellationToken = default);
}
