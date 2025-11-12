using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hartonomous.Core.Pipelines.Ingestion
{
    /// <summary>
    /// Validates atom quality before ingestion using information density, embedding sanity checks,
    /// and duplicate detection. Quarantines low-quality atoms to prevent polluting the knowledge base.
    /// </summary>
    public class QualityValidationStep
    {
        private readonly double _minEntropyThreshold;
        private readonly int _minContentLength;
        private readonly int _maxContentLength;
        private readonly double _maxDuplicateSimilarity;

        public QualityValidationStep(
            double minEntropyThreshold = 2.0,
            int minContentLength = 10,
            int maxContentLength = 100_000,
            double maxDuplicateSimilarity = 0.95)
        {
            _minEntropyThreshold = minEntropyThreshold;
            _minContentLength = minContentLength;
            _maxContentLength = maxContentLength;
            _maxDuplicateSimilarity = maxDuplicateSimilarity;
        }

        /// <summary>
        /// Validates an atom for quality issues.
        /// Returns validation result with issues list and quarantine recommendation.
        /// </summary>
        public ValidationResult ValidateAtom(AtomValidationRequest request)
        {
            var issues = new List<string>();

            // 1. Size checks
            if (request.Content == null || request.Content.Length < _minContentLength)
            {
                issues.Add($"Content too small: {request.Content?.Length ?? 0} bytes (minimum: {_minContentLength})");
            }
            else if (request.Content.Length > _maxContentLength)
            {
                issues.Add($"Content too large: {request.Content.Length} bytes (maximum: {_maxContentLength})");
            }

            // 2. Magic number validation (detect empty/corrupt files)
            if (request.ContentBytes != null)
            {
                var formatIssue = ValidateFormat(request.ContentBytes, request.ExpectedFormat);
                if (formatIssue != null)
                {
                    issues.Add(formatIssue);
                }
            }

            // 3. Information density (Shannon entropy)
            if (!string.IsNullOrEmpty(request.Content))
            {
                var entropy = ComputeShannonEntropy(request.Content);
                if (entropy < _minEntropyThreshold)
                {
                    issues.Add($"Low information density: entropy = {entropy:F2} (minimum: {_minEntropyThreshold:F2})");
                }
            }

            // 4. Embedding validation (no NaN/Inf)
            if (request.Embedding != null)
            {
                var embeddingIssue = ValidateEmbedding(request.Embedding);
                if (embeddingIssue != null)
                {
                    issues.Add(embeddingIssue);
                }
            }

            // 5. Chunking validation (token boundaries)
            if (!string.IsNullOrEmpty(request.Content) && request.ChunkingStrategy != null)
            {
                var chunkIssue = ValidateChunking(request.Content, request.ChunkingStrategy);
                if (chunkIssue != null)
                {
                    issues.Add(chunkIssue);
                }
            }

            return new ValidationResult
            {
                IsValid = issues.Count == 0,
                Issues = issues,
                ShouldQuarantine = issues.Count > 0 && ShouldQuarantineAtom(issues),
                Entropy = !string.IsNullOrEmpty(request.Content) ? ComputeShannonEntropy(request.Content) : 0
            };
        }

        /// <summary>
        /// Computes Shannon entropy: H(X) = -Σ p(x) log₂ p(x)
        /// Measures information density (higher = more diverse/unpredictable).
        /// </summary>
        /// <param name="text">Input text to analyze</param>
        /// <returns>Entropy in bits per character (0 to ~8 for byte data)</returns>
        public static double ComputeShannonEntropy(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            // Count character frequencies
            var frequencies = new Dictionary<char, int>();
            foreach (var c in text)
            {
                if (!frequencies.ContainsKey(c))
                    frequencies[c] = 0;
                frequencies[c]++;
            }

            // Compute entropy: H = -Σ p(x) * log₂(p(x))
            double entropy = 0;
            int length = text.Length;
            
            foreach (var freq in frequencies.Values)
            {
                double probability = (double)freq / length;
                entropy -= probability * Math.Log2(probability);
            }

            return entropy;
        }

        /// <summary>
        /// Validates file format using magic number detection.
        /// </summary>
        private string? ValidateFormat(byte[] data, string? expectedFormat)
        {
            if (data == null || data.Length < 4)
            {
                return "File too small for format validation (< 4 bytes)";
            }

            // Empty file detection
            if (data.All(b => b == 0))
            {
                return "File appears to be empty (all zeros)";
            }

            // Format-specific validation
            if (expectedFormat != null)
            {
                if (expectedFormat.StartsWith("image/"))
                {
                    var detectedFormat = ImageDecoder.DetectFormat(data);
                    if (detectedFormat == ImageDecoder.ImageFormat.Unknown)
                    {
                        return $"Invalid image format (expected {expectedFormat})";
                    }
                }
                else if (expectedFormat.StartsWith("audio/"))
                {
                    var detectedFormat = MediaFormatDetector.DetectAudioFormat(data);
                    if (detectedFormat == "application/octet-stream")
                    {
                        return $"Invalid audio format (expected {expectedFormat})";
                    }
                }
                else if (expectedFormat.StartsWith("video/"))
                {
                    var detectedFormat = MediaFormatDetector.DetectVideoFormat(data);
                    if (detectedFormat == "application/octet-stream")
                    {
                        return $"Invalid video format (expected {expectedFormat})";
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Validates embedding for NaN, Inf, and magnitude sanity checks.
        /// </summary>
        private string? ValidateEmbedding(float[] embedding)
        {
            if (embedding == null || embedding.Length == 0)
            {
                return "Embedding is empty";
            }

            // Check for NaN/Inf
            for (int i = 0; i < embedding.Length; i++)
            {
                if (float.IsNaN(embedding[i]))
                {
                    return $"Embedding contains NaN at index {i}";
                }
                if (float.IsInfinity(embedding[i]))
                {
                    return $"Embedding contains Infinity at index {i}";
                }
            }

            // Magnitude check (L2 norm should be reasonable, typically ~1 for normalized embeddings)
            double magnitudeSquared = 0;
            foreach (var value in embedding)
            {
                magnitudeSquared += value * value;
            }
            double magnitude = Math.Sqrt(magnitudeSquared);

            if (magnitude < 0.01)
            {
                return $"Embedding magnitude too small: {magnitude:F6} (possible zero vector)";
            }
            if (magnitude > 1000)
            {
                return $"Embedding magnitude too large: {magnitude:F2} (possible unnormalized)";
            }

            return null;
        }

        /// <summary>
        /// Validates chunking strategy matches best practices.
        /// </summary>
        private string? ValidateChunking(string content, string strategy)
        {
            // TODO: Implement token boundary validation
            // Check that chunks align with sentence/paragraph boundaries
            // Validate chunk size (256/512/1024 tokens for transformer models)
            // Ensure overlap is reasonable (10-20% for sliding window)
            
            return null; // Placeholder
        }

        /// <summary>
        /// Determines if atom should be quarantined based on issue severity.
        /// </summary>
        private bool ShouldQuarantineAtom(List<string> issues)
        {
            // Quarantine if:
            // 1. Format is invalid/corrupt
            // 2. Embedding has NaN/Inf
            // 3. Content is empty or too small
            // 4. Entropy is extremely low (spam/garbage)
            
            var criticalKeywords = new[] { "empty", "NaN", "Infinity", "invalid", "corrupt", "too small" };
            
            return issues.Any(issue => 
                criticalKeywords.Any(keyword => 
                    issue.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>
        /// Detects duplicate atoms using exact hash matching.
        /// For semantic similarity, use vector search with cosine similarity threshold.
        /// </summary>
        public bool IsDuplicateByHash(byte[] contentHash, HashSet<string> existingHashes)
        {
            var hashString = Convert.ToBase64String(contentHash);
            return existingHashes.Contains(hashString);
        }

        /// <summary>
        /// Detects semantic duplicates using cosine similarity.
        /// Threshold typically 0.95-0.99 for near-duplicates.
        /// </summary>
        public bool IsDuplicateBySimilarity(float[] embedding, float[] existingEmbedding, double threshold)
        {
            if (embedding == null || existingEmbedding == null)
                return false;

            if (embedding.Length != existingEmbedding.Length)
                return false;

            var similarity = CosineSimilarity(embedding, existingEmbedding);
            return similarity >= threshold;
        }

        /// <summary>
        /// Computes cosine similarity: cos(θ) = (A · B) / (||A|| * ||B||)
        /// Returns value in [-1, 1] where 1 = identical, 0 = orthogonal, -1 = opposite.
        /// </summary>
        public static double CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Vectors must have same dimensionality");

            double dotProduct = 0;
            double magnitudeA = 0;
            double magnitudeB = 0;

            for (int i = 0; i < a.Length; i++)
            {
                dotProduct += a[i] * b[i];
                magnitudeA += a[i] * a[i];
                magnitudeB += b[i] * b[i];
            }

            magnitudeA = Math.Sqrt(magnitudeA);
            magnitudeB = Math.Sqrt(magnitudeB);

            if (magnitudeA == 0 || magnitudeB == 0)
                return 0;

            return dotProduct / (magnitudeA * magnitudeB);
        }
    }

    /// <summary>
    /// Request for atom quality validation.
    /// </summary>
    public class AtomValidationRequest
    {
        public string? Content { get; set; }
        public byte[]? ContentBytes { get; set; }
        public float[]? Embedding { get; set; }
        public string? ExpectedFormat { get; set; }
        public string? ChunkingStrategy { get; set; }
    }

    /// <summary>
    /// Result of atom quality validation.
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Issues { get; set; } = new List<string>();
        public bool ShouldQuarantine { get; set; }
        public double Entropy { get; set; }
    }
}
