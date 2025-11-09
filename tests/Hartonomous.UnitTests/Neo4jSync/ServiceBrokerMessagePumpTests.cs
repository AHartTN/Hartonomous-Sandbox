using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Abstracts;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Messaging;
using Hartonomous.Core.Security;
using Hartonomous.Workers.Neo4jSync.Services;
using Hartonomous.Testing.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hartonomous.UnitTests.Neo4jSync;

public sealed class ServiceBrokerMessagePumpTests
{
    [Fact]
    public async Task RunAsync_DispatchesAndCompletesMessage()
    {
        var messageBroker = new StubMessageBroker();
        var dispatcher = new StubDispatcher();
        var deadLetterSink = new StubDeadLetterSink();
        var logger = TestLogger.Create<ServiceBrokerMessagePump>();

        using var cts = new CancellationTokenSource();
        var completionSource = new TaskCompletionSource<bool>();

        var message = CreateMessage(onComplete: () => completionSource.TrySetResult(true));
        messageBroker.Enqueue(message.Message);

        var pump = CreatePump(messageBroker, dispatcher, deadLetterSink, logger);
        var runTask = ((IMessagePump)pump).RunAsync(cts.Token);

        await completionSource.Task.WaitAsync(TimeSpan.FromSeconds(2));
        cts.Cancel();
        await runTask;

        Assert.Equal(1, dispatcher.DispatchCount);
        Assert.Equal(1, message.CompleteCount);
        Assert.Equal(0, message.AbandonCount);
        Assert.Empty(deadLetterSink.Messages);
    }

    [Fact]
    public async Task RunAsync_WhenPolicyDenied_RoutesToDeadLetter()
    {
        var messageBroker = new StubMessageBroker();
        var deadLetterSink = new StubDeadLetterSink();
        var logger = TestLogger.Create<ServiceBrokerMessagePump>();

        var dispatcher = new StubDispatcher
        {
            OnDispatch = (_, _) => throw new PolicyDeniedException("neo4j", "forbidden")
        };

        using var cts = new CancellationTokenSource();
        var message = CreateMessage();
        messageBroker.Enqueue(message.Message);

        var pump = CreatePump(messageBroker, dispatcher, deadLetterSink, logger);
        var runTask = ((IMessagePump)pump).RunAsync(cts.Token);

        // Wait for message to be processed
        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (deadLetterSink.Messages.Count == 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
        }

        cts.Cancel();

        try
        {
            await runTask;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        Assert.Single(deadLetterSink.Messages);
        var deadLetter = deadLetterSink.Messages[0];
        Assert.Equal(message.ConversationHandle, deadLetter.ConversationHandle);
        Assert.Equal("policy_denied:neo4j", deadLetter.Reason);
        Assert.Equal("neo4j", deadLetter.Metadata!["Policy"]);
        Assert.Equal(1, message.CompleteCount);
        Assert.Equal(0, message.AbandonCount);
    }

    [Fact]
    public async Task HandlePoisonMessageAsync_WritesDeadLetterAndResetsAttempts()
    {
        var messageBroker = new StubMessageBroker();
        var dispatcher = new StubDispatcher();
        var deadLetterSink = new StubDeadLetterSink();
        var logger = TestLogger.Create<ServiceBrokerMessagePump>();

        var pump = CreatePump(messageBroker, dispatcher, deadLetterSink, logger);
        var handle = Guid.NewGuid();
        var message = CreateMessage(handle);

        var attemptsField = typeof(ServiceBrokerMessagePump).GetField("_deliveryAttempts", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var attempts = (Dictionary<Guid, int>)attemptsField.GetValue(pump)!;
        attempts[handle] = 3;

        var method = typeof(ServiceBrokerMessagePump).GetMethod("HandlePoisonMessageAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        await (Task)method.Invoke(pump, new object[] { message.Message, 3, CancellationToken.None })!;

        Assert.Single(deadLetterSink.Messages);
        var deadLetter = deadLetterSink.Messages[0];
        Assert.Equal(handle, deadLetter.ConversationHandle);
        Assert.Equal(1, message.CompleteCount);
        Assert.Equal("Exceeded maximum delivery attempts of 5", deadLetter.Reason);
        Assert.False(attempts.ContainsKey(handle));
    }

    private static ServiceBrokerMessagePump CreatePump(
        IMessageBroker messageBroker,
        IMessageDispatcher dispatcher,
        IMessageDeadLetterSink deadLetterSink,
        ILogger<ServiceBrokerMessagePump> logger)
    {
        var brokerOptions = Options.Create(new MessageBrokerOptions
        {
            QueueName = "dbo.TestQueue",
            ReceiveWaitTimeoutMilliseconds = 250
        });

        var resilienceOptions = Options.Create(new ServiceBrokerResilienceOptions
        {
            PoisonMessageMaxAttempts = 5
        });

        return new ServiceBrokerMessagePump(
            logger,
            messageBroker,
            dispatcher,
            deadLetterSink,
            brokerOptions,
            resilienceOptions);
    }

    private static TestMessage CreateMessage(Guid? handle = null, Action? onComplete = null)
        => new(handle ?? Guid.NewGuid(), onComplete);

    private sealed class StubMessageBroker : IMessageBroker
    {
        private readonly ConcurrentQueue<BrokeredMessage?> _queue = new();
        private int _receiveCallsAfterEmpty = 0;

        public int ReceiveCalls { get; private set; }

        public void Enqueue(BrokeredMessage? message) => _queue.Enqueue(message);

        public Task PublishAsync<TPayload>(TPayload payload, CancellationToken cancellationToken = default) where TPayload : class
            => Task.CompletedTask;

        public Task PublishBatchAsync<TPayload>(IEnumerable<TPayload> payloads, CancellationToken cancellationToken = default) where TPayload : class
            => Task.CompletedTask;

        public async Task<BrokeredMessage?> ReceiveAsync(TimeSpan waitTime, CancellationToken cancellationToken = default)
        {
            ReceiveCalls++;
            if (_queue.TryDequeue(out var message))
            {
                _receiveCallsAfterEmpty = 0;
                return message;
            }

            _receiveCallsAfterEmpty++;
            if (_receiveCallsAfterEmpty > 10)
            {
                await Task.Delay(200, cancellationToken);
            }

            return null;
        }
    }

    private sealed class StubDispatcher : IMessageDispatcher
    {
        public int DispatchCount { get; private set; }
        public Func<BrokeredMessage, CancellationToken, Task>? OnDispatch { get; set; }

        public async Task DispatchAsync(BrokeredMessage message, CancellationToken cancellationToken = default)
        {
            DispatchCount++;
            if (OnDispatch != null)
            {
                await OnDispatch(message, cancellationToken).ConfigureAwait(false);
                return;
            }

            await Task.CompletedTask;
        }
    }

    private sealed class StubDeadLetterSink : IMessageDeadLetterSink
    {
        public List<DeadLetterMessage> Messages { get; } = new();

        public Task WriteAsync(DeadLetterMessage message, CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class TestMessage
    {
        private int _completeCount;
        private int _abandonCount;
        private readonly Action? _onComplete;

        public TestMessage(Guid conversationHandle, Action? onComplete)
        {
            ConversationHandle = conversationHandle;
            _onComplete = onComplete;
            Message = new BrokeredMessage(
                conversationHandle,
                "Hartonomous.Test",
                "{}",
                DateTimeOffset.UtcNow,
                completeAsync: ct =>
                {
                    Interlocked.Increment(ref _completeCount);
                    _onComplete?.Invoke();
                    return Task.CompletedTask;
                },
                abandonAsync: ct =>
                {
                    Interlocked.Increment(ref _abandonCount);
                    return Task.CompletedTask;
                });
        }

        public BrokeredMessage Message { get; }
        public Guid ConversationHandle { get; }
        public int CompleteCount => Volatile.Read(ref _completeCount);
        public int AbandonCount => Volatile.Read(ref _abandonCount);

        public TestMessage(Guid conversationHandle)
            : this(conversationHandle, null)
        {
        }
    }
}
