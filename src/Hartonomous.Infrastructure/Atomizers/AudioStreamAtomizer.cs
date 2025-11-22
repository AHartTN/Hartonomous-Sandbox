using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Core.Models.Audio;
using Hartonomous.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Atomizers;

/// <summary>
/// Atomizes audio samples from streaming sources.
/// Converts PCM audio data into individual sample atoms with temporal positions.
/// </summary>
public class AudioStreamAtomizer : BaseAtomizer<AudioBuffer>
{
    public AudioStreamAtomizer(ILogger<AudioStreamAtomizer> logger) : base(logger) { }

    public override int Priority => 70;

    public override bool CanHandle(string contentType, string? fileExtension)
    {
        return false;
    }

    protected override async Task AtomizeCoreAsync(
        AudioBuffer buffer,
        SourceMetadata source,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var bufferIdBytes = Encoding.UTF8.GetBytes(buffer.BufferId);
        var bufferHash = HashUtilities.ComputeSHA256(bufferIdBytes);
        
        var bufferAtom = new AtomData
        {
            AtomicValue = bufferIdBytes,
            ContentHash = bufferHash,
            Modality = "audio",
            Subtype = "buffer-id",
            ContentType = $"audio/x-raw-int{buffer.BitsPerSample}",
            CanonicalText = buffer.BufferId,
            Metadata = $"{{\"streamId\":\"{buffer.StreamId}\",\"sampleRate\":{buffer.SampleRate},\"channels\":{buffer.Channels},\"bitsPerSample\":{buffer.BitsPerSample},\"timestamp\":\"{buffer.Timestamp:O}\"}}"
        };
        atoms.Add(bufferAtom);

        int bytesPerSample = buffer.BitsPerSample / 8;
        int expectedLength = buffer.SampleCount * buffer.Channels * bytesPerSample;

        if (buffer.Samples == null || buffer.Samples.Length != expectedLength)
        {
            warnings.Add($"Invalid sample data: expected {expectedLength} bytes, got {buffer.Samples?.Length ?? 0}");
            return;
        }

        int sampleIndex = 0;
        var sampleHashes = new HashSet<string>();

        for (int i = 0; i < buffer.SampleCount; i++)
        {
            for (int channel = 0; channel < buffer.Channels; channel++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int offset = (i * buffer.Channels + channel) * bytesPerSample;
                var sampleBytes = new byte[bytesPerSample];
                Array.Copy(buffer.Samples, offset, sampleBytes, 0, bytesPerSample);

                var sampleHash = HashUtilities.ComputeSHA256(sampleBytes);
                var sampleHashStr = Convert.ToBase64String(sampleHash);

                if (!sampleHashes.Contains(sampleHashStr))
                {
                    sampleHashes.Add(sampleHashStr);

                    string canonicalValue;
                    if (buffer.BitsPerSample == 16)
                    {
                        short sampleValue = BitConverter.ToInt16(sampleBytes, 0);
                        canonicalValue = sampleValue.ToString();
                    }
                    else if (buffer.BitsPerSample == 32)
                    {
                        int sampleValue = BitConverter.ToInt32(sampleBytes, 0);
                        canonicalValue = sampleValue.ToString();
                    }
                    else
                    {
                        canonicalValue = BitConverter.ToString(sampleBytes).Replace("-", "");
                    }

                    var sampleAtom = new AtomData
                    {
                        AtomicValue = sampleBytes,
                        ContentHash = sampleHash,
                        Modality = "audio",
                        Subtype = $"sample-pcm{buffer.BitsPerSample}",
                        ContentType = $"audio/x-raw-int{buffer.BitsPerSample}",
                        CanonicalText = canonicalValue,
                        Metadata = $"{{\"bitsPerSample\":{buffer.BitsPerSample},\"channel\":{channel}}}"
                    };
                    atoms.Add(sampleAtom);
                }

                double sampleTime = buffer.Timestamp.Ticks / 10000000.0 + (i / (double)buffer.SampleRate);
                
                CreateAtomComposition(
                    bufferHash,
                    sampleHash,
                    sampleIndex,
                    compositions,
                    x: channel,
                    y: i,
                    z: 0,
                    m: sampleTime);

                sampleIndex++;
            }
        }

        await Task.CompletedTask;
    }

    protected override string GetDetectedFormat() => "audio buffer stream";
    protected override string GetModality() => "audio";

    protected override byte[] GetFileMetadataBytes(AudioBuffer input, SourceMetadata source)
    {
        return Encoding.UTF8.GetBytes($"audio-buffer:{input.BufferId}:{input.SampleCount}");
    }

    protected override string GetCanonicalFileText(AudioBuffer input, SourceMetadata source)
    {
        return $"{input.BufferId} ({input.SampleCount} samples)";
    }

    protected override string GetFileMetadataJson(AudioBuffer input, SourceMetadata source)
    {
        return $"{{\"streamId\":\"{input.StreamId}\",\"bufferId\":\"{input.BufferId}\",\"sampleRate\":{input.SampleRate},\"channels\":{input.Channels},\"bitsPerSample\":{input.BitsPerSample},\"sampleCount\":{input.SampleCount}}}";
    }
}
