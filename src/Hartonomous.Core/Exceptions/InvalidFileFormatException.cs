using System;

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
