namespace Hartonomous.Core.Enums;

/// <summary>
/// Represents the current state of a streaming ingestion session.
/// </summary>
public enum SessionState
{
    /// <summary>
    /// Session is initializing.
    /// </summary>
    Starting,

    /// <summary>
    /// Session is actively processing data.
    /// </summary>
    Active,

    /// <summary>
    /// Session is temporarily paused.
    /// </summary>
    Paused,

    /// <summary>
    /// Session is in the process of stopping.
    /// </summary>
    Stopping,

    /// <summary>
    /// Session has completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Session has failed with errors.
    /// </summary>
    Failed
}
