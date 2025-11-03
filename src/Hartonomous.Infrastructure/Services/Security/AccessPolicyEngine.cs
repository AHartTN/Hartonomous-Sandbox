using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Security;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Services.Security;

public sealed class AccessPolicyEngine : IAccessPolicyEngine
{
    private readonly IReadOnlyList<IAccessPolicyRule> _rules;
    private readonly ILogger<AccessPolicyEngine> _logger;

    public AccessPolicyEngine(IEnumerable<IAccessPolicyRule> rules, ILogger<AccessPolicyEngine> logger)
    {
        _rules = rules is IReadOnlyList<IAccessPolicyRule> list ? list : rules.ToList();
        _logger = logger;
    }

    public async Task<AccessPolicyResult> EvaluateAsync(AccessPolicyContext context, CancellationToken cancellationToken = default)
    {
        foreach (var rule in _rules)
        {
            var result = await rule.EvaluateAsync(context, cancellationToken).ConfigureAwait(false);
            if (result is null)
            {
                continue;
            }

            if (!result.IsAllowed)
            {
                _logger.LogWarning("Access denied by policy {Policy} for tenant {Tenant} principal {Principal} operation {Operation}. Reason: {Reason}", result.Policy, context.TenantId, context.PrincipalId, context.Operation, result.Reason);
                return result;
            }
        }

        return AccessPolicyResult.Allow();
    }
}
