using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface ITensorAtomPayload
{
    long PayloadId { get; set; }
    long TensorAtomId { get; set; }
    Guid RowGuid { get; set; }
    byte[]? Payload { get; set; }
    string? Metadata { get; set; }
    DateTime CreatedAt { get; set; }
    TensorAtom TensorAtom { get; set; }
}
