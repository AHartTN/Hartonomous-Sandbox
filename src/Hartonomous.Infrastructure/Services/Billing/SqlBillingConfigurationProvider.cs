using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Billing;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hartonomous.Infrastructure.Services.Billing;

public sealed class SqlBillingConfigurationProvider : IBillingConfigurationProvider
{
    private const string ConfigurationQuery = @"
DECLARE @ResolvedRatePlanId UNIQUEIDENTIFIER;

SELECT TOP(1) @ResolvedRatePlanId = RatePlanId
FROM dbo.BillingRatePlans
WHERE IsActive = 1 AND TenantId = @TenantId
ORDER BY UpdatedUtc DESC;

IF @ResolvedRatePlanId IS NULL
BEGIN
    SELECT TOP(1) @ResolvedRatePlanId = RatePlanId
    FROM dbo.BillingRatePlans
    WHERE IsActive = 1 AND TenantId IS NULL
    ORDER BY UpdatedUtc DESC;
END

SELECT
    RatePlanId,
    PlanCode,
    Name,
    DefaultRate,
    MonthlyFee,
    UnitPricePerDcu,
    IncludedPublicStorageGb,
    IncludedPrivateStorageGb,
    IncludedSeatCount,
    AllowsPrivateData,
    CanQueryPublicCorpus
FROM dbo.BillingRatePlans
WHERE RatePlanId = @ResolvedRatePlanId;

SELECT Operation, Rate, UnitOfMeasure, Category
FROM dbo.BillingOperationRates
WHERE RatePlanId = @ResolvedRatePlanId AND IsActive = 1;

SELECT Dimension, [Key], Multiplier
FROM dbo.BillingMultipliers
WHERE RatePlanId = @ResolvedRatePlanId AND IsActive = 1;";

    private readonly ISqlServerConnectionFactory _connectionFactory;
    private readonly IOptionsMonitor<BillingOptions> _optionsMonitor;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SqlBillingConfigurationProvider> _logger;
    private readonly MemoryCacheEntryOptions _cacheEntryOptions;

    public SqlBillingConfigurationProvider(
        ISqlServerConnectionFactory connectionFactory,
        IOptionsMonitor<BillingOptions> optionsMonitor,
        IMemoryCache cache,
        ILogger<SqlBillingConfigurationProvider> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _cacheEntryOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(5),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
        };
    }

    public async Task<BillingConfiguration> GetConfigurationAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return await Task.FromResult(MapOptionsToConfiguration(_optionsMonitor.CurrentValue)).ConfigureAwait(false);
        }

        if (_cache.TryGetValue(tenantId, out BillingConfiguration? cachedConfiguration) && cachedConfiguration is not null)
        {
            return cachedConfiguration;
        }

        try
        {
            var configuration = await LoadConfigurationAsync(tenantId, cancellationToken).ConfigureAwait(false);
            _cache.Set(tenantId, configuration, _cacheEntryOptions);
            return configuration;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Failed to load billing configuration for tenant {TenantId}. Falling back to application defaults.", tenantId);
            return MapOptionsToConfiguration(_optionsMonitor.CurrentValue);
        }
    }

    private async Task<BillingConfiguration> LoadConfigurationAsync(string tenantId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = ConfigurationQuery;
        command.Parameters.Add(new SqlParameter("@TenantId", tenantId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        var options = _optionsMonitor.CurrentValue;

        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return MapOptionsToConfiguration(options);
        }

        if (reader.IsDBNull(0))
        {
            return MapOptionsToConfiguration(options);
        }

        var ratePlanId = reader.GetGuid(0);
        var planCode = reader.IsDBNull(1) ? options.DefaultPlanCode : reader.GetString(1);
        var planName = reader.IsDBNull(2) ? options.DefaultPlanName : reader.GetString(2);
        var defaultRate = reader.IsDBNull(3) ? options.DefaultRate : reader.GetDecimal(3);
        var monthlyFee = reader.IsDBNull(4) ? options.DefaultMonthlyFee : reader.GetDecimal(4);
        var unitPricePerDcu = reader.IsDBNull(5) ? options.UnitPricePerDcu : reader.GetDecimal(5);
        if (unitPricePerDcu <= 0)
        {
            unitPricePerDcu = options.UnitPricePerDcu;
        }

        var includedPublicStorage = reader.IsDBNull(6) ? options.DefaultIncludedPublicStorageGb : reader.GetDecimal(6);
        var includedPrivateStorage = reader.IsDBNull(7) ? options.DefaultIncludedPrivateStorageGb : reader.GetDecimal(7);
        var includedSeatCount = reader.IsDBNull(8) ? options.DefaultIncludedSeatCount : reader.GetInt32(8);
        var allowsPrivateData = !reader.IsDBNull(9) && reader.GetBoolean(9);
        var canQueryPublicCorpus = !reader.IsDBNull(10) && reader.GetBoolean(10);

        var operationRates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var operationUnits = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var operationCategories = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        if (await reader.NextResultAsync(cancellationToken).ConfigureAwait(false))
        {
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                if (reader.IsDBNull(0) || reader.IsDBNull(1))
                {
                    continue;
                }

                var operation = reader.GetString(0);
                var rate = reader.GetDecimal(1);
                operationRates[operation] = rate;

                if (!reader.IsDBNull(2))
                {
                    var unit = reader.GetString(2);
                    if (!string.IsNullOrWhiteSpace(unit))
                    {
                        operationUnits[operation] = unit;
                    }
                }

                if (!reader.IsDBNull(3))
                {
                    var category = reader.GetString(3);
                    if (!string.IsNullOrWhiteSpace(category))
                    {
                        operationCategories[operation] = category;
                    }
                }
            }
        }

        var generationMultipliers = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var complexityMultipliers = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var contentTypeMultipliers = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var groundingMultipliers = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var guaranteeMultipliers = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var provenanceMultipliers = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        if (await reader.NextResultAsync(cancellationToken).ConfigureAwait(false))
        {
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                if (reader.IsDBNull(0) || reader.IsDBNull(1) || reader.IsDBNull(2))
                {
                    continue;
                }

                var dimension = reader.GetString(0);
                var key = reader.GetString(1);
                var multiplier = reader.GetDecimal(2);

                switch (dimension.ToLowerInvariant())
                {
                    case "generation":
                    case "generationtype":
                        generationMultipliers[key] = multiplier;
                        break;
                    case "complexity":
                        complexityMultipliers[key] = multiplier;
                        break;
                    case "content":
                    case "contenttype":
                        contentTypeMultipliers[key] = multiplier;
                        break;
                    case "grounding":
                        groundingMultipliers[key] = multiplier;
                        break;
                    case "guarantee":
                        guaranteeMultipliers[key] = multiplier;
                        break;
                    case "provenance":
                        provenanceMultipliers[key] = multiplier;
                        break;
                }
            }
        }

        return new BillingConfiguration
        {
            RatePlanId = ratePlanId,
            PlanCode = planCode,
            PlanName = planName,
            DefaultRate = defaultRate,
            MonthlyFee = monthlyFee,
            UnitPricePerDcu = unitPricePerDcu,
            IncludedPublicStorageGb = includedPublicStorage,
            IncludedPrivateStorageGb = includedPrivateStorage,
            IncludedSeatCount = includedSeatCount,
            AllowsPrivateData = allowsPrivateData,
            CanQueryPublicCorpus = canQueryPublicCorpus,
            OperationRates = operationRates,
            OperationUnits = operationUnits,
            OperationCategories = operationCategories,
            GenerationTypeMultipliers = generationMultipliers,
            ComplexityMultipliers = complexityMultipliers,
            ContentTypeMultipliers = contentTypeMultipliers,
            GroundingMultipliers = groundingMultipliers,
            GuaranteeMultipliers = guaranteeMultipliers,
            ProvenanceMultipliers = provenanceMultipliers
        };
    }

    private static BillingConfiguration MapOptionsToConfiguration(BillingOptions options)
    {
        return new BillingConfiguration
        {
            RatePlanId = null,
            PlanCode = options.DefaultPlanCode,
            PlanName = options.DefaultPlanName,
            DefaultRate = options.DefaultRate,
            MonthlyFee = options.DefaultMonthlyFee,
            UnitPricePerDcu = options.UnitPricePerDcu,
            IncludedPublicStorageGb = options.DefaultIncludedPublicStorageGb,
            IncludedPrivateStorageGb = options.DefaultIncludedPrivateStorageGb,
            IncludedSeatCount = options.DefaultIncludedSeatCount,
            AllowsPrivateData = options.DefaultAllowsPrivateData,
            CanQueryPublicCorpus = options.DefaultCanQueryPublicCorpus,
            OperationRates = new Dictionary<string, decimal>(options.OperationRates, StringComparer.OrdinalIgnoreCase),
            OperationUnits = new Dictionary<string, string>(options.OperationUnits, StringComparer.OrdinalIgnoreCase),
            OperationCategories = new Dictionary<string, string?>(options.OperationCategories, StringComparer.OrdinalIgnoreCase),
            GenerationTypeMultipliers = new Dictionary<string, decimal>(options.GenerationTypeMultipliers, StringComparer.OrdinalIgnoreCase),
            ComplexityMultipliers = new Dictionary<string, decimal>(options.ComplexityMultipliers, StringComparer.OrdinalIgnoreCase),
            ContentTypeMultipliers = new Dictionary<string, decimal>(options.ContentTypeMultipliers, StringComparer.OrdinalIgnoreCase),
            GroundingMultipliers = new Dictionary<string, decimal>(options.GroundingMultipliers, StringComparer.OrdinalIgnoreCase),
            GuaranteeMultipliers = new Dictionary<string, decimal>(options.GuaranteeMultipliers, StringComparer.OrdinalIgnoreCase),
            ProvenanceMultipliers = new Dictionary<string, decimal>(options.ProvenanceMultipliers, StringComparer.OrdinalIgnoreCase)
        };
    }
}
