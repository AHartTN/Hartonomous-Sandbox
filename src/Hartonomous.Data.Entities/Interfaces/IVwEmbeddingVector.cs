using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IVwEmbeddingVector
{
    long SourceAtomId { get; set; }
    int? ComponentIndex { get; set; }
    float? ComponentValue { get; set; }
    long AtomRelationId { get; set; }
}
