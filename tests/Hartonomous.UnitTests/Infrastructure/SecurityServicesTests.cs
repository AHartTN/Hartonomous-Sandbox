using System;
using System.Collections.Generic;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Security;
using Hartonomous.Infrastructure.Services.Security;
using Hartonomous.Testing.Common;
using Microsoft.Extensions.Logging;

namespace Hartonomous.UnitTests.Infrastructure;

public sealed class AccessPolicyEngineTests
{
    [Fact]
    public async Task EvaluateAsync_ReturnsFirstDenyAndLogs()
    {
        var logger = TestLogger.Create<AccessPolicyEngine>();
        var rules = new IAccessPolicyRule[]
        {
            new StubRule(null),
            new StubRule(AccessPolicyResult.Deny("test-policy", "failed"))
        };
        var engine = new AccessPolicyEngine(rules, logger);
        var context = new AccessPolicyContext
        {
            TenantId = "tenant-a",
            PrincipalId = "principal-1",
            Operation = "ingest"
        };

        var result = await engine.EvaluateAsync(context);

        Assert.False(result.IsAllowed);
        Assert.Equal("test-policy", result.Policy);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Warning && entry.Message.Contains("Access denied", StringComparison.Ordinal));
    }

    [Fact]
    public async Task EvaluateAsync_AllRulesPass_ReturnsAllow()
    {
        var logger = TestLogger.Create<AccessPolicyEngine>();
        var engine = new AccessPolicyEngine(new[]
        {
            new StubRule(null),
            new StubRule(AccessPolicyResult.Allow())
        }, logger);

        var result = await engine.EvaluateAsync(new AccessPolicyContext { TenantId = "tenant-a" });

        Assert.True(result.IsAllowed);
        Assert.Empty(logger.Entries);
    }

    private sealed class StubRule : IAccessPolicyRule
    {
        private readonly AccessPolicyResult? _result;

        public StubRule(AccessPolicyResult? result)
        {
            _result = result;
        }

        public Task<AccessPolicyResult?> EvaluateAsync(AccessPolicyContext context, CancellationToken cancellationToken = default)
            => Task.FromResult<AccessPolicyResult?>(_result);
    }
}

public sealed class TenantAccessPolicyRuleTests
{
    [Fact]
    public async Task EvaluateAsync_HandlesTenantAndPrincipalBans()
    {
        var options = new SecurityOptions();
        options.BannedTenants.Add("tenant-a");
        options.BannedPrincipals.Add("principal-x");

        var rule = new TenantAccessPolicyRule(new TestOptionsMonitor<SecurityOptions>(options));

        var tenantResult = await rule.EvaluateAsync(new AccessPolicyContext { TenantId = "tenant-a" });
        Assert.NotNull(tenantResult);
        Assert.False(tenantResult!.IsAllowed);
        Assert.Equal("tenant-ban", tenantResult.Policy);

        var principalResult = await rule.EvaluateAsync(new AccessPolicyContext { PrincipalId = "principal-x" });
        Assert.NotNull(principalResult);
        Assert.Equal("principal-ban", principalResult!.Policy);
    }

    [Fact]
    public async Task EvaluateAsync_DeniesOperationsFromDenyList()
    {
        var options = new SecurityOptions();
        options.TenantOperationDenyList["tenant-a"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ingest",
            "analytics"
        };

        var rule = new TenantAccessPolicyRule(new TestOptionsMonitor<SecurityOptions>(options));

        var result = await rule.EvaluateAsync(new AccessPolicyContext
        {
            TenantId = "tenant-a",
            Operation = "ingest"
        });

        Assert.NotNull(result);
        Assert.False(result!.IsAllowed);
        Assert.Equal("tenant-operation-ban", result.Policy);

        var messageTypeResult = await rule.EvaluateAsync(new AccessPolicyContext
        {
            TenantId = "tenant-a",
            Operation = string.Empty,
            MessageType = "analytics"
        });

        Assert.NotNull(messageTypeResult);
        Assert.Equal("tenant-operation-ban", messageTypeResult!.Policy);
    }
}

public sealed class InMemoryThrottleEvaluatorTests
{
    [Fact]
    public async Task EvaluateAsync_AllowsWhenNoRules()
    {
        var evaluator = new InMemoryThrottleEvaluator(
            new TestOptionsMonitor<SecurityOptions>(new SecurityOptions()),
            TestLogger.Create<InMemoryThrottleEvaluator>());

        var result = await evaluator.EvaluateAsync(new ThrottleContext { TenantId = "tenant-a" });

        Assert.False(result.IsThrottled);
    }

    [Fact]
    public async Task EvaluateAsync_EnforcesSlidingWindowLimits()
    {
        var options = new SecurityOptions();
        options.RateLimits.Add(new RateLimitRuleOptions
        {
            Name = "tenant-limit",
            Scope = RateLimitScope.Tenant,
            PermitLimit = 2,
            Window = TimeSpan.FromMinutes(1)
        });

        var timeProvider = new TestTimeProvider(DateTimeOffset.Parse("2025-11-03T12:00:00Z"));
        var evaluator = new InMemoryThrottleEvaluator(
            new TestOptionsMonitor<SecurityOptions>(options),
            TestLogger.Create<InMemoryThrottleEvaluator>(),
            timeProvider);

        var context = new ThrottleContext { TenantId = "tenant-a", Operation = "ingest" };

        Assert.False((await evaluator.EvaluateAsync(context)).IsThrottled);
        Assert.False((await evaluator.EvaluateAsync(context)).IsThrottled);

        var third = await evaluator.EvaluateAsync(context);
        Assert.True(third.IsThrottled);
        Assert.Equal("tenant-limit", third.Policy);
        Assert.True(third.RetryAfter > TimeSpan.Zero);

        timeProvider.Advance(TimeSpan.FromMinutes(1));
        Assert.False((await evaluator.EvaluateAsync(context)).IsThrottled);
    }

    [Fact]
    public async Task EvaluateAsync_UsesOperationScopes()
    {
        var options = new SecurityOptions();
        options.RateLimits.Add(new RateLimitRuleOptions
        {
            Name = "tenant-operation",
            Scope = RateLimitScope.TenantOperation,
            PermitLimit = 1,
            Window = TimeSpan.FromMinutes(1)
        });

        var timeProvider = new TestTimeProvider(DateTimeOffset.UtcNow);
        var evaluator = new InMemoryThrottleEvaluator(
            new TestOptionsMonitor<SecurityOptions>(options),
            TestLogger.Create<InMemoryThrottleEvaluator>(),
            timeProvider);

        var context = new ThrottleContext { TenantId = "tenant-a", Operation = "ingest" };

        Assert.False((await evaluator.EvaluateAsync(context)).IsThrottled);
        Assert.True((await evaluator.EvaluateAsync(context)).IsThrottled);

        var otherOperation = await evaluator.EvaluateAsync(new ThrottleContext { TenantId = "tenant-a", Operation = "search" });
        Assert.False(otherOperation.IsThrottled);
    }

    [Fact]
    public async Task EvaluateAsync_RollsBackWhenSubsequentRuleFails()
    {
        var options = new SecurityOptions();
        options.RateLimits.Add(new RateLimitRuleOptions
        {
            Name = "first",
            Scope = RateLimitScope.Tenant,
            PermitLimit = 1,
            Window = TimeSpan.FromMinutes(1)
        });
        options.RateLimits.Add(new RateLimitRuleOptions
        {
            Name = "second",
            Scope = RateLimitScope.Tenant,
            PermitLimit = 0,
            Window = TimeSpan.FromMinutes(1)
        });

        var evaluator = new InMemoryThrottleEvaluator(
            new TestOptionsMonitor<SecurityOptions>(options),
            TestLogger.Create<InMemoryThrottleEvaluator>());

        var context = new ThrottleContext { TenantId = "tenant-a" };

        var firstAttempt = await evaluator.EvaluateAsync(context);
        Assert.True(firstAttempt.IsThrottled);
        Assert.Equal("second", firstAttempt.Policy);

        var secondAttempt = await evaluator.EvaluateAsync(context);
        Assert.True(secondAttempt.IsThrottled);
        Assert.Equal("second", secondAttempt.Policy);
    }
}