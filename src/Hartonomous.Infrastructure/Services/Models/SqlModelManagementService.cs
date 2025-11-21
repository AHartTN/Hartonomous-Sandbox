using Azure.Core;
using Azure.Identity;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Interfaces.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Infrastructure.Services.Models;

/// <summary>
/// SQL Server implementation of model management service.
/// Provides weight snapshot and versioning capabilities.
/// </summary>
public sealed class SqlModelManagementService : IModelManagementService
{
    private readonly string _connectionString;
    private readonly TokenCredential _credential;
    private readonly ILogger<SqlModelManagementService> _logger;

    public SqlModelManagementService(
        ILogger<SqlModelManagementService> logger,
        IOptions<DatabaseOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var databaseOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _connectionString = databaseOptions.HartonomousDb;
        _credential = new DefaultAzureCredential();
    }

    public async Task<WeightSnapshotResult> CreateSnapshotAsync(
        int modelId,
        string snapshotName,
        string? description = null,
        int tenantId = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(modelId, nameof(modelId));
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshotName, nameof(snapshotName));

        _logger.LogInformation(
            "CreateWeightSnapshot: ModelId {ModelId}, Name {Name}, TenantId {TenantId}",
            modelId, snapshotName, tenantId);

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_CreateWeightSnapshot", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 180 // 3 minutes for large models
        };

        command.Parameters.AddWithValue("@ModelId", modelId);
        command.Parameters.AddWithValue("@SnapshotName", snapshotName);
        command.Parameters.AddWithValue("@Description", (object?)description ?? DBNull.Value);
        command.Parameters.AddWithValue("@TenantId", tenantId);

        var snapshotIdParam = new SqlParameter("@SnapshotId", SqlDbType.Int) { Direction = ParameterDirection.Output };
        var weightsCapturedParam = new SqlParameter("@WeightsCaptured", SqlDbType.Int) { Direction = ParameterDirection.Output };
        var sizeBytesParam = new SqlParameter("@SizeBytes", SqlDbType.BigInt) { Direction = ParameterDirection.Output };

        command.Parameters.Add(snapshotIdParam);
        command.Parameters.Add(weightsCapturedParam);
        command.Parameters.Add(sizeBytesParam);

        await command.ExecuteNonQueryAsync(cancellationToken);

        var snapshotId = snapshotIdParam.Value is int id ? id : 0;
        var weightsCaptured = weightsCapturedParam.Value is int weights ? weights : 0;
        var sizeBytes = sizeBytesParam.Value is long size ? size : 0L;

        _logger.LogInformation(
            "CreateWeightSnapshot completed: SnapshotId {SnapshotId}, Weights {Weights}, Size {Size} bytes",
            snapshotId, weightsCaptured, sizeBytes);

        return new WeightSnapshotResult(snapshotId, snapshotName, weightsCaptured, sizeBytes, DateTime.UtcNow);
    }

    public async Task<IEnumerable<SnapshotInfo>> ListSnapshotsAsync(
        int modelId = 0,
        int tenantId = 0,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ListWeightSnapshots: ModelId {ModelId}, TenantId {TenantId}", modelId, tenantId);

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_ListWeightSnapshots", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 30
        };

        command.Parameters.AddWithValue("@ModelId", modelId);
        command.Parameters.AddWithValue("@TenantId", tenantId);

        var snapshots = new List<SnapshotInfo>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            snapshots.Add(new SnapshotInfo(
                reader.GetInt32(reader.GetOrdinal("SnapshotId")),
                reader.GetInt32(reader.GetOrdinal("ModelId")),
                reader.GetString(reader.GetOrdinal("SnapshotName")),
                reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                reader.GetInt32(reader.GetOrdinal("WeightCount")),
                reader.GetInt64(reader.GetOrdinal("SizeBytes"))));
        }

        _logger.LogInformation("ListWeightSnapshots found {Count} snapshots", snapshots.Count);

        return snapshots;
    }

    public async Task RestoreSnapshotAsync(
        int snapshotId,
        int modelId,
        int tenantId = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(snapshotId, nameof(snapshotId));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(modelId, nameof(modelId));

        _logger.LogInformation(
            "RestoreWeightSnapshot: SnapshotId {SnapshotId}, ModelId {ModelId}, TenantId {TenantId}",
            snapshotId, modelId, tenantId);

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_RestoreWeightSnapshot", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 180 // 3 minutes for large models
        };

        command.Parameters.AddWithValue("@SnapshotId", snapshotId);
        command.Parameters.AddWithValue("@ModelId", modelId);
        command.Parameters.AddWithValue("@TenantId", tenantId);

        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("RestoreWeightSnapshot completed successfully");
    }

    public async Task RollbackToTimestampAsync(
        int modelId,
        DateTime targetTimestamp,
        int tenantId = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(modelId, nameof(modelId));

        if (targetTimestamp > DateTime.UtcNow)
            throw new ArgumentException("Cannot rollback to future timestamp", nameof(targetTimestamp));

        _logger.LogInformation(
            "RollbackWeightsToTimestamp: ModelId {ModelId}, Timestamp {Timestamp}, TenantId {TenantId}",
            modelId, targetTimestamp, tenantId);

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_RollbackWeightsToTimestamp", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 180
        };

        command.Parameters.AddWithValue("@ModelId", modelId);
        command.Parameters.AddWithValue("@TargetTimestamp", targetTimestamp);
        command.Parameters.AddWithValue("@TenantId", tenantId);

        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("RollbackWeightsToTimestamp completed successfully");
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
