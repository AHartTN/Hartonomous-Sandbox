using System;

namespace Hartonomous.Infrastructure.Resilience;

/// <summary>
/// Configuration options for resilience policies (circuit breaker, retry, timeout)
/// </summary>
public class ResilienceOptions
{
    public const string SectionName = "Resilience";

    // Circuit Breaker
    public int CircuitBreakerFailureThreshold { get; set; } = 5;
    public TimeSpan CircuitBreakerSamplingDuration { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan CircuitBreakerBreakDuration { get; set; } = TimeSpan.FromSeconds(60);
    public int CircuitBreakerMinimumThroughput { get; set; } = 10;

    // Retry with Exponential Backoff
    public int RetryMaxAttempts { get; set; } = 5;
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(2);
    public bool RetryUseJitter { get; set; } = true;

    // Timeout
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan InferenceTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan GenerationTimeout { get; set; } = TimeSpan.FromMinutes(10);
}
