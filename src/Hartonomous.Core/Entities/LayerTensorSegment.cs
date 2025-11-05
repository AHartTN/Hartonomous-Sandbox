using NetTopologySuite.Geometries;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents a persisted tensor segment for a model layer. Segments allow
/// massive tensors to be stored out-of-row via FILESTREAM while retaining
/// spatial metadata for fast filtering.
/// </summary>
public class LayerTensorSegment
{
    /// <summary>
    /// Primary key for the tensor segment.
    /// </summary>
    public long LayerTensorSegmentId { get; set; }

    /// <summary>
    /// Foreign key to the parent <see cref="ModelLayer"/>.
    /// </summary>
    public long LayerId { get; set; }

    /// <summary>
    /// Segment ordering within the layer. Starts at zero.
    /// </summary>
    public int SegmentOrdinal { get; set; }

    /// <summary>
    /// Absolute offset (point index) of the first value represented by this segment.
    /// </summary>
    public long PointOffset { get; set; }

    /// <summary>
    /// Number of tensor points represented by this segment.
    /// </summary>
    public int PointCount { get; set; }

    /// <summary>
    /// Quantization type identifier (e.g., F32, Q4_K, Q8_0).
    /// </summary>
    public required string QuantizationType { get; set; }

    /// <summary>
    /// Optional scalar quantization scale applied across the segment.
    /// </summary>
    public double? QuantizationScale { get; set; }

    /// <summary>
    /// Optional quantization zero point applied across the segment.
    /// </summary>
    public double? QuantizationZeroPoint { get; set; }

    /// <summary>
    /// Minimum Z value observed in this segment (importance, gradient, etc.).
    /// </summary>
    public double? ZMin { get; set; }

    /// <summary>
    /// Maximum Z value observed in this segment (importance, gradient, etc.).
    /// </summary>
    public double? ZMax { get; set; }

    /// <summary>
    /// Minimum M value observed in this segment (temporal metadata, etc.).
    /// </summary>
    public double? MMin { get; set; }

    /// <summary>
    /// Maximum M value observed in this segment (temporal metadata, etc.).
    /// </summary>
    public double? MMax { get; set; }

    /// <summary>
    /// Morton (Z-order) code representing the segment's 4D bounding box.
    /// </summary>
    public long? MortonCode { get; set; }

    /// <summary>
    /// Optional geometry footprint for mixed spatial/vector queries.
    /// </summary>
    public Geometry? GeometryFootprint { get; set; }

    /// <summary>
    /// Raw quantized payload stored via FILESTREAM in SQL Server.
    /// </summary>
    public byte[] RawPayload { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// FILESTREAM locator to ensure deterministic storage semantics.
    /// </summary>
    public Guid PayloadRowGuid { get; set; }

    /// <summary>
    /// Timestamp for diagnostic and invalidation scenarios.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the owning model layer.
    /// </summary>
    public ModelLayer Layer { get; set; } = null!;
}