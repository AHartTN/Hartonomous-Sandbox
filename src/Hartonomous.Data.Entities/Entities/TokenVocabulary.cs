using System;
using System.Collections.Generic;
using Microsoft.Data.SqlTypes;

namespace Hartonomous.Data.Entities.Entities;

public partial class TokenVocabulary : ITokenVocabulary
{
    public long VocabId { get; set; }

    public int ModelId { get; set; }

    public string Token { get; set; } = null!;

    public int TokenId { get; set; }

    public string? TokenType { get; set; }

    public SqlVector<float>? Embedding { get; set; }

    public long Frequency { get; set; }

    public DateTime? LastUsed { get; set; }

    public virtual Model Model { get; set; } = null!;
}
