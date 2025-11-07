using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Hartonomous.Core.Abstracts;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Messaging;
using Hartonomous.Core.Serialization;
using Hartonomous.Core.Resilience;
using Hartonomous.Core.Telemetry;
using Hartonomous.Infrastructure.Data.Extensions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hartonomous.Infrastructure.Services.Messaging;

/// <summary>
/// SQL Server Service Broker implementation of <see cref="IMessageBroker"/>.
/// Handles one-shot conversations per message and exposes simple commit/abandon semantics.
/// </summary>
public sealed class SqlMessageBroker : IMessageBroker
{
    private readonly ISqlServerConnectionFactory _connectionFactory;
    private readonly ILogger<SqlMessageBroker> _logger;
    private readonly MessageBrokerOptions _options;
    private readonly IJsonSerializer _serializer;
    private readonly IServiceBrokerResilienceStrategy _resilienceStrategy;
    private readonly int _commandTimeoutSeconds;
    private readonly string _sendCommandText;
    private readonly string _receiveCommandText;

    public SqlMessageBroker(
        ISqlServerConnectionFactory connectionFactory,
        IJsonSerializer serializer,
        IOptions<MessageBrokerOptions> brokerOptions,
        IOptionsMonitor<SqlServerOptions> sqlServerOptions,
        IServiceBrokerResilienceStrategy resilienceStrategy,
        ILogger<SqlMessageBroker> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _resilienceStrategy = resilienceStrategy ?? throw new ArgumentNullException(nameof(resilienceStrategy));
        _options = brokerOptions?.Value ?? throw new ArgumentNullException(nameof(brokerOptions));
        _commandTimeoutSeconds = Math.Max(5, sqlServerOptions?.CurrentValue?.CommandTimeoutSeconds ?? 30);

        ValidateIdentifiers();
        var commands = ServiceBrokerCommandBuilder.BuildCommands(_options);
        _sendCommandText = commands.SendCommand;
        _receiveCommandText = commands.ReceiveCommand;
    }

    [RequiresUnreferencedCode("JSON serialization uses reflection.")]
    [RequiresDynamicCode("JSON serialization uses reflection.")]
    public async Task PublishAsync<TPayload>(TPayload payload, CancellationToken cancellationToken = default) where TPayload : class
    {
        ArgumentNullException.ThrowIfNull(payload);

        var body = _serializer.Serialize(payload);
        GuardMessageLength(body);

        MessagingTelemetry.RecordPublishAttempt(_options.MessageTypeName, _options.TargetServiceName);
        using var activity = MessagingTelemetry.StartActivity(
            name: "ServiceBroker.Publish",
            kind: ActivityKind.Producer,
            tags: new[]
            {
                new KeyValuePair<string, object?>("messaging.system", "mssql"),
                new KeyValuePair<string, object?>("messaging.destination", _options.TargetServiceName),
                new KeyValuePair<string, object?>("messaging.message_type", _options.MessageTypeName)
            });

        try
        {
            await _resilienceStrategy.ExecutePublishAsync(async ct =>
            {
                await using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct).ConfigureAwait(false);
                await using var command = connection.CreateCommand();
                command.CommandType = CommandType.Text;
                command.CommandText = _sendCommandText;
                command.CommandTimeout = _commandTimeoutSeconds;
                command.Parameters.Add(new SqlParameter("@messageBody", SqlDbType.NVarChar, -1) { Value = body });
                command.Parameters.Add(new SqlParameter("@lifetimeSeconds", SqlDbType.Int) { Value = _options.ConversationLifetimeSeconds });

                await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);

            activity?.SetStatus(ActivityStatusCode.Ok);
            MessagingTelemetry.RecordPublishSuccess(_options.MessageTypeName, _options.TargetServiceName);
            _logger.LogDebug("Published payload type {PayloadType} via SQL Service Broker", typeof(TPayload).Name);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            MessagingTelemetry.RecordPublishFailure(_options.MessageTypeName, _options.TargetServiceName, ex);
            _logger.LogError(ex, "Failed to publish payload type {PayloadType} via SQL Service Broker", typeof(TPayload).Name);
            throw;
        }
    }

    [RequiresUnreferencedCode("JSON serialization uses reflection.")]
    [RequiresDynamicCode("JSON serialization uses reflection.")]
    public async Task PublishBatchAsync<TPayload>(IEnumerable<TPayload> payloads, CancellationToken cancellationToken = default) where TPayload : class
    {
        ArgumentNullException.ThrowIfNull(payloads);

        foreach (var payload in payloads)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await PublishAsync(payload, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<BrokeredMessage?> ReceiveAsync(TimeSpan waitTime, CancellationToken cancellationToken = default)
    {
        var timeoutMs = ResolveTimeout(waitTime);
        MessagingTelemetry.RecordReceiveAttempt(_options.QueueName);
        using var activity = MessagingTelemetry.StartActivity(
            name: "ServiceBroker.Receive",
            kind: ActivityKind.Consumer,
            tags: new[]
            {
                new KeyValuePair<string, object?>("messaging.system", "mssql"),
                new KeyValuePair<string, object?>("messaging.destination", _options.QueueName)
            });

        try
        {
            var brokeredMessage = await _resilienceStrategy.ExecuteReceiveAsync(async ct =>
            {
                return await ReceiveInternalAsync(timeoutMs, ct).ConfigureAwait(false);
            }, cancellationToken).ConfigureAwait(false);

            if (brokeredMessage != null)
            {
                activity?.SetTag("messaging.message_type", brokeredMessage.MessageType);
                activity?.SetStatus(ActivityStatusCode.Ok);
                MessagingTelemetry.RecordReceiveSuccess(brokeredMessage.MessageType, _options.QueueName);
            }

            return brokeredMessage;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            MessagingTelemetry.RecordReceiveFailure(_options.QueueName, ex);
            _logger.LogError(ex, "Failed to receive message from SQL Service Broker queue {Queue}", _options.QueueName);
            throw;
        }
    }

    private async Task<BrokeredMessage?> ReceiveInternalAsync(int timeoutMs, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken).ConfigureAwait(false);

            try
            {
                await using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandType = CommandType.Text;
                command.CommandTimeout = _commandTimeoutSeconds;
                command.CommandText = _receiveCommandText;
                command.Parameters.Add(new SqlParameter("@timeoutMs", SqlDbType.Int) { Value = timeoutMs });

                await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleResult, cancellationToken).ConfigureAwait(false);
                if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    await reader.CloseAsync().ConfigureAwait(false);
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    await transaction.DisposeAsync().ConfigureAwait(false);
                    await connection.CloseAsync().ConfigureAwait(false);
                    await connection.DisposeAsync().ConfigureAwait(false);
                    return null;
                }

                var conversationHandle = reader.GetGuid(0);
                var messageTypeName = reader.GetString(1);
                var body = reader.GetStringOrNull(2);
                var enqueueTime = reader.GetDateTime(3);
                await reader.CloseAsync().ConfigureAwait(false);

                if (IsSystemMessage(messageTypeName))
                {
                    await EndConversationAsync(connection, transaction, conversationHandle, cancellationToken).ConfigureAwait(false);
                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                    await transaction.DisposeAsync().ConfigureAwait(false);
                    await connection.CloseAsync().ConfigureAwait(false);
                    await connection.DisposeAsync().ConfigureAwait(false);
                    _logger.LogDebug("Skipped system Service Broker message {MessageType}", messageTypeName);
                    continue;
                }

                _logger.LogDebug("Received broker message type {MessageType}", messageTypeName);

                return new BrokeredMessage(
                    conversationHandle,
                    messageTypeName,
                    body,
                    DateTime.SpecifyKind(enqueueTime, DateTimeKind.Utc),
                    completeAsync: async ct =>
                    {
                        await EndConversationAsync(connection, transaction, conversationHandle, ct).ConfigureAwait(false);
                        await transaction.CommitAsync(ct).ConfigureAwait(false);
                        await transaction.DisposeAsync().ConfigureAwait(false);
                        await connection.CloseAsync().ConfigureAwait(false);
                        await connection.DisposeAsync().ConfigureAwait(false);
                    },
                    abandonAsync: async ct =>
                    {
                        await transaction.RollbackAsync(ct).ConfigureAwait(false);
                        await transaction.DisposeAsync().ConfigureAwait(false);
                        await connection.CloseAsync().ConfigureAwait(false);
                        await connection.DisposeAsync().ConfigureAwait(false);
                    },
                    serializer: _serializer);
            }
            catch (Exception)
            {
                try
                {
                    await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                }
                catch
                {
                    // ignore rollback failures; connection cleanup will follow
                }

                await transaction.DisposeAsync().ConfigureAwait(false);
                await connection.CloseAsync().ConfigureAwait(false);
                await connection.DisposeAsync().ConfigureAwait(false);

                throw;
            }
        }

        return null;
    }

    private int ResolveTimeout(TimeSpan waitTime)
    {
        if (waitTime <= TimeSpan.Zero)
        {
            return _options.ReceiveWaitTimeoutMilliseconds;
        }

        var millis = (int)Math.Min(int.MaxValue, waitTime.TotalMilliseconds);
        return millis > 0 ? millis : _options.ReceiveWaitTimeoutMilliseconds;
    }

    private void GuardMessageLength(string body)
    {
        if (body.Length > _options.MaxMessageCharacters)
        {
            throw new InvalidOperationException($"Message exceeds configured maximum of {_options.MaxMessageCharacters:N0} characters.");
        }
    }

    private bool IsSystemMessage(string messageTypeName)
        => messageTypeName is "http://schemas.microsoft.com/SQL/ServiceBroker/EndDialog"
            or "http://schemas.microsoft.com/SQL/ServiceBroker/DialogTimer"
            or "http://schemas.microsoft.com/SQL/ServiceBroker/Error";

    private static async Task EndConversationAsync(SqlConnection connection, SqlTransaction transaction, Guid conversationHandle, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandType = CommandType.Text;
        command.CommandText = "END CONVERSATION @handle";
        command.Parameters.Add(new SqlParameter("@handle", SqlDbType.UniqueIdentifier) { Value = conversationHandle });
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private void ValidateIdentifiers()
    {
        ValidateSqlIdentifier(_options.QueueName, nameof(MessageBrokerOptions.QueueName), allowSchemaQualified: true);
        ValidateSqlIdentifier(_options.InitiatorServiceName, nameof(MessageBrokerOptions.InitiatorServiceName));
        ValidateSqlIdentifier(_options.TargetServiceName, nameof(MessageBrokerOptions.TargetServiceName));
        ValidateSqlIdentifier(_options.ContractName, nameof(MessageBrokerOptions.ContractName), allowContractSyntax: true);
        ValidateSqlIdentifier(_options.MessageTypeName, nameof(MessageBrokerOptions.MessageTypeName), allowContractSyntax: true);
    }

    private static void ValidateSqlIdentifier(string value, string propertyName, bool allowSchemaQualified = false, bool allowContractSyntax = false)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{propertyName} must be provided.");
        }

        var candidate = value.Trim();

        if (allowContractSyntax && candidate.StartsWith("//", StringComparison.Ordinal))
        {
            if (candidate.Any(ch => char.IsControl(ch) || char.IsWhiteSpace(ch)))
            {
                throw new InvalidOperationException($"{propertyName} contains invalid characters.");
            }
            return;
        }

        var segments = allowSchemaQualified ? candidate.Split('.') : new[] { candidate };
        foreach (var segment in segments)
        {
            if (segment.Length == 0)
            {
                throw new InvalidOperationException($"{propertyName} contains an empty identifier segment.");
            }

            if (!char.IsLetter(segment[0]) && segment[0] != '_' && segment[0] != '[')
            {
                throw new InvalidOperationException($"{propertyName} must start with a letter or underscore.");
            }

            if (segment[0] == '[')
            {
                if (!segment.EndsWith(']'))
                {
                    throw new InvalidOperationException($"{propertyName} has mismatched brackets.");
                }
                continue;
            }

            for (var i = 1; i < segment.Length; i++)
            {
                var ch = segment[i];
                if (!(char.IsLetterOrDigit(ch) || ch == '_' || ch == '$'))
                {
                    throw new InvalidOperationException($"{propertyName} contains invalid character '{ch}'.");
                }
            }
        }
    }

}
