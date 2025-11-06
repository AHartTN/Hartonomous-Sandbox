using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Billing;
using Hartonomous.Core.Messaging;
using Hartonomous.Core.Models;
using Hartonomous.Core.Security;
using Hartonomous.Core.Telemetry;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Neo4jSync.Services;

/// <summary>
/// Centralized event dispatcher for Neo4jSync that routes events to appropriate handlers.
/// Enforces access policies, throttling, and billing measurement before dispatching to graph sync handlers.
/// </summary>
public sealed class EventDispatcher : IMessageDispatcher
{
    private readonly IReadOnlyList<IBaseEventHandler> _handlers;
    private readonly IAccessPolicyEngine _accessPolicyEngine;
    private readonly IThrottleEvaluator _throttleEvaluator;
    private readonly IBillingMeter _billingMeter;
    private readonly IBillingUsageSink _billingUsageSink;
    private readonly ILogger<EventDispatcher> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventDispatcher"/> class.
    /// </summary>
    /// <param name="handlers">Collection of event handlers for different event types.</param>
    /// <param name="accessPolicyEngine">Engine for evaluating access policies.</param>
    /// <param name="throttleEvaluator">Evaluator for rate limiting and throttling.</param>
    /// <param name="billingMeter">Meter for calculating usage billing.</param>
    /// <param name="billingUsageSink">Sink for persisting billing records.</param>
    /// <param name="logger">Logger for tracking dispatch operations and errors.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public EventDispatcher(
        IEnumerable<IBaseEventHandler> handlers,
        IAccessPolicyEngine accessPolicyEngine,
        IThrottleEvaluator throttleEvaluator,
        IBillingMeter billingMeter,
        IBillingUsageSink billingUsageSink,
        ILogger<EventDispatcher> logger)
    {
        _handlers = handlers?.ToList() ?? throw new ArgumentNullException(nameof(handlers));
        _accessPolicyEngine = accessPolicyEngine ?? throw new ArgumentNullException(nameof(accessPolicyEngine));
        _throttleEvaluator = throttleEvaluator ?? throw new ArgumentNullException(nameof(throttleEvaluator));
        _billingMeter = billingMeter ?? throw new ArgumentNullException(nameof(billingMeter));
        _billingUsageSink = billingUsageSink ?? throw new ArgumentNullException(nameof(billingUsageSink));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Dispatches a brokered message to appropriate event handler after validating access policies and throttling.
    /// Records telemetry and billing usage for successfully handled events.
    /// </summary>
    /// <param name="message">The brokered message containing event payload.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is null.</exception>
    /// <exception cref="PolicyDeniedException">Thrown when access policy evaluation fails.</exception>
    /// <exception cref="ThrottleRejectedException">Thrown when throttle limits are exceeded.</exception>
    public async Task DispatchAsync(BrokeredMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        MessagingTelemetry.RecordDispatchAttempt(message.MessageType);
        using var activity = MessagingTelemetry.StartActivity(
            name: "Neo4jSync.Dispatch",
            kind: ActivityKind.Consumer,
            tags: new[]
            {
                new KeyValuePair<string, object?>("messaging.message_type", message.MessageType),
                new KeyValuePair<string, object?>("messaging.conversation", message.ConversationHandle)
            });

        var payload = message.Deserialize<BaseEvent>();
        if (payload is null)
        {
            MessagingTelemetry.RecordDispatchFailure(message.MessageType, null, "deserialization_failed");
            _logger.LogWarning("Received broker message without valid payload. MessageType={MessageType}", message.MessageType);
            return;
        }

        var policyContext = BuildPolicyContext(message, payload);
        var policyResult = await _accessPolicyEngine.EvaluateAsync(policyContext, cancellationToken).ConfigureAwait(false);
        if (!policyResult.IsAllowed)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "access_denied");
            MessagingTelemetry.RecordDispatchFailure(message.MessageType, null, "access_denied");
            throw new PolicyDeniedException(policyResult.Policy ?? "unknown", policyResult.Reason ?? "Access denied");
        }

        var throttleContext = BuildThrottleContext(message, payload);
        var throttleResult = await _throttleEvaluator.EvaluateAsync(throttleContext, cancellationToken).ConfigureAwait(false);
        if (throttleResult.IsThrottled)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "rate_limited");
            MessagingTelemetry.RecordDispatchFailure(message.MessageType, null, "rate_limited");
            throw new ThrottleRejectedException(throttleResult.Policy ?? "default", throttleResult.RetryAfter);
        }

        foreach (var handler in _handlers)
        {
            if (!handler.CanHandle(payload))
            {
                continue;
            }

            var handlerName = handler.GetType().Name;
            activity?.SetTag("messaging.handler", handlerName);

            try
            {
                await handler.HandleAsync(payload, cancellationToken).ConfigureAwait(false);
                activity?.SetStatus(ActivityStatusCode.Ok);
                MessagingTelemetry.RecordDispatchSuccess(message.MessageType, handlerName);
                _logger.LogDebug("Dispatched event {EventId} to handler {Handler}", payload.Id, handlerName);

                var usageRecord = await _billingMeter
                    .MeasureAsync(payload, message.MessageType, handlerName, policyContext, cancellationToken)
                    .ConfigureAwait(false);
                if (usageRecord is not null)
                {
                    try
                    {
                        await _billingUsageSink.WriteAsync(usageRecord, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception billingEx)
                    {
                        _logger.LogError(billingEx, "Failed to persist billing usage for tenant {TenantId}, operation {Operation}", usageRecord.TenantId, usageRecord.Operation);
                    }
                }
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                MessagingTelemetry.RecordDispatchFailure(message.MessageType, handlerName, "handler_exception", ex);
                _logger.LogError(ex, "Handler {Handler} failed processing event {EventId}", handlerName, payload.Id);
                throw;
            }

            return;
        }

        activity?.SetStatus(ActivityStatusCode.Error, "no_handler");
        MessagingTelemetry.RecordDispatchFailure(message.MessageType, null, "no_handler");
        _logger.LogWarning("No handler found for event {EventId} ({EventType})", payload.Id, payload.Type);
    }

    private static AccessPolicyContext BuildPolicyContext(BrokeredMessage message, BaseEvent payload)
    {
        return new AccessPolicyContext
        {
            TenantId = ResolveTenantId(payload),
            PrincipalId = ResolvePrincipalId(payload),
            Operation = NormalizeOperation(payload),
            MessageType = message.MessageType,
            Attributes = payload.Extensions
        };
    }

    private static ThrottleContext BuildThrottleContext(BrokeredMessage message, BaseEvent payload)
    {
        return new ThrottleContext
        {
            TenantId = ResolveTenantId(payload),
            PrincipalId = ResolvePrincipalId(payload),
            Operation = NormalizeOperation(payload),
            MessageType = message.MessageType
        };
    }

    private static string ResolveTenantId(BaseEvent payload)
        => ResolveString(payload.Extensions, ["tenantId", "tenant", "tenant_id", "organization", "account"], "id");

    private static string ResolvePrincipalId(BaseEvent payload)
    {
        var principal = ResolveString(payload.Extensions, ["principalId", "principal", "user", "userId", "subject"], "id");
        return string.IsNullOrWhiteSpace(principal) ? payload.Subject ?? string.Empty : principal;
    }

    private static string ResolveString(Dictionary<string, object> extensions, IReadOnlyList<string> keys, string nestedKey)
    {
        foreach (var key in keys)
        {
            if (extensions.TryGetValue(key, out var value))
            {
                if (value is string str && !string.IsNullOrWhiteSpace(str))
                {
                    return str;
                }

                if (value is Dictionary<string, object> nested && nested.TryGetValue(nestedKey, out var nestedValue) && nestedValue is string nestedStr && !string.IsNullOrWhiteSpace(nestedStr))
                {
                    return nestedStr;
                }
            }
        }

        return string.Empty;
    }

    private static string NormalizeOperation(BaseEvent payload)
    {
        if (!string.IsNullOrWhiteSpace(payload.Type))
        {
            return payload.Type;
        }

        return "generic";
    }
}
