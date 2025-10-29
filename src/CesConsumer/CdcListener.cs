using Hartonomous.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using System.Collections.Generic;

namespace CesConsumer
{
    /// <summary>
    /// Processes Change Event Streaming (CES) events from SQL Server 2025
    /// Performs semantic enrichment and publishes to message queue
    /// </summary>
    public class CdcListener
    {
        private readonly ICdcRepository _cdcRepository;
        private readonly EventHubProducerClient _eventHubProducer;
        private readonly ILogger<CdcListener> _logger;
        private readonly string _lastProcessedLsnKey = "LastProcessedLsn";

        public CdcListener(
            ICdcRepository cdcRepository,
            ILogger<CdcListener> logger,
            string eventHubConnectionString,
            string eventHubName)
        {
            _cdcRepository = cdcRepository;
            _logger = logger;
            _eventHubProducer = new EventHubProducerClient(eventHubConnectionString, eventHubName);
        }

        public async Task StartListeningAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting CES Consumer with CloudEvent processing...");
            _logger.LogInformation("Event Hub: Connected and ready");

            // Get last processed LSN to avoid reprocessing
            var lastLsn = await GetLastProcessedLsnAsync(cancellationToken);
            _logger.LogInformation("Starting from LSN: {LastLsn}", lastLsn ?? "Beginning");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessChangeEventsAsync(lastLsn, cancellationToken);
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken); // Poll every second
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing change events");
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }
            }
        }

        private async Task ProcessChangeEventsAsync(string? lastLsn, CancellationToken cancellationToken)
        {
            // Get change events from repository
            var changeEvents = await _cdcRepository.GetChangeEventsSinceAsync(lastLsn, cancellationToken);

            var events = new List<CloudEvent>();
            string? maxLsn = null;

            foreach (var changeEvent in changeEvents)
            {
                maxLsn = changeEvent.Lsn;

                // Create CloudEvent
                var cloudEvent = new CloudEvent
                {
                    Id = Guid.NewGuid().ToString(),
                    Source = new Uri($"/sqlserver/{Environment.MachineName}/Hartonomous"),
                    Type = GetCloudEventType(changeEvent.Operation),
                    Time = DateTimeOffset.UtcNow,
                    Subject = $"{changeEvent.TableName}/lsn:{changeEvent.Lsn}",
                    DataSchema = new Uri("https://schemas.microsoft.com/sqlserver/2025/ces"),
                    Data = changeEvent.Data
                };

                // Add SQL Server specific extensions
                cloudEvent.Extensions["sqlserver"] = new
                {
                    operation = GetOperationName(changeEvent.Operation),
                    table = changeEvent.TableName,
                    lsn = changeEvent.Lsn,
                    database = "Hartonomous",
                    server = Environment.MachineName
                };

                events.Add(cloudEvent);
            }

            // Process events in batches
            if (events.Count > 0)
            {
                await ProcessEventsBatchAsync(events, cancellationToken);

                // Update last processed LSN
                if (maxLsn != null)
                {
                    await UpdateLastProcessedLsnAsync(maxLsn, cancellationToken);
                }

                _logger.LogInformation("Processed {Count} change events, new LSN: {MaxLsn}", events.Count, maxLsn);
            }
        }

        private async Task ProcessEventsBatchAsync(List<CloudEvent> events, CancellationToken cancellationToken)
        {
            // Perform semantic enrichment
            foreach (var cloudEvent in events)
            {
                await EnrichEventAsync(cloudEvent, cancellationToken);
            }

            // Publish to Event Hub in batches
            var eventBatch = await _eventHubProducer.CreateBatchAsync(cancellationToken);
            var batchCount = 0;

            foreach (var cloudEvent in events)
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                var eventData = new EventData(JsonSerializer.Serialize(cloudEvent, options));
                eventData.ContentType = "application/cloudevents+json";

                if (!eventBatch.TryAdd(eventData))
                {
                    // Batch is full, send it and create a new one
                    await _eventHubProducer.SendAsync(eventBatch, cancellationToken);
                    batchCount++;

                    eventBatch = await _eventHubProducer.CreateBatchAsync(cancellationToken);
                    eventBatch.TryAdd(eventData);
                }
            }

            // Send remaining events
            if (eventBatch.Count > 0)
            {
                await _eventHubProducer.SendAsync(eventBatch, cancellationToken);
                batchCount++;
            }

            _logger.LogInformation("Published {Count} enriched CloudEvents to Event Hub in {BatchCount} batches", events.Count, batchCount);
        }

        private async Task EnrichEventAsync(CloudEvent cloudEvent, CancellationToken cancellationToken)
        {
            // Perform semantic enrichment based on event type
            var sqlExtensions = cloudEvent.Extensions["sqlserver"] as Dictionary<string, object>;
            var operation = sqlExtensions?["operation"] as string ?? "";
            var table = sqlExtensions?["table"] as string ?? "";

            if (table == "dbo.Models" && operation == "insert")
            {
                // For new models, add metadata about capabilities
                var data = cloudEvent.Data as Dictionary<string, object>;
                if (data != null && data.TryGetValue("model_name", out var modelNameObj) && modelNameObj is string modelName)
                {
                    cloudEvent.Extensions["semantic"] = new Dictionary<string, object>
                    {
                        ["inferred_capabilities"] = InferModelCapabilities(modelName),
                        ["content_type"] = ClassifyModelType(modelName),
                        ["expected_performance"] = EstimateModelPerformance(modelName),
                        ["compliance_requirements"] = GetComplianceRequirements(modelName)
                    };
                }
            }
            else if (table == "dbo.InferenceRequests" && operation == "insert")
            {
                // For inference requests, add reasoning context
                var data = cloudEvent.Data as Dictionary<string, object>;
                if (data != null && data.TryGetValue("task_type", out var taskTypeObj) && taskTypeObj is string taskType)
                {
                    cloudEvent.Extensions["reasoning"] = new Dictionary<string, object>
                    {
                        ["reasoning_mode"] = DetermineReasoningMode(taskType),
                        ["expected_complexity"] = EstimateTaskComplexity(taskType),
                        ["audit_trail_required"] = RequiresAuditTrail(taskType),
                        ["performance_sla"] = GetPerformanceSLA(taskType)
                    };
                }
            }

            // Add general enrichment
            cloudEvent.Extensions["enrichment"] = new Dictionary<string, object>
            {
                ["processed_at"] = DateTimeOffset.UtcNow,
                ["processor_version"] = "1.0.0",
                ["enrichment_level"] = "semantic",
                ["confidence_score"] = 0.95
            };
        }

        private string GetCloudEventType(int operation)
        {
            return operation switch
            {
                1 => "com.microsoft.sqlserver.cdc.delete",
                2 => "com.microsoft.sqlserver.cdc.insert",
                3 => "com.microsoft.sqlserver.cdc.update.before",
                4 => "com.microsoft.sqlserver.cdc.update.after",
                _ => "com.microsoft.sqlserver.cdc.unknown"
            };
        }

        private string GetOperationName(int operation)
        {
            return operation switch
            {
                1 => "delete",
                2 => "insert",
                3 => "update_before",
                4 => "update_after",
                _ => "unknown"
            };
        }

        private string[] InferModelCapabilities(string modelName)
        {
            var capabilities = new List<string>();

            if (modelName.Contains("llama", StringComparison.OrdinalIgnoreCase) ||
                modelName.Contains("gpt", StringComparison.OrdinalIgnoreCase))
            {
                capabilities.AddRange(new[] { "text_generation", "question_answering", "summarization" });
            }

            if (modelName.Contains("clip", StringComparison.OrdinalIgnoreCase) ||
                modelName.Contains("vision", StringComparison.OrdinalIgnoreCase))
            {
                capabilities.AddRange(new[] { "image_classification", "image_captioning", "visual_question_answering" });
            }

            if (modelName.Contains("wav2vec", StringComparison.OrdinalIgnoreCase) ||
                modelName.Contains("whisper", StringComparison.OrdinalIgnoreCase))
            {
                capabilities.AddRange(new[] { "speech_recognition", "audio_classification" });
            }

            return capabilities.ToArray();
        }

        private string ClassifyModelType(string modelName)
        {
            if (modelName.Contains("llama", StringComparison.OrdinalIgnoreCase) ||
                modelName.Contains("gpt", StringComparison.OrdinalIgnoreCase))
                return "text";

            if (modelName.Contains("clip", StringComparison.OrdinalIgnoreCase) ||
                modelName.Contains("vit", StringComparison.OrdinalIgnoreCase))
                return "vision";

            if (modelName.Contains("wav2vec", StringComparison.OrdinalIgnoreCase) ||
                modelName.Contains("whisper", StringComparison.OrdinalIgnoreCase))
                return "audio";

            return "multimodal";
        }

        private double EstimateModelPerformance(string modelName)
        {
            // Rough performance estimation based on model size indicators
            if (modelName.Contains("7b", StringComparison.OrdinalIgnoreCase) ||
                modelName.Contains("large", StringComparison.OrdinalIgnoreCase))
                return 0.85;

            if (modelName.Contains("1b", StringComparison.OrdinalIgnoreCase) ||
                modelName.Contains("base", StringComparison.OrdinalIgnoreCase))
                return 0.75;

            if (modelName.Contains("small", StringComparison.OrdinalIgnoreCase) ||
                modelName.Contains("tiny", StringComparison.OrdinalIgnoreCase))
                return 0.65;

            return 0.70; // Default
        }

        private string[] GetComplianceRequirements(string modelName)
        {
            var requirements = new List<string> { "data_privacy" }; // All models need this

            if (modelName.Contains("medical", StringComparison.OrdinalIgnoreCase) ||
                modelName.Contains("health", StringComparison.OrdinalIgnoreCase))
            {
                requirements.AddRange(new[] { "hipaa", "gdpr", "health_data_protection" });
            }

            if (modelName.Contains("financial", StringComparison.OrdinalIgnoreCase))
            {
                requirements.AddRange(new[] { "pci_dss", "sox", "financial_regulation" });
            }

            return requirements.ToArray();
        }

        private string DetermineReasoningMode(string taskType)
        {
            return taskType.ToLowerInvariant() switch
            {
                "text_generation" => "generative",
                "question_answering" => "analytical",
                "classification" => "categorical",
                "summarization" => "synthetic",
                "translation" => "transformational",
                _ => "general"
            };
        }

        private string EstimateTaskComplexity(string taskType)
        {
            return taskType.ToLowerInvariant() switch
            {
                "text_generation" => "high",
                "question_answering" => "medium",
                "classification" => "low",
                "summarization" => "medium",
                "translation" => "medium",
                _ => "medium"
            };
        }

        private bool RequiresAuditTrail(string taskType)
        {
            return taskType.ToLowerInvariant() switch
            {
                "medical_diagnosis" => true,
                "financial_decision" => true,
                "legal_analysis" => true,
                "safety_critical" => true,
                _ => false
            };
        }

        private TimeSpan GetPerformanceSLA(string taskType)
        {
            return taskType.ToLowerInvariant() switch
            {
                "real_time" => TimeSpan.FromMilliseconds(100),
                "interactive" => TimeSpan.FromSeconds(1),
                "batch" => TimeSpan.FromMinutes(5),
                _ => TimeSpan.FromSeconds(3)
            };
        }

        private async Task<string?> GetLastProcessedLsnAsync(CancellationToken cancellationToken)
        {
            // In production, this would be stored in a persistent store
            // For now, we'll use a simple file-based approach
            try
            {
                if (File.Exists("last_processed_lsn.txt"))
                {
                    return await File.ReadAllTextAsync("last_processed_lsn.txt", cancellationToken);
                }
            }
            catch
            {
                // Ignore errors
            }
            return null;
        }

        private async Task UpdateLastProcessedLsnAsync(string lsn, CancellationToken cancellationToken)
        {
            try
            {
                await File.WriteAllTextAsync("last_processed_lsn.txt", lsn, cancellationToken);
            }
            catch
            {
                // Ignore errors in demo
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _eventHubProducer.CloseAsync(cancellationToken);
        }
    }

    /// <summary>
    /// CloudEvent representation following CNCF CloudEvents specification
    /// </summary>
    public class CloudEvent
    {
        public string Id { get; set; } = string.Empty;
        public Uri Source { get; set; } = null!;
        public string Type { get; set; } = string.Empty;
        public DateTimeOffset Time { get; set; }
        public string? Subject { get; set; }
        public Uri? DataSchema { get; set; }
        public object? Data { get; set; }
        public Dictionary<string, object> Extensions { get; set; } = new();
    }
}
