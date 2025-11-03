using System;
using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Core.Configuration;

public sealed class ServiceBrokerResilienceOptions
{
    public const string SectionName = "MessageBroker:Resilience";

    [Range(1, 20)]
    public int PublishMaxAttempts { get; set; } = 5;

    [Range(1, 20)]
    public int ReceiveMaxAttempts { get; set; } = 5;

    public TimeSpan PublishBaseDelay { get; set; } = TimeSpan.FromMilliseconds(250);

    public TimeSpan ReceiveBaseDelay { get; set; } = TimeSpan.FromMilliseconds(500);

    public TimeSpan PublishMaxDelay { get; set; } = TimeSpan.FromSeconds(10);

    public TimeSpan ReceiveMaxDelay { get; set; } = TimeSpan.FromSeconds(15);

    [Range(1.0, 10.0)]
    public double ExponentialFactor { get; set; } = 2.0;

    [Range(0.0, 1.0)]
    public double JitterFactor { get; set; } = 0.2;

    [Range(1, 100)]
    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    public TimeSpan CircuitBreakerBreakDuration { get; set; } = TimeSpan.FromSeconds(30);

    [Range(1, 10)]
    public int CircuitBreakerHalfOpenSuccessThreshold { get; set; } = 1;

    [Range(1, 20)]
    public int PoisonMessageMaxAttempts { get; set; } = 3;
}
