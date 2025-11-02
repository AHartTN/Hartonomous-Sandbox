namespace Hartonomous.Core.Configuration;

/// <summary>
/// Configuration options for Neo4j graph database connectivity
/// </summary>
public sealed class Neo4jOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "Neo4j";

    /// <summary>
    /// Neo4j connection URI (e.g., bolt://localhost:7687)
    /// </summary>
    public string? Uri { get; set; }

    /// <summary>
    /// Neo4j username
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Neo4j password
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Default database name (null for default database)
    /// </summary>
    public string? Database { get; set; }

    /// <summary>
    /// Maximum connection pool size
    /// </summary>
    public int MaxConnectionPoolSize { get; set; } = 100;

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;
}
