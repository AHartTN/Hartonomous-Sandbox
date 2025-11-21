using Azure.Core;
using Azure.Identity;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Interfaces.Streaming;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Infrastructure.Services.Streaming;

/// <summary>
/// SQL Server implementation of stream processing service.
/// Handles real-time event streams and multi-modal fusion.
/// </summary>
public sealed class SqlStreamProcessingService : IStreamProcessingService
{
    private readonly string _connectionString;
    private readonly TokenCredential _credential;
    private readonly ILogger<SqlStreamProcessingService> _logger;

    public SqlStreamProcessingService(
        ILogger<SqlStreamProcessingService> logger,
        IOptions<DatabaseOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var databaseOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _connectionString = databaseOptions.HartonomousDb;
        _credential = new DefaultAzureCredential();
    }

    public async Task<StreamOrchestrationResult> OrchestrateAsync(
        Guid streamId,
        string sensorType,
        int windowSizeMs = 1000,
        int tenantId = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sensorType, nameof(sensorType));

        if (windowSizeMs < 100 || windowSizeMs > 60000)
            throw new ArgumentOutOfRangeException(nameof(windowSizeMs), "WindowSize must be between 100ms and 60000ms");

        _logger.LogInformation(
            "OrchestrateSensorStream: StreamId {StreamId}, Sensor {Sensor}, Window {Window}ms",
            streamId, sensorType, windowSizeMs);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_OrchestrateSensorStream", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 60
        };

        command.Parameters.AddWithValue("@StreamId", streamId);
        command.Parameters.AddWithValue("@SensorType", sensorType);
        command.Parameters.AddWithValue("@WindowSizeMs", windowSizeMs);
        command.Parameters.AddWithValue("@TenantId", tenantId);

        var eventsProcessedParam = new SqlParameter("@EventsProcessed", SqlDbType.Int) { Direction = ParameterDirection.Output };
        var throughputParam = new SqlParameter("@Throughput", SqlDbType.Float) { Direction = ParameterDirection.Output };

        command.Parameters.Add(eventsProcessedParam);
        command.Parameters.Add(throughputParam);

        await command.ExecuteNonQueryAsync(cancellationToken);
        sw.Stop();

        var eventsProcessed = eventsProcessedParam.Value is int events ? events : 0;
        var throughput = throughputParam.Value is double tp ? (float)tp : 0.0f;

        _logger.LogInformation(
            "OrchestrateSensorStream completed: Processed {Events} events, Throughput {Throughput:F2} events/sec",
            eventsProcessed, throughput);

        return new StreamOrchestrationResult(streamId, eventsProcessed, (int)sw.ElapsedMilliseconds, throughput);
    }

    public async Task<MultiModalFusionResult> FuseStreamsAsync(
        string streamIds,
        string fusionStrategy = "attention",
        int synchronizationToleranceMs = 100,
        int tenantId = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamIds, nameof(streamIds));
        ArgumentException.ThrowIfNullOrWhiteSpace(fusionStrategy, nameof(fusionStrategy));

        if (synchronizationToleranceMs < 10 || synchronizationToleranceMs > 1000)
            throw new ArgumentOutOfRangeException(nameof(synchronizationToleranceMs), "Tolerance must be between 10ms and 1000ms");

        _logger.LogInformation(
            "FuseMultiModalStreams: Streams {Streams}, Strategy {Strategy}, Tolerance {Tolerance}ms",
            streamIds, fusionStrategy, synchronizationToleranceMs);

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_FuseMultiModalStreams", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 120
        };

        command.Parameters.AddWithValue("@StreamIds", streamIds);
        command.Parameters.AddWithValue("@FusionStrategy", fusionStrategy);
        command.Parameters.AddWithValue("@SynchronizationToleranceMs", synchronizationToleranceMs);
        command.Parameters.AddWithValue("@TenantId", tenantId);

        var fusedStreamIdParam = new SqlParameter("@FusedStreamId", SqlDbType.UniqueIdentifier) { Direction = ParameterDirection.Output };
        var modalitiesParam = new SqlParameter("@ModalitiesFused", SqlDbType.Int) { Direction = ParameterDirection.Output };
        var syncQualityParam = new SqlParameter("@SyncQuality", SqlDbType.Float) { Direction = ParameterDirection.Output };

        command.Parameters.Add(fusedStreamIdParam);
        command.Parameters.Add(modalitiesParam);
        command.Parameters.Add(syncQualityParam);

        await command.ExecuteNonQueryAsync(cancellationToken);

        var fusedStreamId = fusedStreamIdParam.Value is Guid guid ? guid : Guid.NewGuid();
        var modalities = modalitiesParam.Value is int m ? m : 0;
        var syncQuality = syncQualityParam.Value is double sq ? (float)sq : 0.0f;

        _logger.LogInformation(
            "FuseMultiModalStreams completed: FusedStreamId {FusedStreamId}, Modalities {Modalities}, SyncQuality {Quality:F2}",
            fusedStreamId, modalities, syncQuality);

        return new MultiModalFusionResult(fusedStreamId, modalities, syncQuality);
    }

    public async Task<int> GenerateEventsAsync(
        Guid streamId,
        float eventThreshold = 0.7f,
        int minEventDurationMs = 500,
        CancellationToken cancellationToken = default)
    {
        if (eventThreshold < 0.0f || eventThreshold > 1.0f)
            throw new ArgumentOutOfRangeException(nameof(eventThreshold), "EventThreshold must be between 0.0 and 1.0");

        if (minEventDurationMs < 100 || minEventDurationMs > 60000)
            throw new ArgumentOutOfRangeException(nameof(minEventDurationMs), "MinEventDuration must be between 100ms and 60000ms");

        _logger.LogInformation(
            "GenerateEventsFromStream: StreamId {StreamId}, Threshold {Threshold}, MinDuration {MinDuration}ms",
            streamId, eventThreshold, minEventDurationMs);

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_GenerateEventsFromStream", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 90
        };

        command.Parameters.AddWithValue("@StreamId", streamId);
        command.Parameters.AddWithValue("@EventThreshold", eventThreshold);
        command.Parameters.AddWithValue("@MinEventDurationMs", minEventDurationMs);

        var eventsGeneratedParam = new SqlParameter("@EventsGenerated", SqlDbType.Int) { Direction = ParameterDirection.Output };
        command.Parameters.Add(eventsGeneratedParam);

        await command.ExecuteNonQueryAsync(cancellationToken);

        var eventsGenerated = eventsGeneratedParam.Value is int events ? events : 0;

        _logger.LogInformation("GenerateEventsFromStream completed: Generated {Events} events", eventsGenerated);

        return eventsGenerated;
    }

    private async Task SetupConnectionAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        if (!_connectionString.Contains("Password=", StringComparison.OrdinalIgnoreCase) &&
            !_connectionString.Contains("Integrated Security=true", StringComparison.OrdinalIgnoreCase))
        {
            var tokenRequestContext = new TokenRequestContext(["https://database.windows.net/.default"]);
            var token = await _credential.GetTokenAsync(tokenRequestContext, cancellationToken);
            connection.AccessToken = token.Token;
        }

        await connection.OpenAsync(cancellationToken);
    }
}
