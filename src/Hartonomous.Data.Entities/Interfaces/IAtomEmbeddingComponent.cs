using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public interface IAtomEmbeddingComponent
{
    long AtomEmbeddingComponentId { get; set; }
    long AtomEmbeddingId { get; set; }
    int ComponentIndex { get; set; }
    float ComponentValue { get; set; }
    AtomEmbedding AtomEmbedding { get; set; }
}
