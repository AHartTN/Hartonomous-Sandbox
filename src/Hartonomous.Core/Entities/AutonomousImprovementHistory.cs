using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Tracks autonomous self-improvement operations and their outcomes
/// </summary>
public class AutonomousImprovementHistory
{
    [Key]
    public Guid ImprovementId { get; set; } = Guid.NewGuid();

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string AnalysisResults { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string GeneratedCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(512)]
    [Column(TypeName = "nvarchar(512)")]
    public string TargetFile { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column(TypeName = "nvarchar(50)")]
    public string ChangeType { get; set; } = string.Empty; // optimization, bugfix, feature

    [Required]
    [MaxLength(20)]
    [Column(TypeName = "nvarchar(20)")]
    public string RiskLevel { get; set; } = string.Empty; // low, medium, high

    [MaxLength(20)]
    [Column(TypeName = "nvarchar(20)")]
    public string? EstimatedImpact { get; set; } // low, medium, high

    [MaxLength(64)]
    [Column(TypeName = "nvarchar(64)")]
    public string? GitCommitHash { get; set; }

    [Column(TypeName = "decimal(5,4)")]
    [Range(0, 1)]
    public decimal? SuccessScore { get; set; } // 0.0000 to 1.0000

    public int? TestsPassed { get; set; }
    public int? TestsFailed { get; set; }

    [Column(TypeName = "decimal(10,4)")]
    public decimal? PerformanceDelta { get; set; } // Percentage change in performance

    [Column(TypeName = "nvarchar(max)")]
    public string? ErrorMessage { get; set; }

    public bool WasDeployed { get; set; } = false;
    public bool WasRolledBack { get; set; } = false;

    [Required]
    [Column(TypeName = "datetime2(7)")]
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "datetime2(7)")]
    public DateTime? CompletedAt { get; set; }

    [Column(TypeName = "datetime2(7)")]
    public DateTime? RolledBackAt { get; set; }
}