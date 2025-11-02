using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Abstracts;
using Hartonomous.Core.Models;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Services.Enrichment;

/// <summary>
/// Semantic enrichment service for events
/// Adds domain-specific metadata based on event type and content
/// </summary>
public class EventEnricher : IEventEnricher
{
    private readonly ILogger<EventEnricher> _logger;

    public EventEnricher(ILogger<EventEnricher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

        _logger.LogDebug("Enriched evt {EventId} of type {EventType}", 
            evt.Id, evt.Type);
    }

    public async Task EnrichBatchAsync(IEnumerable<evt> events, CancellationToken cancellationToken = default)
    {
        var tasks = events.Select(ce => EnrichAsync(ce, cancellationToken));
        await Task.WhenAll(tasks);
    }

    private Task EnrichByTableAsync(evt evt, string table, string operation, CancellationToken cancellationToken)
    {
        return table.ToLowerInvariant() switch
        {
            "dbo.models" when operation == "insert" => EnrichModelEventAsync(evt, cancellationToken),
            "dbo.inferencerequests" when operation == "insert" => EnrichInferenceEventAsync(evt, cancellationToken),
            "dbo.knowledgebase" when operation == "insert" => EnrichKnowledgeEventAsync(evt, cancellationToken),
            _ => Task.CompletedTask
        };
    }

    private Task EnrichModelEventAsync(evt evt, CancellationToken cancellationToken)
    {
        // Extract model data
        if (evt.Data is Dictionary<string, object> data &&
            data.TryGetValue("model_name", out var modelNameObj) &&
            modelNameObj is string modelName)
        {
            evt.Extensions["semantic"] = new Dictionary<string, object>
            {
                ["inferred_capabilities"] = ModelCapabilityInference.InferCapabilities(modelName),
                ["content_type"] = ModelCapabilityInference.ClassifyModelType(modelName),
                ["expected_performance"] = ModelCapabilityInference.EstimatePerformance(modelName),
                ["compliance_requirements"] = ModelCapabilityInference.GetComplianceRequirements(modelName)
            };
        }

        return Task.CompletedTask;
    }

    private Task EnrichInferenceEventAsync(evt evt, CancellationToken cancellationToken)
    {
        if (evt.Data is Dictionary<string, object> data &&
            data.TryGetValue("task_type", out var taskTypeObj) &&
            taskTypeObj is string taskType)
        {
            evt.Extensions["reasoning"] = new Dictionary<string, object>
            {
                ["reasoning_mode"] = InferenceMetadataHelper.DetermineReasoningMode(taskType),
                ["expected_complexity"] = InferenceMetadataHelper.EstimateComplexity(taskType),
                ["audit_trail_required"] = InferenceMetadataHelper.RequiresAuditTrail(taskType),
                ["performance_sla"] = InferenceMetadataHelper.GetPerformanceSLA(taskType).TotalMilliseconds
            };
        }

        return Task.CompletedTask;
    }

    private Task EnrichKnowledgeEventAsync(evt evt, CancellationToken cancellationToken)
    {
        // Add knowledge-specific enrichment
        evt.Extensions["knowledge"] = new Dictionary<string, object>
        {
            ["indexed"] = true,
            ["searchable"] = true,
            ["retention_policy"] = "standard"
        };

        return Task.CompletedTask;
    }
}

/// <summary>
/// Helper class for model capability inference logic
/// Extracted for reusability and testability
/// </summary>
public static class ModelCapabilityInference
{
    public static string[] InferCapabilities(string modelName)
    {
        var capabilities = new List<string>();
        var lowerName = modelName.ToLowerInvariant();

        if (lowerName.Contains("llama") || lowerName.Contains("gpt"))
        {
            capabilities.AddRange(new[] { "text_generation", "question_answering", "summarization" });
        }

        if (lowerName.Contains("clip") || lowerName.Contains("vision"))
        {
            capabilities.AddRange(new[] { "image_classification", "image_captioning", "visual_question_answering" });
        }

        if (lowerName.Contains("wav2vec") || lowerName.Contains("whisper"))
        {
            capabilities.AddRange(new[] { "speech_recognition", "audio_classification" });
        }

        return capabilities.ToArray();
    }

    public static string ClassifyModelType(string modelName)
    {
        var lowerName = modelName.ToLowerInvariant();

        if (lowerName.Contains("llama") || lowerName.Contains("gpt")) return "text";
        if (lowerName.Contains("clip") || lowerName.Contains("vit")) return "vision";
        if (lowerName.Contains("wav2vec") || lowerName.Contains("whisper")) return "audio";

        return "multimodal";
    }

    public static double EstimatePerformance(string modelName)
    {
        var lowerName = modelName.ToLowerInvariant();

        if (lowerName.Contains("7b") || lowerName.Contains("large")) return 0.85;
        if (lowerName.Contains("1b") || lowerName.Contains("base")) return 0.75;
        if (lowerName.Contains("small") || lowerName.Contains("tiny")) return 0.65;

        return 0.70;
    }

    public static string[] GetComplianceRequirements(string modelName)
    {
        var requirements = new List<string> { "data_privacy" };
        var lowerName = modelName.ToLowerInvariant();

        if (lowerName.Contains("medical") || lowerName.Contains("health"))
        {
            requirements.AddRange(new[] { "hipaa", "gdpr", "health_data_protection" });
        }

        if (lowerName.Contains("financial"))
        {
            requirements.AddRange(new[] { "pci_dss", "sox", "financial_regulation" });
        }

        return requirements.ToArray();
    }
}

/// <summary>
/// Helper class for inference metadata logic
/// </summary>
public static class InferenceMetadataHelper
{
    public static string DetermineReasoningMode(string taskType) => taskType.ToLowerInvariant() switch
    {
        "text_generation" => "generative",
        "question_answering" => "analytical",
        "classification" => "categorical",
        "summarization" => "synthetic",
        "translation" => "transformational",
        _ => "general"
    };

    public static string EstimateComplexity(string taskType) => taskType.ToLowerInvariant() switch
    {
        "text_generation" => "high",
        "question_answering" => "medium",
        "classification" => "low",
        "summarization" => "medium",
        "translation" => "medium",
        _ => "medium"
    };

    public static bool RequiresAuditTrail(string taskType) => taskType.ToLowerInvariant() switch
    {
        "medical_diagnosis" => true,
        "financial_decision" => true,
        "legal_analysis" => true,
        "safety_critical" => true,
        _ => false
    };

    public static TimeSpan GetPerformanceSLA(string taskType) => taskType.ToLowerInvariant() switch
    {
        "real_time" => TimeSpan.FromMilliseconds(100),
        "interactive" => TimeSpan.FromSeconds(1),
        "batch" => TimeSpan.FromMinutes(5),
        _ => TimeSpan.FromSeconds(3)
    };
}
