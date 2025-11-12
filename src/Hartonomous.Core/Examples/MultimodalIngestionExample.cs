using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Pipelines.Ingestion;
using Hartonomous.Core.Pipelines.Ingestion.Atomizers;
using Hartonomous.Core.Pipelines.Ingestion.Readers;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Examples;

/// <summary>
/// EXAMPLE: Complete multimodal ingestion pipeline usage
/// 
/// Demonstrates the Reader → Atomizer → Ingestor pattern for:
/// - Text documents with sentence/chunk splitting
/// - Images with tile extraction (TODO)
/// - Audio with transcription segments (TODO)
/// - Video with scene detection (TODO)
/// - Batch ingestion with progress tracking
/// </summary>
public sealed class MultimodalIngestionExample
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IAtomIngestionService _atomIngestionService;

    public MultimodalIngestionExample(
        ILoggerFactory loggerFactory,
        IAtomIngestionService atomIngestionService)
    {
        _loggerFactory = loggerFactory;
        _atomIngestionService = atomIngestionService;
    }

    /// <summary>
    /// Example 1: Ingest a text file with sentence-level atomization
    /// </summary>
    public async Task<IngestionResult> IngestTextFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        // Create orchestrator
        var readerFactory = new ContentReaderFactory(_loggerFactory);
        var orchestrator = new MultimodalIngestionOrchestrator(
            readerFactory,
            _atomIngestionService,
            _loggerFactory.CreateLogger<MultimodalIngestionOrchestrator>(),
            batchSize: 100,
            maxParallelism: 4);

        // Register text atomizer with sentence splitting
        var textAtomizer = new TextAtomizer(
            strategy: TextChunkingStrategy.Sentence,
            logger: _loggerFactory.CreateLogger<TextAtomizer>());
        
        orchestrator.RegisterAtomizer("text", textAtomizer);

        // Setup progress tracking
        var progress = new Progress<IngestionProgress>(p =>
        {
            Console.WriteLine($"[{p.Phase}] {p.PercentComplete:F1}% - " +
                            $"Atoms: {p.AtomsProcessed}/{p.AtomsGenerated}");
        });

        // Ingest with automatic modality detection
        var result = await orchestrator.IngestAsync(
            sourceUri: filePath,
            modalityHint: "text",
            options: new Dictionary<string, object>
            {
                ["maxChunkSize"] = 1000,
                ["overlapSize"] = 100
            },
            progress: progress,
            cancellationToken: cancellationToken);

        // Report results
        if (result.Success)
        {
            Console.WriteLine($"✅ Ingested {result.Statistics.AtomsIngested} atoms");
            Console.WriteLine($"   Duplicates: {result.Statistics.AtomsDuplicated}");
            Console.WriteLine($"   Duration: {result.Statistics.DurationMs}ms");
            Console.WriteLine($"   Throughput: {result.Statistics.ThroughputAtomsPerSecond:F2} atoms/sec");
        }
        else
        {
            Console.WriteLine($"❌ Ingestion failed: {result.Error}");
        }

        return result;
    }

    /// <summary>
    /// Example 2: Ingest multiple files in batch
    /// </summary>
    public async Task<List<IngestionResult>> IngestBatchAsync(
        string[] filePaths,
        CancellationToken cancellationToken = default)
    {
        var readerFactory = new ContentReaderFactory(_loggerFactory);
        var orchestrator = new MultimodalIngestionOrchestrator(
            readerFactory,
            _atomIngestionService,
            _loggerFactory.CreateLogger<MultimodalIngestionOrchestrator>(),
            batchSize: 100,
            maxParallelism: 8);

        // Register atomizers for each modality
        orchestrator.RegisterAtomizer("text", new TextAtomizer(
            TextChunkingStrategy.FixedSize,
            _loggerFactory.CreateLogger<TextAtomizer>()));
        
        orchestrator.RegisterAtomizer("image", new ImageAtomizer(
            ImageAtomizationStrategy.WholeImage,
            _loggerFactory.CreateLogger<ImageAtomizer>()));
        
        orchestrator.RegisterAtomizer("audio", new AudioAtomizer(
            AudioAtomizationStrategy.WholeAudio,
            _loggerFactory.CreateLogger<AudioAtomizer>()));
        
        orchestrator.RegisterAtomizer("video", new VideoAtomizer(
            VideoAtomizationStrategy.WholeVideo,
            _loggerFactory.CreateLogger<VideoAtomizer>()));

        var results = new List<IngestionResult>();
        var completedCount = 0;

        Console.WriteLine($"Ingesting {filePaths.Length} files...");

        foreach (var filePath in filePaths)
        {
            try
            {
                var result = await orchestrator.IngestAsync(
                    filePath,
                    progress: new Progress<IngestionProgress>(p => 
                    {
                        Console.Write($"\r[{completedCount}/{filePaths.Length}] {System.IO.Path.GetFileName(filePath)}: {p.PercentComplete:F0}%");
                    }),
                    cancellationToken: cancellationToken);

                results.Add(result);
                completedCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Failed to ingest {filePath}: {ex.Message}");
                results.Add(new IngestionResult
                {
                    Success = false,
                    Error = ex.Message,
                    Statistics = new IngestionStatistics { SourceUri = filePath }
                });
            }
        }

        Console.WriteLine($"\n✅ Batch complete: {results.Count(r => r.Success)}/{filePaths.Length} succeeded");
        
        return results;
    }

    /// <summary>
    /// Example 3: Custom atomization strategy - paragraph chunking with overlap
    /// </summary>
    public async Task<IngestionResult> IngestWithCustomStrategyAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var readerFactory = new ContentReaderFactory(_loggerFactory);
        var orchestrator = new MultimodalIngestionOrchestrator(
            readerFactory,
            _atomIngestionService,
            _loggerFactory.CreateLogger<MultimodalIngestionOrchestrator>());

        // Use paragraph chunking for long documents
        var textAtomizer = new TextAtomizer(
            strategy: TextChunkingStrategy.Paragraph,
            logger: _loggerFactory.CreateLogger<TextAtomizer>());
        
        orchestrator.RegisterAtomizer("text", textAtomizer);

        var result = await orchestrator.IngestAsync(
            sourceUri: filePath,
            modalityHint: "text",
            options: new Dictionary<string, object>
            {
                ["maxChunkSize"] = 2000,      // Larger chunks
                ["overlapSize"] = 200,         // More overlap
                ["minContentLength"] = 50      // Filter very short paragraphs
            },
            cancellationToken: cancellationToken);

        return result;
    }

    /// <summary>
    /// Example 4: Ingest from HTTP URL with streaming
    /// </summary>
    public async Task<IngestionResult> IngestFromHttpAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        var readerFactory = new ContentReaderFactory(_loggerFactory);
        var orchestrator = new MultimodalIngestionOrchestrator(
            readerFactory,
            _atomIngestionService,
            _loggerFactory.CreateLogger<MultimodalIngestionOrchestrator>());

        // Register atomizers
        orchestrator.RegisterAtomizer("text", new TextAtomizer(
            TextChunkingStrategy.Sentence,
            _loggerFactory.CreateLogger<TextAtomizer>()));
        
        orchestrator.RegisterAtomizer("image", new ImageAtomizer(
            ImageAtomizationStrategy.WholeImage,
            _loggerFactory.CreateLogger<ImageAtomizer>()));

        // Ingest with custom HTTP headers (e.g., authentication)
        var result = await orchestrator.IngestAsync(
            sourceUri: url,
            modalityHint: "auto", // Detect from Content-Type header
            options: new Dictionary<string, object>
            {
                ["headers"] = new Dictionary<string, string>
                {
                    ["Authorization"] = "Bearer YOUR_TOKEN_HERE",
                    ["User-Agent"] = "Hartonomous/1.0"
                }
            },
            progress: new Progress<IngestionProgress>(p =>
            {
                Console.WriteLine($"[HTTP Ingestion] {p.Phase}: {p.BytesProcessed:N0} bytes");
            }),
            cancellationToken: cancellationToken);

        if (result.Success)
        {
            Console.WriteLine($"✅ Ingested from {url}");
            Console.WriteLine($"   Atoms: {result.Statistics.AtomsIngested}");
            Console.WriteLine($"   Downloaded: {result.Statistics.BytesRead:N0} bytes");
        }

        return result;
    }
}

/// <summary>
/// FUTURE: Image ingestion with object detection atomization
/// </summary>
public class ImageIngestionExample
{
    /*
    public async Task<IngestionResult> IngestImageWithObjectDetectionAsync(
        string imagePath,
        CancellationToken cancellationToken = default)
    {
        // TODO: Integrate YOLO/Faster R-CNN for object detection
        // Each detected object becomes a separate atom with:
        // - Bounding box coordinates (AtomBoundary.SpatialBounds)
        // - Object class label (Metadata["objectClass"])
        // - Confidence score (AtomCandidate.QualityScore)
        // - Cropped image region (BinaryPayload)
        // - CLIP embedding for the region
        // - Spatial relationship graph (RelatedAtomIds for overlapping objects)
        
        var imageAtomizer = new ImageAtomizer(
            strategy: ImageAtomizationStrategy.ObjectDetection);
        
        // orchestrator.RegisterAtomizer("image", imageAtomizer);
        // ...
    }
    */
}

/// <summary>
/// FUTURE: Audio ingestion with speaker diarization and transcription
/// </summary>
public class AudioIngestionExample
{
    /*
    public async Task<IngestionResult> IngestAudioWithDiarizationAsync(
        string audioPath,
        CancellationToken cancellationToken = default)
    {
        // TODO: Integrate pyannote.audio for speaker diarization + Whisper for ASR
        // Each speaker turn becomes an atom with:
        // - Start/end timestamps (AtomBoundary.StartTime/EndTime)
        // - Speaker ID (Metadata["speakerId"])
        // - Transcription (CanonicalText)
        // - Audio segment (BinaryPayload)
        // - Whisper embeddings
        // - Hierarchical parent (whole conversation atom)
        
        var audioAtomizer = new AudioAtomizer(
            strategy: AudioAtomizationStrategy.SpeakerDiarization);
        
        // orchestrator.RegisterAtomizer("audio", audioAtomizer);
        // ...
    }
    */
}

/// <summary>
/// FUTURE: Video ingestion with scene detection and keyframe extraction
/// </summary>
public class VideoIngestionExample
{
    /*
    public async Task<IngestionResult> IngestVideoWithSceneDetectionAsync(
        string videoPath,
        CancellationToken cancellationToken = default)
    {
        // TODO: Integrate PySceneDetect + FFmpeg
        // Each scene becomes an atom with:
        // - Scene boundaries (AtomBoundary.StartFrameIndex/EndFrameIndex, StartTime/EndTime)
        // - Keyframe as BinaryPayload
        // - Scene description (CanonicalText from BLIP/LLaVA)
        // - VideoMAE embeddings
        // - Related frames (RelatedAtomIds for keyframes in scene)
        // - Audio track aligned (ParentAtomId links to audio atoms)
        
        var videoAtomizer = new VideoAtomizer(
            strategy: VideoAtomizationStrategy.SceneDetection);
        
        // orchestrator.RegisterAtomizer("video", videoAtomizer);
        // ...
    }
    */
}
