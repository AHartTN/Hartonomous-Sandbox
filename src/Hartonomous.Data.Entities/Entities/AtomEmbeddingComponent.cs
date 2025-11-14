using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class AtomEmbeddingComponent : IAtomEmbeddingComponent
{
    public long AtomEmbeddingComponentId { get; set; }

    public long AtomEmbeddingId { get; set; }

    public int ComponentIndex { get; set; }

    public float ComponentValue { get; set; }

    public virtual AtomEmbedding AtomEmbedding { get; set; } = null!;
}
