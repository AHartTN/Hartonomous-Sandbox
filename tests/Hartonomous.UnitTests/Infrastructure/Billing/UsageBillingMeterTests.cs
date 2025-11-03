using System.Text.Json;
using Hartonomous.Core.Billing;
using Hartonomous.Core.Models;
using Hartonomous.Core.Security;
using Hartonomous.Infrastructure.Services.Billing;

namespace Hartonomous.UnitTests.Infrastructure.Billing;

public sealed class UsageBillingMeterTests
{
    [Fact]
    public async Task MeasureAsync_ComputesTotalsAndMetadata()
    {
        var configuration = new BillingConfiguration
        {
            RatePlanId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            PlanName = "Enterprise",
            PlanCode = "ENT-1",
            DefaultRate = 1.1m,
            UnitPricePerDcu = 0.0025m,
            AllowsPrivateData = true,
            CanQueryPublicCorpus = true,
            IncludedPublicStorageGb = 1024m,
            IncludedPrivateStorageGb = 256m,
            IncludedSeatCount = 50,
            OperationRates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["ingest"] = 2.5m
            },
            OperationUnits = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ingest"] = "thousand_tokens"
            },
            OperationCategories = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["ingest"] = "ai_inference"
            },
            GenerationTypeMultipliers = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["text"] = 1.2m
            },
            ComplexityMultipliers = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["medium"] = 1.4m
            },
            ContentTypeMultipliers = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["compliance"] = 1.3m
            },
            GroundingMultipliers = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["private"] = 1.1m
            },
            GuaranteeMultipliers = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["strict"] = 1.05m
            },
            ProvenanceMultipliers = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["full"] = 1.02m
            }
        };

        var provider = new TestBillingConfigurationProvider(configuration);
        var meter = new UsageBillingMeter(provider);

        var evt = new BaseEvent
        {
            Type = "ingest.created",
            Time = DateTimeOffset.UtcNow,
            Extensions =
            {
                ["generation"] = new Dictionary<string, object>
                {
                    ["modality"] = "text"
                },
                ["reasoning"] = new Dictionary<string, object>
                {
                    ["complexity"] = 4
                },
                ["content"] = new Dictionary<string, object>
                {
                    ["type"] = "compliance"
                },
                ["grounding"] = JsonDocument.Parse("{\"grounding_mode\":\"private\"}").RootElement,
                ["guarantee"] = JsonDocument.Parse("{\"model_lock\":\"strict\"}").RootElement,
                ["provenance"] = "full",
                ["usage_units"] = 12.5m
            }
        };

        var context = new AccessPolicyContext
        {
            TenantId = "tenant-a",
            PrincipalId = "principal-x",
            Operation = "ingest"
        };

        var record = await meter.MeasureAsync(evt, "atom.ingested", "IngestionHandler", context);

        Assert.NotNull(record);
        Assert.Equal("tenant-a", record!.TenantId);
        Assert.Equal("principal-x", record.PrincipalId);
        Assert.Equal("ingest", record.Operation);
        Assert.Equal(12.5m, record.Units);
        Assert.Equal(2.5m, record.BaseRate);

        var expectedMultiplier = 1.2m * 1.4m * 1.3m * 1.1m * 1.05m * 1.02m;
        Assert.Equal(expectedMultiplier, record.Multiplier);

        var expectedDcu = Math.Round(12.5m * 2.5m * expectedMultiplier, 6, MidpointRounding.AwayFromZero);
        Assert.Equal(expectedDcu, record.Metadata!["total_dcu_estimate"]);
        Assert.Equal("thousand_tokens", record.Metadata!["unit_of_measure"]);
        Assert.Equal("ai_inference", record.Metadata!["operation_category"]);
        Assert.Equal("Enterprise", record.Metadata!["rate_plan_name"]);
    }

    [Fact]
    public async Task MeasureAsync_UsesDefaultRateAndUnitPrice_WhenMissing()
    {
        var configuration = new BillingConfiguration
        {
            PlanCode = "BASIC",
            DefaultRate = 0.5m,
            UnitPricePerDcu = 0m
        };

        var provider = new TestBillingConfigurationProvider(configuration);
        var meter = new UsageBillingMeter(provider);

        var evt = new BaseEvent
        {
            Extensions =
            {
                ["token_count"] = 3200
            }
        };

        var context = new AccessPolicyContext
        {
            TenantId = "tenant-b",
            Operation = "summarize"
        };

        var record = await meter.MeasureAsync(evt, "summary.generated", "SummaryHandler", context);

        Assert.NotNull(record);
        Assert.Equal(0.5m, record!.BaseRate);
        Assert.Equal(3.2m, record.Units);
        Assert.Equal(UsageBillingMeterTestsConstants.DefaultUnitPrice, record.UnitPrice);
        var expectedDcu = Math.Round(3.2m * 0.5m, 6, MidpointRounding.AwayFromZero);
        Assert.Equal(expectedDcu, record.Metadata!["total_dcu_estimate"]);
        var expectedCost = Math.Round(expectedDcu * UsageBillingMeterTestsConstants.DefaultUnitPrice, 6, MidpointRounding.AwayFromZero);
        Assert.Equal(expectedCost, record.Metadata!["total_cost_estimate"]);
    }

    [Fact]
    public async Task MeasureAsync_ReturnsNull_WhenTenantOrOperationMissing()
    {
        var meter = new UsageBillingMeter(new TestBillingConfigurationProvider(BillingConfiguration.Empty));
        var evt = new BaseEvent();

        var contextWithoutTenant = new AccessPolicyContext { Operation = "op" };
        var contextWithoutOperation = new AccessPolicyContext { TenantId = "tenant" };

        Assert.Null(await meter.MeasureAsync(evt, "message", "Handler", contextWithoutTenant));
        Assert.Null(await meter.MeasureAsync(evt, "message", "Handler", contextWithoutOperation));
    }

    [Fact]
    public async Task MeasureAsync_ResolvesComplexityFromReasoningLevel()
    {
        var configuration = new BillingConfiguration
        {
            DefaultRate = 1m,
            ComplexityMultipliers = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["large"] = 2.5m
            }
        };

        var meter = new UsageBillingMeter(new TestBillingConfigurationProvider(configuration));
        var evt = new BaseEvent
        {
            Extensions =
            {
                ["reasoning"] = JsonDocument.Parse("{\"complexity_level\":\"large\"}").RootElement,
                ["usage"] = new Dictionary<string, object>
                {
                    ["units"] = 4
                }
            }
        };

        var context = new AccessPolicyContext { TenantId = "tenant", Operation = "classify" };
        var record = await meter.MeasureAsync(evt, "classification", "Classifier", context);

        Assert.NotNull(record);
        Assert.Equal(2.5m, record!.Multiplier);
        Assert.Equal(4m, record.Units);
    }

    [Fact]
    public async Task MeasureAsync_FallsBackToDuration_WhenNoOtherUnits()
    {
        var meter = new UsageBillingMeter(new TestBillingConfigurationProvider(new BillingConfiguration { DefaultRate = 1m }));
        var evt = new BaseEvent
        {
            Extensions =
            {
                ["duration_seconds"] = 42
            }
        };

        var context = new AccessPolicyContext { TenantId = "tenant", Operation = "transcribe" };
        var record = await meter.MeasureAsync(evt, "audio.transcribed", "TranscriptionHandler", context);

        Assert.NotNull(record);
        Assert.Equal(42m, record!.Units);
    }
}

internal static class UsageBillingMeterTestsConstants
{
    public const decimal DefaultUnitPrice = 0.00008m;
}

internal sealed class TestBillingConfigurationProvider : IBillingConfigurationProvider
{
    private readonly BillingConfiguration _configuration;

    public TestBillingConfigurationProvider(BillingConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<BillingConfiguration> GetConfigurationAsync(string tenantId, CancellationToken cancellationToken = default)
        => Task.FromResult(_configuration);
}