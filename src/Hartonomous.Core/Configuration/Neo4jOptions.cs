using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Core.Configuration;

/// <summary>
/// Neo4j database configuration options.
/// </summary>
public class Neo4jOptions
{
    public const string SectionName = "Neo4j";

    /// <summary>
    /// Whether Neo4j is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Neo4j connection URI (e.g., bolt://localhost:7687).
    /// </summary>
    [Required]
    public string Uri { get; set; } = "bolt://localhost:7687";

    /// <summary>
    /// Neo4j username.
    /// </summary>
    [Required]
    public string Username { get; set; } = "neo4j";

    /// <summary>
    /// Neo4j password.
    /// </summary>
    [Required]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Neo4j database name.
    /// </summary>
    public string Database { get; set; } = "neo4j";

    /// <summary>
    /// Maximum connection pool size.
    /// </summary>
    [Range(1, 1000)]
    public int MaxConnectionPoolSize { get; set; } = 50;

    /// <summary>
    /// Connection timeout in seconds.
    /// </summary>
    [Range(1, 300)]
    public int ConnectionTimeoutSeconds { get; set; } = 30;
}
