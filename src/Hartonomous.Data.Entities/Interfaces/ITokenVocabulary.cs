using System;
using System.Collections.Generic;
using Microsoft.Data.SqlTypes;

namespace Hartonomous.Data.Entities;

public interface ITokenVocabulary
{
    long VocabId { get; set; }
    int ModelId { get; set; }
    string Token { get; set; }
    int TokenId { get; set; }
    string? TokenType { get; set; }
    SqlVector<float>? Embedding { get; set; }
    long Frequency { get; set; }
    DateTime? LastUsed { get; set; }
    Model Model { get; set; }
}
