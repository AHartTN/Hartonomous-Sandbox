using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public interface INeo4jSyncLog
{
    long LogId { get; set; }
    string EntityType { get; set; }
    long EntityId { get; set; }
    string SyncType { get; set; }
    string SyncStatus { get; set; }
    string? Response { get; set; }
    string? ErrorMessage { get; set; }
    int RetryCount { get; set; }
    DateTime SyncTimestamp { get; set; }
}
