using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Billing;
using Hartonomous.Core.Models;
using Hartonomous.Core.Security;

namespace Hartonomous.Infrastructure.Services.Billing;

public sealed class UsageBillingMeter : IBillingMeter
{
    private readonly IBillingConfigurationProvider _configurationProvider;

    public UsageBillingMeter(IBillingConfigurationProvider configurationProvider)
    {
        _configurationProvider = configurationProvider ?? throw new ArgumentNullException(nameof(configurationProvider));
    }

    public async Task<BillingUsageRecord?> MeasureAsync(
        BaseEvent evt,
        string messageType,
        string handlerName,
        AccessPolicyContext policyContext,
        CancellationToken cancellationToken = default)
    {
        var operation = policyContext.Operation;
        var tenantId = policyContext.TenantId;

        if (string.IsNullOrWhiteSpace(operation) || string.IsNullOrWhiteSpace(tenantId))
        {
            return null;
        }

        var configuration = await _configurationProvider
            .GetConfigurationAsync(tenantId, cancellationToken)
            .ConfigureAwait(false);

        var baseRate = ResolveBaseRate(configuration, operation);
        var multiplier = ResolveMultiplier(configuration, evt);
        var units = ResolveUnits(evt);

        var metadata = new Dictionary<string, object?>
        {
            ["operation"] = operation,
            ["handler"] = handlerName,
            ["message_type"] = messageType,
            ["units"] = units,
            ["base_rate"] = baseRate,
            ["multiplier"] = multiplier,
            ["rate_plan_id"] = configuration.RatePlanId?.ToString()
        };

        return new BillingUsageRecord
        {
            TenantId = tenantId,
            PrincipalId = policyContext.PrincipalId,
            Operation = operation,
            MessageType = messageType,
            Handler = handlerName,
            Units = units,
            BaseRate = baseRate,
            Multiplier = multiplier,
            Metadata = metadata
        };
    }

    private static decimal ResolveBaseRate(BillingConfiguration configuration, string operation)
        => configuration.OperationRates.TryGetValue(operation, out var rate)
            ? rate
            : configuration.DefaultRate;

    private static decimal ResolveMultiplier(BillingConfiguration configuration, BaseEvent evt)
    {
        var multiplier = 1m;

        var extensions = evt.Extensions;
        if (extensions.TryGetValue("generation", out var generationObj) && TryResolveString(generationObj, out var generationType))
        {
            if (configuration.GenerationTypeMultipliers.TryGetValue(generationType, out var generationMultiplier))
            {
                multiplier *= generationMultiplier;
            }
        }

        if (extensions.TryGetValue("complexity", out var complexityObj) && TryResolveString(complexityObj, out var complexityLevel))
        {
            if (configuration.ComplexityMultipliers.TryGetValue(complexityLevel, out var complexityMultiplier))
            {
                multiplier *= complexityMultiplier;
            }
        }

        if (extensions.TryGetValue("content_type", out var contentObj) && TryResolveString(contentObj, out var contentType))
        {
            if (configuration.ContentTypeMultipliers.TryGetValue(contentType, out var contentMultiplier))
            {
                multiplier *= contentMultiplier;
            }
        }

        return multiplier;
    }

    private static decimal ResolveUnits(BaseEvent evt)
    {
        if (evt.Extensions.TryGetValue("usage_units", out var unitsObj) && unitsObj is double doubleUnits)
        {
            return (decimal)doubleUnits;
        }

        if (evt.Extensions.TryGetValue("token_count", out var tokensObj) && tokensObj is double tokenCount)
        {
            return (decimal)tokenCount / 1000m;
        }

        return 1m;
    }

    private static bool TryResolveString(object? value, out string result)
    {
        switch (value)
        {
            case string str when !string.IsNullOrWhiteSpace(str):
                result = str;
                return true;
            case Dictionary<string, object> dict when dict.TryGetValue("value", out var nested) && nested is string nestedStr && !string.IsNullOrWhiteSpace(nestedStr):
                result = nestedStr;
                return true;
            default:
                result = string.Empty;
                return false;
        }
    }
}
