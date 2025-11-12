using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Pipelines.Ingestion;

/// <summary>
/// ATOMIZER LAYER: Raw streams → semantic atoms
/// 
/// Atomizers transform raw content streams into semantically meaningful units (atoms).
/// Each modality has specialized atomization strategies:
/// 
/// - Text: Sentence splitting, chunk windowing, semantic segmentation
/// - Image: Tile extraction, object detection regions, OCR blocks
/// - Audio: Transcription segments, speaker turns, silence-based splits
/// - Video: Scene detection, keyframe extraction, shot boundaries
/// - Sensor: Time-series windowing, event detection, anomaly segments
/// - Graph: Subgraph extraction, community detection, ego networks
/// 
/// Atomizers return IAsyncEnumerable<AtomCandidate> for streaming large sources.
/// </summary>
public interface IAtomizer<TSource>
{
    /// <summary>
    /// Target modality for atoms produced by this atomizer
    /// </summary>
    string Modality { get; }

    /// <summary>
    /// Atomize source content into semantic units.
    /// Returns candidates that will be deduplicated, embedded, and ingested.
    /// </summary>
    IAsyncEnumerable<AtomCandidate> AtomizeAsync(
        TSource source,
        AtomizationContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a candidate atom before deduplication and embedding
/// </summary>
public sealed class AtomCandidate
{
    public required string Modality { get; init; }
    public required string Subtype { get; init; }
    
    // Content (exactly ONE should be non-null)
    public string? CanonicalText { get; init; }
    public byte[]? BinaryPayload { get; init; }
    public string? ContentJson { get; init; }
    
    // Provenance
    public required string SourceUri { get; init; }
    public required string SourceType { get; init; }
    public AtomBoundary? Boundary { get; init; } // Where in source this atom came from
    
    // Relationships
    public long? ParentAtomId { get; init; } // For hierarchical atoms (e.g., sentence → paragraph)
    public List<long>? RelatedAtomIds { get; init; } // Sibling atoms (e.g., video frames in same scene)
    
    // Metadata
    public Dictionary<string, object>? Metadata { get; init; }
    public Dictionary<string, object>? Semantics { get; init; }
    
    // Quality
    public double QualityScore { get; init; } = 1.0; // 0-1, used for filtering
    public string? HashInput { get; init; } // Override hash computation
}

/// <summary>
/// Describes where an atom was extracted from within a source
/// </summary>
public sealed class AtomBoundary
{
    // Text boundaries
    public int? StartCharIndex { get; init; }
    public int? EndCharIndex { get; init; }
    public int? StartLineNumber { get; init; }
    public int? EndLineNumber { get; init; }
    
    // Binary boundaries
    public long? StartByteOffset { get; init; }
    public long? EndByteOffset { get; init; }
    
    // Temporal boundaries (audio/video)
    public TimeSpan? StartTime { get; init; }
    public TimeSpan? EndTime { get; init; }
    public int? StartFrameIndex { get; init; }
    public int? EndFrameIndex { get; init; }
    
    // Spatial boundaries (images/video frames)
    public BoundingBox? SpatialBounds { get; init; }
    
    // Hierarchical path (e.g., "document/chapter[2]/paragraph[5]")
    public string? StructuralPath { get; init; }
}

/// <summary>
/// Bounding box for spatial atomization (images, video, spatial data)
/// </summary>
public sealed class BoundingBox
{
    public int X { get; init; }
    public int Y { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public double Confidence { get; init; } = 1.0; // For detected regions
    public string? Label { get; init; } // For labeled regions (e.g., "person", "car")
}

/// <summary>
/// Configuration for atomization behavior
/// </summary>
public sealed record AtomizationContext
{
    public required string SourceUri { get; init; }
    public required string SourceType { get; init; }
    
    // Chunking configuration
    public int MaxChunkSize { get; init; } = 1000; // Characters for text, bytes for binary
    public int OverlapSize { get; init; } = 100; // Overlap between chunks
    public bool PreserveStructure { get; init; } = true; // Respect sentence/paragraph boundaries
    
    // Quality filters
    public double MinQualityScore { get; init; } = 0.5;
    public int MinContentLength { get; init; } = 10; // Minimum chars/bytes
    
    // Modality-specific hints
    public Dictionary<string, object>? Hints { get; init; }
    
    // Provenance
    public string? IngestionJobId { get; init; }
    public DateTime IngestionTimestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// TEXT ATOMIZER: Splits text into semantic chunks
/// 
/// Strategies:
/// - Sentence splitting (respects punctuation, quotes, abbreviations)
/// - Fixed-size chunks with overlap (for long documents)
/// - Semantic segmentation (topic boundaries, structural breaks)
/// - Hierarchical chunks (document → section → paragraph → sentence)
/// </summary>
public interface ITextAtomizer : IAtomizer<string>
{
    /// <summary>
    /// Chunking strategy to use
    /// </summary>
    TextChunkingStrategy Strategy { get; }
}

public enum TextChunkingStrategy
{
    /// <summary>Split on sentence boundaries</summary>
    Sentence,
    
    /// <summary>Fixed character count with overlap</summary>
    FixedSize,
    
    /// <summary>Respect paragraph boundaries</summary>
    Paragraph,
    
    /// <summary>Semantic topic segmentation</summary>
    Semantic,
    
    /// <summary>Preserve document structure (headings, lists, code blocks)</summary>
    Structural
}

/// <summary>
/// IMAGE ATOMIZER: Extracts visual atoms from images
/// 
/// Strategies:
/// - Tile extraction (sliding window for large images)
/// - Object detection regions (YOLO, Faster R-CNN bounding boxes)
/// - OCR text blocks (Tesseract, PaddleOCR)
/// - Salient region detection (attention maps)
/// - Perceptual hashing for deduplication
/// </summary>
public interface IImageAtomizer : IAtomizer<byte[]>
{
    ImageAtomizationStrategy Strategy { get; }
}

public enum ImageAtomizationStrategy
{
    /// <summary>Whole image as single atom</summary>
    WholeImage,
    
    /// <summary>Fixed-size tiles with overlap</summary>
    TileExtraction,
    
    /// <summary>Object detection bounding boxes</summary>
    ObjectDetection,
    
    /// <summary>OCR text regions</summary>
    OcrBlocks,
    
    /// <summary>Salient region detection</summary>
    SalientRegions
}

/// <summary>
/// AUDIO ATOMIZER: Segments audio into semantic units
/// 
/// Strategies:
/// - Silence-based segmentation (split on pauses)
/// - Speaker diarization (one atom per speaker turn)
/// - Transcription segments (aligned with ASR timestamps)
/// - Fixed-duration windows with overlap
/// - Music segmentation (intro, verse, chorus, outro)
/// </summary>
public interface IAudioAtomizer : IAtomizer<byte[]>
{
    AudioAtomizationStrategy Strategy { get; }
}

public enum AudioAtomizationStrategy
{
    /// <summary>Whole audio file as single atom</summary>
    WholeAudio,
    
    /// <summary>Split on silence gaps</summary>
    SilenceBased,
    
    /// <summary>One atom per speaker turn</summary>
    SpeakerDiarization,
    
    /// <summary>ASR transcription segments</summary>
    TranscriptionSegments,
    
    /// <summary>Fixed duration windows</summary>
    FixedDuration
}

/// <summary>
/// VIDEO ATOMIZER: Extracts video segments and frames
/// 
/// Strategies:
/// - Scene detection (PySceneDetect, content-based cuts)
/// - Keyframe extraction (I-frames, visual diversity sampling)
/// - Shot boundary detection (hard cuts, dissolves, fades)
/// - Object tracking segments (track person/object across frames)
/// - Audio-visual alignment (combine with audio atomization)
/// </summary>
public interface IVideoAtomizer : IAtomizer<byte[]>
{
    VideoAtomizationStrategy Strategy { get; }
}

public enum VideoAtomizationStrategy
{
    /// <summary>Whole video as single atom</summary>
    WholeVideo,
    
    /// <summary>Scene detection (content-based cuts)</summary>
    SceneDetection,
    
    /// <summary>Keyframe extraction</summary>
    KeyframeExtraction,
    
    /// <summary>Shot boundary detection</summary>
    ShotBoundaries,
    
    /// <summary>Fixed-duration segments</summary>
    FixedDuration
}

/// <summary>
/// SENSOR ATOMIZER: Windows time-series sensor data
/// 
/// Strategies:
/// - Fixed-duration windows (e.g., 5-second segments)
/// - Event detection (anomalies, threshold crossings)
/// - Statistical change-point detection
/// - Frequency-domain segmentation (FFT-based)
/// </summary>
public interface ISensorAtomizer : IAtomizer<(DateTime Timestamp, double Value)[]>
{
    SensorAtomizationStrategy Strategy { get; }
}

public enum SensorAtomizationStrategy
{
    /// <summary>Fixed-duration time windows</summary>
    FixedDuration,
    
    /// <summary>Event-triggered segments</summary>
    EventDetection,
    
    /// <summary>Statistical change-point detection</summary>
    ChangePoint,
    
    /// <summary>Sliding window with overlap</summary>
    SlidingWindow
}
