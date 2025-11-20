using System;

namespace Hartonomous.Api.DTOs.Audit;


public class AuditEvent
{
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string VersionHash { get; set; } = string.Empty;
    public string? PreviousVersionHash { get; set; }
}
