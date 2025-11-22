using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Core.Models.Video;

namespace Hartonomous.Infrastructure.Atomizers;

/// <summary>
/// Atomizes video frames from streaming sources.
/// Extracts frames as pixel atoms with temporal positions.
/// </summary>
public class VideoStreamAtomizer : IAtomizer<VideoFrame>
{
    private const int MaxAtomSize = 64;
    public int Priority => 70;

    public bool CanHandle(string contentType, string? fileExtension)
    {
        return false; // Invoked explicitly via VideoFrame
    }

    public async Task<AtomizationResult> AtomizeAsync(
        VideoFrame frame,
        SourceMetadata source,
        CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var atoms = new List<AtomData>();
        var compositions = new List<AtomComposition>();
        var warnings = new List<string>();

        try
        {
            // Create frame metadata atom
            var frameIdBytes = Encoding.UTF8.GetBytes(frame.FrameId);
            var frameHash = SHA256.HashData(frameIdBytes);
            var frameAtom = new AtomData
            {
                AtomicValue = frameIdBytes,
                ContentHash = frameHash,
                Modality = "video",
                Subtype = "frame-id",
                ContentType = "video/x-raw-rgb",
                CanonicalText = frame.FrameId,
                Metadata = $"{{\"streamId\":\"{frame.StreamId}\",\"width\":{frame.Width},\"height\":{frame.Height},\"timestamp\":\"{frame.Timestamp:O}\"}}"
            };
            atoms.Add(frameAtom);

            // Atomize pixels (RGBA format - 4 bytes per pixel)
            if (frame.PixelData == null || frame.PixelData.Length != frame.Width * frame.Height * 4)
            {
                warnings.Add($"Invalid pixel data: expected {frame.Width * frame.Height * 4} bytes, got {frame.PixelData?.Length ?? 0}");
                return new AtomizationResult
                {
                    Atoms = atoms,
                    Compositions = compositions,
                    ProcessingInfo = new ProcessingMetadata
                    {
                        TotalAtoms = atoms.Count,
                        UniqueAtoms = atoms.Count,
                        DurationMs = sw.ElapsedMilliseconds,
                        AtomizerType = nameof(VideoStreamAtomizer),
                        DetectedFormat = $"Video Frame {frame.Width}x{frame.Height}",
                        Warnings = warnings
                    }
                };
            }

            int pixelIndex = 0;
            var pixelHashes = new HashSet<string>();

            for (int y = 0; y < frame.Height; y++)
            {
                for (int x = 0; x < frame.Width; x++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    int offset = (y * frame.Width + x) * 4;
                    var rgba = new byte[4];
                    Array.Copy(frame.PixelData, offset, rgba, 0, 4);

                    var pixelHash = SHA256.HashData(rgba);
                    var pixelHashStr = Convert.ToBase64String(pixelHash);

                    // Deduplicate identical pixels
                    if (!pixelHashes.Contains(pixelHashStr))
                    {
                        pixelHashes.Add(pixelHashStr);
                        
                        var pixelAtom = new AtomData
                        {
                            AtomicValue = rgba,
                            ContentHash = pixelHash,
                            Modality = "video",
                            Subtype = "pixel-rgba",
                            ContentType = "video/x-raw-rgb",
                            CanonicalText = $"rgba({rgba[0]},{rgba[1]},{rgba[2]},{rgba[3]})",
                            Metadata = $"{{\"r\":{rgba[0]},\"g\":{rgba[1]},\"b\":{rgba[2]},\"a\":{rgba[3]}}}"
                        };
                        atoms.Add(pixelAtom);
                    }

                    // Link frame → pixel with spatial position
                    compositions.Add(new AtomComposition
                    {
                        ParentAtomHash = frameHash,
                        ComponentAtomHash = pixelHash,
                        SequenceIndex = pixelIndex,
                        Position = new SpatialPosition
                        {
                            X = x,
                            Y = y,
                            Z = 0,
                            M = frame.Timestamp.Ticks / 10000000.0 // Seconds as M (temporal) coordinate
                        }
                    });

                    pixelIndex++;
                }
            }

            sw.Stop();

            return new AtomizationResult
            {
                Atoms = atoms,
                Compositions = compositions,
                ProcessingInfo = new ProcessingMetadata
                {
                    TotalAtoms = atoms.Count,
                    UniqueAtoms = pixelHashes.Count + 1, // +1 for frame atom
                    DurationMs = sw.ElapsedMilliseconds,
                    AtomizerType = nameof(VideoStreamAtomizer),
                    DetectedFormat = $"Video Frame {frame.Width}x{frame.Height} ({pixelIndex} pixels)",
                    Warnings = warnings.Count > 0 ? warnings : null
                }
            };
        }
        catch (Exception ex)
        {
            warnings.Add($"Video frame atomization failed: {ex.Message}");
            throw;
        }
    }
}
