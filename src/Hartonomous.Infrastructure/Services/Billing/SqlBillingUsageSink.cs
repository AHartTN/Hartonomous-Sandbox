using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Billing;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Serialization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Services.Billing;

/// <summary>
/// SQL-based sink for writing billing usage records to the database.
/// Uses natively compiled In-Memory OLTP stored procedure (sp_InsertBillingUsageRecord_Native) for 2-10x performance improvement.
/// </summary>
public sealed class SqlBillingUsageSink : IBillingUsageSink
{
    // Using In-Memory OLTP natively compiled procedure for 2-10x performance improvement
    private const string InsertProcedureName = "dbo.sp_InsertBillingUsageRecord_Native";

    private readonly ISqlServerConnectionFactory _connectionFactory;
    private readonly IJsonSerializer _serializer;
    private readonly ILogger<SqlBillingUsageSink> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlBillingUsageSink"/> class.
    /// </summary>
    /// <param name="connectionFactory">Factory for creating SQL Server connections.</param>
    /// <param name="serializer">JSON serializer for metadata serialization.</param>
    /// <param name="logger">Logger for tracking write operations and errors.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public SqlBillingUsageSink(
        ISqlServerConnectionFactory connectionFactory,
        IJsonSerializer serializer,
        ILogger<SqlBillingUsageSink> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Writes a billing usage record to the database using a natively compiled In-Memory OLTP stored procedure.
    /// Serializes metadata as JSON before inserting.
    /// </summary>
    /// <param name="record">The billing usage record to persist.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="record"/> is null.</exception>
    public async Task WriteAsync(BillingUsageRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = InsertProcedureName;
        command.Parameters.Add(new SqlParameter("@TenantId", record.TenantId));
        command.Parameters.Add(new SqlParameter("@PrincipalId", record.PrincipalId));
        command.Parameters.Add(new SqlParameter("@Operation", record.Operation));
        command.Parameters.Add(new SqlParameter("@MessageType", (object?)record.MessageType ?? DBNull.Value));
        command.Parameters.Add(new SqlParameter("@Handler", (object?)record.Handler ?? DBNull.Value));
        command.Parameters.Add(new SqlParameter("@Units", SqlDbType.Decimal)
        {
            Precision = 18,
            Scale = 6,
            Value = record.Units
        });
        command.Parameters.Add(new SqlParameter("@BaseRate", SqlDbType.Decimal)
        {
            Precision = 18,
            Scale = 6,
            Value = record.BaseRate
        });
        command.Parameters.Add(new SqlParameter("@Multiplier", SqlDbType.Decimal)
        {
            Precision = 18,
            Scale = 6,
            Value = record.Multiplier
        });
        command.Parameters.Add(new SqlParameter("@TotalCost", SqlDbType.Decimal)
        {
            Precision = 18,
            Scale = 6,
            Value = record.TotalCost
        });

        var metadataJson = record.Metadata.Count == 0
            ? null
            : _serializer.Serialize(record.Metadata);
        command.Parameters.Add(new SqlParameter("@MetadataJson", (object?)metadataJson ?? DBNull.Value));

        var rows = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Persisted billing usage record via In-Memory OLTP for tenant {TenantId}, operation {Operation}. Rows affected: {Rows}", record.TenantId, record.Operation, rows);
    }
}
