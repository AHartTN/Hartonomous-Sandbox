using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Security policies and access controls per tenant
/// </summary>
public class TenantSecurityPolicy
{
    [Key]
    public int PolicyId { get; set; }

    [Required]
    [MaxLength(128)]
    [Column(TypeName = "nvarchar(128)")]
    public string TenantId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column(TypeName = "nvarchar(100)")]
    public string PolicyName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column(TypeName = "nvarchar(50)")]
    public string PolicyType { get; set; } = string.Empty; // rate_limit, access_control, encryption, etc.

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string PolicyRules { get; set; } = string.Empty; // JSON policy definition

    [Required]
    public bool IsActive { get; set; } = true;

    [Column(TypeName = "datetime2")]
    public DateTime? EffectiveFrom { get; set; }

    [Column(TypeName = "datetime2")]
    public DateTime? EffectiveTo { get; set; }

    [Required]
    [Column(TypeName = "datetime2")]
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "datetime2")]
    public DateTime? UpdatedUtc { get; set; }

    [MaxLength(256)]
    [Column(TypeName = "nvarchar(256)")]
    public string? CreatedBy { get; set; }

    [MaxLength(256)]
    [Column(TypeName = "nvarchar(256)")]
    public string? UpdatedBy { get; set; }
}