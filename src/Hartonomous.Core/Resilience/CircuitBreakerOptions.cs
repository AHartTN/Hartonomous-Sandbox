using System;
using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Core.Resilience;

public sealed class CircuitBreakerOptions
{
    [Range(1, 100)]
    public int FailureThreshold { get; set; } = 5;

    [Range(1, 100)]
    public int HalfOpenSuccessThreshold { get; set; } = 1;

    public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(30);
}
