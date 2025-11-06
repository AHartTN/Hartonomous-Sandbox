using Microsoft.Data.SqlTypes;
using NetTopologySuite.Geometries;

namespace Hartonomous.Core.Entities;

/// <summary>
/// Represents an image with spatial and vector representations for multi-modal AI processing.
/// Combines traditional image metadata with geometric spatial representations and embeddings.
/// Maps to dbo.Images table from 02_MultiModalData.sql.
/// </summary>
public sealed class Image
{
    /// <summary>
    /// Gets or sets the unique identifier for the image.
    /// </summary>
    public long ImageId { get; set; }

    /// <summary>
    /// Gets or sets the file system path where the image is stored.
    /// </summary>
    public string? SourcePath { get; set; }

    /// <summary>
    /// Gets or sets the URL from which the image was retrieved.
    /// </summary>
    public string? SourceUrl { get; set; }

    /// <summary>
    /// Gets or sets the width of the image in pixels.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Gets or sets the height of the image in pixels.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Gets or sets the number of color channels (1 for grayscale, 3 for RGB, 4 for RGBA).
    /// </summary>
    public int Channels { get; set; }

    /// <summary>
    /// Gets or sets the image format (e.g., 'png', 'jpg', 'bmp', 'webp').
    /// </summary>
    public string? Format { get; set; }

    /// <summary>
    /// Gets or sets a geometric MULTIPOINT representation of representative pixels for spatial queries.
    /// </summary>
    public Geometry? PixelCloud { get; set; }

    /// <summary>
    /// Gets or sets a LINESTRING geometry representing detected edges in the image.
    /// </summary>
    public Geometry? EdgeMap { get; set; }

    /// <summary>
    /// Gets or sets a MULTIPOLYGON geometry representing segmented objects in the image.
    /// </summary>
    public Geometry? ObjectRegions { get; set; }

    /// <summary>
    /// Gets or sets a POLYGON geometry representing attention or saliency regions.
    /// </summary>
    public Geometry? SaliencyRegions { get; set; }

    /// <summary>
    /// Gets or sets the global image embedding vector (typically CLIP, ResNet, or similar).
    /// </summary>
    public SqlVector<float>? GlobalEmbedding { get; set; }

    /// <summary>
    /// Gets or sets the dimensionality of the global embedding vector.
    /// </summary>
    public int? GlobalEmbeddingDim { get; set; }

    /// <summary>
    /// Gets or sets additional metadata as JSON (e.g., EXIF data, detected objects, classifications).
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the image was ingested into the system.
    /// </summary>
    public DateTime? IngestionDate { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last access to this image.
    /// </summary>
    public DateTime? LastAccessed { get; set; }

    /// <summary>
    /// Gets or sets the total number of times this image has been accessed.
    /// </summary>
    public long AccessCount { get; set; }

    /// <summary>
    /// Gets or sets the collection of image patches for fine-grained analysis.
    /// </summary>
    public ICollection<ImagePatch> Patches { get; set; } = new List<ImagePatch>();
}
