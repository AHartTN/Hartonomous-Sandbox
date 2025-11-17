using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public interface ICdcCheckpoint
{
    string ConsumerGroup { get; set; }
    string PartitionId { get; set; }
    long Offset { get; set; }
    long SequenceNumber { get; set; }
    DateTime LastModified { get; set; }
}
