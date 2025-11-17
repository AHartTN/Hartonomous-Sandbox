using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public partial class Neo4jSyncLog : INeo4jSyncLog
{
    public long LogId { get; set; }

    public string EntityType { get; set; } = null!;

    public long EntityId { get; set; }

    public string SyncType { get; set; } = null!;

    public string SyncStatus { get; set; } = null!;

    public string? Response { get; set; }

    public string? ErrorMessage { get; set; }

    public int RetryCount { get; set; }

    public DateTime SyncTimestamp { get; set; }
}
