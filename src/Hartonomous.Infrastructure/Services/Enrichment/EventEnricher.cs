using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Abstracts;
using Hartonomous.Core.Enums;
using Hartonomous.Core.Models;
using Hartonomous.Core.Services;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Services.Enrichment;

/// <summary>
/// Semantic enrichment service for events.
/// Thin orchestrator that delegates to domain services for business logic.
/// </summary>
public class EventEnricher : IEventEnricher
{
    private readonly ILogger<EventEnricher> _logger;
    private readonly IModelCapabilityService _capabilityService;
    private readonly IInferenceMetadataService _metadataService;

    public EventEnricher(
        ILogger<EventEnricher> logger,
        IModelCapabilityService capabilityService,
        IInferenceMetadataService metadataService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _capabilityService = capabilityService ?? throw new ArgumentNullException(nameof(capabilityService));
        _metadataService = metadataService ?? throw new ArgumentNullException(nameof(metadataService));
    }

    public async Task EnrichAsync(BaseEvent evt, CancellationToken cancellationToken = default)
    {
        if (evt == null) throw new ArgumentNullException(nameof(evt));

        // Extract SQL Server extensions
        if (evt.Extensions.TryGetValue("sqlserver", out var sqlExtObj) &&
            sqlExtObj is Dictionary<string, object> sqlExtensions)
        {
            var operation = sqlExtensions.GetValueOrDefault("operation")?.ToString() ?? "";
            var table = sqlExtensions.GetValueOrDefault("table")?.ToString() ?? "";

            await EnrichByTableAsync(evt, table, operation, cancellationToken);
        }

        // Add general enrichment metadata
        evt.Extensions["enrichment"] = new Dictionary<string, object>
        {
            ["processed_at"] = DateTimeOffset.UtcNow,
            ["processor_version"] = "2.0.0",
            ["enrichment_level"] = "semantic",
            ["confidence_score"] = 0.95
        };

        _logger.LogDebug("Enriched event {EventId} of type {EventType}", 
            evt.Id, evt.Type);
    }

    public async Task EnrichBatchAsync(IEnumerable<BaseEvent> events, CancellationToken cancellationToken = default)
    {
        var tasks = events.Select(evt => EnrichAsync(evt, cancellationToken));
        await Task.WhenAll(tasks);
    }

    private Task EnrichByTableAsync(BaseEvent evt, string table, string operation, CancellationToken cancellationToken)
    {
        return table.ToLowerInvariant() switch
        {
            "dbo.models" when operation == "insert" => EnrichModelEventAsync(evt, cancellationToken),
            "dbo.inferencerequests" when operation == "insert" => EnrichInferenceEventAsync(evt, cancellationToken),
            "dbo.knowledgebase" when operation == "insert" => EnrichKnowledgeEventAsync(evt, cancellationToken),
            _ => Task.CompletedTask
        };
    }

    private async Task EnrichModelEventAsync(BaseEvent evt, CancellationToken cancellationToken)
    {
        if (evt.Data is Dictionary<string, object> data &&
            data.TryGetValue("ModelName", out var modelNameObj) &&
            modelNameObj is string modelName)
        {
            var capabilities = await _capabilityService.GetCapabilitiesAsync(modelName, cancellationToken);
            evt.Extensions["semantic"] = new Dictionary<string, object>
            {
                ["primary_modality"] = capabilities.PrimaryModality.ToJsonString(),
                ["supports_text"] = capabilities.SupportsTask(Hartonomous.Core.Enums.TaskType.TextGeneration),
                ["supports_vision"] = capabilities.SupportsTask(Hartonomous.Core.Enums.TaskType.ObjectDetection) || 
                                      capabilities.SupportsTask(Hartonomous.Core.Enums.TaskType.ImageEmbedding),
                ["supports_function_calling"] = capabilities.SupportedTasks.Any(), // Placeholder - need specific function calling enum
                ["max_tokens"] = capabilities.MaxTokens,
                ["context_window"] = capabilities.MaxContextWindow
            };
        }
    }

    private Task EnrichInferenceEventAsync(BaseEvent evt, CancellationToken cancellationToken)
    {
        if (evt.Data is Dictionary<string, object> data &&
            data.TryGetValue("TaskType", out var taskTypeObj) &&
            taskTypeObj is string taskType)
        {
            var complexity = _metadataService.CalculateComplexity(
                GetTokenCount(data), 
                GetBool(data, "requires_multimodal"),
                GetBool(data, "requires_tools"));
            
            var reasoning = _metadataService.DetermineReasoningMode(taskType, complexity > 7);
            var sla = _metadataService.DetermineSla(GetString(data, "priority", "medium"), complexity);
            
            evt.Extensions["reasoning"] = new Dictionary<string, object>
            {
                ["reasoning_mode"] = reasoning,
                ["complexity"] = complexity,
                ["sla_tier"] = sla
            };
        }

        return Task.CompletedTask;
    }

    private Task EnrichKnowledgeEventAsync(BaseEvent evt, CancellationToken cancellationToken)
    {
        evt.Extensions["knowledge"] = new Dictionary<string, object>
        {
            ["indexed"] = true,
            ["searchable"] = true,
            ["retention_policy"] = "standard"
        };

        return Task.CompletedTask;
    }

    private static int GetTokenCount(Dictionary<string, object> data)
    {
        return data.TryGetValue("token_count", out var count) && count is int tokenCount
            ? tokenCount
            : 0;
    }

    private static bool GetBool(Dictionary<string, object> data, string key)
    {
        return data.TryGetValue(key, out var val) && val is bool boolVal && boolVal;
    }

    private static string GetString(Dictionary<string, object> data, string key, string defaultValue)
    {
        return data.TryGetValue(key, out var val) && val is string strVal
            ? strVal
            : defaultValue;
    }
}
