using System.Collections.Generic;
using System.Text.Json;
using Hartonomous.Core.Models;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace Hartonomous.Neo4jSync.Services;

public sealed class ProvenanceGraphBuilder
{
    private readonly ILogger<ProvenanceGraphBuilder> _logger;

    public ProvenanceGraphBuilder(ILogger<ProvenanceGraphBuilder> logger)
    {
        _logger = logger;
    }

    public async Task CreateModelNodeAsync(IAsyncSession session, BaseEvent evt, CancellationToken ct)
    {
        var data = evt.Data as Dictionary<string, object>;
        if (data == null)
        {
            return;
        }

        var modelId = data.GetValueOrDefault("ModelId")?.ToString();
        var modelName = data.GetValueOrDefault("ModelName")?.ToString();
        var architecture = data.GetValueOrDefault("architecture")?.ToString();

        if (string.IsNullOrWhiteSpace(modelId) || string.IsNullOrWhiteSpace(modelName))
        {
            return;
        }

        var semanticExtensions = evt.Extensions.GetValueOrDefault("semantic") as Dictionary<string, object>;
        var capabilities = semanticExtensions?.GetValueOrDefault("inferred_capabilities") as string[];
        var contentType = semanticExtensions?.GetValueOrDefault("content_type") as string;
        var performance = semanticExtensions?.GetValueOrDefault("expected_performance") as double?;
        var compliance = semanticExtensions?.GetValueOrDefault("compliance_requirements") as string[];

        const string cypher = @"
            MERGE (m:Model {ModelId: $modelId})
            SET m.ModelName = $modelName,
                m.architecture = $architecture,
                m.content_type = $contentType,
                m.expected_performance = $performance,
                m.capabilities = $capabilities,
                m.compliance_requirements = $compliance,
                m.LastSynced = datetime()
            ON CREATE SET
                m.CreatedAt = datetime(),
                m.source_event = $eventId";

        await session.RunAsync(cypher, new
        {
            modelId,
            modelName,
            architecture,
            contentType,
            performance,
            capabilities,
            compliance,
            eventId = evt.Id
        }).ConfigureAwait(false);

        _logger.LogInformation("Created model node: {ModelName} ({ModelId})", modelName, modelId);
    }

    public async Task CreateInferenceNodeAsync(IAsyncSession session, BaseEvent evt, CancellationToken ct)
    {
        var data = evt.Data as Dictionary<string, object>;
        if (data == null)
        {
            return;
        }

        var inferenceId = data.GetValueOrDefault("InferenceId")?.ToString();
        var taskType = data.GetValueOrDefault("TaskType")?.ToString();
        var modelsUsed = data.GetValueOrDefault("ModelsUsed")?.ToString();

        if (string.IsNullOrWhiteSpace(inferenceId))
        {
            return;
        }

        var reasoningExtensions = evt.Extensions.GetValueOrDefault("reasoning") as Dictionary<string, object>;
        var reasoningMode = reasoningExtensions?.GetValueOrDefault("reasoning_mode") as string;
        var complexity = reasoningExtensions?.GetValueOrDefault("expected_complexity") as string;
        var auditRequired = reasoningExtensions?.GetValueOrDefault("audit_trail_required") as bool?;
        var performanceSla = reasoningExtensions?.GetValueOrDefault("performance_sla") as TimeSpan?;

        const string cypher = @"
            MERGE (i:Inference {InferenceId: $inferenceId})
            SET i.TaskType = $taskType,
                i.ModelsUsed = $modelsUsed,
                i.ReasoningMode = $reasoningMode,
                i.Complexity = $complexity,
                i.AuditRequired = $auditRequired,
                i.PerformanceSlaMs = $performanceSlaMs,
                i.CreatedAt = datetime(),
                i.SourceEvent = $eventId";

        await session.RunAsync(cypher, new
        {
            inferenceId,
            taskType,
            modelsUsed,
            reasoningMode,
            complexity,
            auditRequired,
            performanceSlaMs = performanceSla?.TotalMilliseconds,
            eventId = evt.Id
        }).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(modelsUsed))
        {
            var modelIds = modelsUsed.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var modelId in modelIds)
            {
                const string relCypher = @"
                    MATCH (i:Inference {InferenceId: $inferenceId})
                    MATCH (m:Model {ModelId: $modelId})
                    MERGE (m)-[:USED_IN {InferenceId: $inferenceId}]->(i)";

                await session.RunAsync(relCypher, new { inferenceId, modelId = modelId.Trim() }).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("Created inference node: {InferenceId} ({TaskType})", inferenceId, taskType);
    }

    public async Task CreateKnowledgeNodeAsync(IAsyncSession session, BaseEvent evt, CancellationToken ct)
    {
        var data = evt.Data as Dictionary<string, object>;
        if (data == null)
        {
            return;
        }

        var docId = data.GetValueOrDefault("DocId")?.ToString();
        var content = data.GetValueOrDefault("content")?.ToString();
        var category = data.GetValueOrDefault("category")?.ToString();

        if (string.IsNullOrWhiteSpace(docId) || string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        const string cypher = @"
            MERGE (k:Knowledge {DocId: $docId})
            SET k.Content = $content,
                k.Category = $category,
                k.CreatedAt = datetime(),
                k.SourceEvent = $eventId";

        await session.RunAsync(cypher, new
        {
            docId,
            content,
            category,
            eventId = evt.Id
        }).ConfigureAwait(false);

        _logger.LogInformation("Created knowledge node: {DocId} ({Category})", docId, category);
    }

    public async Task CreateGenericEventNodeAsync(IAsyncSession session, BaseEvent evt, CancellationToken ct)
    {
        const string cypher = @"
            MERGE (e:Event {EventId: $eventId})
            SET e.EventType = $eventType,
                e.Subject = $subject,
                e.Source = $source,
                e.Data = $data,
                e.Extensions = $extensions,
                e.CreatedAt = datetime()";

        await session.RunAsync(cypher, new
        {
            eventId = evt.Id,
            eventType = evt.Type,
            subject = evt.Subject,
            source = evt.Source.ToString(),
            data = JsonSerializer.Serialize(evt.Data),
            extensions = JsonSerializer.Serialize(evt.Extensions)
        }).ConfigureAwait(false);

        _logger.LogInformation("Created generic event node: {EventId} ({EventType})", evt.Id, evt.Type);
    }
}
