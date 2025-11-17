using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public partial class InferenceSteps : IInferenceSteps
{
    public long StepId { get; set; }

    public long InferenceId { get; set; }

    public int StepNumber { get; set; }

    public int? ModelId { get; set; }

    public long? LayerId { get; set; }

    public string? OperationType { get; set; }

    public string? QueryText { get; set; }

    public string? IndexUsed { get; set; }

    public long? RowsExamined { get; set; }

    public long? RowsReturned { get; set; }

    public int? DurationMs { get; set; }

    public bool CacheUsed { get; set; }

    public virtual InferenceRequests Inference { get; set; } = null!;

    public virtual Models? Model { get; set; }
}
