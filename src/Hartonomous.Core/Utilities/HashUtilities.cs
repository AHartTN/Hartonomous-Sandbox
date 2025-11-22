using System.Security.Cryptography;
using System.Text;

namespace Hartonomous.Core.Utilities;

/// <summary>
/// Centralized hashing utilities for consistent hash computation across the system.
/// Eliminates duplicate SHA256 implementations in 10+ files.
/// </summary>
public static class HashUtilities
{
    /// <summary>
    /// Computes SHA256 hash of byte array.
    /// </summary>
    public static byte[] ComputeSHA256(byte[] data)
    {
        if (data == null || data.Length == 0)
            throw new ArgumentException("Data cannot be null or empty", nameof(data));
            
        return SHA256.HashData(data);
    }

    /// <summary>
    /// Computes SHA256 hash of string (UTF-8 encoded).
    /// </summary>
    public static byte[] ComputeSHA256(string text)
    {
        if (string.IsNullOrEmpty(text))
            throw new ArgumentException("Text cannot be null or empty", nameof(text));
            
        return ComputeSHA256(Encoding.UTF8.GetBytes(text));
    }

    /// <summary>
    /// Computes deterministic GUID from string using SHA256.
    /// Used for correlation IDs and deterministic identifiers.
    /// </summary>
    public static Guid ComputeDeterministicGuid(string input)
    {
        var hash = ComputeSHA256(input ?? string.Empty);
        var guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);
        return new Guid(guidBytes);
    }

    /// <summary>
    /// Computes 64-byte fingerprint for content larger than 64 bytes.
    /// Format: SHA256 hash (32 bytes) + first 32 bytes of content.
    /// </summary>
    public static byte[] ComputeFingerprint(byte[] content, int maxSize = 64)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));
            
        var fingerprint = new byte[maxSize];
        
        // First half: SHA256 hash
        var hashSize = maxSize / 2;
        var hash = ComputeSHA256(content);
        Array.Copy(hash, 0, fingerprint, 0, Math.Min(hashSize, hash.Length));
        
        // Second half: First N bytes of content
        int copyLength = Math.Min(maxSize - hashSize, content.Length);
        Array.Copy(content, 0, fingerprint, hashSize, copyLength);
        
        return fingerprint;
    }

    /// <summary>
    /// Computes FNV-1a hash for deterministic integer seed generation.
    /// </summary>
    public static int ComputeFNV1aHash(byte[] data)
    {
        if (data == null || data.Length == 0)
            return 42; // Default seed
        
        unchecked
        {
            const uint FnvPrime = 16777619;
            const uint FnvOffsetBasis = 2166136261;
            
            uint hash = FnvOffsetBasis;
            int step = Math.Max(1, data.Length / 64); // Sample every Nth byte
            
            for (int i = 0; i < data.Length; i += step)
            {
                hash ^= data[i];
                hash *= FnvPrime;
            }
            
            return (int)hash;
        }
    }

    /// <summary>
    /// Computes hash of multiple byte arrays (composite hash).
    /// </summary>
    public static byte[] ComputeCompositeHash(params byte[][] arrays)
    {
        using var ms = new MemoryStream();
        
        foreach (var array in arrays)
        {
            if (array != null && array.Length > 0)
            {
                ms.Write(array, 0, array.Length);
            }
        }
        
        ms.Position = 0;
        return ComputeSHA256(ms.ToArray());
    }
}
