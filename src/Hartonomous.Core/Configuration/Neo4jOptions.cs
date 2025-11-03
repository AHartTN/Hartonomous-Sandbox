using System.ComponentModel.DataAnnotations;

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

    [Required]
    public string Uri { get; set; } = "bolt://localhost:7687";

    [Required]
    public string User { get; set; } = "neo4j";

    [Required]
    public string Password { get; set; } = "neo4jneo4j";

    /// <summary>
    /// Optional database name for multi-database clusters.
    /// </summary>
    public string? Database { get; set; }

    /// <summary>
    /// Maximum size of the driver connection pool.
    /// </summary>
    [Range(1, 400)]
    public int MaxConnectionPoolSize { get; set; } = 100;

    /// <summary>
    /// Connection timeout in seconds.
    /// </summary>
    [Range(1, 300)]
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum concurrent sessions the sync worker should open.
    /// </summary>
    [Range(1, 100)]
    public int MaxConcurrentSessions { get; set; } = 4;

    /// <summary>
    /// Base retry backoff (milliseconds) for transient Neo4j faults.
    /// </summary>
    [Range(50, 10_000)]
    public int RetryBackoffMilliseconds { get; set; } = 500;
}
