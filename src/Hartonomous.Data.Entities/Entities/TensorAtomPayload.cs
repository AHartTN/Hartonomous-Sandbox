using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class TensorAtomPayload : ITensorAtomPayload
{
    public long PayloadId { get; set; }

    public long TensorAtomId { get; set; }

    public Guid RowGuid { get; set; }

    public byte[]? Payload { get; set; }

    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual TensorAtom TensorAtom { get; set; } = null!;
}
