using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Api.DTOs.Feedback;

public class AtomImportanceUpdate
{
    [Required]
    public long AtomId { get; set; }

    [Range(-1.0, 1.0)]
    public double ImportanceDelta { get; set; }
}
