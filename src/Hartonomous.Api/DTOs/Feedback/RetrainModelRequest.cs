using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Feedback;

public class RetrainModelRequest
{
    [Required]
    public int ModelId { get; set; }

    public required List<long> AtomIds { get; set; }

    public string Strategy { get; set; } = "importance_boost"; // importance_boost, fine_tune, distill

    [Range(0.0, 2.0)]
    public double BoostFactor { get; set; } = 1.5;
}
