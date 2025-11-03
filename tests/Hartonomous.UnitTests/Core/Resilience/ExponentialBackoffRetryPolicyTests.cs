using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Resilience;
using Hartonomous.Testing.Common;
using Microsoft.Extensions.Logging;

namespace Hartonomous.UnitTests.Core.Resilience;

public sealed class ExponentialBackoffRetryPolicyTests
{
    [Fact]
    public async Task ExecuteAsync_RetriesTransientFailuresUntilSuccess()
    {
        var options = new RetryPolicyOptions
        {
            MaxAttempts = 5,
            BaseDelay = TimeSpan.FromMilliseconds(1),
            MaxDelay = TimeSpan.FromMilliseconds(4),
            ExponentialFactor = 2.0,
            JitterFactor = 0.0
        };

        var logger = TestLogger.Create<ExponentialBackoffRetryPolicy>();
        var policy = new ExponentialBackoffRetryPolicy(options, new StubTransientDetector(ex => ex is InvalidOperationException), logger);

        var attempt = 0;
        var result = await policy.ExecuteAsync(async cancellationToken =>
        {
            attempt++;
            await Task.Yield();

            if (attempt < 3)
            {
                throw new InvalidOperationException("retry me");
            }

            return "ok";
        });

        Assert.Equal(3, attempt);
        Assert.Equal("ok", result);

        Assert.Equal(2, logger.Entries.Count(entry => entry.Level == LogLevel.Warning));
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsLastTransientExceptionAfterMaxAttempts()
    {
        var options = new RetryPolicyOptions
        {
            MaxAttempts = 3,
            BaseDelay = TimeSpan.FromMilliseconds(1),
            MaxDelay = TimeSpan.FromMilliseconds(3),
            ExponentialFactor = 2.0,
            JitterFactor = 0.0
        };

        var logger = TestLogger.Create<ExponentialBackoffRetryPolicy>();
        var policy = new ExponentialBackoffRetryPolicy(options, new StubTransientDetector(_ => true), logger);

        var attempt = 0;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await policy.ExecuteAsync(async cancellationToken =>
        {
            attempt++;
            await Task.Yield();
            throw new InvalidOperationException("still failing");
        }));

        Assert.Equal(options.MaxAttempts, attempt);
        Assert.Equal("still failing", exception.Message);

        Assert.Equal(options.MaxAttempts - 1, logger.Entries.Count(entry => entry.Level == LogLevel.Warning));
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotRetryWhenExceptionIsNotTransient()
    {
        var options = new RetryPolicyOptions
        {
            MaxAttempts = 5,
            BaseDelay = TimeSpan.FromMilliseconds(1),
            MaxDelay = TimeSpan.FromMilliseconds(3),
            ExponentialFactor = 2.0,
            JitterFactor = 0.0
        };

        var logger = TestLogger.Create<ExponentialBackoffRetryPolicy>();
        var policy = new ExponentialBackoffRetryPolicy(options, new StubTransientDetector(_ => false), logger);

        var attempt = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await policy.ExecuteAsync(async cancellationToken =>
        {
            attempt++;
            await Task.Yield();
            throw new InvalidOperationException("non-transient");
        }));

        Assert.Equal(1, attempt);
        Assert.Empty(logger.Entries);
    }

    private sealed class StubTransientDetector : ITransientErrorDetector
    {
        private readonly Func<Exception, bool> _predicate;

        public StubTransientDetector(Func<Exception, bool> predicate)
        {
            _predicate = predicate;
        }

        public bool IsTransient(Exception exception) => _predicate(exception);
    }
}
