using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Abstracts;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Messaging;
using Hartonomous.Core.Security;
using Hartonomous.Core.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hartonomous.Neo4jSync.Services;

public sealed class ServiceBrokerMessagePump : BackgroundService, IMessagePump
{
    private readonly ILogger<ServiceBrokerMessagePump> _logger;
    private readonly IMessageBroker _messageBroker;
    private readonly IMessageDispatcher _dispatcher;
    private readonly IMessageDeadLetterSink _deadLetterSink;
    private readonly TimeSpan _waitTimeout;
    private readonly int _poisonMessageMaxAttempts;
    private readonly string _queueName;
    private readonly Dictionary<Guid, int> _deliveryAttempts = new();

    public ServiceBrokerMessagePump(
        ILogger<ServiceBrokerMessagePump> logger,
        IMessageBroker messageBroker,
        IMessageDispatcher dispatcher,
        IMessageDeadLetterSink deadLetterSink,
        IOptions<MessageBrokerOptions> brokerOptions,
        IOptions<ServiceBrokerResilienceOptions> resilienceOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _deadLetterSink = deadLetterSink ?? throw new ArgumentNullException(nameof(deadLetterSink));
        var timeoutMs = Math.Max(250, brokerOptions.Value.ReceiveWaitTimeoutMilliseconds);
        _waitTimeout = TimeSpan.FromMilliseconds(timeoutMs);
        _poisonMessageMaxAttempts = Math.Max(1, resilienceOptions.Value.PoisonMessageMaxAttempts);
    _queueName = brokerOptions.Value.QueueName;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SQL Service Broker message pump starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            BrokeredMessage? message = null;
            try
            {
                message = await _messageBroker.ReceiveAsync(_waitTimeout, stoppingToken).ConfigureAwait(false);
                if (message is null)
                {
                    continue;
                }

                var attempts = IncrementAttempts(message.ConversationHandle);
                if (attempts > _poisonMessageMaxAttempts)
                {
                    await HandlePoisonMessageAsync(message, attempts, stoppingToken).ConfigureAwait(false);
                    continue;
                }

                await _dispatcher.DispatchAsync(message, stoppingToken).ConfigureAwait(false);
                await message.CompleteAsync(stoppingToken).ConfigureAwait(false);
                ResetAttempts(message.ConversationHandle);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (PolicyDeniedException ex)
            {
                if (message != null)
                {
                    await HandlePolicyDeniedAsync(message, ex, stoppingToken).ConfigureAwait(false);
                }
            }
            catch (ThrottleRejectedException ex)
            {
                if (message != null)
                {
                    await message.AbandonAsync(stoppingToken).ConfigureAwait(false);
                    ResetAttempts(message.ConversationHandle);
                    _logger.LogWarning("Throttled message {ConversationHandle} by policy {Policy}. Retrying after {RetryAfter}.", message.ConversationHandle, ex.Policy, ex.RetryAfter);
                    var delay = ex.RetryAfter > TimeSpan.Zero ? ex.RetryAfter : TimeSpan.FromSeconds(1);
                    await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from SQL Service Broker queue");
                if (message != null)
                {
                    await message.AbandonAsync(stoppingToken).ConfigureAwait(false);
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
            }
            finally
            {
                if (message != null)
                {
                    await message.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        _logger.LogInformation("SQL Service Broker message pump stopped.");
    }

    Task IMessagePump.RunAsync(CancellationToken cancellationToken)
        => ExecuteAsync(cancellationToken);

    private int IncrementAttempts(Guid conversationHandle)
    {
        if (_deliveryAttempts.TryGetValue(conversationHandle, out var attempts))
        {
            attempts++;
        }
        else
        {
            attempts = 1;
        }

        _deliveryAttempts[conversationHandle] = attempts;
        return attempts;
    }

    private void ResetAttempts(Guid conversationHandle)
    {
        _deliveryAttempts.Remove(conversationHandle);
    }

    private async Task HandlePoisonMessageAsync(BrokeredMessage message, int attempts, CancellationToken cancellationToken)
    {
        var reason = $"Exceeded maximum delivery attempts of {_poisonMessageMaxAttempts}";

        var metadata = new Dictionary<string, string>
        {
            ["Queue"] = _queueName,
            ["MessageType"] = message.MessageType,
            ["Attempts"] = attempts.ToString()
        };

        var deadLetter = new DeadLetterMessage
        {
            ConversationHandle = message.ConversationHandle,
            MessageType = message.MessageType,
            Body = message.Body,
            EnqueueTime = message.EnqueueTime,
            AttemptCount = attempts,
            Reason = reason,
            Metadata = metadata
        };

        await _deadLetterSink.WriteAsync(deadLetter, cancellationToken).ConfigureAwait(false);
        await message.CompleteAsync(cancellationToken).ConfigureAwait(false);
        ResetAttempts(message.ConversationHandle);

        MessagingTelemetry.RecordDeadLetter(message.MessageType, _queueName, attempts, reason);
        _logger.LogWarning("Moved message {ConversationHandle} to dead-letter after {Attempts} failed attempts.", message.ConversationHandle, attempts);
    }

    private async Task HandlePolicyDeniedAsync(BrokeredMessage message, PolicyDeniedException exception, CancellationToken cancellationToken)
    {
        var reason = $"policy_denied:{exception.Policy}";
        var metadata = new Dictionary<string, string>
        {
            ["Policy"] = exception.Policy,
            ["Reason"] = exception.Reason,
            ["Queue"] = _queueName
        };

        var deadLetter = new DeadLetterMessage
        {
            ConversationHandle = message.ConversationHandle,
            MessageType = message.MessageType,
            Body = message.Body,
            EnqueueTime = message.EnqueueTime,
            AttemptCount = 1,
            Reason = reason,
            Metadata = metadata
        };

        await _deadLetterSink.WriteAsync(deadLetter, cancellationToken).ConfigureAwait(false);
        await message.CompleteAsync(cancellationToken).ConfigureAwait(false);
        ResetAttempts(message.ConversationHandle);

        MessagingTelemetry.RecordDeadLetter(message.MessageType, _queueName, 1, reason);
        _logger.LogWarning("Message {ConversationHandle} denied by policy {Policy}. Routed to dead-letter.", message.ConversationHandle, exception.Policy);
    }
}
