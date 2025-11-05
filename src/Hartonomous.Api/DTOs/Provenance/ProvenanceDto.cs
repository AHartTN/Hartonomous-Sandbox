namespace Hartonomous.Api.DTOs.Provenance;

public class GenerationStreamDetail
{
    public Guid StreamId { get; set; }
    public required string Scope { get; set; }
    public string? Model { get; set; }
    public DateTime CreatedUtc { get; set; }
    public byte[]? StreamData { get; set; }
}

public class InferenceDetail
{
    public long InferenceId { get; set; }
    public required string TaskType { get; set; }
    public string? InputDataJson { get; set; }
    public string? OutputDataJson { get; set; }
    public string? ModelsUsedJson { get; set; }
    public string? EnsembleStrategy { get; set; }
    public int? TotalDurationMs { get; set; }
    public string? OutputMetadataJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public int StepCount { get; set; }
}

public class InferenceStepDetail
{
    public long InferenceStepId { get; set; }
    public int StepNumber { get; set; }
    public int? ModelId { get; set; }
    public string? OperationType { get; set; }
    public int? DurationMs { get; set; }
    public int? RowsReturned { get; set; }
    public string? MetadataJson { get; set; }
}
