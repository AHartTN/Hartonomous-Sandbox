using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class SessionPathsInMemory : ISessionPathsInMemory
{
    public long SessionPathId { get; set; }

    public Guid SessionId { get; set; }

    public int PathNumber { get; set; }

    public Guid? HypothesisId { get; set; }

    public string? ResponseText { get; set; }

    public byte[]? ResponseVector { get; set; }

    public double? Score { get; set; }

    public bool IsSelected { get; set; }

    public DateTime CreatedUtc { get; set; }
}
