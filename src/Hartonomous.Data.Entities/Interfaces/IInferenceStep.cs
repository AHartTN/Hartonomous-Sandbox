using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public interface IInferenceStep
{
    long StepId { get; set; }
    long InferenceId { get; set; }
    int StepNumber { get; set; }
    int? ModelId { get; set; }
    long? LayerId { get; set; }
    string? OperationType { get; set; }
    string? QueryText { get; set; }
    string? IndexUsed { get; set; }
    long? RowsExamined { get; set; }
    long? RowsReturned { get; set; }
    int? DurationMs { get; set; }
    bool CacheUsed { get; set; }
    InferenceRequest Inference { get; set; }
    Model? Model { get; set; }
}
