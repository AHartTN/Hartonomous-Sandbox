using Hartonomous.Core.Entities;

namespace Hartonomous.Infrastructure.Repositories;

/// <summary>
/// Repository interface for AtomicPixel entity operations
/// </summary>
public interface IAtomicPixelRepository
{
    Task<AtomicPixel?> GetByHashAsync(byte[] pixelHash, CancellationToken cancellationToken = default);
    Task<AtomicPixel> AddAsync(AtomicPixel pixel, CancellationToken cancellationToken = default);
    Task UpdateReferenceCountAsync(byte[] pixelHash, CancellationToken cancellationToken = default);
    Task<long> GetReferenceCountAsync(byte[] pixelHash, CancellationToken cancellationToken = default);
}