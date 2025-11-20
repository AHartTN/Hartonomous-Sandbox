using System;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Hartonomous.Api.DTOs.Reasoning;
using Hartonomous.Infrastructure.Atomizers;
using Hartonomous.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Api.Controllers;

/// <summary>
/// Real-time streaming ingestion API using SignalR.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/streaming")]
[Authorize(Policy = "DataIngestion")]
public class StreamingIngestionController : ControllerBase
{
    private readonly StreamingIngestionService _streamingService;
    private readonly ILogger<StreamingIngestionController> _logger;

    public StreamingIngestionController(
        StreamingIngestionService streamingService,
        ILogger<StreamingIngestionController> logger)
    {
        _streamingService = streamingService ?? throw new ArgumentNullException(nameof(streamingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Start a new streaming ingestion session.
    /// </summary>
    [HttpPost("start")]
    public IActionResult StartSession(
        [FromBody] StartSessionRequest request,
        [FromQuery] string? connectionId = null)
    {
        try
        {
            var sessionId = Guid.NewGuid().ToString();
            var session = _streamingService.StartSession(
                sessionId,
                request.StreamType,
                connectionId ?? HttpContext.Connection.Id ?? sessionId);

            return Ok(new
            {
                sessionId,
                streamType = request.StreamType,
                connectionId = session.ConnectionId,
                startTime = session.StartTime
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start streaming session");
            throw new InvalidOperationException(ex.Message);
        }
    }

    /// <summary>
    /// Ingest a telemetry batch.
    /// </summary>
    [HttpPost("telemetry/{sessionId}")]
    public async Task<IActionResult> IngestTelemetry(
        string sessionId,
        [FromBody] TelemetryBatch batch,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _streamingService.ProcessTelemetryBatchAsync(
                sessionId,
                batch,
                cancellationToken);

            return Ok(new
            {
                sessionId,
                atomsProcessed = result.Atoms.Count,
                compositionsCreated = result.Compositions.Count,
                durationMs = result.ProcessingInfo.DurationMs
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest telemetry for session {SessionId}", sessionId);
            throw new InvalidOperationException(ex.Message);
        }
    }

    /// <summary>
    /// Ingest a video frame.
    /// </summary>
    [HttpPost("video/{sessionId}")]
    public async Task<IActionResult> IngestVideoFrame(
        string sessionId,
        [FromBody] VideoFrame frame,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _streamingService.ProcessVideoFrameAsync(
                sessionId,
                frame,
                cancellationToken);

            return Ok(new
            {
                sessionId,
                frameId = frame.FrameId,
                atomsProcessed = result.Atoms.Count,
                compositionsCreated = result.Compositions.Count,
                durationMs = result.ProcessingInfo.DurationMs
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest video frame for session {SessionId}", sessionId);
            throw new InvalidOperationException(ex.Message);
        }
    }

    /// <summary>
    /// Ingest an audio buffer.
    /// </summary>
    [HttpPost("audio/{sessionId}")]
    public async Task<IActionResult> IngestAudioBuffer(
        string sessionId,
        [FromBody] AudioBuffer buffer,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _streamingService.ProcessAudioBufferAsync(
                sessionId,
                buffer,
                cancellationToken);

            return Ok(new
            {
                sessionId,
                bufferId = buffer.BufferId,
                atomsProcessed = result.Atoms.Count,
                compositionsCreated = result.Compositions.Count,
                durationMs = result.ProcessingInfo.DurationMs
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest audio buffer for session {SessionId}", sessionId);
            throw new InvalidOperationException(ex.Message);
        }
    }

    /// <summary>
    /// Get streaming session status.
    /// </summary>
    [HttpGet("status/{sessionId}")]
    public IActionResult GetSessionStatus(string sessionId)
    {
        var status = _streamingService.GetSessionStatus(sessionId);
        if (status == null)
            return NotFound($"Session {sessionId} not found");

        return Ok(status);
    }

    /// <summary>
    /// Get all active streaming sessions.
    /// </summary>
    [HttpGet("sessions")]
    public IActionResult GetAllSessions()
    {
        var sessions = _streamingService.GetAllSessions();
        return Ok(sessions);
    }

    /// <summary>
    /// Stop a streaming session.
    /// </summary>
    [HttpPost("stop/{sessionId}")]
    public async Task<IActionResult> StopSession(string sessionId)
    {
        try
        {
            var summary = await _streamingService.StopSessionAsync(sessionId);
            return Ok(summary);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop session {SessionId}", sessionId);
            throw new InvalidOperationException(ex.Message);
        }
    }
}
