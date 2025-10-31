namespace Hartonomous.Core.Configuration;

/// <summary>
/// Options governing SQL Server connectivity for Hartonomous services.
/// </summary>
public sealed class SqlServerOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "SqlServer";

    /// <summary>
    /// Primary SQL Server connection string.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Default command timeout (seconds) for raw SQL commands.
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 30;
}
