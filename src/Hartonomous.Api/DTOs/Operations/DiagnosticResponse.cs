namespace Hartonomous.Api.DTOs.Operations;

public class DiagnosticResponse
{
    public required string DiagnosticType { get; set; }
    public required List<DiagnosticEntry> Entries { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
