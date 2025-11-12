using System;
using System.Linq;

namespace Hartonomous.Core.Pipelines.Ingestion;

/// <summary>
/// Perceptual hash result containing the 64-bit hash value.
/// Used for image similarity detection via Hamming distance comparison.
/// </summary>
public readonly record struct PerceptualHash
{
    public ulong Hash { get; init; }
    
    /// <summary>
    /// Calculate Hamming distance (number of differing bits) between two hashes.
    /// Lower distance = more similar images. Typical threshold: 10 bits for "similar".
    /// </summary>
    public static int HammingDistance(ulong hash1, ulong hash2)
    {
        ulong xor = hash1 ^ hash2;
        int distance = 0;
        
        // Count set bits (Brian Kernighan's algorithm)
        while (xor != 0)
        {
            distance++;
            xor &= xor - 1; // Clear least significant set bit
        }
        
        return distance;
    }
}

/// <summary>
/// DCT-based perceptual image hashing (pHash) implementation.
/// 
/// Algorithm:
/// 1. Resize image to 32x32 grayscale (removes high-frequency detail, size variations)
/// 2. Compute 2D Discrete Cosine Transform (DCT) on pixel data
/// 3. Extract top-left 8x8 DCT coefficients (low-frequency components)
/// 4. Calculate median of 64 coefficients
/// 5. Generate 64-bit hash: bit[i] = 1 if coeff[i] > median, else 0
/// 
/// Usage:
/// - Identical images → hash = 0 distance
/// - Similar images (crops, compression, minor edits) → low distance (0-10 bits)
/// - Different images → high distance (>15 bits)
/// 
/// References:
/// - "Kind of Like That" perceptual hashing (Zauner, 2010)
/// - DCT-II formula: https://en.wikipedia.org/wiki/Discrete_cosine_transform
/// - pHash.org reference implementation concepts
/// </summary>
public static class PerceptualHasher
{
    private const int HashSize = 32; // Resize to 32x32 for DCT input
    private const int DctSize = 8;   // Extract 8x8 low-freq coefficients
    private const int HashBits = DctSize * DctSize; // 64 bits
    
    /// <summary>
    /// Compute perceptual hash from raw image bytes.
    /// Currently expects raw grayscale pixel data (32x32 bytes).
    /// TODO: Add image decoding for PNG/JPEG/etc formats.
    /// </summary>
    public static PerceptualHash ComputeHash(byte[] imageData)
    {
        if (imageData == null || imageData.Length == 0)
            throw new ArgumentException("Image data cannot be empty", nameof(imageData));
        
        // For now, assume imageData is already 32x32 grayscale
        // TODO: Implement image decoding and resizing
        if (imageData.Length != HashSize * HashSize)
        {
            throw new NotImplementedException(
                $"Image decoding not yet implemented. Expected {HashSize}x{HashSize} grayscale bytes, got {imageData.Length} bytes. " +
                "TODO: Integrate image decoder (PNG/JPEG magic number detection + resize to 32x32)");
        }
        
        // Convert bytes to doubles for DCT
        double[,] pixels = new double[HashSize, HashSize];
        for (int y = 0; y < HashSize; y++)
        {
            for (int x = 0; x < HashSize; x++)
            {
                pixels[y, x] = imageData[y * HashSize + x];
            }
        }
        
        // Compute 2D DCT
        double[,] dctCoeffs = Compute2dDct(pixels);
        
        // Extract top-left 8x8 coefficients (low-frequency components)
        double[] lowFreq = new double[HashBits];
        int idx = 0;
        for (int y = 0; y < DctSize; y++)
        {
            for (int x = 0; x < DctSize; x++)
            {
                lowFreq[idx++] = dctCoeffs[y, x];
            }
        }
        
        // Calculate median
        double median = ComputeMedian(lowFreq);
        
        // Generate hash: bit = 1 if coefficient > median
        ulong hash = 0;
        for (int i = 0; i < HashBits; i++)
        {
            if (lowFreq[i] > median)
            {
                hash |= 1UL << i;
            }
        }
        
        return new PerceptualHash { Hash = hash };
    }
    
    /// <summary>
    /// Compute 2D Discrete Cosine Transform using separable approach.
    /// Applies 1D DCT to each row, then to each column of the result.
    /// 
    /// DCT-II formula:
    /// X[k] = α[k] * Σ(n=0 to N-1) x[n] * cos(π * k * (2n + 1) / (2N))
    /// where α[0] = √(1/N), α[k] = √(2/N) for k > 0
    /// </summary>
    private static double[,] Compute2dDct(double[,] pixels)
    {
        int size = pixels.GetLength(0);
        double[,] temp = new double[size, size];
        double[,] result = new double[size, size];
        
        // Step 1: DCT on each row
        for (int y = 0; y < size; y++)
        {
            double[] row = new double[size];
            for (int x = 0; x < size; x++)
                row[x] = pixels[y, x];
            
            double[] dctRow = Dct1D(row);
            
            for (int x = 0; x < size; x++)
                temp[y, x] = dctRow[x];
        }
        
        // Step 2: DCT on each column of temp
        for (int x = 0; x < size; x++)
        {
            double[] col = new double[size];
            for (int y = 0; y < size; y++)
                col[y] = temp[y, x];
            
            double[] dctCol = Dct1D(col);
            
            for (int y = 0; y < size; y++)
                result[y, x] = dctCol[y];
        }
        
        return result;
    }
    
    /// <summary>
    /// Compute 1D Discrete Cosine Transform (DCT-II).
    /// 
    /// Formula:
    /// X[k] = α[k] * Σ(n=0 to N-1) x[n] * cos(π * k * (2n + 1) / (2N))
    /// 
    /// Scaling factors:
    /// α[0] = √(1/N)
    /// α[k] = √(2/N) for k > 0
    /// </summary>
    private static double[] Dct1D(double[] input)
    {
        int N = input.Length;
        double[] output = new double[N];
        
        double sqrtN = Math.Sqrt(N);
        double sqrt2N = Math.Sqrt(2.0 / N);
        double sqrtN_inv = 1.0 / sqrtN;
        
        for (int k = 0; k < N; k++)
        {
            double sum = 0.0;
            
            for (int n = 0; n < N; n++)
            {
                double angle = Math.PI * k * (2 * n + 1) / (2.0 * N);
                sum += input[n] * Math.Cos(angle);
            }
            
            // Apply scaling factor
            double alpha = (k == 0) ? sqrtN_inv : sqrt2N;
            output[k] = alpha * sum;
        }
        
        return output;
    }
    
    /// <summary>
    /// Compute median of values using partial sort (QuickSelect would be faster for large arrays).
    /// For 64 values, simple sort is fine.
    /// </summary>
    private static double ComputeMedian(double[] values)
    {
        double[] sorted = values.ToArray();
        Array.Sort(sorted);
        
        int mid = sorted.Length / 2;
        
        // For even count, average middle two values
        if (sorted.Length % 2 == 0)
            return (sorted[mid - 1] + sorted[mid]) / 2.0;
        else
            return sorted[mid];
    }
}
