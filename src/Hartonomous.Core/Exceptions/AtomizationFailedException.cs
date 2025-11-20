using System;

namespace Hartonomous.Core.Exceptions;

/// <summary>
/// Exception thrown when atomization process fails.
/// </summary>
public class AtomizationFailedException : Exception
{
    public AtomizationFailedException(string message) : base(message) { }

    public AtomizationFailedException(string message, Exception innerException)
        : base(message, innerException) { }
}
