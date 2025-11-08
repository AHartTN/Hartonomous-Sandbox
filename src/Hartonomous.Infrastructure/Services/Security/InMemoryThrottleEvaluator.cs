using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hartonomous.Infrastructure.Services.Security;

public sealed class InMemoryThrottleEvaluator : IThrottleEvaluator
{
    private readonly IOptionsMonitor<SecurityOptions> _optionsMonitor;
    private readonly ILogger<InMemoryThrottleEvaluator> _logger;
    private readonly ConcurrentDictionary<string, SlidingWindowCounter> _counters = new();
    private readonly TimeProvider _timeProvider;

    public InMemoryThrottleEvaluator(
        IOptionsMonitor<SecurityOptions> optionsMonitor,
        ILogger<InMemoryThrottleEvaluator> logger,
        TimeProvider? timeProvider = null)
    {
        _optionsMonitor = optionsMonitor;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Task<ThrottleResult> EvaluateAsync(ThrottleContext context, CancellationToken cancellationToken = default)
    {
        var options = _optionsMonitor.CurrentValue;
        if (options.RateLimits.Count == 0)
        {
            return Task.FromResult(ThrottleResult.Allow());
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var appliedCounters = new List<SlidingWindowCounter>();

        foreach (var rule in options.RateLimits)
        {
            if (!IsRuleApplicable(rule, context))
            {
                continue;
            }

            var key = BuildKey(rule, context);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (!TryIncrement(rule, key, now, out var counter, out var retryAfter))
            {
                Rollback(appliedCounters);
                _logger.LogWarning("Rate limit {Rule} exceeded for key {Key}. Retry after {RetryAfter}.", rule.Name, key, retryAfter);
                return Task.FromResult(ThrottleResult.Deny(rule.Name, retryAfter));
            }

            appliedCounters.Add(counter);
        }

        return Task.FromResult(ThrottleResult.Allow());
    }

    private static bool IsRuleApplicable(RateLimitRuleOptions rule, ThrottleContext context)
    {
        if (!string.IsNullOrWhiteSpace(rule.TenantId) && !string.Equals(rule.TenantId, context.TenantId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(rule.PrincipalId) && !string.Equals(rule.PrincipalId, context.PrincipalId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(rule.Operation) && !string.Equals(rule.Operation, context.Operation, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private string? BuildKey(RateLimitRuleOptions rule, ThrottleContext context)
    {
        return rule.Scope switch
        {
            RateLimitScope.Tenant => string.IsNullOrWhiteSpace(context.TenantId) ? null : context.TenantId,
            RateLimitScope.Principal => string.IsNullOrWhiteSpace(context.PrincipalId) ? null : context.PrincipalId,
            RateLimitScope.TenantOperation => string.IsNullOrWhiteSpace(context.TenantId) ? null : $"{context.TenantId}:{ResolveOperation(rule, context)}",
            RateLimitScope.PrincipalOperation => string.IsNullOrWhiteSpace(context.PrincipalId) ? null : $"{context.PrincipalId}:{ResolveOperation(rule, context)}",
            _ => null
        };
    }

    private static string ResolveOperation(RateLimitRuleOptions rule, ThrottleContext context)
        => string.IsNullOrWhiteSpace(rule.Operation) ? context.Operation : rule.Operation;

    private bool TryIncrement(RateLimitRuleOptions rule, string key, DateTime timestampUtc, out SlidingWindowCounter counter, out TimeSpan retryAfter)
    {
        counter = _counters.GetOrAdd(key, _ => new SlidingWindowCounter(timestampUtc));
        lock (counter.SyncRoot)
        {
            if (timestampUtc - counter.WindowStartUtc >= rule.Window)
            {
                counter.WindowStartUtc = timestampUtc;
                counter.Count = 0;
            }

            if (counter.Count >= rule.PermitLimit)
            {
                var nextWindow = counter.WindowStartUtc + rule.Window;
                retryAfter = nextWindow > timestampUtc ? nextWindow - timestampUtc : TimeSpan.Zero;
                return false;
            }

            counter.Count++;
            retryAfter = TimeSpan.Zero;
            return true;
        }
    }

    private static void Rollback(List<SlidingWindowCounter> counters)
    {
        foreach (var counter in counters)
        {
            lock (counter.SyncRoot)
            {
                if (counter.Count > 0)
                {
                    counter.Count--;
                }
            }
        }
    }

    private sealed class SlidingWindowCounter
    {
        public SlidingWindowCounter(DateTime windowStartUtc)
        {
            WindowStartUtc = windowStartUtc;
        }

        public DateTime WindowStartUtc { get; set; }

        public int Count { get; set; }

        public object SyncRoot { get; } = new();
    }
}
