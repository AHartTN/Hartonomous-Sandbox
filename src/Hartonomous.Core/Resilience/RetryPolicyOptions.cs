using System;
using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Core.Resilience;

public sealed class RetryPolicyOptions
{
    [Range(1, 20)]
    public int MaxAttempts { get; set; } = 5;

    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(200);

    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(10);

    [Range(1.0, 10.0)]
    public double ExponentialFactor { get; set; } = 2.0;

    [Range(0.0, 1.0)]
    public double JitterFactor { get; set; } = 0.2;
}
