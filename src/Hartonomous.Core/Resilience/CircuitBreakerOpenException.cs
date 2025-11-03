using System;

namespace Hartonomous.Core.Resilience;

public sealed class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerOpenException() : base("Circuit breaker is open. Operation is temporarily blocked.")
    {
    }

    public CircuitBreakerOpenException(string message) : base(message)
    {
    }

    public CircuitBreakerOpenException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
