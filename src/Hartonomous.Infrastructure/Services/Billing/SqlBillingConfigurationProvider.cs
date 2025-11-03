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

SELECT @ResolvedRatePlanId AS RatePlanId;

SELECT DefaultRate
FROM dbo.BillingRatePlans
WHERE RatePlanId = @ResolvedRatePlanId;

SELECT Operation, Rate
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

        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return MapOptionsToConfiguration(_optionsMonitor.CurrentValue);
        }

        if (reader.IsDBNull(0))
        {
            return MapOptionsToConfiguration(_optionsMonitor.CurrentValue);
        }

    var ratePlanId = reader.GetGuid(0);

        if (!await reader.NextResultAsync(cancellationToken).ConfigureAwait(false))
        {
            return MapOptionsToConfiguration(_optionsMonitor.CurrentValue);
        }

        decimal defaultRate = _optionsMonitor.CurrentValue.DefaultRate;
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false) && !reader.IsDBNull(0))
        {
            defaultRate = reader.GetDecimal(0);
        }

        var operationRates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
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
            }
        }

        var generationMultipliers = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var complexityMultipliers = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var contentTypeMultipliers = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

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
                }
            }
        }

        return new BillingConfiguration
        {
            RatePlanId = ratePlanId,
            DefaultRate = defaultRate,
            OperationRates = operationRates,
            GenerationTypeMultipliers = generationMultipliers,
            ComplexityMultipliers = complexityMultipliers,
            ContentTypeMultipliers = contentTypeMultipliers
        };
    }

    private static BillingConfiguration MapOptionsToConfiguration(BillingOptions options)
    {
        return new BillingConfiguration
        {
            RatePlanId = null,
            DefaultRate = options.DefaultRate,
            OperationRates = new Dictionary<string, decimal>(options.OperationRates, StringComparer.OrdinalIgnoreCase),
            GenerationTypeMultipliers = new Dictionary<string, decimal>(options.GenerationTypeMultipliers, StringComparer.OrdinalIgnoreCase),
            ComplexityMultipliers = new Dictionary<string, decimal>(options.ComplexityMultipliers, StringComparer.OrdinalIgnoreCase),
            ContentTypeMultipliers = new Dictionary<string, decimal>(options.ContentTypeMultipliers, StringComparer.OrdinalIgnoreCase)
        };
    }
}
