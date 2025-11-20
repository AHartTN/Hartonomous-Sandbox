namespace Hartonomous.Core.Exceptions;

/// <summary>
/// Exception thrown when a file format is invalid or unsupported.
/// </summary>
public class InvalidFileFormatException : Exception
{
    public InvalidFileFormatException(string message) : base(message) { }

    public InvalidFileFormatException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when atomization process fails.
/// </summary>
public class AtomizationFailedException : Exception
{
    public AtomizationFailedException(string message) : base(message) { }

    public AtomizationFailedException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when provenance tracking fails.
/// </summary>
public class ProvenanceTrackingException : Exception
{
    public ProvenanceTrackingException(string message) : base(message) { }

    public ProvenanceTrackingException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when tenant isolation is violated.
/// </summary>
public class TenantIsolationException : Exception
{
    public TenantIsolationException(string message) : base(message) { }

    public TenantIsolationException(string message, Exception innerException)
        : base(message, innerException) { }
}
