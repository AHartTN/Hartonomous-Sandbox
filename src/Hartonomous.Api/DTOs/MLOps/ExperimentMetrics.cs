namespace Hartonomous.Api.DTOs.MLOps;

public class ExperimentMetrics
{
    public double ControlAccuracy { get; set; }
    public double TreatmentAccuracy { get; set; }
    public double StatisticalSignificance { get; set; }
    public long SampleSize { get; set; }
    public string ConfidenceInterval { get; set; } = string.Empty;
}
