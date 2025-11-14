using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class CdcCheckpoint : ICdcCheckpoint
{
    public string ConsumerGroup { get; set; } = null!;

    public string PartitionId { get; set; } = null!;

    public long Offset { get; set; }

    public long SequenceNumber { get; set; }

    public DateTime LastModified { get; set; }
}
