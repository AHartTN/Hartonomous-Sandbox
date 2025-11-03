using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Resilience;
using Hartonomous.Infrastructure.Services.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Hartonomous.UnitTests.Infrastructure.Messaging;

public sealed class ServiceBrokerResilienceStrategyTests
{
    [Fact]
    public void Ctor_ConfiguresRetryPoliciesFromOptions()
    {
        var options = Options.Create(new ServiceBrokerResilienceOptions
        {
            PublishMaxAttempts = 7,
            PublishBaseDelay = TimeSpan.FromMilliseconds(10),
            PublishMaxDelay = TimeSpan.FromSeconds(2),
            ReceiveMaxAttempts = 9,
            ReceiveBaseDelay = TimeSpan.FromMilliseconds(20),
            ReceiveMaxDelay = TimeSpan.FromSeconds(3),
            ExponentialFactor = 3.5,
            JitterFactor = 0.1
        });

        var captured = new List<RetryPolicyOptions>();
        var publishRetry = new StubRetryPolicy();
        var receiveRetry = new StubRetryPolicy();
        var factoryQueue = new Queue<IRetryPolicy>(new[] { publishRetry, receiveRetry });

        var strategy = new ServiceBrokerResilienceStrategy(
            options,
            opt =>
            {
                captured.Add(opt);
                if (!factoryQueue.TryDequeue(out var policy))
                {
                    throw new InvalidOperationException("Factory invoked more times than expected.");
                }

                return policy;
            },
            new StubCircuitBreaker(),
            NullLogger<ServiceBrokerResilienceStrategy>.Instance);

        Assert.Collection(captured,
            publish =>
            {
                Assert.Equal(7, publish.MaxAttempts);
                Assert.Equal(TimeSpan.FromMilliseconds(10), publish.BaseDelay);
                Assert.Equal(TimeSpan.FromSeconds(2), publish.MaxDelay);
                Assert.Equal(3.5, publish.ExponentialFactor);
                Assert.Equal(0.1, publish.JitterFactor);
            },
            receive =>
            {
                Assert.Equal(9, receive.MaxAttempts);
                Assert.Equal(TimeSpan.FromMilliseconds(20), receive.BaseDelay);
                Assert.Equal(TimeSpan.FromSeconds(3), receive.MaxDelay);
                Assert.Equal(3.5, receive.ExponentialFactor);
                Assert.Equal(0.1, receive.JitterFactor);
            });
    }

    [Fact]
    public async Task ExecutePublishAndReceiveAsync_DelegatesThroughCircuitBreakerAndRetryPolicies()
    {
        var options = Options.Create(new ServiceBrokerResilienceOptions());
        var publishRetry = new StubRetryPolicy();
        var receiveRetry = new StubRetryPolicy();
        var circuitBreaker = new StubCircuitBreaker();

        var queue = new Queue<IRetryPolicy>(new[] { publishRetry, receiveRetry });

        var strategy = new ServiceBrokerResilienceStrategy(
            options,
            _ => queue.Dequeue(),
            circuitBreaker,
            NullLogger<ServiceBrokerResilienceStrategy>.Instance);

        var publishCalled = false;
        await strategy.ExecutePublishAsync(async ct =>
        {
            publishCalled = true;
            await Task.Yield();
        });

        Assert.True(publishCalled);
        Assert.Equal(1, publishRetry.VoidExecutions);
        Assert.Equal(1, circuitBreaker.VoidExecutions);

        var receiveResult = await strategy.ExecuteReceiveAsync(async ct =>
        {
            await Task.Delay(1, ct);
            return 42;
        });

        Assert.Equal(42, receiveResult);
        Assert.Equal(1, receiveRetry.ResultExecutions);
        Assert.Equal(1, circuitBreaker.ResultExecutions);
    }

    private sealed class StubRetryPolicy : IRetryPolicy
    {
        public int VoidExecutions { get; private set; }
        public int ResultExecutions { get; private set; }

        public Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
        {
            VoidExecutions++;
            return operation(cancellationToken);
        }

        public Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default)
        {
            ResultExecutions++;
            return operation(cancellationToken);
        }
    }

    private sealed class StubCircuitBreaker : ICircuitBreakerPolicy
    {
        public int VoidExecutions { get; private set; }
        public int ResultExecutions { get; private set; }

        public Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
        {
            VoidExecutions++;
            return operation(cancellationToken);
        }

        public Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default)
        {
            ResultExecutions++;
            return operation(cancellationToken);
        }
    }
}
