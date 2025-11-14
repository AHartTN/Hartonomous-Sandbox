using System;
using System.Collections.Generic;
using Hartonomous.Shared.Contracts.Entities;
using Microsoft.Data.SqlTypes;

namespace Hartonomous.Data.Entities;

public partial class AtomicTextToken : IAtomicTextToken, IReferenceTrackedEntity
{
    public long TokenId { get; set; }

    public byte[] TokenHash { get; set; } = null!;

    public string TokenText { get; set; } = null!;

    public int TokenLength { get; set; }

    public SqlVector<float>? TokenEmbedding { get; set; }

    public string? EmbeddingModel { get; set; }

    public int? VocabId { get; set; }

    public long ReferenceCount { get; set; }

    public DateTime FirstSeen { get; set; }

    public DateTime LastReferenced { get; set; }
}
