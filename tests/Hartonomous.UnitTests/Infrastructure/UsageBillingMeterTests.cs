using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Billing;
using Hartonomous.Core.Entities;
using Hartonomous.Core.Models;
using Hartonomous.Core.Security;
using Hartonomous.Infrastructure.Services.Billing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hartonomous.UnitTests.Infrastructure;

public sealed class UsageBillingMeterTests
{
    private readonly StubBillingConfigurationProvider _configProvider;
    private readonly UsageBillingMeter _meter;

    public UsageBillingMeterTests()
    {
        _configProvider = new StubBillingConfigurationProvider();
        _meter = new UsageBillingMeter(_configProvider);
    }

    [Fact]
    public async Task MeasureAsync_AppliesModalityMultiplier()
    {
        var evt = new GenericInferenceEvent();
        evt.Extensions["generation_type"] = "image";
        evt.Extensions["complexity"] = "standard";
        evt.Extensions["content_type"] = "knowledge_graph";
        evt.Extensions["grounding"] = "none";
        evt.Extensions["guarantee"] = "standard_sla";
        evt.Extensions["provenance"] = "basic";
        evt.Extensions["usage_units"] = 100;

        var policyContext = new AccessPolicyContext
        {
            TenantId = "1",
            PrincipalId = "user-123",
            Operation = "neo4j_sync.inference_completed"
        };

        var record = await _meter.MeasureAsync(
            evt,
            "InferenceCompleted",
            "InferenceEventHandler",
            policyContext,
            CancellationToken.None);

        Assert.NotNull(record);
        Assert.Equal("1", record!.TenantId);
        Assert.Equal("user-123", record.PrincipalId);
        Assert.Equal("neo4j_sync.inference_completed", record.Operation);
        Assert.Equal(100m, record.Units);
        Assert.Equal(0.04m, record.BaseRate);

        // image = 3.0, standard = 1.0, knowledge_graph = 1.2, none = 1.0, standard_sla = 1.0, basic = 1.0
        // Total multiplier = 3.0 * 1.0 * 1.2 * 1.0 * 1.0 * 1.0 = 3.6
        Assert.Equal(3.6m, record.Multiplier);

        // Total DCU = 100 * 0.04 * 3.6 = 14.4
        Assert.Equal(14.4m, record.TotalDcu);
    }

    private sealed class GenericInferenceEvent : BaseEvent
    {
    }

    private sealed class StubBillingConfigurationProvider : IBillingConfigurationProvider
    {
        public Task<BillingConfiguration> GetConfigurationAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            var operationRates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["neo4j_sync.model_updated"] = 0.025m,
                ["neo4j_sync.inference_completed"] = 0.04m,
                ["neo4j_sync.ingest_completed"] = 0.018m
            };

            var operationCategories = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["neo4j_sync.model_updated"] = "model_management",
                ["neo4j_sync.inference_completed"] = "generation",
                ["neo4j_sync.ingest_completed"] = "ingestion"
            };

            var operationUnits = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["neo4j_sync.model_updated"] = "dcu",
                ["neo4j_sync.inference_completed"] = "dcu",
                ["neo4j_sync.ingest_completed"] = "dcu"
            };

            var generationTypes = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["text"] = 1.0m,
                ["image"] = 3.0m,
                ["audio"] = 2.2m,
                ["video"] = 3.8m
            };

            var complexities = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["standard"] = 1.0m,
                ["premium"] = 1.5m,
                ["enterprise"] = 2.0m
            };

            var contentTypes = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["knowledge_graph"] = 1.2m,
                ["time_series"] = 1.4m,
                ["spatial"] = 1.6m
            };

            var groundings = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["none"] = 1.0m,
                ["enterprise_context"] = 1.3m,
                ["private_vector_index"] = 1.55m
            };

            var guarantees = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["standard_sla"] = 1.0m,
                ["premium_sla"] = 1.35m,
                ["model_lock"] = 1.2m
            };

            var provenances = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["basic"] = 1.0m,
                ["audit_trail"] = 1.25m,
                ["immutable_ledger"] = 1.5m
            };

            return Task.FromResult(new BillingConfiguration
            {
                PlanCode = "publisher_core",
                PlanName = "Publisher Core",
                DefaultRate = 0.0105m,
                MonthlyFee = 2500m,
                UnitPricePerDcu = 0.00008m,
                AllowsPrivateData = true,
                CanQueryPublicCorpus = true,
                OperationRates = operationRates,
                OperationCategories = operationCategories,
                OperationUnits = operationUnits,
                GenerationTypeMultipliers = generationTypes,
                ComplexityMultipliers = complexities,
                ContentTypeMultipliers = contentTypes,
                GroundingMultipliers = groundings,
                GuaranteeMultipliers = guarantees,
                ProvenanceMultipliers = provenances
            });
        }
    }
}
