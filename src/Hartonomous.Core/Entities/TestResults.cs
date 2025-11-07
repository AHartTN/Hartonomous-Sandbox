using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Stores test execution results and performance metrics
/// </summary>
public class TestResults
{
    [Key]
    public long TestResultId { get; set; }

    [Required]
    [MaxLength(200)]
    [Column(TypeName = "nvarchar(200)")]
    public string TestName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column(TypeName = "nvarchar(100)")]
    public string TestSuite { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column(TypeName = "nvarchar(50)")]
    public string TestStatus { get; set; } = string.Empty; // Passed, Failed, Skipped, Error

    [Column(TypeName = "float")]
    public double? ExecutionTimeMs { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? ErrorMessage { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? StackTrace { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? TestOutput { get; set; }

    [Required]
    [Column(TypeName = "datetime2")]
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    [Column(TypeName = "nvarchar(100)")]
    public string? Environment { get; set; }

    [MaxLength(50)]
    [Column(TypeName = "nvarchar(50)")]
    public string? TestCategory { get; set; }

    [Column(TypeName = "float")]
    public double? MemoryUsageMB { get; set; }

    [Column(TypeName = "float")]
    public double? CpuUsagePercent { get; set; }
}