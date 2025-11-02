using System.Linq;
using System.Linq.Expressions;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IDeduplicationPolicyRepository"/>.
/// Inherits base CRUD from EfRepository, adds policy-specific queries.
/// </summary>
public class DeduplicationPolicyRepository : EfRepository<DeduplicationPolicy, int>, IDeduplicationPolicyRepository
{
    public DeduplicationPolicyRepository(HartonomousDbContext context, ILogger<DeduplicationPolicyRepository> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// DeduplicationPolicies are identified by DeduplicationPolicyId property.
    /// </summary>
    protected override Expression<Func<DeduplicationPolicy, int>> GetIdExpression() => p => p.DeduplicationPolicyId;

    // Domain-specific queries

    public async Task<DeduplicationPolicy?> GetActivePolicyAsync(string policyName, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(p => p.PolicyName == policyName && p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }

    public new async Task<IReadOnlyList<DeduplicationPolicy>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .OrderByDescending(p => p.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
