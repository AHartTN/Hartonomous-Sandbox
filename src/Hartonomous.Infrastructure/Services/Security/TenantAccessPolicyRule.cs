using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Security;
using Microsoft.Extensions.Options;

namespace Hartonomous.Infrastructure.Services.Security;

public sealed class TenantAccessPolicyRule : IAccessPolicyRule
{
    private readonly IOptionsMonitor<SecurityOptions> _optionsMonitor;

    public TenantAccessPolicyRule(IOptionsMonitor<SecurityOptions> options)
    {
        _optionsMonitor = options;
    }

    public Task<AccessPolicyResult?> EvaluateAsync(AccessPolicyContext context, CancellationToken cancellationToken = default)
    {
        var options = _optionsMonitor.CurrentValue;
        if (string.IsNullOrWhiteSpace(context.TenantId) && string.IsNullOrWhiteSpace(context.PrincipalId))
        {
            return Task.FromResult<AccessPolicyResult?>(null);
        }

        if (!string.IsNullOrWhiteSpace(context.TenantId) && options.BannedTenants.Contains(context.TenantId))
        {
            return Task.FromResult<AccessPolicyResult?>(AccessPolicyResult.Deny("tenant-ban", $"Tenant {context.TenantId} is banned."));
        }

        if (!string.IsNullOrWhiteSpace(context.PrincipalId) && options.BannedPrincipals.Contains(context.PrincipalId))
        {
            return Task.FromResult<AccessPolicyResult?>(AccessPolicyResult.Deny("principal-ban", $"Principal {context.PrincipalId} is banned."));
        }

        if (!string.IsNullOrWhiteSpace(context.TenantId) && options.TenantOperationDenyList.TryGetValue(context.TenantId, out var blockedOperations))
        {
            var operationKey = NormalizeOperation(context.Operation, context.MessageType);
            if (blockedOperations.Contains(operationKey))
            {
                return Task.FromResult<AccessPolicyResult?>(AccessPolicyResult.Deny("tenant-operation-ban", $"Operation {operationKey} is not permitted for tenant {context.TenantId}."));
            }
        }

        return Task.FromResult<AccessPolicyResult?>(null);
    }

    private static string NormalizeOperation(string operation, string messageType)
    {
        if (!string.IsNullOrWhiteSpace(operation))
        {
            return operation;
        }

        if (!string.IsNullOrWhiteSpace(messageType))
        {
            return messageType;
        }

        return "*";
    }
}
