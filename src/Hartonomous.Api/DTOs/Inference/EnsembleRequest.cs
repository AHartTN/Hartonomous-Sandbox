namespace Hartonomous.Api.DTOs.Inference;

public class EnsembleRequest
{
    public required float[] Embedding { get; set; }
    public List<int>? ModelIds { get; set; }
    public string? TaskType { get; set; }
    public int? TopK { get; set; }
}

public class EnsembleResponse
{
    public long InferenceId { get; set; }
    public List<EnsembleResult> Results { get; set; } = new();
}

public class EnsembleResult
{
    public long AtomId { get; set; }
    public string? Modality { get; set; }
    public string? Subtype { get; set; }
    public double EnsembleScore { get; set; }
    public int ModelCount { get; set; }
    public bool IsConsensus { get; set; }
}
