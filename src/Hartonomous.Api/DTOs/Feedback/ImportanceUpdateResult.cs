namespace Hartonomous.Api.DTOs.Feedback;

public class ImportanceUpdateResult
{
    public long AtomId { get; set; }
    public bool Success { get; set; }
    public double? PreviousImportance { get; set; }
    public double? NewImportance { get; set; }
    public string? Message { get; set; }
}
