namespace Hartonomous.Api.DTOs.Operations;

public class BackupRequest
{
    public string BackupType { get; set; } = "full"; // full, differential, log
    public string? BackupPath { get; set; }
    public bool Compression { get; set; } = true;
    public bool Checksum { get; set; } = true;
}
