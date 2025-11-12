using System;
using System.IO;
using System.Text;

namespace Hartonomous.Core.Pipelines.Ingestion
{
    /// <summary>
    /// Parses WAV (RIFF/WAVE) file headers to extract audio format metadata.
    /// Based on Microsoft's WAVEFORMATEX structure and RIFF chunk parsing patterns.
    /// Implements internal parsing - does NOT use external audio libraries.
    /// </summary>
    public class WavFileParser
    {
        /// <summary>
        /// Parsed WAV format information.
        /// </summary>
        public class WavFormat
        {
            public int SampleRate { get; set; }
            public short Channels { get; set; }
            public short BitsPerSample { get; set; }
            public int DataSize { get; set; }
            public double DurationSeconds { get; set; }
            public int ByteRate { get; set; }
            public short BlockAlign { get; set; }
        }

        /// <summary>
        /// Parses a WAV file header to extract format information.
        /// Reads RIFF header, fmt chunk, and data chunk size.
        /// </summary>
        /// <param name="audioData">Raw audio file bytes</param>
        /// <returns>Parsed WAV format info, or null if not a valid WAV file</returns>
        public static WavFormat? ParseWavHeader(byte[] audioData)
        {
            if (audioData == null || audioData.Length < 44) // Minimum WAV header size
            {
                return null;
            }

            try
            {
                using var stream = new MemoryStream(audioData);
                using var reader = new BinaryReader(stream);

                // Read RIFF header (12 bytes)
                string riffId = Encoding.ASCII.GetString(reader.ReadBytes(4)); // "RIFF"
                if (riffId != "RIFF")
                {
                    return null;
                }

                int fileSize = reader.ReadInt32(); // File size - 8
                string waveId = Encoding.ASCII.GetString(reader.ReadBytes(4)); // "WAVE"
                if (waveId != "WAVE")
                {
                    return null;
                }

                // Find and read fmt chunk
                WavFormat? format = null;
                int dataSize = 0;

                while (stream.Position < stream.Length - 8)
                {
                    string chunkId = Encoding.ASCII.GetString(reader.ReadBytes(4));
                    int chunkSize = reader.ReadInt32();

                    if (chunkId == "fmt ")
                    {
                        // Parse WAVEFORMATEX structure
                        short audioFormat = reader.ReadInt16(); // 1 = PCM
                        short channels = reader.ReadInt16();
                        int sampleRate = reader.ReadInt32();
                        int byteRate = reader.ReadInt32(); // SampleRate * Channels * BitsPerSample/8
                        short blockAlign = reader.ReadInt16(); // Channels * BitsPerSample/8
                        short bitsPerSample = reader.ReadInt16();

                        format = new WavFormat
                        {
                            SampleRate = sampleRate,
                            Channels = channels,
                            BitsPerSample = bitsPerSample,
                            ByteRate = byteRate,
                            BlockAlign = blockAlign
                        };

                        // Skip any extra format bytes
                        int extraBytes = chunkSize - 16;
                        if (extraBytes > 0 && stream.Position + extraBytes <= stream.Length)
                        {
                            reader.ReadBytes(extraBytes);
                        }
                    }
                    else if (chunkId == "data")
                    {
                        dataSize = chunkSize;
                        // Don't read the data, just note its size
                        break; // Data chunk is typically last
                    }
                    else
                    {
                        // Skip unknown chunks
                        if (stream.Position + chunkSize <= stream.Length)
                        {
                            reader.ReadBytes(chunkSize);
                        }
                        else
                        {
                            break; // Malformed chunk
                        }
                    }
                }

                if (format != null && dataSize > 0)
                {
                    format.DataSize = dataSize;
                    // Calculate duration: data_size / (sample_rate * channels * bytes_per_sample)
                    int bytesPerSample = format.BitsPerSample / 8;
                    if (format.SampleRate > 0 && format.Channels > 0 && bytesPerSample > 0)
                    {
                        format.DurationSeconds = (double)dataSize / (format.SampleRate * format.Channels * bytesPerSample);
                    }
                }

                return format;
            }
            catch
            {
                return null; // Invalid WAV format
            }
        }

        /// <summary>
        /// Extracts raw PCM samples from WAV data chunk.
        /// Returns 16-bit signed samples (converted from bytes).
        /// </summary>
        /// <param name="audioData">Complete WAV file bytes</param>
        /// <param name="format">Parsed WAV format (from ParseWavHeader)</param>
        /// <returns>Array of PCM samples, or empty if parsing fails</returns>
        public static short[] ExtractPcmSamples(byte[] audioData, WavFormat format)
        {
            if (audioData == null || format == null)
            {
                return Array.Empty<short>();
            }

            try
            {
                using var stream = new MemoryStream(audioData);
                using var reader = new BinaryReader(stream);

                // Skip to data chunk
                while (stream.Position < stream.Length - 8)
                {
                    string chunkId = Encoding.ASCII.GetString(reader.ReadBytes(4));
                    int chunkSize = reader.ReadInt32();

                    if (chunkId == "data")
                    {
                        // Read PCM samples
                        int sampleCount = chunkSize / (format.BitsPerSample / 8);
                        short[] samples = new short[sampleCount];

                        for (int i = 0; i < sampleCount && stream.Position < stream.Length; i++)
                        {
                            if (format.BitsPerSample == 16)
                            {
                                samples[i] = reader.ReadInt16();
                            }
                            else if (format.BitsPerSample == 8)
                            {
                                // 8-bit PCM is unsigned (0-255), convert to signed (-128 to 127)
                                byte unsigned8 = reader.ReadByte();
                                samples[i] = (short)((unsigned8 - 128) * 256); // Scale to 16-bit range
                            }
                            else
                            {
                                // Unsupported bit depth
                                return Array.Empty<short>();
                            }
                        }

                        return samples;
                    }
                    else
                    {
                        // Skip this chunk
                        if (stream.Position + chunkSize <= stream.Length)
                        {
                            reader.ReadBytes(chunkSize);
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                return Array.Empty<short>();
            }
            catch
            {
                return Array.Empty<short>();
            }
        }
    }
}
