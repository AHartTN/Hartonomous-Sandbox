using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Infrastructure.Atomizers;
using Hartonomous.Infrastructure.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// Background service for processing streaming telemetry data with immediate atomization.
/// Supports batching, real-time alerts, and anomaly detection triggers.
/// </summary>
public class TelemetryStreamProcessor : BackgroundService
{
    private readonly IAtomizer<TelemetryDataPoint> _telemetryAtomizer;
    private readonly AtomBulkInsertService _bulkInsertService;
    private readonly ILogger<TelemetryStreamProcessor> _logger;
    private readonly ConcurrentQueue<TelemetryDataPoint> _dataPointQueue;
    private readonly ConcurrentDictionary<string, DeviceStreamState> _deviceStates;
    private readonly ConcurrentDictionary<string, int> _deviceTenantCache;
    private readonly List<ITelemetryResponseHandler> _responseHandlers;
    
    private const int BatchSize = 100;
    private const int BatchTimeoutMs = 1000; // 1 second max batch time
    private const int MaxQueueSize = 10000;

    public TelemetryStreamProcessor(
        IAtomizer<TelemetryDataPoint> telemetryAtomizer,
        AtomBulkInsertService bulkInsertService,
        IEnumerable<ITelemetryResponseHandler> responseHandlers,
        ILogger<TelemetryStreamProcessor> logger)
    {
        _telemetryAtomizer = telemetryAtomizer;
        _bulkInsertService = bulkInsertService;
        _responseHandlers = responseHandlers.ToList();
        _logger = logger;
        _dataPointQueue = new ConcurrentQueue<TelemetryDataPoint>();
        _deviceStates = new ConcurrentDictionary<string, DeviceStreamState>();
        _deviceTenantCache = new ConcurrentDictionary<string, int>();
    }

    /// <summary>
    /// Enqueue a telemetry data point for processing.
    /// </summary>
    public bool EnqueueDataPoint(TelemetryDataPoint dataPoint)
    {
        if (_dataPointQueue.Count >= MaxQueueSize)
        {
            _logger.LogWarning("Telemetry queue full ({QueueSize}), dropping data point from {DeviceId}", 
                MaxQueueSize, dataPoint.DeviceId);
            return false;
        }

        _dataPointQueue.Enqueue(dataPoint);
        return true;
    }

    /// <summary>
    /// Get current queue statistics.
    /// </summary>
    public StreamStatistics GetStatistics()
    {
        return new StreamStatistics
        {
            QueueSize = _dataPointQueue.Count,
            ActiveDevices = _deviceStates.Count,
            TotalProcessed = _deviceStates.Values.Sum(s => s.DataPointsProcessed),
            TotalErrors = _deviceStates.Values.Sum(s => s.Errors)
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Telemetry stream processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var batch = new List<TelemetryDataPoint>();
                var batchStartTime = DateTime.UtcNow;

                // Collect batch (up to BatchSize or BatchTimeoutMs)
                while (batch.Count < BatchSize && 
                       (DateTime.UtcNow - batchStartTime).TotalMilliseconds < BatchTimeoutMs)
                {
                    if (_dataPointQueue.TryDequeue(out var dataPoint))
                    {
                        batch.Add(dataPoint);
                    }
                    else if (batch.Count == 0)
                    {
                        // Queue empty, wait a bit
                        await Task.Delay(10, stoppingToken);
                    }
                    else
                    {
                        // Have some data, process it
                        break;
                    }
                }

                if (batch.Count > 0)
                {
                    await ProcessBatchAsync(batch, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing telemetry batch");
                await Task.Delay(1000, stoppingToken); // Back off on error
            }
        }

        _logger.LogInformation("Telemetry stream processor stopped");
    }

    private async Task ProcessBatchAsync(List<TelemetryDataPoint> batch, CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            // Atomize all data points in parallel
            var atomizationTasks = batch.Select(async dataPoint =>
            {
                try
                {
                    var source = new SourceMetadata
                    {
                        SourceUri = $"telemetry://{dataPoint.DeviceId}",
                        SourceType = "telemetry-stream",
                        ContentType = "application/json",
                        TenantId = ResolveDeviceTenantId(dataPoint.DeviceId)
                    };

                    var result = await _telemetryAtomizer.AtomizeAsync(dataPoint, source, cancellationToken);

                    // Update device state
                    _deviceStates.AddOrUpdate(
                        dataPoint.DeviceId,
                        _ => new DeviceStreamState
                        {
                            DeviceId = dataPoint.DeviceId,
                            LastDataPoint = dataPoint.Timestamp,
                            DataPointsProcessed = 1
                        },
                        (_, state) =>
                        {
                            state.LastDataPoint = dataPoint.Timestamp;
                            state.DataPointsProcessed++;
                            return state;
                        });

                    // Check for response triggers (anomalies, alerts, etc.)
                    await CheckResponseTriggersAsync(dataPoint, result, cancellationToken);

                    return (dataPoint, result, error: (string?)null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to atomize data point from {DeviceId}", dataPoint.DeviceId);
                    
                    _deviceStates.AddOrUpdate(
                        dataPoint.DeviceId,
                        _ => new DeviceStreamState { DeviceId = dataPoint.DeviceId, Errors = 1 },
                        (_, state) => { state.Errors++; return state; });

                    return (dataPoint, result: (AtomizationResult?)null, error: ex.Message);
                }
            }).ToList();

            var atomizationResults = await Task.WhenAll(atomizationTasks);

            // Bulk insert all atoms
            var allAtoms = atomizationResults
                .Where(r => r.result != null)
                .SelectMany(r => r.result!.Atoms)
                .ToList();

            var allCompositions = atomizationResults
                .Where(r => r.result != null)
                .SelectMany(r => r.result!.Compositions)
                .ToList();

            if (allAtoms.Count > 0)
            {
                var atomIdMap = await _bulkInsertService.BulkInsertAtomsAsync(allAtoms, 0, cancellationToken);
                
                if (allCompositions.Count > 0)
                {
                    await _bulkInsertService.BulkInsertCompositionsAsync(allCompositions, atomIdMap, 0, cancellationToken);
                }
            }

            sw.Stop();

            var successCount = atomizationResults.Count(r => r.result != null);
            var errorCount = atomizationResults.Count(r => r.result == null);

            _logger.LogInformation(
                "Processed telemetry batch: {BatchSize} data points, {Atoms} atoms, {Duration}ms ({Success} success, {Errors} errors)",
                batch.Count, allAtoms.Count, sw.ElapsedMilliseconds, successCount, errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch processing failed");
        }
    }

    private async Task CheckResponseTriggersAsync(
        TelemetryDataPoint dataPoint,
        AtomizationResult result,
        CancellationToken cancellationToken)
    {
        // Check for critical events
        var criticalEvents = dataPoint.Events?
            .Where(e => e.Severity == "Critical" || e.Severity == "Error")
            .ToList();

        if (criticalEvents?.Count > 0)
        {
            foreach (var handler in _responseHandlers)
            {
                try
                {
                    await handler.HandleCriticalEventAsync(dataPoint, criticalEvents, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Response handler failed: {HandlerType}", handler.GetType().Name);
                }
            }
        }

        // Check for anomalous metric values (simple threshold-based for now)
        foreach (var metric in dataPoint.Metrics)
        {
            if (metric.Value is double dblVal || metric.Value is float)
            {
                var numericValue = Convert.ToDouble(metric.Value);
                
                // Get device state to check against historical values
                if (_deviceStates.TryGetValue(dataPoint.DeviceId, out var state))
                {
                    if (!state.MetricHistory.ContainsKey(metric.Name))
                    {
                        state.MetricHistory[metric.Name] = new List<double>();
                    }

                    var history = state.MetricHistory[metric.Name];
                    history.Add(numericValue);

                    // Keep last 100 values
                    if (history.Count > 100)
                        history.RemoveAt(0);

                    // Simple anomaly detection: > 3 standard deviations from mean
                    if (history.Count > 10)
                    {
                        var mean = history.Average();
                        var stdDev = Math.Sqrt(history.Sum(v => Math.Pow(v - mean, 2)) / history.Count);
                        
                        if (Math.Abs(numericValue - mean) > 3 * stdDev)
                        {
                            _logger.LogWarning(
                                "Anomaly detected: {DeviceId} {MetricName} = {Value} (mean={Mean:F2}, stdDev={StdDev:F2})",
                                dataPoint.DeviceId, metric.Name, numericValue, mean, stdDev);

                            // Trigger response handlers
                            foreach (var handler in _responseHandlers)
                            {
                                try
                                {
                                    await handler.HandleAnomalyAsync(dataPoint, metric, mean, stdDev, cancellationToken);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Anomaly handler failed: {HandlerType}", handler.GetType().Name);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Resolve tenant ID from device ID.
    /// In production, this should query a device registry table.
    /// For now, uses a simple convention: deviceId format like "tenant1-device123" or defaults to tenant 1.
    /// </summary>
    private int ResolveDeviceTenantId(string deviceId)
    {
        if (string.IsNullOrEmpty(deviceId))
            return 1; // Default tenant

        // Check cache first for performance
        if (_deviceTenantCache.TryGetValue(deviceId, out var cachedTenantId))
            return cachedTenantId;

        // Parse device ID convention (e.g., "tenant2-device456" → tenant 2)
        var parts = deviceId.Split('-', 2);
        if (parts.Length >= 2 && parts[0].StartsWith("tenant", StringComparison.OrdinalIgnoreCase))
        {
            var tenantStr = parts[0].Substring(6); // Remove "tenant" prefix
            if (int.TryParse(tenantStr, out var parsedTenantId) && parsedTenantId > 0)
            {
                _deviceTenantCache[deviceId] = parsedTenantId;
                return parsedTenantId;
            }
        }

        // Default to tenant 1 if no convention found
        // TODO: Replace with database query to device registry table when available:
        // SELECT TenantId FROM dbo.Device WHERE DeviceId = @deviceId
        var defaultTenantId = 1;
        _deviceTenantCache[deviceId] = defaultTenantId;
        return defaultTenantId;
    }

    private class DeviceStreamState
    {
        public string DeviceId { get; set; } = "";
        public DateTimeOffset LastDataPoint { get; set; }
        public long DataPointsProcessed { get; set; }
        public long Errors { get; set; }
        public Dictionary<string, List<double>> MetricHistory { get; set; } = new();
    }
}

/// <summary>
/// Interface for handling telemetry response triggers (alerts, anomalies, etc.)
/// </summary>
public interface ITelemetryResponseHandler
{
    Task HandleCriticalEventAsync(TelemetryDataPoint dataPoint, List<TelemetryEvent> events, CancellationToken cancellationToken);
    Task HandleAnomalyAsync(TelemetryDataPoint dataPoint, TelemetryMetric metric, double mean, double stdDev, CancellationToken cancellationToken);
}

/// <summary>
/// Telemetry stream statistics.
/// </summary>
public class StreamStatistics
{
    public int QueueSize { get; set; }
    public int ActiveDevices { get; set; }
    public long TotalProcessed { get; set; }
    public long TotalErrors { get; set; }
}
