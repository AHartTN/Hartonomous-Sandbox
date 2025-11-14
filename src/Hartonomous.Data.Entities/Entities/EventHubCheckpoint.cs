using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class EventHubCheckpoint : IEventHubCheckpoint
{
    public Guid CheckpointId { get; set; }

    public string FullyQualifiedNamespace { get; set; } = null!;

    public string EventHubName { get; set; } = null!;

    public string ConsumerGroup { get; set; } = null!;

    public string PartitionId { get; set; } = null!;

    public string? OwnerIdentifier { get; set; }

    public long? SequenceNumber { get; set; }

    public long? Offset { get; set; }

    public DateTime LastModifiedTimeUtc { get; set; }

    public string Etag { get; set; } = null!;

    public byte[]? UniqueKeyHash { get; set; }
}
