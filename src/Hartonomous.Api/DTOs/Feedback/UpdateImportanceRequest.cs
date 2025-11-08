using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Feedback;

public class UpdateImportanceRequest
{
    [Required]
    public required List<AtomImportanceUpdate> Updates { get; set; }

    public string? Reason { get; set; }
}
