using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IAtomEmbeddingComponents
{
    long AtomEmbeddingComponentId { get; set; }
    long AtomEmbeddingId { get; set; }
    int ComponentIndex { get; set; }
    float ComponentValue { get; set; }
    AtomEmbeddings AtomEmbedding { get; set; }
}
