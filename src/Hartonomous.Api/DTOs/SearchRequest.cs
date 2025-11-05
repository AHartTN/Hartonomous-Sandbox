using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs;

public class SearchRequest
{
    public string? QueryText { get; set; }
    public float[]? QueryEmbedding { get; set; }
    public float[]? QueryVector { get; set; }
    
    [Range(1, 1000)]
    public int TopK { get; set; } = 10;
    
    public string? ModalityFilter { get; set; }
    public double? MinimumSimilarity { get; set; }
    
    // Additional filter properties
    public string? TopicFilter { get; set; }
    public float? MinSentiment { get; set; }
    public int? MaxAge { get; set; }
    
    [Range(10, 10000)]
    public int CandidateCount { get; set; } = 100;
}
