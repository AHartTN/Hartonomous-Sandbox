using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Messaging;
using Hartonomous.Core.Serialization;
using Hartonomous.Infrastructure.Services.Messaging;
using Hartonomous.Testing.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hartonomous.UnitTests.Infrastructure.Messaging;

public sealed class SqlMessageBrokerTests
{
    [Fact]
    public void Ctor_WithInvalidQueueName_Throws()
    {
        var options = new MessageBrokerOptions
        {
            QueueName = "1InvalidQueue"
        };

        Assert.Throws<InvalidOperationException>(() =>
            CreateBroker(brokerOptions: options));
    }

    [Fact]
    public async Task PublishAsync_WhenPayloadTooLarge_ThrowsInvalidOperationException()
    {
        var serializer = new StubSerializer
        {
            SerializedValue = new string('x', 2_048)
        };

        var options = new MessageBrokerOptions
        {
            MaxMessageCharacters = 1_024
        };

        var broker = CreateBroker(serializer: serializer, brokerOptions: options);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            broker.PublishAsync(new object()));
    }

    [Fact]
    public async Task PublishAsync_InvokesResilienceStrategy()
    {
        var strategy = new StubResilienceStrategy();
        var broker = CreateBroker(resilienceStrategy: strategy);

        await broker.PublishAsync("payload");

        Assert.Equal(1, strategy.PublishCalls);
        Assert.NotNull(strategy.LastPublishOperation);
    }

    [Fact]
    public async Task PublishBatchAsync_InvokesPublishForEachPayload()
    {
        var strategy = new StubResilienceStrategy();
        var broker = CreateBroker(resilienceStrategy: strategy);

        var payloads = new[] { "first", "second", "third" };
        await broker.PublishBatchAsync(payloads);

        Assert.Equal(payloads.Length, strategy.PublishCalls);
    }

    [Fact]
    public async Task PublishBatchAsync_HonorsCancellationBetweenPayloads()
    {
        var strategy = new StubResilienceStrategy();
        using var cts = new CancellationTokenSource();
        var broker = CreateBroker(resilienceStrategy: strategy);

        strategy.PublishOverride = async (operation, token) =>
        {
            if (!cts.IsCancellationRequested)
            {
                cts.Cancel();
            }
            await Task.CompletedTask;
        };

        var payloads = new[] { "first", "second" };

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            broker.PublishBatchAsync(payloads, cts.Token));

        Assert.Equal(1, strategy.PublishCalls);
    }

    [Fact]
    public async Task ReceiveAsync_UsesResilienceStrategyResult()
    {
        var expected = new BrokeredMessage(
            Guid.NewGuid(),
            "Hartonomous.Test",
            "{ }",
            DateTimeOffset.UtcNow,
            completeAsync: _ => Task.CompletedTask,
            abandonAsync: _ => Task.CompletedTask);

        var strategy = new StubResilienceStrategy();
    strategy.SetReceiveOverride<BrokeredMessage?>((_, _) => Task.FromResult<BrokeredMessage?>(expected));

        var broker = CreateBroker(resilienceStrategy: strategy);

        var result = await broker.ReceiveAsync(TimeSpan.FromSeconds(1));

        Assert.Same(expected, result);
        Assert.Equal(1, strategy.ReceiveCalls);
    }

    [Fact]
    public async Task ReceiveAsync_WhenResilienceThrows_PropagatesException()
    {
        var strategy = new StubResilienceStrategy();
    strategy.SetReceiveOverride<BrokeredMessage?>((_, _) => Task.FromException<BrokeredMessage?>(new InvalidOperationException("boom")));

        var broker = CreateBroker(resilienceStrategy: strategy);

        await Assert.ThrowsAsync<InvalidOperationException>(() => broker.ReceiveAsync(TimeSpan.FromMilliseconds(100)));
    }

    private static SqlMessageBroker CreateBroker(
        ISqlServerConnectionFactory? connectionFactory = null,
        IJsonSerializer? serializer = null,
        MessageBrokerOptions? brokerOptions = null,
        SqlServerOptions? sqlServerOptions = null,
        IServiceBrokerResilienceStrategy? resilienceStrategy = null,
        ILogger<SqlMessageBroker>? logger = null)
    {
        var effectiveBrokerOptions = NormalizeBrokerOptions(brokerOptions ?? new MessageBrokerOptions());
        var effectiveSqlOptions = sqlServerOptions ?? new SqlServerOptions { CommandTimeoutSeconds = 15 };

        return new SqlMessageBroker(
            connectionFactory ?? new StubConnectionFactory(),
            serializer ?? new StubSerializer(),
            Options.Create(effectiveBrokerOptions),
            new TestOptionsMonitor<SqlServerOptions>(effectiveSqlOptions),
            resilienceStrategy ?? new StubResilienceStrategy(),
            logger ?? TestLogger.Silent<SqlMessageBroker>());
    }

    private static MessageBrokerOptions NormalizeBrokerOptions(MessageBrokerOptions options)
    {
        return new MessageBrokerOptions
        {
            InitiatorServiceName = SanitizeIdentifier(options.InitiatorServiceName),
            TargetServiceName = SanitizeIdentifier(options.TargetServiceName),
            ContractName = options.ContractName,
            MessageTypeName = options.MessageTypeName,
            QueueName = options.QueueName,
            ReceiveWaitTimeoutMilliseconds = options.ReceiveWaitTimeoutMilliseconds,
            MaxMessageCharacters = options.MaxMessageCharacters,
            ConversationLifetimeSeconds = options.ConversationLifetimeSeconds
        };
    }

    private static string SanitizeIdentifier(string value)
        => value.Replace('.', '_');

    private sealed class StubConnectionFactory : ISqlServerConnectionFactory
    {
        public Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new SqlConnection());
        }
    }

    private sealed class StubSerializer : IJsonSerializer
    {
        public string SerializedValue { get; set; } = "{}";

        public string Serialize<T>(T value) => SerializedValue;

        public T? Deserialize<T>(string json) => default;
    }

    private sealed class StubResilienceStrategy : IServiceBrokerResilienceStrategy
    {
        private readonly Dictionary<Type, object> _receiveOverrides = new();

        public int PublishCalls { get; private set; }
        public int ReceiveCalls { get; private set; }
        public Func<CancellationToken, Task>? LastPublishOperation { get; private set; }
        public Delegate? LastReceiveOperation { get; private set; }
        public Func<Func<CancellationToken, Task>, CancellationToken, Task>? PublishOverride { get; set; }

        public Task ExecutePublishAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
        {
            PublishCalls++;
            LastPublishOperation = operation;

            if (PublishOverride != null)
            {
                return PublishOverride(operation, cancellationToken);
            }

            return Task.CompletedTask;
        }

        public Task<TResult> ExecuteReceiveAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default)
        {
            ReceiveCalls++;
            LastReceiveOperation = operation;

            if (_receiveOverrides.TryGetValue(typeof(TResult), out var overrideDelegate))
            {
                var typedOverride = (Func<Func<CancellationToken, Task<TResult>>, CancellationToken, Task<TResult>>)overrideDelegate;
                return typedOverride(operation, cancellationToken);
            }

            return operation(cancellationToken);
        }

        public void SetReceiveOverride<TResult>(Func<Func<CancellationToken, Task<TResult>>, CancellationToken, Task<TResult>> overrideFunc)
        {
            _receiveOverrides[typeof(TResult)] = overrideFunc;
        }
    }
}
