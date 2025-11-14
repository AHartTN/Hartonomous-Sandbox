using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IProvenanceValidationResult
{
    int Id { get; set; }
    Guid OperationId { get; set; }
    string? ValidationResults { get; set; }
    string OverallStatus { get; set; }
    int ValidationDurationMs { get; set; }
    DateTime ValidatedAt { get; set; }
    OperationProvenance Operation { get; set; }
}
