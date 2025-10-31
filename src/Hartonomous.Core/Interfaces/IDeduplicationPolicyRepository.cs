using Hartonomous.Core.Entities;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Repository abstraction for deduplication policy management.
/// </summary>
public interface IDeduplicationPolicyRepository
{
    Task<DeduplicationPolicy?> GetActivePolicyAsync(string policyName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeduplicationPolicy>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<DeduplicationPolicy> AddAsync(DeduplicationPolicy policy, CancellationToken cancellationToken = default);
    Task UpdateAsync(DeduplicationPolicy policy, CancellationToken cancellationToken = default);
}
