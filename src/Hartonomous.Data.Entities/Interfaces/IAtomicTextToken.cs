using System;
using System.Collections.Generic;
using Microsoft.Data.SqlTypes;

namespace Hartonomous.Data.Entities;

public interface IAtomicTextToken
{
    long TokenId { get; set; }
    byte[] TokenHash { get; set; }
    string TokenText { get; set; }
    int TokenLength { get; set; }
    SqlVector<float>? TokenEmbedding { get; set; }
    string? EmbeddingModel { get; set; }
    int? VocabId { get; set; }
    long ReferenceCount { get; set; }
    DateTime FirstSeen { get; set; }
    DateTime LastReferenced { get; set; }
}
