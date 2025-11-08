namespace Hartonomous.Core.Interfaces.Ingestion;

/// <summary>
/// Request object for model ingestion.
/// </summary>
public class ModelIngestionRequest
{
    public string ModelPath { get; set; } = string.Empty;
    public string? CustomName { get; set; }
}
