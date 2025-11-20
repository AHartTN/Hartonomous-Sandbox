using System;

namespace Hartonomous.Core.Exceptions;

/// <summary>
/// Exception thrown when tenant isolation is violated.
/// </summary>
public class TenantIsolationException : Exception
{
    public TenantIsolationException(string message) : base(message) { }

    public TenantIsolationException(string message, Exception innerException)
        : base(message, innerException) { }
}
