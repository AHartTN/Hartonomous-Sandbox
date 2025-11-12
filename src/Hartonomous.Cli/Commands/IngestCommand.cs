using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Pipelines.Ingestion;
using Hartonomous.Core.Pipelines.Ingestion.Atomizers;
using Hartonomous.Core.Pipelines.Ingestion.Readers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Cli.Commands;

/// <summary>
/// CLI command for multimodal content ingestion
/// 
/// Usage:
///   hartonomous ingest --path ./data --modality text --strategy sentence
///   hartonomous ingest --path ./images/*.jpg --batch-size 50 --parallel 8
///   hartonomous ingest --path ./video.mp4 --modality video --strategy scene-detection
/// </summary>
public sealed class IngestCommand : Command
{
    public IngestCommand() : base("ingest", "Ingest multimodal content into Hartonomous")
    {
        var pathOption = new Option<string>(
            aliases: new[] { "--path", "-p" },
            description: "File path, directory, or glob pattern to ingest")
        {
            IsRequired = true
        };

        var modalityOption = new Option<string?>(
            aliases: new[] { "--modality", "-m" },
            description: "Content modality (text, image, audio, video, auto)")
        {
            IsRequired = false
        };
        modalityOption.SetDefaultValue("auto");

        var strategyOption = new Option<string?>(
            aliases: new[] { "--strategy", "-s" },
            description: "Atomization strategy (sentence, fixed-size, paragraph, whole)")
        {
            IsRequired = false
        };

        var batchSizeOption = new Option<int>(
            aliases: new[] { "--batch-size", "-b" },
            description: "Number of atoms to batch before ingestion")
        {
            IsRequired = false
        };
        batchSizeOption.SetDefaultValue(100);

        var parallelismOption = new Option<int>(
            aliases: new[] { "--parallel" },
            description: "Maximum parallel ingestion tasks")
        {
            IsRequired = false
        };
        parallelismOption.SetDefaultValue(4);

        var chunkSizeOption = new Option<int>(
            aliases: new[] { "--chunk-size" },
            description: "Maximum chunk size for fixed-size atomization (characters)")
        {
            IsRequired = false
        };
        chunkSizeOption.SetDefaultValue(1000);

        var overlapOption = new Option<int>(
            aliases: new[] { "--overlap" },
            description: "Overlap size between chunks (characters)")
        {
            IsRequired = false
        };
        overlapOption.SetDefaultValue(100);

        var minQualityOption = new Option<double>(
            aliases: new[] { "--min-quality" },
            description: "Minimum quality score to accept atoms (0.0-1.0)")
        {
            IsRequired = false
        };
        minQualityOption.SetDefaultValue(0.0);

        var recursiveOption = new Option<bool>(
            aliases: new[] { "--recursive", "-r" },
            description: "Recursively scan directories")
        {
            IsRequired = false
        };
        recursiveOption.SetDefaultValue(false);

        var dryRunOption = new Option<bool>(
            aliases: new[] { "--dry-run" },
            description: "Show what would be ingested without actually ingesting")
        {
            IsRequired = false
        };
        dryRunOption.SetDefaultValue(false);

        AddOption(pathOption);
        AddOption(modalityOption);
        AddOption(strategyOption);
        AddOption(batchSizeOption);
        AddOption(parallelismOption);
        AddOption(chunkSizeOption);
        AddOption(overlapOption);
        AddOption(minQualityOption);
        AddOption(recursiveOption);
        AddOption(dryRunOption);

        this.SetHandler(HandleAsync,
            pathOption,
            modalityOption,
            strategyOption,
            batchSizeOption,
            parallelismOption,
            chunkSizeOption,
            overlapOption,
            minQualityOption,
            recursiveOption,
            dryRunOption);
    }

    private async Task<int> HandleAsync(
        string path,
        string? modality,
        string? strategy,
        int batchSize,
        int parallelism,
        int chunkSize,
        int overlap,
        double minQuality,
        bool recursive,
        bool dryRun)
    {
        var serviceProvider = BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<IngestCommand>>();
        var atomIngestionService = serviceProvider.GetRequiredService<IAtomIngestionService>();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        try
        {
            // Resolve files from path/glob pattern
            var files = ResolveFiles(path, recursive);
            if (files.Count == 0)
            {
                logger.LogError("No files found matching pattern: {Path}", path);
                return 1;
            }

            logger.LogInformation("Found {Count} file(s) to ingest", files.Count);

            if (dryRun)
            {
                foreach (var file in files)
                {
                    Console.WriteLine($"  {file}");
                }
                logger.LogInformation("[DRY RUN] Would ingest {Count} files", files.Count);
                return 0;
            }

            // Create orchestrator
            var readerFactory = new ContentReaderFactory(loggerFactory);
            var orchestrator = new MultimodalIngestionOrchestrator(
                readerFactory,
                atomIngestionService,
                loggerFactory.CreateLogger<MultimodalIngestionOrchestrator>(),
                batchSize,
                parallelism);

            // Register atomizers based on modality/strategy
            RegisterAtomizers(orchestrator, modality, strategy, loggerFactory);

            // Ingest files
            var totalIngested = 0;
            var totalDuplicates = 0;
            var totalErrors = 0;
            var startTime = DateTime.UtcNow;

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                
                try
                {
                    var progress = new Progress<IngestionProgress>(p =>
                    {
                        Console.Write($"\r[{totalIngested + totalErrors + 1}/{files.Count}] {fileName}: " +
                                    $"{p.Phase} {p.PercentComplete:F0}% ({p.AtomsProcessed}/{p.AtomsGenerated} atoms)");
                    });

                    var result = await orchestrator.IngestAsync(
                        sourceUri: file,
                        modalityHint: modality ?? "auto",
                        options: new Dictionary<string, object>
                        {
                            ["maxChunkSize"] = chunkSize,
                            ["overlapSize"] = overlap,
                            ["minQualityScore"] = minQuality
                        },
                        progress: progress,
                        cancellationToken: CancellationToken.None);

                    Console.WriteLine(); // New line after progress

                    if (result.Success)
                    {
                        totalIngested += result.Statistics.AtomsIngested;
                        totalDuplicates += result.Statistics.AtomsDuplicated;
                        logger.LogInformation("✅ {FileName}: {Count} atoms ingested ({Duplicates} duplicates)",
                            fileName, result.Statistics.AtomsIngested, result.Statistics.AtomsDuplicated);
                    }
                    else
                    {
                        totalErrors++;
                        logger.LogError("❌ {FileName}: {Error}", fileName, result.Error);
                    }
                }
                catch (Exception ex)
                {
                    totalErrors++;
                    logger.LogError(ex, "❌ {FileName}: Exception during ingestion", fileName);
                }
            }

            // Summary
            var duration = DateTime.UtcNow - startTime;
            var throughput = totalIngested / duration.TotalSeconds;

            Console.WriteLine();
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Console.WriteLine($"✅ Ingestion complete");
            Console.WriteLine($"   Files processed: {files.Count - totalErrors}/{files.Count}");
            Console.WriteLine($"   Atoms ingested: {totalIngested:N0}");
            Console.WriteLine($"   Duplicates: {totalDuplicates:N0}");
            Console.WriteLine($"   Errors: {totalErrors}");
            Console.WriteLine($"   Duration: {duration.TotalSeconds:F1}s");
            Console.WriteLine($"   Throughput: {throughput:F2} atoms/sec");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            return totalErrors > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error during ingestion");
            return 1;
        }
    }

    private List<string> ResolveFiles(string path, bool recursive)
    {
        var files = new List<string>();

        // Check if path is a file
        if (File.Exists(path))
        {
            files.Add(Path.GetFullPath(path));
            return files;
        }

        // Check if path is a directory
        if (Directory.Exists(path))
        {
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            files.AddRange(Directory.GetFiles(path, "*.*", searchOption)
                .Select(Path.GetFullPath));
            return files;
        }

        // Check if path contains glob pattern
        var directory = Path.GetDirectoryName(path) ?? ".";
        var pattern = Path.GetFileName(path);
        
        if (Directory.Exists(directory))
        {
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            files.AddRange(Directory.GetFiles(directory, pattern, searchOption)
                .Select(Path.GetFullPath));
        }

        return files;
    }

    private void RegisterAtomizers(
        MultimodalIngestionOrchestrator orchestrator,
        string? modality,
        string? strategy,
        ILoggerFactory loggerFactory)
    {
        // Text atomizer
        if (modality is null or "auto" or "text")
        {
            var textStrategy = strategy?.ToLowerInvariant() switch
            {
                "sentence" => TextChunkingStrategy.Sentence,
                "fixed-size" or "fixed" => TextChunkingStrategy.FixedSize,
                "paragraph" => TextChunkingStrategy.Paragraph,
                "semantic" => TextChunkingStrategy.Semantic,
                "structural" or "structure" => TextChunkingStrategy.Structural,
                _ => TextChunkingStrategy.FixedSize
            };

            orchestrator.RegisterAtomizer("text",
                new TextAtomizer(textStrategy, loggerFactory.CreateLogger<TextAtomizer>()));
        }

        // Image atomizer
        if (modality is null or "auto" or "image")
        {
            var imageStrategy = strategy?.ToLowerInvariant() switch
            {
                "whole" or "whole-image" => ImageAtomizationStrategy.WholeImage,
                "tile" or "tiles" => ImageAtomizationStrategy.TileExtraction,
                "object" or "object-detection" => ImageAtomizationStrategy.ObjectDetection,
                "ocr" => ImageAtomizationStrategy.OcrRegions,
                "salient" or "salient-regions" => ImageAtomizationStrategy.SalientRegions,
                _ => ImageAtomizationStrategy.WholeImage
            };

            orchestrator.RegisterAtomizer("image",
                new ImageAtomizer(imageStrategy, loggerFactory.CreateLogger<ImageAtomizer>()));
        }

        // Audio atomizer
        if (modality is null or "auto" or "audio")
        {
            var audioStrategy = strategy?.ToLowerInvariant() switch
            {
                "whole" or "whole-audio" => AudioAtomizationStrategy.WholeAudio,
                "silence" or "silence-detection" => AudioAtomizationStrategy.SilenceDetection,
                "diarization" or "speaker-diarization" => AudioAtomizationStrategy.SpeakerDiarization,
                "transcription" => AudioAtomizationStrategy.TranscriptionSegments,
                "fixed" or "fixed-duration" => AudioAtomizationStrategy.FixedDuration,
                _ => AudioAtomizationStrategy.WholeAudio
            };

            orchestrator.RegisterAtomizer("audio",
                new AudioAtomizer(audioStrategy, loggerFactory.CreateLogger<AudioAtomizer>()));
        }

        // Video atomizer
        if (modality is null or "auto" or "video")
        {
            var videoStrategy = strategy?.ToLowerInvariant() switch
            {
                "whole" or "whole-video" => VideoAtomizationStrategy.WholeVideo,
                "scene" or "scene-detection" => VideoAtomizationStrategy.SceneDetection,
                "keyframe" or "keyframes" => VideoAtomizationStrategy.KeyframeExtraction,
                "shot" or "shot-boundary" => VideoAtomizationStrategy.ShotBoundaryDetection,
                "fixed" or "fixed-duration" => VideoAtomizationStrategy.FixedDuration,
                _ => VideoAtomizationStrategy.WholeVideo
            };

            orchestrator.RegisterAtomizer("video",
                new VideoAtomizer(videoStrategy, loggerFactory.CreateLogger<VideoAtomizer>()));
        }
    }

    private IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        
        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // TODO: Register IAtomIngestionService from DI configuration
        // For now, this will fail - needs to be integrated with Hartonomous.Infrastructure
        // services.AddHartonomousInfrastructure(configuration);

        return services.BuildServiceProvider();
    }
}
