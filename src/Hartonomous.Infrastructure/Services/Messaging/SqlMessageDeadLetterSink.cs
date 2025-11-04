using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Messaging;
using Hartonomous.Core.Serialization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Services.Messaging;

public sealed class SqlMessageDeadLetterSink : IMessageDeadLetterSink
{
    private const string InsertCommandText = @"
INSERT INTO dbo.MessageDeadLetters
(
    ConversationHandle,
    MessageType,
    Body,
    EnqueueTimeUtc,
    AttemptCount,
    Reason,
    ExceptionType,
    ExceptionMessage,
    ExceptionStackTrace,
    MetadataJson,
    CreatedAtUtc
)
VALUES
(
    @ConversationHandle,
    @MessageType,
    @Body,
    @EnqueueTimeUtc,
    @AttemptCount,
    @Reason,
    @ExceptionType,
    @ExceptionMessage,
    @ExceptionStackTrace,
    @MetadataJson,
    SYSUTCDATETIME()
);";

    private readonly ISqlServerConnectionFactory _connectionFactory;
    private readonly IJsonSerializer _serializer;
    private readonly ILogger<SqlMessageDeadLetterSink> _logger;

    public SqlMessageDeadLetterSink(
        ISqlServerConnectionFactory connectionFactory,
        IJsonSerializer serializer,
        ILogger<SqlMessageDeadLetterSink> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task WriteAsync(DeadLetterMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = InsertCommandText;
        command.Parameters.Add(new SqlParameter("@ConversationHandle", message.ConversationHandle));
        command.Parameters.Add(new SqlParameter("@MessageType", message.MessageType));
        command.Parameters.Add(new SqlParameter("@Body", (object?)message.Body ?? DBNull.Value));
        command.Parameters.Add(new SqlParameter("@EnqueueTimeUtc", message.EnqueueTime.UtcDateTime));
        command.Parameters.Add(new SqlParameter("@AttemptCount", message.AttemptCount));
        command.Parameters.Add(new SqlParameter("@Reason", message.Reason));
        command.Parameters.Add(new SqlParameter("@ExceptionType", (object?)message.ExceptionType ?? DBNull.Value));
        command.Parameters.Add(new SqlParameter("@ExceptionMessage", (object?)message.ExceptionMessage ?? DBNull.Value));
        command.Parameters.Add(new SqlParameter("@ExceptionStackTrace", (object?)message.ExceptionStackTrace ?? DBNull.Value));

        var metadataJson = message.Metadata is null || message.Metadata.Count == 0
            ? null
            : _serializer.Serialize(message.Metadata);
        command.Parameters.Add(new SqlParameter("@MetadataJson", (object?)metadataJson ?? DBNull.Value));

        var rows = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Persisted dead-letter message {ConversationHandle} ({MessageType}) with attempt count {AttemptCount}. Rows affected: {Rows}", message.ConversationHandle, message.MessageType, message.AttemptCount, rows);
    }
}
