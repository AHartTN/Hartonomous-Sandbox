namespace Hartonomous.Core.Models.Database;

/// <summary>
/// Database connection information for atomization.
/// </summary>
public class DatabaseConnectionInfo
{
    public required string ConnectionString { get; set; }
    public int MaxTables { get; set; } = 50;
    public int MaxRowsPerTable { get; set; } = 1000;
}
