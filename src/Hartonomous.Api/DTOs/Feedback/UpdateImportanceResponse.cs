namespace Hartonomous.Api.DTOs.Feedback;

public class UpdateImportanceResponse
{
    public int UpdatedCount { get; set; }
    public required List<ImportanceUpdateResult> Results { get; set; }
}
