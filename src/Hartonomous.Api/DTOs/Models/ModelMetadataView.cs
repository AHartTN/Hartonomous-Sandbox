namespace Hartonomous.Api.DTOs.Models;

public class ModelMetadataView
{
    public ModelMetadataView(string? supportedTasks, string? supportedModalities, int? maxInputLength, int? maxOutputLength,
        int? embeddingDimension, string? performanceMetrics, string? trainingDataset, DateTime? trainingDate, string? license, string? sourceUrl)
    {
        SupportedTasks = supportedTasks;
        SupportedModalities = supportedModalities;
        MaxInputLength = maxInputLength;
        MaxOutputLength = maxOutputLength;
        EmbeddingDimension = embeddingDimension;
        PerformanceMetrics = performanceMetrics;
        TrainingDataset = trainingDataset;
        TrainingDate = trainingDate;
        License = license;
        SourceUrl = sourceUrl;
    }

    public string? SupportedTasks { get; set; }
    public string? SupportedModalities { get; set; }
    public int? MaxInputLength { get; set; }
    public int? MaxOutputLength { get; set; }
    public int? EmbeddingDimension { get; set; }
    public string? PerformanceMetrics { get; set; }
    public string? TrainingDataset { get; set; }
    public DateTime? TrainingDate { get; set; }
    public string? License { get; set; }
    public string? SourceUrl { get; set; }
}
