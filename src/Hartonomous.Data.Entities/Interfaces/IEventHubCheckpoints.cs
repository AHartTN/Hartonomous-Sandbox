using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IEventHubCheckpoints
{
    Guid CheckpointId { get; set; }
    string FullyQualifiedNamespace { get; set; }
    string EventHubName { get; set; }
    string ConsumerGroup { get; set; }
    string PartitionId { get; set; }
    string? OwnerIdentifier { get; set; }
    long? SequenceNumber { get; set; }
    long? Offset { get; set; }
    DateTime LastModifiedTimeUtc { get; set; }
    string Etag { get; set; }
    byte[]? UniqueKeyHash { get; set; }
}
