using System;
using System.Collections.Generic;
using System.Linq;

namespace Hartonomous.Core.Pipelines.Ingestion
{
    /// <summary>
    /// Video scene detection using histogram-based shot boundary detection.
    /// Implements chi-square distance and histogram intersection for frame comparison.
    /// 
    /// Algorithm:
    /// 1. Extract frames at fixed interval (e.g., every 30th frame for 30fps = 1sec intervals)
    /// 2. Compute RGB or HSV histogram for each frame
    /// 3. Compare consecutive histograms using distance metric
    /// 4. If distance exceeds threshold, mark as scene cut
    /// 5. Extract keyframe from each scene (first frame or middle frame)
    /// 
    /// References:
    /// - Chi-square distance: χ² = Σ((h1[i] - h2[i])² / (h1[i] + h2[i]))
    /// - Histogram intersection: d = 1 - (Σ min(h1[i], h2[i]) / Σ h1[i])
    /// </summary>
    public class VideoSceneDetector
    {
        public class SceneCut
        {
            public int FrameIndex { get; set; }
            public double Distance { get; set; }
            public SceneTransitionType Type { get; set; }
        }

        public enum SceneTransitionType
        {
            Cut,        // Abrupt change (high distance)
            Fade,       // Gradual change over multiple frames
            Unknown
        }

        public class VideoScene
        {
            public int StartFrame { get; set; }
            public int EndFrame { get; set; }
            public int KeyframeIndex { get; set; }
            public double AverageHistogramEntropy { get; set; }
        }

        private const int DefaultBins = 32; // Histogram bins per channel
        private const double DefaultCutThreshold = 0.5; // Chi-square distance threshold for cuts
        private const int DefaultSamplingInterval = 30; // Sample every Nth frame

        /// <summary>
        /// Detect scene boundaries in video by comparing frame histograms.
        /// NOTE: This requires frame extraction - use FFmpeg.NET or OpenCV in production.
        /// </summary>
        /// <param name="frames">RGB frames as byte arrays (width*height*3 bytes per frame)</param>
        /// <param name="width">Frame width</param>
        /// <param name="height">Frame height</param>
        /// <param name="threshold">Distance threshold for scene cuts (default 0.5)</param>
        /// <returns>List of detected scene cuts with frame indices</returns>
        public static List<SceneCut> DetectSceneCuts(
            List<byte[]> frames,
            int width,
            int height,
            double threshold = DefaultCutThreshold)
        {
            if (frames == null || frames.Count < 2)
                throw new ArgumentException("Need at least 2 frames for scene detection", nameof(frames));

            var cuts = new List<SceneCut>();
            double[]? previousHist = null;

            for (int i = 0; i < frames.Count; i++)
            {
                var currentHist = ComputeRgbHistogram(frames[i], width, height, DefaultBins);

                if (previousHist != null)
                {
                    double distance = ChiSquareDistance(previousHist, currentHist);

                    if (distance > threshold)
                    {
                        cuts.Add(new SceneCut
                        {
                            FrameIndex = i,
                            Distance = distance,
                            Type = SceneTransitionType.Cut
                        });
                    }
                }

                previousHist = currentHist;
            }

            return cuts;
        }

        /// <summary>
        /// Group scene cuts into scenes and select keyframes.
        /// </summary>
        public static List<VideoScene> GroupIntoScenes(
            List<SceneCut> cuts,
            int totalFrames,
            KeyframeSelectionStrategy strategy = KeyframeSelectionStrategy.MiddleFrame)
        {
            var scenes = new List<VideoScene>();

            int sceneStart = 0;
            foreach (var cut in cuts)
            {
                int sceneEnd = cut.FrameIndex - 1;
                int keyframe = SelectKeyframe(sceneStart, sceneEnd, strategy);

                scenes.Add(new VideoScene
                {
                    StartFrame = sceneStart,
                    EndFrame = sceneEnd,
                    KeyframeIndex = keyframe
                });

                sceneStart = cut.FrameIndex;
            }

            // Last scene
            if (sceneStart < totalFrames)
            {
                scenes.Add(new VideoScene
                {
                    StartFrame = sceneStart,
                    EndFrame = totalFrames - 1,
                    KeyframeIndex = SelectKeyframe(sceneStart, totalFrames - 1, strategy)
                });
            }

            return scenes;
        }

        public enum KeyframeSelectionStrategy
        {
            FirstFrame,    // Use first frame of scene
            MiddleFrame,   // Use middle frame (most representative)
            HighestEntropy // Use frame with highest visual complexity (not implemented)
        }

        private static int SelectKeyframe(int start, int end, KeyframeSelectionStrategy strategy)
        {
            return strategy switch
            {
                KeyframeSelectionStrategy.FirstFrame => start,
                KeyframeSelectionStrategy.MiddleFrame => (start + end) / 2,
                _ => start
            };
        }

        /// <summary>
        /// Compute RGB histogram for a frame.
        /// Histogram has bins*3 buckets (R, G, B channels concatenated).
        /// </summary>
        private static double[] ComputeRgbHistogram(byte[] frameData, int width, int height, int bins)
        {
            int expectedSize = width * height * 3;
            if (frameData.Length != expectedSize)
                throw new ArgumentException($"Frame data must be {expectedSize} bytes (width*height*3), got {frameData.Length}");

            // Histogram: [R bins][G bins][B bins]
            double[] histogram = new double[bins * 3];
            int pixelCount = width * height;

            for (int i = 0; i < pixelCount; i++)
            {
                int offset = i * 3;
                byte r = frameData[offset];
                byte g = frameData[offset + 1];
                byte b = frameData[offset + 2];

                // Map 0-255 to 0-(bins-1)
                int rBin = r * bins / 256;
                int gBin = g * bins / 256;
                int bBin = b * bins / 256;

                // Ensure bin indices are in range
                rBin = Math.Clamp(rBin, 0, bins - 1);
                gBin = Math.Clamp(gBin, 0, bins - 1);
                bBin = Math.Clamp(bBin, 0, bins - 1);

                histogram[rBin]++;               // R channel
                histogram[bins + gBin]++;        // G channel
                histogram[bins * 2 + bBin]++;    // B channel
            }

            // Normalize histogram (convert counts to probabilities)
            for (int i = 0; i < histogram.Length; i++)
            {
                histogram[i] /= pixelCount;
            }

            return histogram;
        }

        /// <summary>
        /// Chi-square distance between two histograms.
        /// χ² = Σ((h1[i] - h2[i])² / (h1[i] + h2[i]))
        /// Lower distance = more similar histograms.
        /// </summary>
        private static double ChiSquareDistance(double[] hist1, double[] hist2)
        {
            if (hist1.Length != hist2.Length)
                throw new ArgumentException("Histograms must have same length");

            double distance = 0.0;
            const double epsilon = 1e-10; // Avoid division by zero

            for (int i = 0; i < hist1.Length; i++)
            {
                double sum = hist1[i] + hist2[i];
                if (sum > epsilon)
                {
                    double diff = hist1[i] - hist2[i];
                    distance += (diff * diff) / sum;
                }
            }

            return distance;
        }

        /// <summary>
        /// Histogram intersection distance (alternative metric).
        /// d = 1 - (Σ min(h1[i], h2[i]) / Σ h1[i])
        /// Range: [0, 1], where 0 = identical, 1 = completely different.
        /// </summary>
        private static double HistogramIntersectionDistance(double[] hist1, double[] hist2)
        {
            if (hist1.Length != hist2.Length)
                throw new ArgumentException("Histograms must have same length");

            double intersection = 0.0;
            double sum1 = 0.0;

            for (int i = 0; i < hist1.Length; i++)
            {
                intersection += Math.Min(hist1[i], hist2[i]);
                sum1 += hist1[i];
            }

            return 1.0 - (intersection / (sum1 + 1e-10));
        }

        /// <summary>
        /// Extract frames from video file using FFmpeg.
        /// This is a STUB - requires FFmpeg.NET or Accord.Video.FFMPEG in production.
        /// </summary>
        /// <param name="videoPath">Path to video file</param>
        /// <param name="samplingInterval">Extract every Nth frame</param>
        /// <returns>List of RGB frames</returns>
        public static List<byte[]> ExtractFrames(string videoPath, int samplingInterval = DefaultSamplingInterval)
        {
            throw new NotImplementedException(
                "Frame extraction requires FFmpeg. Install FFmpeg.NET or use OpenCVSharp: " +
                "dotnet add package FFmpeg.NET --version 4.0.0 " +
                "OR dotnet add package OpenCvSharp4 --version 4.10.0. " +
                "Example: Use VideoCapture.Read() to extract frames at specified intervals.");
        }
    }
}
