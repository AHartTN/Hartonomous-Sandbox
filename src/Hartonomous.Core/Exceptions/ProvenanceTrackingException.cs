using System;

namespace Hartonomous.Core.Exceptions;

/// <summary>
/// Exception thrown when provenance tracking fails.
/// </summary>
public class ProvenanceTrackingException : Exception
{
    public ProvenanceTrackingException(string message) : base(message) { }

    public ProvenanceTrackingException(string message, Exception innerException)
        : base(message, innerException) { }
}
