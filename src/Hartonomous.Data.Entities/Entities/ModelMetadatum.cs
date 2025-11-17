using System;
using System.Collections.Generic;

namespace Hartonomous.Data.Entities.Entities;

public partial class ModelMetadatum : IModelMetadatum
{
    public int MetadataId { get; set; }

    public int ModelId { get; set; }

    public string? SupportedTasks { get; set; }

    public string? SupportedModalities { get; set; }

    public int? MaxInputLength { get; set; }

    public int? MaxOutputLength { get; set; }

    public int? EmbeddingDimension { get; set; }

    public string? PerformanceMetrics { get; set; }

    public string? TrainingDataset { get; set; }

    public DateOnly? TrainingDate { get; set; }

    public string? License { get; set; }

    public string? SourceUrl { get; set; }

    public virtual Model Model { get; set; } = null!;
}
