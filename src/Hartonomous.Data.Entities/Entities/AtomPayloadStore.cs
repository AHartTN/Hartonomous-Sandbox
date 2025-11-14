using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class AtomPayloadStore : IAtomPayloadStore
{
    public long PayloadId { get; set; }

    public Guid RowGuid { get; set; }

    public long AtomId { get; set; }

    public string ContentType { get; set; } = null!;

    public byte[] ContentHash { get; set; } = null!;

    public long SizeBytes { get; set; }

    public byte[] PayloadData { get; set; } = null!;

    public string? CreatedBy { get; set; }

    public DateTime CreatedUtc { get; set; }

    public virtual Atom Atom { get; set; } = null!;
}
