using Hartonomous.Core.Entities;

namespace Hartonomous.Core.Interfaces.Ingestion;

/// <summary>
/// Result of model ingestion operation.
/// </summary>
public class ModelIngestionResult
{
    public bool Success { get; set; }
    public int ModelId { get; set; }
    public Model? Model { get; set; }
    public string? ErrorMessage { get; set; }
}
