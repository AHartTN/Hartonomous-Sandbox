using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Hartonomous.Core.Telemetry;

public static class MessagingTelemetry
{
    public const string ActivitySourceName = "Hartonomous.Messaging";
    public const string ActivityVersion = "1.0.0";

    private static readonly ActivitySource _activitySource = new(ActivitySourceName, ActivityVersion);
    private static readonly Meter _meter = new(ActivitySourceName, ActivityVersion);

    private static readonly Counter<long> _publishAttempts = _meter.CreateCounter<long>(
        name: "messaging.publish.attempts",
        unit: "messages",
        description: "Number of publish attempts initiated.");

    private static readonly Counter<long> _publishSuccess = _meter.CreateCounter<long>(
        name: "messaging.publish.success",
        unit: "messages",
        description: "Number of messages successfully published.");

    private static readonly Counter<long> _publishFailures = _meter.CreateCounter<long>(
        name: "messaging.publish.failures",
        unit: "messages",
        description: "Number of publish attempts that failed.");

    private static readonly Counter<long> _receiveAttempts = _meter.CreateCounter<long>(
        name: "messaging.receive.attempts",
        unit: "messages",
        description: "Number of receive attempts initiated.");

    private static readonly Counter<long> _receiveSuccess = _meter.CreateCounter<long>(
        name: "messaging.receive.success",
        unit: "messages",
        description: "Number of messages successfully retrieved from the broker.");

    private static readonly Counter<long> _receiveFailures = _meter.CreateCounter<long>(
        name: "messaging.receive.failures",
        unit: "messages",
        description: "Number of receive attempts that failed.");

    private static readonly Counter<long> _deadLetters = _meter.CreateCounter<long>(
        name: "messaging.deadletters",
        unit: "messages",
        description: "Number of messages routed to dead-letter storage.");

    private static readonly Counter<long> _dispatchAttempts = _meter.CreateCounter<long>(
        name: "messaging.dispatch.attempts",
        unit: "messages",
        description: "Number of dispatch attempts initiated by the dispatcher.");

    private static readonly Counter<long> _dispatchSuccess = _meter.CreateCounter<long>(
        name: "messaging.dispatch.success",
        unit: "messages",
        description: "Number of messages dispatched successfully to handlers.");

    private static readonly Counter<long> _dispatchFailures = _meter.CreateCounter<long>(
        name: "messaging.dispatch.failures",
        unit: "messages",
        description: "Number of dispatch attempts that resulted in an error or no handler.");

    public static Activity? StartActivity(string name, ActivityKind kind, IEnumerable<KeyValuePair<string, object?>>? tags = null)
    {
        var activity = _activitySource.StartActivity(name, kind);
        if (activity != null && tags != null)
        {
            foreach (var tag in tags)
            {
                activity.SetTag(tag.Key, tag.Value);
            }
        }

        return activity;
    }

    public static void RecordPublishAttempt(string messageType, string? queueName)
    {
        _publishAttempts.Add(1, BuildTags(messageType, queueName));
    }

    public static void RecordPublishSuccess(string messageType, string? queueName)
    {
        _publishSuccess.Add(1, BuildTags(messageType, queueName));
    }

    public static void RecordPublishFailure(string messageType, string? queueName, Exception exception)
    {
        _publishFailures.Add(1, BuildFailureTags(messageType, queueName, exception));
    }

    public static void RecordReceiveAttempt(string? queueName)
    {
        _receiveAttempts.Add(1, BuildQueueTags(queueName));
    }

    public static void RecordReceiveSuccess(string messageType, string? queueName)
    {
        _receiveSuccess.Add(1, BuildTags(messageType, queueName));
    }

    public static void RecordReceiveFailure(string? queueName, Exception exception)
    {
        _receiveFailures.Add(1, BuildQueueFailureTags(queueName, exception));
    }

    public static void RecordDeadLetter(string messageType, string? queueName, int attempts, string reason)
    {
        _deadLetters.Add(1, new KeyValuePair<string, object?>[]
        {
            new("messaging.message_type", messageType),
            new("messaging.destination", queueName ?? string.Empty),
            new("messaging.deadletter.reason", reason),
            new("messaging.deadletter.attempts", attempts)
        });
    }

    public static void RecordDispatchAttempt(string messageType)
    {
        _dispatchAttempts.Add(1, BuildMessageTags(messageType));
    }

    public static void RecordDispatchSuccess(string messageType, string handlerName)
    {
        _dispatchSuccess.Add(1, BuildHandlerTags(messageType, handlerName));
    }

    public static void RecordDispatchFailure(string messageType, string? handlerName, string reason, Exception? exception = null)
    {
        var tags = new List<KeyValuePair<string, object?>>
        {
            new("messaging.message_type", messageType),
            new("messaging.handler", handlerName ?? string.Empty),
            new("messaging.dispatch.reason", reason)
        };

        if (exception != null)
        {
            tags.Add(new("exception.type", exception.GetType().FullName));
            tags.Add(new("exception.message", exception.Message));
        }

        _dispatchFailures.Add(1, tags.ToArray());
    }

    private static KeyValuePair<string, object?>[] BuildTags(string messageType, string? queueName)
        => new[]
        {
            new KeyValuePair<string, object?>("messaging.message_type", messageType),
            new KeyValuePair<string, object?>("messaging.destination", queueName ?? string.Empty)
        };

    private static KeyValuePair<string, object?>[] BuildFailureTags(string messageType, string? queueName, Exception exception)
        => new[]
        {
            new KeyValuePair<string, object?>("messaging.message_type", messageType),
            new KeyValuePair<string, object?>("messaging.destination", queueName ?? string.Empty),
            new KeyValuePair<string, object?>("exception.type", exception.GetType().FullName),
            new KeyValuePair<string, object?>("exception.message", exception.Message)
        };

    private static KeyValuePair<string, object?>[] BuildQueueTags(string? queueName)
        => new[] { new KeyValuePair<string, object?>("messaging.destination", queueName ?? string.Empty) };

    private static KeyValuePair<string, object?>[] BuildQueueFailureTags(string? queueName, Exception exception)
        => new[]
        {
            new KeyValuePair<string, object?>("messaging.destination", queueName ?? string.Empty),
            new KeyValuePair<string, object?>("exception.type", exception.GetType().FullName),
            new KeyValuePair<string, object?>("exception.message", exception.Message)
        };

    private static KeyValuePair<string, object?>[] BuildMessageTags(string messageType)
        => new[] { new KeyValuePair<string, object?>("messaging.message_type", messageType) };

    private static KeyValuePair<string, object?>[] BuildHandlerTags(string messageType, string handlerName)
        => new[]
        {
            new KeyValuePair<string, object?>("messaging.message_type", messageType),
            new KeyValuePair<string, object?>("messaging.handler", handlerName)
        };
}
