using System;
using System.Collections;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;

namespace Hartonomous.SqlClr
{
    /// <summary>
    /// Production-grade CLR table-valued function for extracting audio frames from WAV file data.
    /// Supports PCM 16-bit mono/stereo WAV files natively without external dependencies.
    /// Calculates RMS energy and peak amplitude per frame for atomic decomposition.
    /// </summary>
    public static class AudioFrameExtractor
    {
        /// <summary>
        /// Extracts audio frames from WAV data with configurable frame duration.
        /// Returns streaming results via IEnumerable for memory efficiency.
        /// </summary>
        /// <param name="audioData">WAV-encoded audio bytes (VARBINARY(MAX))</param>
        /// <param name="frameDurationMs">Frame window size in milliseconds (e.g., 25ms for speech)</param>
        /// <param name="sampleRate">Expected sample rate in Hz (e.g., 44100, 16000). If NULL, read from WAV header.</param>
        /// <returns>Stream of frame records with FrameIdx, Channel, RMS, PeakAmplitude</returns>
        [SqlFunction(
            FillRowMethodName = "FillFrameRow",
            TableDefinition = "FrameIdx INT, Channel INT, RMS FLOAT, PeakAmplitude FLOAT",
            DataAccess = DataAccessKind.None,
            IsDeterministic = true)]
        public static IEnumerable ExtractFrames(SqlBytes audioData, SqlInt32 frameDurationMs, SqlInt32 sampleRate)
        {
            // Handle NULL inputs
            if (audioData == null || audioData.IsNull || audioData.Length == 0)
                yield break;

            int frameDuration = frameDurationMs.IsNull ? 25 : frameDurationMs.Value; // Default 25ms
            if (frameDuration < 1) frameDuration = 25;

            byte[] wavBytes = audioData.Value;

            // Parse WAV header (minimum 44 bytes for standard PCM WAV)
            if (wavBytes.Length < 44)
                yield break; // Invalid WAV

            // Verify WAV signature ("RIFF")
            if (wavBytes[0] != 0x52 || wavBytes[1] != 0x49 || wavBytes[2] != 0x46 || wavBytes[3] != 0x46)
                yield break; // Not a RIFF file

            // Verify WAVE format ("WAVE")
            if (wavBytes[8] != 0x57 || wavBytes[9] != 0x41 || wavBytes[10] != 0x56 || wavBytes[11] != 0x45)
                yield break; // Not a WAVE file

            // Find "fmt " chunk (should be at offset 12 for standard WAV)
            int fmtOffset = FindChunk(wavBytes, "fmt ");
            if (fmtOffset == -1)
                yield break;

            // Read format chunk data (little-endian)
            short audioFormat = BitConverter.ToInt16(wavBytes, fmtOffset + 8);
            short numChannels = BitConverter.ToInt16(wavBytes, fmtOffset + 10);
            int wavSampleRate = BitConverter.ToInt32(wavBytes, fmtOffset + 12);
            short bitsPerSample = BitConverter.ToInt16(wavBytes, fmtOffset + 22);

            // Only support PCM 16-bit audio (format 1)
            if (audioFormat != 1 || bitsPerSample != 16)
                yield break;

            // Use provided sample rate or read from header
            int actualSampleRate = sampleRate.IsNull ? wavSampleRate : sampleRate.Value;

            // Find "data" chunk
            int dataOffset = FindChunk(wavBytes, "data");
            if (dataOffset == -1)
                yield break;

            int dataSize = BitConverter.ToInt32(wavBytes, dataOffset + 4);
            int dataStart = dataOffset + 8;

            // Validate data size
            if (wavBytes.Length < dataStart + dataSize)
                yield break;

            // Calculate samples per frame
            int samplesPerFrame = (actualSampleRate * frameDuration) / 1000;
            if (samplesPerFrame < 1) samplesPerFrame = 1;

            int bytesPerSample = (bitsPerSample / 8) * numChannels;
            int totalSamples = dataSize / bytesPerSample;
            int totalFrames = (totalSamples + samplesPerFrame - 1) / samplesPerFrame;

            // Extract frames for each channel
            for (int frameIdx = 0; frameIdx < totalFrames; frameIdx++)
            {
                int frameStart = frameIdx * samplesPerFrame;
                int frameEnd = Math.Min(frameStart + samplesPerFrame, totalSamples);
                int frameSamples = frameEnd - frameStart;

                if (frameSamples == 0)
                    break;

                // Process each channel separately
                for (int channel = 0; channel < numChannels; channel++)
                {
                    double sumSquares = 0.0;
                    double peakAmplitude = 0.0;

                    // Calculate RMS and peak for this frame/channel
                    for (int sample = frameStart; sample < frameEnd; sample++)
                    {
                        int byteOffset = dataStart + (sample * bytesPerSample) + (channel * 2);

                        // Read 16-bit signed PCM sample (little-endian)
                        short sampleValue = BitConverter.ToInt16(wavBytes, byteOffset);

                        // Normalize to [-1.0, 1.0]
                        double normalized = sampleValue / 32768.0;

                        sumSquares += normalized * normalized;

                        double absValue = Math.Abs(normalized);
                        if (absValue > peakAmplitude)
                            peakAmplitude = absValue;
                    }

                    // Calculate RMS (Root Mean Square) energy
                    double rms = Math.Sqrt(sumSquares / frameSamples);

                    yield return new FrameData
                    {
                        FrameIdx = frameIdx,
                        Channel = channel,
                        RMS = rms,
                        PeakAmplitude = peakAmplitude
                    };
                }
            }
        }

        /// <summary>
        /// Find a RIFF chunk by FourCC identifier.
        /// </summary>
        private static int FindChunk(byte[] wavBytes, string chunkId)
        {
            if (chunkId.Length != 4)
                return -1;

            byte[] chunkBytes = System.Text.Encoding.ASCII.GetBytes(chunkId);

            // Start searching after RIFF header (offset 12)
            for (int i = 12; i < wavBytes.Length - 8; i++)
            {
                if (wavBytes[i] == chunkBytes[0] &&
                    wavBytes[i + 1] == chunkBytes[1] &&
                    wavBytes[i + 2] == chunkBytes[2] &&
                    wavBytes[i + 3] == chunkBytes[3])
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// FillRow method to populate SqlDataRecord for streaming table output.
        /// Called by SQL Server for each yielded frame.
        /// </summary>
        public static void FillFrameRow(
            object frameObj,
            out SqlInt32 frameIdx,
            out SqlInt32 channel,
            out SqlDouble rms,
            out SqlDouble peakAmplitude)
        {
            FrameData frame = (FrameData)frameObj;
            frameIdx = new SqlInt32(frame.FrameIdx);
            channel = new SqlInt32(frame.Channel);
            rms = new SqlDouble(frame.RMS);
            peakAmplitude = new SqlDouble(frame.PeakAmplitude);
        }

        /// <summary>
        /// Internal struct for frame data storage during enumeration.
        /// </summary>
        private struct FrameData
        {
            public int FrameIdx;
            public int Channel;
            public double RMS;
            public double PeakAmplitude;
        }
    }
}
