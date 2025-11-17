using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class AtomEmbeddingComponents : IAtomEmbeddingComponents
{
    public long AtomEmbeddingComponentId { get; set; }

    public long AtomEmbeddingId { get; set; }

    public int ComponentIndex { get; set; }

    public float ComponentValue { get; set; }

    public virtual AtomEmbeddings AtomEmbedding { get; set; } = null!;
}
