using System;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Services.CDC;

/// <summary>
/// Database-backed implementation of ICdcCheckpointManager
/// Suitable for production and multi-instance deployments
/// </summary>
public class SqlCdcCheckpointManager : ICdcCheckpointManager
{
    private readonly ISqlServerConnectionFactory _connectionFactory;
    private readonly ILogger<SqlCdcCheckpointManager> _logger;
    private readonly string _consumerName;

    public SqlCdcCheckpointManager(
        ISqlServerConnectionFactory connectionFactory,
        ILogger<SqlCdcCheckpointManager> logger,
        string consumerName = "CesConsumer")
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _consumerName = consumerName;
    }

    public async Task<string?> GetLastProcessedLsnAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(
                @"SELECT LastProcessedLsn 
                  FROM dbo.CdcCheckpoints 
                  WHERE ConsumerName = @ConsumerName",
                connection);

            command.Parameters.AddWithValue("@ConsumerName", _consumerName);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            var lsn = result as string;

            _logger.LogDebug("Loaded checkpoint LSN for {ConsumerName}: {Lsn}", _consumerName, lsn ?? "NULL");
            return lsn;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read checkpoint from database, starting from beginning");
            return null;
        }
    }

    public async Task UpdateLastProcessedLsnAsync(string lsn, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lsn))
        {
            throw new ArgumentException("LSN cannot be null or empty", nameof(lsn));
        }

        try
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
            await using var command = new SqlCommand(
                @"MERGE dbo.CdcCheckpoints AS target
                  USING (SELECT @ConsumerName AS ConsumerName, @Lsn AS LastProcessedLsn) AS source
                  ON target.ConsumerName = source.ConsumerName
                  WHEN MATCHED THEN
                      UPDATE SET LastProcessedLsn = source.LastProcessedLsn, LastUpdated = SYSUTCDATETIME()
                  WHEN NOT MATCHED THEN
                      INSERT (ConsumerName, LastProcessedLsn, LastUpdated)
                      VALUES (source.ConsumerName, source.LastProcessedLsn, SYSUTCDATETIME());",
                connection);

            command.Parameters.AddWithValue("@ConsumerName", _consumerName);
            command.Parameters.AddWithValue("@Lsn", lsn);

            await command.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogDebug("Updated checkpoint LSN for {ConsumerName}: {Lsn}", _consumerName, lsn);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update checkpoint in database");
            throw;
        }
    }
}
