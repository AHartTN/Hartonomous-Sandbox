using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Large blob storage using FILESTREAM for images, audio, video
/// Enables direct file system access via SqlFileStream API
/// </summary>
public class AtomPayloadStore
{
    [Key]
    public long PayloadId { get; set; }

    [Required]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public Guid RowGuid { get; set; }

    [Required]
    public long AtomId { get; set; }

    [Required]
    [MaxLength(256)]
    [Column(TypeName = "nvarchar(256)")]
    public string ContentType { get; set; } = string.Empty; // MIME type

    [Required]
    [MaxLength(32)]
    [Column(TypeName = "binary(32)")]
    public byte[] ContentHash { get; set; } = Array.Empty<byte>(); // SHA-256 hash

    [Required]
    public long SizeBytes { get; set; }

    [Required]
    [Column(TypeName = "varbinary(max) FILESTREAM")]
    public byte[] PayloadData { get; set; } = Array.Empty<byte>();

    [Column(TypeName = "nvarchar(256)")]
    public string? CreatedBy { get; set; }

    [Required]
    [Column(TypeName = "datetime2")]
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey(nameof(AtomId))]
    public virtual Atom? Atom { get; set; }
}