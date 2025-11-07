using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hartonomous.Core.Entities;

/// <summary>
/// High-frequency append-only ledger for billing events
/// Optimized for tenant-based queries and analytics
/// </summary>
public class BillingUsageLedger
{
    [Key]
    public long LedgerId { get; set; }

    [Required]
    [MaxLength(128)]
    [Column(TypeName = "nvarchar(128)")]
    public string TenantId { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    [Column(TypeName = "nvarchar(256)")]
    public string PrincipalId { get; set; } = string.Empty;

    [Required]
    [MaxLength(128)]
    [Column(TypeName = "nvarchar(128)")]
    public string Operation { get; set; } = string.Empty;

    [MaxLength(128)]
    [Column(TypeName = "nvarchar(128)")]
    public string? MessageType { get; set; }

    [MaxLength(256)]
    [Column(TypeName = "nvarchar(256)")]
    public string? Handler { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,6)")]
    public decimal Units { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,6)")]
    public decimal BaseRate { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,6)")]
    public decimal Multiplier { get; set; } = 1.0m;

    [Required]
    [Column(TypeName = "decimal(18,6)")]
    public decimal TotalCost { get; set; }

    [Column(TypeName = "nvarchar(max)")]
    public string? MetadataJson { get; set; }

    [Required]
    [Column(TypeName = "datetime2(7)")]
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}