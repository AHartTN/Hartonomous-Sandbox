using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Core.Models.Audio;

namespace Hartonomous.Infrastructure.Atomizers;

/// <summary>
/// Atomizes audio samples from streaming sources.
/// Converts PCM audio data into individual sample atoms with temporal positions.
/// </summary>
public class AudioStreamAtomizer : IAtomizer<AudioBuffer>
{
    private const int MaxAtomSize = 64;
    public int Priority => 70;

    public bool CanHandle(string contentType, string? fileExtension)
    {
        return false; // Invoked explicitly via AudioBuffer
    }

    public async Task<AtomizationResult> AtomizeAsync(
        AudioBuffer buffer,
        SourceMetadata source,
        CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var atoms = new List<AtomData>();
        var compositions = new List<AtomComposition>();
        var warnings = new List<string>();

        try
        {
            // Create buffer metadata atom
            var bufferIdBytes = Encoding.UTF8.GetBytes(buffer.BufferId);
            var bufferHash = SHA256.HashData(bufferIdBytes);
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

            // Determine bytes per sample
            int bytesPerSample = buffer.BitsPerSample / 8;
            int expectedLength = buffer.SampleCount * buffer.Channels * bytesPerSample;

            if (buffer.Samples == null || buffer.Samples.Length != expectedLength)
            {
                warnings.Add($"Invalid sample data: expected {expectedLength} bytes, got {buffer.Samples?.Length ?? 0}");
                return new AtomizationResult
                {
                    Atoms = atoms,
                    Compositions = compositions,
                    ProcessingInfo = new ProcessingMetadata
                    {
                        TotalAtoms = atoms.Count,
                        UniqueAtoms = atoms.Count,
                        DurationMs = sw.ElapsedMilliseconds,
                        AtomizerType = nameof(AudioStreamAtomizer),
                        DetectedFormat = $"Audio Buffer {buffer.SampleRate}Hz {buffer.Channels}ch",
                        Warnings = warnings
                    }
                };
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

                    var sampleHash = SHA256.HashData(sampleBytes);
                    var sampleHashStr = Convert.ToBase64String(sampleHash);

                    // Deduplicate identical samples
                    if (!sampleHashes.Contains(sampleHashStr))
                    {
                        sampleHashes.Add(sampleHashStr);

                        // Convert sample bytes to numeric value for canonical text
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

                    // Link buffer → sample with temporal position
                    // X = channel, Y = sample index, Z = 0, M = timestamp in seconds
                    double sampleTime = buffer.Timestamp.Ticks / 10000000.0 + (i / (double)buffer.SampleRate);
                    
                    compositions.Add(new AtomComposition
                    {
                        ParentAtomHash = bufferHash,
                        ComponentAtomHash = sampleHash,
                        SequenceIndex = sampleIndex,
                        Position = new SpatialPosition
                        {
                            X = channel,
                            Y = i,
                            Z = 0,
                            M = sampleTime
                        }
                    });

                    sampleIndex++;
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
                    UniqueAtoms = sampleHashes.Count + 1, // +1 for buffer atom
                    DurationMs = sw.ElapsedMilliseconds,
                    AtomizerType = nameof(AudioStreamAtomizer),
                    DetectedFormat = $"Audio Buffer {buffer.SampleRate}Hz {buffer.Channels}ch {buffer.BitsPerSample}bit ({sampleIndex} samples)",
                    Warnings = warnings.Count > 0 ? warnings : null
                }
            };
        }
        catch (Exception ex)
        {
            warnings.Add($"Audio buffer atomization failed: {ex.Message}");
            throw;
        }
    }
}
