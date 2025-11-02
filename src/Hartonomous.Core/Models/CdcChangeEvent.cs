namespace Hartonomous.Core.Models;

/// <summary>
/// Represents a SQL Server Change Data Capture event.
/// Encapsulates CDC metadata and changed data.
/// </summary>
public class CdcChangeEvent
{
    /// <summary>
    /// Log Sequence Number - unique identifier for this change
    /// </summary>
    public string Lsn { get; init; } = string.Empty;

    /// <summary>
    /// CDC operation type: 1=delete, 2=insert, 3=update_before, 4=update_after
    /// </summary>
    public int Operation { get; init; }

    /// <summary>
    /// Table name in format "schema.table"
    /// </summary>
    public string TableName { get; init; } = string.Empty;

    /// <summary>
    /// The changed data (column values)
    /// </summary>
    public Dictionary<string, object>? Data { get; init; }
}
