using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class VwEmbeddingVector : IVwEmbeddingVector
{
    public long SourceAtomId { get; set; }

    public int? ComponentIndex { get; set; }

    public float? ComponentValue { get; set; }

    public long AtomRelationId { get; set; }
}
