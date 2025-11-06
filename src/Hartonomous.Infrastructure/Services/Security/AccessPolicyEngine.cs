using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Security;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Services.Security;

/// <summary>
/// Policy engine that evaluates access requests against a collection of policy rules.
/// Processes rules in sequence and returns the first denial or allows access if all rules pass.
/// </summary>
public sealed class AccessPolicyEngine : IAccessPolicyEngine
{
    private readonly IReadOnlyList<IAccessPolicyRule> _rules;
    private readonly ILogger<AccessPolicyEngine> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccessPolicyEngine"/> class.
    /// </summary>
    /// <param name="rules">Collection of policy rules to evaluate. Evaluated in order until first denial.</param>
    /// <param name="logger">Logger for recording policy evaluation outcomes.</param>
    public AccessPolicyEngine(IEnumerable<IAccessPolicyRule> rules, ILogger<AccessPolicyEngine> logger)
    {
        _rules = rules is IReadOnlyList<IAccessPolicyRule> list ? list : rules.ToList();
        _logger = logger;
    }

    /// <summary>
    /// Evaluates the access request against all configured policy rules.
    /// Returns the first denial result or allows access if all rules pass or return null.
    /// </summary>
    /// <param name="context">Access policy context containing tenant ID, principal ID, operation, and other request details.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>An <see cref="AccessPolicyResult"/> indicating whether access is allowed, with reason and policy information if denied.</returns>
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
