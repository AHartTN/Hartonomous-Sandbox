using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Enums;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Infrastructure.Atomizers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// Manages streaming ingestion sessions for real-time telemetry, video, and audio data.
/// Provides immediate atomization and response via SignalR.
/// </summary>
public class StreamingIngestionService
{
    private readonly IHubContext<IngestionHub> _hubContext;
    private readonly IAtomBulkInsertService _atomBulkInsertService;
    private readonly TelemetryStreamAtomizer _telemetryAtomizer;
    private readonly VideoStreamAtomizer _videoAtomizer;
    private readonly AudioStreamAtomizer _audioAtomizer;
    private readonly ILogger<StreamingIngestionService> _logger;
    
    private readonly ConcurrentDictionary<string, StreamingSession> _activeSessions = new();

    public StreamingIngestionService(
        IHubContext<IngestionHub> hubContext,
        IAtomBulkInsertService atomBulkInsertService,
        TelemetryStreamAtomizer telemetryAtomizer,
        VideoStreamAtomizer videoAtomizer,
        AudioStreamAtomizer audioAtomizer,
        ILogger<StreamingIngestionService> logger)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _atomBulkInsertService = atomBulkInsertService ?? throw new ArgumentNullException(nameof(atomBulkInsertService));
        _telemetryAtomizer = telemetryAtomizer ?? throw new ArgumentNullException(nameof(telemetryAtomizer));
        _videoAtomizer = videoAtomizer ?? throw new ArgumentNullException(nameof(videoAtomizer));
        _audioAtomizer = audioAtomizer ?? throw new ArgumentNullException(nameof(audioAtomizer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public StreamingSession StartSession(string sessionId, StreamType streamType, string connectionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or whitespace.", nameof(sessionId));
        if (string.IsNullOrWhiteSpace(connectionId))
            throw new ArgumentException("Connection ID cannot be null or whitespace.", nameof(connectionId));

        var session = new StreamingSession
        {
            SessionId = sessionId,
            StreamType = streamType,
            State = SessionState.Starting,
            ConnectionId = connectionId,
            StartTime = DateTime.UtcNow,
            CancellationTokenSource = new CancellationTokenSource()
        };

        if (!_activeSessions.TryAdd(sessionId, session))
        {
            throw new InvalidOperationException($"Session {sessionId} already exists");
        }

        session.State = SessionState.Active;
        _logger.LogInformation("Started streaming session {SessionId} of type {StreamType}", sessionId, streamType);
        return session;
    }

    public async Task<AtomizationResult> ProcessTelemetryBatchAsync(
        string sessionId,
        TelemetryBatch batch,
        CancellationToken cancellationToken)
    {
        if (!_activeSessions.TryGetValue(sessionId, out var session))
        {
            throw new InvalidOperationException($"Session {sessionId} not found");
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Atomize telemetry batch
        var source = new SourceMetadata
        {
            SourceType = "Stream",
            SourceUri = $"stream://{sessionId}/{batch.SensorId}",
            ContentType = "application/x-telemetry",
            SizeBytes = batch.Readings.Count * 16, // Approximate
            TenantId = 1 // Default tenant
        };

        var result = await _telemetryAtomizer.AtomizeAsync(batch, source, cancellationToken);

        // Bulk insert atoms
        await _atomBulkInsertService.BulkInsertAsync(result, cancellationToken);

        sw.Stop();

        // Update session stats
        session.TotalAtoms += result.Atoms.Count;
        session.TotalCompositions += result.Compositions.Count;
        session.LastActivityTime = DateTime.UtcNow;

        // Notify client via SignalR
        await _hubContext.Clients.Client(session.ConnectionId).SendAsync(
            "AtomizationProgress",
            new
            {
                sessionId,
                atomsProcessed = result.Atoms.Count,
                totalAtoms = session.TotalAtoms,
                durationMs = sw.ElapsedMilliseconds,
                timestamp = DateTime.UtcNow
            },
            cancellationToken);

        _logger.LogDebug(
            "Processed telemetry batch for session {SessionId}: {AtomCount} atoms in {Duration}ms",
            sessionId, result.Atoms.Count, sw.ElapsedMilliseconds);

        return result;
    }

    public async Task<AtomizationResult> ProcessVideoFrameAsync(
        string sessionId,
        VideoFrame frame,
        CancellationToken cancellationToken)
    {
        if (!_activeSessions.TryGetValue(sessionId, out var session))
        {
            throw new InvalidOperationException($"Session {sessionId} not found");
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Atomize video frame
        var source = new SourceMetadata
        {
            SourceType = "Stream",
            SourceUri = $"stream://{sessionId}/{frame.FrameId}",
            ContentType = "video/x-raw-rgb",
            SizeBytes = frame.PixelData.Length,
            TenantId = 1 // Default tenant
        };

        var result = await _videoAtomizer.AtomizeAsync(frame, source, cancellationToken);

        // Bulk insert atoms
        await _atomBulkInsertService.BulkInsertAsync(result, cancellationToken);

        sw.Stop();

        // Update session stats
        session.TotalAtoms += result.Atoms.Count;
        session.TotalCompositions += result.Compositions.Count;
        session.LastActivityTime = DateTime.UtcNow;

        // Notify client via SignalR
        await _hubContext.Clients.Client(session.ConnectionId).SendAsync(
            "AtomizationProgress",
            new
            {
                sessionId,
                atomsProcessed = result.Atoms.Count,
                totalAtoms = session.TotalAtoms,
                durationMs = sw.ElapsedMilliseconds,
                frameId = frame.FrameId,
                timestamp = DateTime.UtcNow
            },
            cancellationToken);

        _logger.LogDebug(
            "Processed video frame for session {SessionId}: {AtomCount} atoms in {Duration}ms",
            sessionId, result.Atoms.Count, sw.ElapsedMilliseconds);

        return result;
    }

    public async Task<AtomizationResult> ProcessAudioBufferAsync(
        string sessionId,
        AudioBuffer buffer,
        CancellationToken cancellationToken)
    {
        if (!_activeSessions.TryGetValue(sessionId, out var session))
        {
            throw new InvalidOperationException($"Session {sessionId} not found");
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Atomize audio buffer
        var source = new SourceMetadata
        {
            SourceType = "Stream",
            SourceUri = $"stream://{sessionId}/{buffer.BufferId}",
            ContentType = $"audio/x-raw-int{buffer.BitsPerSample}",
            SizeBytes = buffer.Samples.Length,
            TenantId = 1 // Default tenant
        };

        var result = await _audioAtomizer.AtomizeAsync(buffer, source, cancellationToken);

        // Bulk insert atoms
        await _atomBulkInsertService.BulkInsertAsync(result, cancellationToken);

        sw.Stop();

        // Update session stats
        session.TotalAtoms += result.Atoms.Count;
        session.TotalCompositions += result.Compositions.Count;
        session.LastActivityTime = DateTime.UtcNow;

        // Notify client via SignalR
        await _hubContext.Clients.Client(session.ConnectionId).SendAsync(
            "AtomizationProgress",
            new
            {
                sessionId,
                atomsProcessed = result.Atoms.Count,
                totalAtoms = session.TotalAtoms,
                durationMs = sw.ElapsedMilliseconds,
                bufferId = buffer.BufferId,
                timestamp = DateTime.UtcNow
            },
            cancellationToken);

        _logger.LogDebug(
            "Processed audio buffer for session {SessionId}: {AtomCount} atoms in {Duration}ms",
            sessionId, result.Atoms.Count, sw.ElapsedMilliseconds);

        return result;
    }

    public async Task<SessionSummary> StopSessionAsync(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or whitespace.", nameof(sessionId));

        if (!_activeSessions.TryRemove(sessionId, out var session))
        {
            throw new InvalidOperationException($"Session {sessionId} not found");
        }

        session.State = SessionState.Stopping;
        session.CancellationTokenSource.Cancel();
        session.EndTime = DateTime.UtcNow;
        session.State = SessionState.Completed;

        var summary = new SessionSummary
        {
            SessionId = sessionId,
            StreamType = session.StreamType,
            State = session.State,
            StartTime = session.StartTime,
            EndTime = session.EndTime.Value,
            TotalAtoms = session.TotalAtoms,
            TotalCompositions = session.TotalCompositions,
            DurationSeconds = (session.EndTime.Value - session.StartTime).TotalSeconds
        };

        // Notify client
        await _hubContext.Clients.Client(session.ConnectionId).SendAsync(
            "SessionEnded",
            summary);

        _logger.LogInformation(
            "Ended streaming session {SessionId}: {TotalAtoms} atoms, {Duration:F2}s",
            sessionId, summary.TotalAtoms, summary.DurationSeconds);

        return summary;
    }

    public SessionStatus? GetSessionStatus(string sessionId)
    {
        if (!_activeSessions.TryGetValue(sessionId, out var session))
        {
            return null;
        }

        return new SessionStatus
        {
            SessionId = sessionId,
            StreamType = session.StreamType,
            State = session.State,
            StartTime = session.StartTime,
            TotalAtoms = session.TotalAtoms,
            TotalCompositions = session.TotalCompositions,
            DurationSeconds = (DateTime.UtcNow - session.StartTime).TotalSeconds,
            IsActive = session.State == SessionState.Active
        };
    }

    public IEnumerable<SessionStatus> GetAllSessions()
    {
        return _activeSessions.Values.Select(s => new SessionStatus
        {
            SessionId = s.SessionId,
            StreamType = s.StreamType,
            State = s.State,
            StartTime = s.StartTime,
            TotalAtoms = s.TotalAtoms,
            TotalCompositions = s.TotalCompositions,
            DurationSeconds = (DateTime.UtcNow - s.StartTime).TotalSeconds,
            IsActive = s.State == SessionState.Active
        });
    }
}

public class StreamingSession
{
    public required string SessionId { get; set; }
    public required StreamType StreamType { get; set; }
    public required SessionState State { get; set; }
    public required string ConnectionId { get; set; }
    public required DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public required CancellationTokenSource CancellationTokenSource { get; set; }
    public long TotalAtoms { get; set; }
    public long TotalCompositions { get; set; }
    public DateTime LastActivityTime { get; set; }
}

public class SessionSummary
{
    public required string SessionId { get; set; }
    public required StreamType StreamType { get; set; }
    public required SessionState State { get; set; }
    public required DateTime StartTime { get; set; }
    public required DateTime EndTime { get; set; }
    public required long TotalAtoms { get; set; }
    public required long TotalCompositions { get; set; }
    public required double DurationSeconds { get; set; }
}

public class SessionStatus
{
    public required string SessionId { get; set; }
    public required StreamType StreamType { get; set; }
    public required SessionState State { get; set; }
    public required DateTime StartTime { get; set; }
    public required long TotalAtoms { get; set; }
    public required long TotalCompositions { get; set; }
    public required double DurationSeconds { get; set; }
    public required bool IsActive { get; set; }
}

public enum StreamType
{
    Telemetry,
    Video,
    Audio,
    Mixed
}
