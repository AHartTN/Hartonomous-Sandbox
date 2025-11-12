using System;
using System.Collections.Generic;

namespace Hartonomous.Core.Pipelines.Ingestion
{
    /// <summary>
    /// Implements RMS (Root Mean Square) energy-based silence detection for audio.
    /// Based on Microsoft audio processing documentation and speech processing research.
    /// Calculates energy per frame, converts to dB, applies threshold for silence/speech classification.
    /// </summary>
    public class SilenceDetector
    {
        private readonly double _silenceThresholdDb;
        private readonly int _frameSizeMs;
        private readonly int _minSegmentMs;
        private readonly int _maxSegmentMs;

        /// <summary>
        /// Represents a detected audio segment (silence or speech).
        /// </summary>
        public class AudioSegment
        {
            public int StartSample { get; set; }
            public int EndSample { get; set; }
            public bool IsSilence { get; set; }
            public double AverageEnergyDb { get; set; }
        }

        public SilenceDetector(
            double silenceThresholdDb = -40.0,
            int frameSizeMs = 20,
            int minSegmentMs = 2000,
            int maxSegmentMs = 30000)
        {
            _silenceThresholdDb = silenceThresholdDb;
            _frameSizeMs = frameSizeMs;
            _minSegmentMs = minSegmentMs;
            _maxSegmentMs = maxSegmentMs;
        }

        /// <summary>
        /// Detects silence boundaries in PCM audio samples and splits into segments.
        /// Algorithm:
        /// 1. Calculate RMS energy per frame (e.g., 20ms frames)
        /// 2. Convert energy to dB: 20 * log10(RMS / maxPCM)
        /// 3. Classify frame as silent if energy &lt; threshold (default -40dB)
        /// 4. Merge consecutive frames of same type (silence/speech)
        /// 5. Filter segments by min/max duration
        /// </summary>
        /// <param name="samples">PCM samples (16-bit signed)</param>
        /// <param name="sampleRate">Sample rate in Hz (e.g., 16000)</param>
        /// <returns>List of audio segments with start/end sample positions</returns>
        public List<AudioSegment> DetectSilenceBoundaries(short[] samples, int sampleRate)
        {
            if (samples == null || samples.Length == 0 || sampleRate <= 0)
            {
                return new List<AudioSegment>();
            }

            // Calculate frame size in samples
            int frameSizeSamples = (sampleRate * _frameSizeMs) / 1000;
            if (frameSizeSamples <= 0)
            {
                frameSizeSamples = 1;
            }

            // Step 1 & 2: Calculate RMS energy and convert to dB for each frame
            var frameEnergies = new List<(int startSample, int endSample, double energyDb, bool isSilent)>();
            
            for (int i = 0; i < samples.Length; i += frameSizeSamples)
            {
                int frameEnd = Math.Min(i + frameSizeSamples, samples.Length);
                int frameLength = frameEnd - i;

                // Calculate RMS: sqrt(mean(samples²))
                double sumSquares = 0.0;
                for (int j = i; j < frameEnd; j++)
                {
                    double normalized = samples[j] / 32768.0; // Normalize to -1.0 to 1.0
                    sumSquares += normalized * normalized;
                }

                double rms = Math.Sqrt(sumSquares / frameLength);

                // Convert to dB: 20 * log10(RMS / reference)
                // Reference is 1.0 (full scale for normalized samples)
                double energyDb = rms > 0.0 ? 20.0 * Math.Log10(rms) : -100.0; // -100dB for silence floor

                // Step 3: Classify as silence or speech
                bool isSilent = energyDb < _silenceThresholdDb;

                frameEnergies.Add((i, frameEnd, energyDb, isSilent));
            }

            // Step 4: Merge consecutive frames of same type
            var segments = new List<AudioSegment>();
            if (frameEnergies.Count == 0)
            {
                return segments;
            }

            int segmentStart = frameEnergies[0].startSample;
            bool currentType = frameEnergies[0].isSilent;
            double energySum = frameEnergies[0].energyDb;
            int frameCount = 1;

            for (int i = 1; i < frameEnergies.Count; i++)
            {
                if (frameEnergies[i].isSilent == currentType)
                {
                    // Continue current segment
                    energySum += frameEnergies[i].energyDb;
                    frameCount++;
                }
                else
                {
                    // Start new segment
                    segments.Add(new AudioSegment
                    {
                        StartSample = segmentStart,
                        EndSample = frameEnergies[i].startSample,
                        IsSilence = currentType,
                        AverageEnergyDb = energySum / frameCount
                    });

                    segmentStart = frameEnergies[i].startSample;
                    currentType = frameEnergies[i].isSilent;
                    energySum = frameEnergies[i].energyDb;
                    frameCount = 1;
                }
            }

            // Add final segment
            segments.Add(new AudioSegment
            {
                StartSample = segmentStart,
                EndSample = frameEnergies[^1].endSample,
                IsSilence = currentType,
                AverageEnergyDb = energySum / frameCount
            });

            // Step 5: Filter segments by duration constraints
            int minSegmentSamples = (sampleRate * _minSegmentMs) / 1000;
            int maxSegmentSamples = (sampleRate * _maxSegmentMs) / 1000;

            var filteredSegments = new List<AudioSegment>();
            foreach (var segment in segments)
            {
                // Skip silence segments (we only care about speech segments for atomization)
                if (segment.IsSilence)
                {
                    continue;
                }

                int segmentLength = segment.EndSample - segment.StartSample;

                // Skip segments that are too short
                if (segmentLength < minSegmentSamples)
                {
                    continue;
                }

                // Split segments that are too long
                if (segmentLength > maxSegmentSamples)
                {
                    int numChunks = (int)Math.Ceiling((double)segmentLength / maxSegmentSamples);
                    int chunkSize = segmentLength / numChunks;

                    for (int i = 0; i < numChunks; i++)
                    {
                        int chunkStart = segment.StartSample + (i * chunkSize);
                        int chunkEnd = (i == numChunks - 1) ? segment.EndSample : chunkStart + chunkSize;

                        filteredSegments.Add(new AudioSegment
                        {
                            StartSample = chunkStart,
                            EndSample = chunkEnd,
                            IsSilence = false,
                            AverageEnergyDb = segment.AverageEnergyDb
                        });
                    }
                }
                else
                {
                    filteredSegments.Add(segment);
                }
            }

            // If no segments found, return whole audio as single segment
            if (filteredSegments.Count == 0)
            {
                filteredSegments.Add(new AudioSegment
                {
                    StartSample = 0,
                    EndSample = samples.Length,
                    IsSilence = false,
                    AverageEnergyDb = -20.0 // Assume reasonable energy
                });
            }

            return filteredSegments;
        }
    }
}
