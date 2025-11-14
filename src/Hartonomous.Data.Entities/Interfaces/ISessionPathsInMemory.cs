using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface ISessionPathsInMemory
{
    long SessionPathId { get; set; }
    Guid SessionId { get; set; }
    int PathNumber { get; set; }
    Guid? HypothesisId { get; set; }
    string? ResponseText { get; set; }
    byte[]? ResponseVector { get; set; }
    double? Score { get; set; }
    bool IsSelected { get; set; }
    DateTime CreatedUtc { get; set; }
}
