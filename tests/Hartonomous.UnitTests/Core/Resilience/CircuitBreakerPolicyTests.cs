using System;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Resilience;
using Hartonomous.Testing.Common;
using Microsoft.Extensions.Logging;

namespace Hartonomous.UnitTests.Core.Resilience;

public sealed class CircuitBreakerPolicyTests
{
    [Fact]
    public async Task ExecuteAsync_OpensCircuitAfterThresholdAndBlocksUntilBreakExpires()
    {
        var options = new CircuitBreakerOptions
        {
            FailureThreshold = 2,
            HalfOpenSuccessThreshold = 1,
            BreakDuration = TimeSpan.FromMilliseconds(200)
        };

        var logger = TestLogger.Create<CircuitBreakerPolicy>();
        var policy = new CircuitBreakerPolicy(options, new StubTransientDetector(_ => true), logger);

        async Task<bool> FailingOperation(CancellationToken cancellationToken)
        {
            await Task.Yield();
            throw new InvalidOperationException("Transient failure");
        }

    await Assert.ThrowsAsync<InvalidOperationException>(async () => await policy.ExecuteAsync(FailingOperation));
    await Assert.ThrowsAsync<InvalidOperationException>(async () => await policy.ExecuteAsync(FailingOperation));

    var openException = await Assert.ThrowsAsync<CircuitBreakerOpenException>(async () => await policy.ExecuteAsync(FailingOperation));
        Assert.Contains("Circuit breaker is open", openException.Message, StringComparison.OrdinalIgnoreCase);

    var warning = Assert.Single(logger.Entries, entry => entry.Level == LogLevel.Warning);
        Assert.Contains("opened", warning.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_HalfOpenRecoversAfterSuccessfulProbe()
    {
        var options = new CircuitBreakerOptions
        {
            FailureThreshold = 1,
            HalfOpenSuccessThreshold = 1,
            BreakDuration = TimeSpan.FromMilliseconds(50)
        };

        var logger = TestLogger.Create<CircuitBreakerPolicy>();
        var policy = new CircuitBreakerPolicy(options, new StubTransientDetector(_ => true), logger);

        async Task<bool> FailingOperation(CancellationToken cancellationToken)
        {
            await Task.Yield();
            throw new InvalidOperationException("Transient failure");
        }

    await Assert.ThrowsAsync<InvalidOperationException>(async () => await policy.ExecuteAsync(FailingOperation));

        await Task.Delay(options.BreakDuration + TimeSpan.FromMilliseconds(25));

        var successfulResult = await policy.ExecuteAsync(async cancellationToken =>
        {
            await Task.Yield();
            return 42;
        });

        Assert.Equal(42, successfulResult);

    Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Information && entry.Message.Contains("closed", StringComparison.OrdinalIgnoreCase));

    await Assert.ThrowsAsync<InvalidOperationException>(async () => await policy.ExecuteAsync(FailingOperation));
    }

    [Fact]
    public async Task ExecuteAsync_AllowsOnlySingleProbeWhileHalfOpen()
    {
        var options = new CircuitBreakerOptions
        {
            FailureThreshold = 1,
            HalfOpenSuccessThreshold = 1,
            BreakDuration = TimeSpan.FromMilliseconds(10)
        };

        var policy = new CircuitBreakerPolicy(options, new StubTransientDetector(_ => true), TestLogger.Create<CircuitBreakerPolicy>());

        async Task<bool> FailingOperation(CancellationToken cancellationToken)
        {
            await Task.Yield();
            throw new InvalidOperationException("Transient failure");
        }

    await Assert.ThrowsAsync<InvalidOperationException>(async () => await policy.ExecuteAsync(FailingOperation));

        await Task.Delay(options.BreakDuration + TimeSpan.FromMilliseconds(5));

        var probeGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var probeTask = policy.ExecuteAsync(async cancellationToken =>
        {
            await probeGate.Task.ConfigureAwait(false);
            return true;
        });

        var secondaryCall = policy.ExecuteAsync(async cancellationToken =>
        {
            await Task.Yield();
            return true;
        });

        await Assert.ThrowsAsync<CircuitBreakerOpenException>(async () => await secondaryCall);

        probeGate.SetResult(true);
        var probeResult = await probeTask;
        Assert.True(probeResult);
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
