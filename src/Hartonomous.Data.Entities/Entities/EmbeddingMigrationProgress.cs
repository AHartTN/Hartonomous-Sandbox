using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class EmbeddingMigrationProgress : IEmbeddingMigrationProgress
{
    public long AtomEmbeddingId { get; set; }

    public DateTime MigratedAt { get; set; }

    public int AtomCount { get; set; }

    public int RelationCount { get; set; }
}
