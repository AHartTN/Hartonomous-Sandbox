using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IEmbeddingMigrationProgress
{
    long AtomEmbeddingId { get; set; }
    DateTime MigratedAt { get; set; }
    int AtomCount { get; set; }
    int RelationCount { get; set; }
}
