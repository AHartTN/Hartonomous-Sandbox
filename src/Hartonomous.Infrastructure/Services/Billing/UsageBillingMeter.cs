using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Billing;
using Hartonomous.Core.Models;
using Hartonomous.Core.Security;

namespace Hartonomous.Infrastructure.Services.Billing;

/// <summary>
/// Billing meter implementation that calculates usage costs based on operation rates, multipliers, and consumption units.
/// Applies multi-dimensional pricing including complexity, content type, grounding, guarantees, and provenance factors.
/// </summary>
public sealed class UsageBillingMeter : IBillingMeter
{
    private const decimal DefaultUnitPrice = 0.00008m;

    private readonly IBillingConfigurationProvider _configurationProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="UsageBillingMeter"/> class.
    /// </summary>
    /// <param name="configurationProvider">Provider for tenant-specific billing configuration including rate plans and multipliers.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configurationProvider"/> is null.</exception>
    public UsageBillingMeter(IBillingConfigurationProvider configurationProvider)
    {
        _configurationProvider = configurationProvider ?? throw new ArgumentNullException(nameof(configurationProvider));
    }

    /// <summary>
    /// Measures usage for a given event and calculates billing estimates based on tenant configuration.
    /// </summary>
    /// <param name="evt">The base event containing usage information in extensions.</param>
    /// <param name="messageType">Type of message triggering the billing calculation.</param>
    /// <param name="handlerName">Name of the handler processing the event.</param>
    /// <param name="policyContext">Access policy context containing tenant ID, principal ID, and operation details.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A billing usage record with calculated costs and metadata, or null if insufficient context is available.</returns>
    public async Task<BillingUsageRecord?> MeasureAsync(
        BaseEvent evt,
        string messageType,
        string handlerName,
        AccessPolicyContext policyContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evt);
        ArgumentNullException.ThrowIfNull(policyContext);

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
        var unitPrice = configuration.UnitPricePerDcu > 0m ? configuration.UnitPricePerDcu : DefaultUnitPrice;
        var totalDcuEstimate = Math.Round(units * baseRate * multiplier, 6, MidpointRounding.AwayFromZero);
        var totalCostEstimate = Math.Round(totalDcuEstimate * unitPrice, 6, MidpointRounding.AwayFromZero);

        configuration.OperationUnits.TryGetValue(operation, out var unitOfMeasure);
        configuration.OperationCategories.TryGetValue(operation, out var operationCategory);

        var metadata = new Dictionary<string, object?>
        {
            ["operation"] = operation,
            ["handler"] = handlerName,
            ["message_type"] = messageType,
            ["units"] = units,
            ["base_rate_dcu"] = baseRate,
            ["multiplier"] = multiplier,
            ["unit_price"] = unitPrice,
            ["total_dcu_estimate"] = totalDcuEstimate,
            ["total_cost_estimate"] = totalCostEstimate,
            ["rate_plan_id"] = configuration.RatePlanId?.ToString(),
            ["rate_plan_code"] = configuration.PlanCode,
            ["rate_plan_name"] = configuration.PlanName,
            ["allows_private_data"] = configuration.AllowsPrivateData,
            ["can_query_public_corpus"] = configuration.CanQueryPublicCorpus,
            ["included_public_storage_gb"] = configuration.IncludedPublicStorageGb,
            ["included_private_storage_gb"] = configuration.IncludedPrivateStorageGb,
            ["included_seat_count"] = configuration.IncludedSeatCount
        };

        if (!string.IsNullOrWhiteSpace(unitOfMeasure))
        {
            metadata["unit_of_measure"] = unitOfMeasure;
        }

        if (!string.IsNullOrWhiteSpace(operationCategory))
        {
            metadata["operation_category"] = operationCategory;
        }

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
            UnitPrice = unitPrice,
            Metadata = metadata
        };
    }

    /// <summary>
    /// Resolves the base rate (DCU per unit) for a given operation from configuration.
    /// Falls back to default rate if operation-specific rate is not found.
    /// </summary>
    private static decimal ResolveBaseRate(BillingConfiguration configuration, string operation)
        => configuration.OperationRates.TryGetValue(operation, out var rate)
            ? rate
            : configuration.DefaultRate;

    /// <summary>
    /// Computes the cumulative billing multiplier from multiple dimensions (generation type, complexity, content type, grounding, guarantee, provenance).
    /// Each dimension contributes a factor that's multiplied together for the final multiplier.
    /// </summary>
    private static decimal ResolveMultiplier(BillingConfiguration configuration, BaseEvent evt)
    {
        var multiplier = 1m;

        multiplier *= ResolveDimension(configuration.GenerationTypeMultipliers, evt, "generation", "generation_type", "modality");
        multiplier *= ResolveComplexity(configuration, evt);
        multiplier *= ResolveDimension(configuration.ContentTypeMultipliers, evt, "content_type", "content", "dataset_type");
        multiplier *= ResolveDimension(configuration.GroundingMultipliers, evt, "grounding", "grounding_mode", "context_mode", "contextuality");
        multiplier *= ResolveDimension(configuration.GuaranteeMultipliers, evt, "guarantee", "guarantee_mode", "model_lock", "version_policy");
        multiplier *= ResolveDimension(configuration.ProvenanceMultipliers, evt, "provenance", "audit_mode", "audit");

        return multiplier;
    }

    /// <summary>
    /// Resolves a billing multiplier from event extensions based on provided keys.
    /// Returns 1.0 if no matching key is found or multiplier configuration is empty.
    /// </summary>
    private static decimal ResolveDimension(
        IReadOnlyDictionary<string, decimal> multipliers,
        BaseEvent evt,
        params string[] keys)
    {
        if (multipliers.Count == 0)
        {
            return 1m;
        }

        if (TryResolveFromExtensions(evt.Extensions, keys, out var resolved) && multipliers.TryGetValue(resolved, out var multiplier))
        {
            return multiplier;
        }

        return 1m;
    }

    /// <summary>
    /// Resolves complexity multiplier by checking both direct complexity values and nested reasoning structures.
    /// Numeric complexity scores are categorized into distilled/small/medium/large tiers.
    /// </summary>
    private static decimal ResolveComplexity(BillingConfiguration configuration, BaseEvent evt)
    {
        var complexityMultiplier = ResolveDimension(configuration.ComplexityMultipliers, evt, "complexity", "complexity_level");
        if (complexityMultiplier != 1m)
        {
            return complexityMultiplier;
        }

        if (TryResolveComplexityCategory(evt, out var category) && configuration.ComplexityMultipliers.TryGetValue(category, out var resolved))
        {
            return resolved;
        }

        return 1m;
    }

    /// <summary>
    /// Attempts to extract complexity category from event extensions or reasoning metadata.
    /// Supports both numeric scores (which are categorized) and string complexity levels.
    /// </summary>
    private static bool TryResolveComplexityCategory(BaseEvent evt, out string category)
    {
        if (evt.Extensions.TryGetValue("complexity", out var complexityObj) && TryResolveNumber(complexityObj, out var numericComplexity))
        {
            category = CategorizeComplexity(numericComplexity);
            return true;
        }

        if (evt.Extensions.TryGetValue("reasoning", out var reasoningObj))
        {
            if (reasoningObj is Dictionary<string, object> reasoningDict)
            {
                if (reasoningDict.TryGetValue("complexity", out var nestedComplexity) && TryResolveNumber(nestedComplexity, out var nestedNumeric))
                {
                    category = CategorizeComplexity(nestedNumeric);
                    return true;
                }

                if (reasoningDict.TryGetValue("complexity_level", out var nestedLevel) && TryResolveString(nestedLevel, out var levelString))
                {
                    category = levelString;
                    return true;
                }
            }
            else if (reasoningObj is JsonElement reasoningJson && reasoningJson.ValueKind == JsonValueKind.Object)
            {
                if (TryResolveJsonNumber(reasoningJson, "complexity", out var jsonComplexity))
                {
                    category = CategorizeComplexity(jsonComplexity);
                    return true;
                }

                if (TryResolveJsonString(reasoningJson, "complexity_level", out var jsonLevel))
                {
                    category = jsonLevel;
                    return true;
                }
            }
        }

        category = string.Empty;
        return false;
    }

    /// <summary>
    /// Categorizes a numeric complexity score into billing tiers: distilled (≤1), small (≤3), medium (≤6), or large (&gt;6).
    /// </summary>
    private static string CategorizeComplexity(decimal score)
    {
        if (score <= 1m)
        {
            return "distilled";
        }

        if (score <= 3m)
        {
            return "small";
        }

        if (score <= 6m)
        {
            return "medium";
        }

        return "large";
    }

    /// <summary>
    /// Extracts usage units from event extensions. Checks usage_units, usage.units, token_count (converted to K tokens), and duration_seconds.
    /// Defaults to 1 if no usage information is found.
    /// </summary>
    private static decimal ResolveUnits(BaseEvent evt)
    {
        if (evt.Extensions.TryGetValue("usage_units", out var unitsObj))
        {
            if (TryResolveNumber(unitsObj, out var units) && units > 0m)
            {
                return units;
            }
        }

        if (evt.Extensions.TryGetValue("usage", out var usageObj))
        {
            if (TryResolveNestedNumber(usageObj, "units", out var nestedUnits) && nestedUnits > 0m)
            {
                return nestedUnits;
            }
        }

        if (evt.Extensions.TryGetValue("token_count", out var tokensObj))
        {
            if (TryResolveNumber(tokensObj, out var tokens) && tokens > 0m)
            {
                return tokens / 1000m;
            }
        }

        if (evt.Extensions.TryGetValue("duration_seconds", out var durationObj))
        {
            if (TryResolveNumber(durationObj, out var seconds) && seconds > 0m)
            {
                return seconds;
            }
        }

        return 1m;
    }

    /// <summary>
    /// Attempts to extract a nested numeric value from a dictionary or JsonElement.
    /// </summary>
    private static bool TryResolveNestedNumber(object? source, string key, out decimal value)
    {
        switch (source)
        {
            case Dictionary<string, object> dict when dict.TryGetValue(key, out var nested) && TryResolveNumber(nested, out value):
                return true;
            case JsonElement json when json.ValueKind == JsonValueKind.Object && TryResolveJsonNumber(json, key, out value):
                return true;
        }

        value = 0m;
        return false;
    }

    /// <summary>
    /// Attempts to resolve a string value from various object types (string, dictionary with nested properties, JsonElement).
    /// Checks common property names like "value", "name", "code", "id", and "type" for nested resolution.
    /// </summary>
    private static bool TryResolveString(object? value, out string result)
    {
        switch (value)
        {
            case string str when !string.IsNullOrWhiteSpace(str):
                result = str;
                return true;
            case Dictionary<string, object> dict:
                foreach (var key in new[] { "value", "name", "code", "id", "type" })
                {
                    if (dict.TryGetValue(key, out var nested) && TryResolveString(nested, out var nestedValue))
                    {
                        result = nestedValue;
                        return true;
                    }
                }
                break;
            case JsonElement json:
                if (json.ValueKind == JsonValueKind.String)
                {
                    var jsonValue = json.GetString();
                    if (!string.IsNullOrWhiteSpace(jsonValue))
                    {
                        result = jsonValue;
                        return true;
                    }
                }
                else if (json.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in json.EnumerateObject())
                    {
                        if (property.NameEquals("value") || property.NameEquals("name") || property.NameEquals("code") || property.NameEquals("id") || property.NameEquals("type"))
                        {
                            if (TryResolveString(property.Value, out var nestedString))
                            {
                                result = nestedString;
                                return true;
                            }
                        }
                    }
                }
                break;
        }

        result = string.Empty;
        return false;
    }

    /// <summary>
    /// Attempts to resolve a value from event extensions by searching for any of the provided keys.
    /// Supports recursive search up to depth limit of 4 to prevent infinite recursion.
    /// </summary>
    private static bool TryResolveFromExtensions(
        IDictionary<string, object> extensions,
        IReadOnlyList<string> keys,
        out string value,
        int depth = 0)
    {
        if (depth > 4)
        {
            value = string.Empty;
            return false;
        }

        foreach (var key in keys)
        {
            if (extensions.TryGetValue(key, out var direct) && TryResolveString(direct, out value))
            {
                return true;
            }
        }

        foreach (var entry in extensions.Values)
        {
            switch (entry)
            {
                case Dictionary<string, object> nestedDict when TryResolveFromExtensions(nestedDict, keys, out value, depth + 1):
                    return true;
                case JsonElement json when json.ValueKind == JsonValueKind.Object && TryResolveFromJson(json, keys, out value, depth + 1):
                    return true;
            }
        }

        value = string.Empty;
        return false;
    }

    /// <summary>
    /// Recursively searches JsonElement for any of the provided keys.
    /// Limited to depth of 4 to prevent stack overflow.
    /// </summary>
    private static bool TryResolveFromJson(
        JsonElement json,
        IReadOnlyList<string> keys,
        out string value,
        int depth = 0)
    {
        if (depth > 4)
        {
            value = string.Empty;
            return false;
        }

        foreach (var key in keys)
        {
            if (json.TryGetProperty(key, out var property) && TryResolveString(property, out value))
            {
                return true;
            }
        }

        foreach (var property in json.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Object && TryResolveFromJson(property.Value, keys, out value, depth + 1))
            {
                return true;
            }
        }

        value = string.Empty;
        return false;
    }

    /// <summary>
    /// Attempts to extract a string property from a JsonElement.
    /// </summary>
    private static bool TryResolveJsonString(JsonElement json, string propertyName, out string value)
    {
        if (json.TryGetProperty(propertyName, out var property) && TryResolveString(property, out var resolved))
        {
            value = resolved;
            return true;
        }

        value = string.Empty;
        return false;
    }

    /// <summary>
    /// Attempts to extract a numeric property from a JsonElement and convert to decimal.
    /// </summary>
    private static bool TryResolveJsonNumber(JsonElement json, string propertyName, out decimal value)
    {
        if (json.TryGetProperty(propertyName, out var property))
        {
            return TryResolveNumber(property, out value);
        }

        value = 0m;
        return false;
    }

    /// <summary>
    /// Attempts to convert various numeric types and string representations to decimal.
    /// Supports decimal, double, float, long, int, numeric strings, and JsonElement numbers.
    /// </summary>
    private static bool TryResolveNumber(object? value, out decimal result)
    {
        switch (value)
        {
            case decimal dec:
                result = dec;
                return true;
            case double dbl:
                result = (decimal)dbl;
                return true;
            case float fl:
                result = (decimal)fl;
                return true;
            case long lng:
                result = lng;
                return true;
            case int integer:
                result = integer;
                return true;
            case string str when decimal.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed):
                result = parsed;
                return true;
            case JsonElement json when json.ValueKind == JsonValueKind.Number:
                if (json.TryGetDecimal(out var jsonDecimal))
                {
                    result = jsonDecimal;
                    return true;
                }

                if (json.TryGetDouble(out var jsonDouble))
                {
                    result = (decimal)jsonDouble;
                    return true;
                }
                break;
        }

        result = 0m;
        return false;
    }
}
