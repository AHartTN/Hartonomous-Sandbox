using Hartonomous.Core.Entities;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Repository abstraction for deduplicated atomic text tokens.
/// </summary>
public interface IAtomicTextTokenRepository
{
    Task<AtomicTextToken?> GetByHashAsync(byte[] tokenHash, CancellationToken cancellationToken = default);
    Task<AtomicTextToken> AddAsync(AtomicTextToken token, CancellationToken cancellationToken = default);
    Task UpdateReferenceCountAsync(long tokenId, CancellationToken cancellationToken = default);
    Task<long> GetReferenceCountAsync(long tokenId, CancellationToken cancellationToken = default);
}
