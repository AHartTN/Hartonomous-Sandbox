using Hartonomous.Core.Entities;

namespace Hartonomous.Infrastructure.Repositories;

/// <summary>
/// Repository interface for AtomicTextToken entity operations
/// </summary>
public interface IAtomicTextTokenRepository
{
    Task<AtomicTextToken?> GetByHashAsync(byte[] tokenHash, CancellationToken cancellationToken = default);
    Task<AtomicTextToken?> GetByIdAsync(long tokenId, CancellationToken cancellationToken = default);
    Task<AtomicTextToken> AddAsync(AtomicTextToken token, CancellationToken cancellationToken = default);
    Task UpdateReferenceCountAsync(byte[] tokenHash, CancellationToken cancellationToken = default);
    Task<long> GetReferenceCountAsync(byte[] tokenHash, CancellationToken cancellationToken = default);
}