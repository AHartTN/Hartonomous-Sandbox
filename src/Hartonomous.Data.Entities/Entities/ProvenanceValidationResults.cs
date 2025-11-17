using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class ProvenanceValidationResults : IProvenanceValidationResults
{
    public int Id { get; set; }

    public Guid OperationId { get; set; }

    public string? ValidationResults { get; set; }

    public string OverallStatus { get; set; } = null!;

    public int ValidationDurationMs { get; set; }

    public DateTime ValidatedAt { get; set; }

    public virtual OperationProvenance Operation { get; set; } = null!;
}
