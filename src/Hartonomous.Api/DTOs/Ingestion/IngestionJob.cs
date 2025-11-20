using System;
using System.Collections.Generic;

namespace Hartonomous.Api.DTOs.Ingestion;


public class IngestionJob
{
    public string JobId { get; set; } = "";
    public string? FileName { get; set; }
    public long FileSizeBytes { get; set; }
    public string Status { get; set; } = "pending";
    public string? DetectedType { get; set; }
    public string? DetectedCategory { get; set; }
    public int TotalAtoms { get; set; }
    public int UniqueAtoms { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long DurationMs { get; set; }
    public List<string>? ChildJobs { get; set; }
    public string? ErrorMessage { get; set; }
    public int TenantId { get; set; }
}
