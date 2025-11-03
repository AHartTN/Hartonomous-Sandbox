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

public sealed class SqlBillingUsageSink : IBillingUsageSink
{
    private const string InsertCommandText = @"
INSERT INTO dbo.BillingUsageLedger
(
    TenantId,
    PrincipalId,
    Operation,
    MessageType,
    Handler,
    Units,
    BaseRate,
    Multiplier,
    TotalCost,
    MetadataJson,
    TimestampUtc
)
VALUES
(
    @TenantId,
    @PrincipalId,
    @Operation,
    @MessageType,
    @Handler,
    @Units,
    @BaseRate,
    @Multiplier,
    @TotalCost,
    @MetadataJson,
    SYSUTCDATETIME()
);";

    private readonly ISqlServerConnectionFactory _connectionFactory;
    private readonly IJsonSerializer _serializer;
    private readonly ILogger<SqlBillingUsageSink> _logger;

    public SqlBillingUsageSink(
        ISqlServerConnectionFactory connectionFactory,
        IJsonSerializer serializer,
        ILogger<SqlBillingUsageSink> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task WriteAsync(BillingUsageRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = InsertCommandText;
        command.Parameters.Add(new SqlParameter("@TenantId", record.TenantId));
        command.Parameters.Add(new SqlParameter("@PrincipalId", record.PrincipalId));
        command.Parameters.Add(new SqlParameter("@Operation", record.Operation));
        command.Parameters.Add(new SqlParameter("@MessageType", record.MessageType));
        command.Parameters.Add(new SqlParameter("@Handler", record.Handler));
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
        _logger.LogDebug("Persisted billing usage record for tenant {TenantId}, operation {Operation}. Rows affected: {Rows}", record.TenantId, record.Operation, rows);
    }
}
