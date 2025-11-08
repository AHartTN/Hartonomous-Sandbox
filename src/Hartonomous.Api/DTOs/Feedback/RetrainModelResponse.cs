namespace Hartonomous.Api.DTOs.Feedback;

public class RetrainModelResponse
{
    public int ModelId { get; set; }
    public required string Strategy { get; set; }
    public int AtomsAffected { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
    public TimeSpan Duration { get; set; }
}
