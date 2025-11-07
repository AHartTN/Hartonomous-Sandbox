using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Caches inference results to avoid redundant computations
/// </summary>
public class InferenceCache
{
    [Key]
    public long CacheId { get; set; }

    [Required]
    [MaxLength(64)]
    [Column(TypeName = "nvarchar(64)")]
    public string CacheKey { get; set; } = string.Empty; // Hash of inputs

    [Required]
    public int ModelId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column(TypeName = "nvarchar(100)")]
    public string InferenceType { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "varbinary(max)")]
    public byte[] InputHash { get; set; } = Array.Empty<byte>();

    [Required]
    [Column(TypeName = "varbinary(max)")]
    public byte[] OutputData { get; set; } = Array.Empty<byte>();

    [Column(TypeName = "varbinary(max)")]
    public byte[]? IntermediateStates { get; set; }

    [Required]
    [Column(TypeName = "datetime2")]
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "datetime2")]
    public DateTime? LastAccessedUtc { get; set; }

    public long AccessCount { get; set; } = 0;

    [Column(TypeName = "bigint")]
    public long? SizeBytes { get; set; }

    [Column(TypeName = "float")]
    public double? ComputeTimeMs { get; set; }

    // Navigation property
    [ForeignKey(nameof(ModelId))]
    public virtual Model? Model { get; set; }
}