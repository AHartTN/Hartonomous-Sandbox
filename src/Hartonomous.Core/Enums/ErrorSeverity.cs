namespace Hartonomous.Core.Enums;

/// <summary>
/// Represents the severity level of an error or validation failure.
/// </summary>
public enum ErrorSeverity
{
    /// <summary>
    /// No error occurred.
    /// </summary>
    None = 0,

    /// <summary>
    /// Informational message (not an error).
    /// </summary>
    Information = 1,

    /// <summary>
    /// Warning - operation succeeded with caveats.
    /// </summary>
    Warning = 2,

    /// <summary>
    /// Error - operation failed but can be retried.
    /// </summary>
    Error = 3,

    /// <summary>
    /// Critical error - system-level failure, requires immediate attention.
    /// </summary>
    Critical = 4,

    /// <summary>
    /// Fatal error - unrecoverable failure, application should terminate.
    /// </summary>
    Fatal = 5
}
