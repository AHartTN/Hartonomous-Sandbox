namespace Hartonomous.Api.DTOs.Operations;

public class BackupResponse
{
    public required string BackupType { get; set; }
    public required string BackupPath { get; set; }
    public long BackupSizeMB { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime CompletedAt { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
}
