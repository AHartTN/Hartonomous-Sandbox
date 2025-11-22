namespace Hartonomous.Workers.Neo4jSync.Models;

/// <summary>
/// Represents a message from the Service Broker Neo4jSyncQueue.
/// Matches the XML schema from dbo.sp_EnqueueNeo4jSync.
/// </summary>
public class Neo4jSyncMessage
{
    public string EntityType { get; set; } = string.Empty;
    public long EntityId { get; set; }
    public string SyncType { get; set; } = "CREATE";
    public int TenantId { get; set; }
}
