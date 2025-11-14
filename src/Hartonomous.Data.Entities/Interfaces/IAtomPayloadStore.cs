using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IAtomPayloadStore
{
    long PayloadId { get; set; }
    Guid RowGuid { get; set; }
    long AtomId { get; set; }
    string ContentType { get; set; }
    byte[] ContentHash { get; set; }
    long SizeBytes { get; set; }
    byte[] PayloadData { get; set; }
    string? CreatedBy { get; set; }
    DateTime CreatedUtc { get; set; }
    Atom Atom { get; set; }
}
