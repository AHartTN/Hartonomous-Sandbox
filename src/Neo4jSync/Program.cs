using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Neo4j.Driver;
using Microsoft.Data.SqlClient;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using System.Text.Json;
using Hartonomous.Core.Models;

var builder = Host.CreateApplicationBuilder(args);

// Configure Event Hub processor
var eventHubConnectionString = Environment.GetEnvironmentVariable("EVENTHUB_CONNECTION_STRING")
    ?? "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=your-key";
var eventHubName = Environment.GetEnvironmentVariable("EVENTHUB_NAME") ?? "sqlserver-ces-events";
var consumerGroup = Environment.GetEnvironmentVariable("EVENTHUB_CONSUMER_GROUP") ?? "$Default";

builder.Services.AddSingleton<EventProcessorClient>(sp =>
{
    var storageConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")
        ?? "UseDevelopmentStorage=true"; // Local Azurite
    var blobContainerName = "eventhub-checkpoints";

    var blobServiceClient = new Azure.Storage.Blobs.BlobServiceClient(storageConnectionString);
    var blobContainerClient = blobServiceClient.GetBlobContainerClient(blobContainerName);

    return new EventProcessorClient(
        blobContainerClient,
        consumerGroup,
        eventHubConnectionString,
        eventHubName);
});

builder.Services.AddSingleton<IDriver>(sp =>
{
    var uri = Environment.GetEnvironmentVariable("NEO4J_URI") ?? "bolt://localhost:7687";
    var user = Environment.GetEnvironmentVariable("NEO4J_USER") ?? "neo4j";
    var password = Environment.GetEnvironmentVariable("NEO4J_PASSWORD") ?? "neo4jneo4j";
    return GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
});

builder.Services.AddHostedService<EventProcessor>();
builder.Services.AddSingleton<ProvenanceGraphBuilder>();

var app = builder.Build();
await app.RunAsync();

public class EventProcessor : BackgroundService
{
    private readonly ILogger<EventProcessor> _logger;
    private readonly EventProcessorClient _eventProcessor;
    private readonly IDriver _neo4jDriver;
    private readonly ProvenanceGraphBuilder _graphBuilder;

    public EventProcessor(
        ILogger<EventProcessor> logger,
        EventProcessorClient eventProcessor,
        IDriver neo4jDriver,
        ProvenanceGraphBuilder graphBuilder)
    {
        _logger = logger;
        _eventProcessor = eventProcessor;
        _neo4jDriver = neo4jDriver;
        _graphBuilder = graphBuilder;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Event Processor starting...");

        _eventProcessor.ProcessEventAsync += ProcessEventHandler;
        _eventProcessor.ProcessErrorAsync += ProcessErrorHandler;

        try
        {
            await _eventProcessor.StartProcessingAsync(stoppingToken);
            _logger.LogInformation("Event processing started");

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in event processing");
        }
        finally
        {
            await _eventProcessor.StopProcessingAsync();
            _logger.LogInformation("Event processing stopped");
        }
    }

    private async Task ProcessEventHandler(ProcessEventArgs eventArgs)
    {
        try
        {
            var evt = JsonSerializer.Deserialize<BaseEvent>(eventArgs.Data.Body.ToString());
            if (evt == null)
            {
                _logger.LogWarning("Received null or invalid event");
                return;
            }

            _logger.LogInformation("Processing event: {Id} - {Type}", evt.Id, evt.Type);

            // Process the event based on its type
            await ProcessEventAsync(evt, eventArgs.CancellationToken);

            // Update checkpoint
            await eventArgs.UpdateCheckpointAsync(eventArgs.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing BaseEvent");
        }
    }

    private async Task ProcessEventAsync(BaseEvent evt, CancellationToken cancellationToken)
    {
        await using var session = _neo4jDriver.AsyncSession();

        // Extract SQL Server extensions
        var sqlExtensions = evt.Extensions.GetValueOrDefault("sqlserver") as Dictionary<string, object>;
        var operation = sqlExtensions?.GetValueOrDefault("operation") as string ?? "";
        var table = sqlExtensions?.GetValueOrDefault("table") as string ?? "";

        if (table == "dbo.Models" && operation == "insert")
        {
            await _graphBuilder.CreateModelNodeAsync(session, BaseEvent, cancellationToken);
        }
        else if (table == "dbo.InferenceRequests" && operation == "insert")
        {
            await _graphBuilder.CreateInferenceNodeAsync(session, BaseEvent, cancellationToken);
        }
        else if (table == "dbo.KnowledgeBase" && operation == "insert")
        {
            await _graphBuilder.CreateKnowledgeNodeAsync(session, BaseEvent, cancellationToken);
        }
        else
        {
            // Generic event processing
            await _graphBuilder.CreateGenericEventNodeAsync(session, BaseEvent, cancellationToken);
        }
    }

    private Task ProcessErrorHandler(ProcessErrorEventArgs eventArgs)
    {
        _logger.LogError(eventArgs.Exception, "Error in event processing for partition {PartitionId}",
            eventArgs.PartitionId);
        return Task.CompletedTask;
    }
}

public class ProvenanceGraphBuilder
{
    private readonly ILogger<ProvenanceGraphBuilder> _logger;

    public ProvenanceGraphBuilder(ILogger<ProvenanceGraphBuilder> logger)
    {
        _logger = logger;
    }

    public async Task CreateModelNodeAsync(IAsyncSession session, BaseEvent evt, CancellationToken ct)
    {
        var data = evt.Data as Dictionary<string, object>;
        if (data == null) return;

        var modelId = data.GetValueOrDefault("model_id")?.ToString();
        var modelName = data.GetValueOrDefault("model_name")?.ToString();
        var architecture = data.GetValueOrDefault("architecture")?.ToString();

        if (string.IsNullOrEmpty(modelId) || string.IsNullOrEmpty(modelName)) return;

        // Create model node with semantic enrichment
        var semanticExtensions = evt.Extensions.GetValueOrDefault("semantic") as Dictionary<string, object>;
        var capabilities = semanticExtensions?.GetValueOrDefault("inferred_capabilities") as string[];
        var contentType = semanticExtensions?.GetValueOrDefault("content_type") as string;
        var performance = semanticExtensions?.GetValueOrDefault("expected_performance") as double?;
        var compliance = semanticExtensions?.GetValueOrDefault("compliance_requirements") as string[];

        var cypher = @"
            MERGE (m:Model {model_id: $modelId})
            SET m.model_name = $modelName,
                m.architecture = $architecture,
                m.content_type = $contentType,
                m.expected_performance = $performance,
                m.capabilities = $capabilities,
                m.compliance_requirements = $compliance,
                m.created_at = datetime(),
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
        });

        _logger.LogInformation("Created model node: {ModelName} ({ModelId})", modelName, modelId);
    }

    public async Task CreateInferenceNodeAsync(IAsyncSession session, BaseEvent evt, CancellationToken ct)
    {
        var data = evt.Data as Dictionary<string, object>;
        if (data == null) return;

        var inferenceId = data.GetValueOrDefault("inference_id")?.ToString();
        var taskType = data.GetValueOrDefault("task_type")?.ToString();
        var modelsUsed = data.GetValueOrDefault("models_used")?.ToString();

        if (string.IsNullOrEmpty(inferenceId)) return;

        // Create inference node with reasoning context
        var reasoningExtensions = evt.Extensions.GetValueOrDefault("reasoning") as Dictionary<string, object>;
        var reasoningMode = reasoningExtensions?.GetValueOrDefault("reasoning_mode") as string;
        var complexity = reasoningExtensions?.GetValueOrDefault("expected_complexity") as string;
        var auditRequired = reasoningExtensions?.GetValueOrDefault("audit_trail_required") as bool?;
        var performanceSla = reasoningExtensions?.GetValueOrDefault("performance_sla") as TimeSpan?;

        var cypher = @"
            MERGE (i:Inference {inference_id: $inferenceId})
            SET i.task_type = $taskType,
                i.models_used = $modelsUsed,
                i.reasoning_mode = $reasoningMode,
                i.complexity = $complexity,
                i.audit_required = $auditRequired,
                i.performance_sla_ms = $performanceSlaMs,
                i.created_at = datetime(),
                i.source_event = $eventId";

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
        });

        // Create relationships to models used
        if (!string.IsNullOrEmpty(modelsUsed))
        {
            var modelIds = modelsUsed.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var modelId in modelIds)
            {
                var relCypher = @"
                    MATCH (i:Inference {inference_id: $inferenceId})
                    MATCH (m:Model {model_id: $modelId})
                    MERGE (m)-[:USED_IN {inference_id: $inferenceId}]->(i)";

                await session.RunAsync(relCypher, new { inferenceId, modelId = modelId.Trim() });
            }
        }

        _logger.LogInformation("Created inference node: {InferenceId} ({TaskType})", inferenceId, taskType);
    }

    public async Task CreateKnowledgeNodeAsync(IAsyncSession session, BaseEvent evt, CancellationToken ct)
    {
        var data = evt.Data as Dictionary<string, object>;
        if (data == null) return;

        var docId = data.GetValueOrDefault("doc_id")?.ToString();
        var content = data.GetValueOrDefault("content")?.ToString();
        var category = data.GetValueOrDefault("category")?.ToString();

        if (string.IsNullOrEmpty(docId) || string.IsNullOrEmpty(content)) return;

        var cypher = @"
            MERGE (k:Knowledge {doc_id: $docId})
            SET k.content = $content,
                k.category = $category,
                k.created_at = datetime(),
                k.source_event = $eventId";

        await session.RunAsync(cypher, new
        {
            docId,
            content,
            category,
            eventId = evt.Id
        });

        _logger.LogInformation("Created knowledge node: {DocId} ({Category})", docId, category);
    }

    public async Task CreateGenericEventNodeAsync(IAsyncSession session, BaseEvent evt, CancellationToken ct)
    {
        var cypher = @"
            MERGE (e:Event {event_id: $eventId})
            SET e.event_type = $eventType,
                e.subject = $subject,
                e.source = $source,
                e.data = $data,
                e.extensions = $extensions,
                e.created_at = datetime()";

        await session.RunAsync(cypher, new
        {
            eventId = evt.Id,
            eventType = evt.Type,
            subject = evt.Subject,
            source = evt.Source.ToString(),
            data = JsonSerializer.Serialize(evt.Data),
            extensions = JsonSerializer.Serialize(evt.Extensions)
        });

        _logger.LogInformation("Created generic event node: {EventId} ({EventType})", evt.Id, evt.Type);
    }
}
