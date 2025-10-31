using System.Linq;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Microsoft.EntityFrameworkCore;

namespace Hartonomous.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IDeduplicationPolicyRepository"/>.
/// </summary>
public class DeduplicationPolicyRepository : IDeduplicationPolicyRepository
{
    private readonly HartonomousDbContext _context;

    public DeduplicationPolicyRepository(HartonomousDbContext context)
    {
        _context = context;
    }

    public async Task<DeduplicationPolicy?> GetActivePolicyAsync(string policyName, CancellationToken cancellationToken = default)
    {
        return await _context.DeduplicationPolicies
            .Where(p => p.PolicyName == policyName && p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DeduplicationPolicy>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DeduplicationPolicies
            .OrderByDescending(p => p.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<DeduplicationPolicy> AddAsync(DeduplicationPolicy policy, CancellationToken cancellationToken = default)
    {
        _context.DeduplicationPolicies.Add(policy);
        await _context.SaveChangesAsync(cancellationToken);
        return policy;
    }

    public async Task UpdateAsync(DeduplicationPolicy policy, CancellationToken cancellationToken = default)
    {
        _context.DeduplicationPolicies.Update(policy);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
