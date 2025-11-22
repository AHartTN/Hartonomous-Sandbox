using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Core.Models.Video;
using Hartonomous.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Atomizers;

public class VideoStreamAtomizer : BaseAtomizer<VideoFrame>
{
    public VideoStreamAtomizer(ILogger<VideoStreamAtomizer> logger) : base(logger) { }

    public override int Priority => 70;

    public override bool CanHandle(string contentType, string? fileExtension) => false;

    protected override async Task AtomizeCoreAsync(
        VideoFrame frame,
        SourceMetadata source,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var frameIdBytes = Encoding.UTF8.GetBytes(frame.FrameId);
        var frameHash = HashUtilities.ComputeSHA256(frameIdBytes);
        
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

        if (frame.PixelData == null || frame.PixelData.Length != frame.Width * frame.Height * 4)
        {
            warnings.Add($"Invalid pixel data: expected {frame.Width * frame.Height * 4} bytes, got {frame.PixelData?.Length ?? 0}");
            return;
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

                var pixelHash = HashUtilities.ComputeSHA256(rgba);
                var pixelHashStr = Convert.ToBase64String(pixelHash);

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

                CreateAtomComposition(
                    frameHash,
                    pixelHash,
                    pixelIndex,
                    compositions,
                    x: x,
                    y: y,
                    z: 0,
                    m: frame.Timestamp.Ticks / 10000000.0);

                pixelIndex++;
            }
        }

        await Task.CompletedTask;
    }

    protected override string GetDetectedFormat() => "video frame stream";
    protected override string GetModality() => "video";

    protected override byte[] GetFileMetadataBytes(VideoFrame input, SourceMetadata source)
    {
        return Encoding.UTF8.GetBytes($"video-frame:{input.FrameId}:{input.Width}x{input.Height}");
    }

    protected override string GetCanonicalFileText(VideoFrame input, SourceMetadata source)
    {
        return $"{input.FrameId} ({input.Width}×{input.Height})";
    }

    protected override string GetFileMetadataJson(VideoFrame input, SourceMetadata source)
    {
        return $"{{\"streamId\":\"{input.StreamId}\",\"frameId\":\"{input.FrameId}\",\"width\":{input.Width},\"height\":{input.Height},\"timestamp\":\"{input.Timestamp:O}\"}}";
    }
}
