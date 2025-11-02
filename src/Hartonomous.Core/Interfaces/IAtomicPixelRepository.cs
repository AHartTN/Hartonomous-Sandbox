using Hartonomous.Core.Entities;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Repository abstraction for atomic pixel operations within the atom substrate.
/// </summary>
public interface IAtomicPixelRepository
{
    Task<AtomicPixel?> GetByHashAsync(byte[] pixelHash, CancellationToken cancellationToken = default);
    Task<AtomicPixel> AddAsync(AtomicPixel pixel, CancellationToken cancellationToken = default);
    Task UpdateReferenceCountAsync(byte[] pixelHash, CancellationToken cancellationToken = default);
    Task<long> GetReferenceCountAsync(byte[] pixelHash, CancellationToken cancellationToken = default);
}
