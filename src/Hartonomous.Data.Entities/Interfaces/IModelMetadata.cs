using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities;

public interface IModelMetadata
{
    int MetadataId { get; set; }
    int ModelId { get; set; }
    string? SupportedTasks { get; set; }
    string? SupportedModalities { get; set; }
    int? MaxInputLength { get; set; }
    int? MaxOutputLength { get; set; }
    int? EmbeddingDimension { get; set; }
    string? PerformanceMetrics { get; set; }
    string? TrainingDataset { get; set; }
    DateOnly? TrainingDate { get; set; }
    string? License { get; set; }
    string? SourceUrl { get; set; }
    Model Model { get; set; }
}
