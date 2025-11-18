namespace Hartonomous.Api.DTOs.Reasoning;

/// <summary>
/// Single reasoning sample in Self-Consistency analysis.
/// </summary>
public sealed class ReasoningSample
{
    /// <summary>
    /// Sample identifier (1-indexed).
    /// </summary>
    public int SampleId { get; set; }

    /// <summary>
    /// Generated reasoning response.
    /// </summary>
    public required string Response { get; set; }

    /// <summary>
    /// Confidence score for this sample (0.0 to 1.0).
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Timestamp when this sample was generated.
    /// </summary>
    public DateTime Timestamp { get; set; }
}
