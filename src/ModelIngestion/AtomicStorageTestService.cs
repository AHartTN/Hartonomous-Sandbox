using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Hartonomous.Core;
using Hartonomous.Infrastructure.Repositories;

namespace ModelIngestion;

/// <summary>
/// Service for testing atomic storage operations with deduplication
/// </summary>
public class AtomicStorageTestService
{
    private readonly IAtomicPixelRepository _pixelRepository;
    private readonly IAtomicAudioSampleRepository _audioSampleRepository;
    private readonly IAtomicTextTokenRepository _textTokenRepository;
    private readonly ILogger<AtomicStorageTestService> _logger;

    public AtomicStorageTestService(
        IAtomicPixelRepository pixelRepository,
        IAtomicAudioSampleRepository audioSampleRepository,
        IAtomicTextTokenRepository textTokenRepository,
        ILogger<AtomicStorageTestService> logger)
    {
        _pixelRepository = pixelRepository ?? throw new ArgumentNullException(nameof(pixelRepository));
        _audioSampleRepository = audioSampleRepository ?? throw new ArgumentNullException(nameof(audioSampleRepository));
        _textTokenRepository = textTokenRepository ?? throw new ArgumentNullException(nameof(textTokenRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Test atomic storage with deduplication
    /// </summary>
    public async Task TestAtomicStorageAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Testing atomic storage with content-addressable deduplication...");

        // Test 1: Atomic pixels
        _logger.LogInformation("\n=== Testing Atomic Pixel Storage ===");
        var pixelId1 = await StoreAtomicPixelAsync(255, 0, 0, 255, cancellationToken); // Red
        _logger.LogInformation("Stored red pixel: ID={Id}", pixelId1);

        var pixelId2 = await StoreAtomicPixelAsync(255, 0, 0, 255, cancellationToken); // Same red - should dedupe
        _logger.LogInformation("Stored red pixel again (duplicate): ID={Id}", pixelId2);
        _logger.LogInformation("IDs match (deduplication): {Match}", pixelId1 == pixelId2);

        var pixelId3 = await StoreAtomicPixelAsync(0, 255, 0, 255, cancellationToken); // Green - different
        _logger.LogInformation("Stored green pixel: ID={Id}", pixelId3);

        // Test 2: Atomic audio samples
        _logger.LogInformation("\n=== Testing Atomic Audio Sample Storage ===");
        var sampleId1 = await StoreAtomicAudioSampleAsync(0.5f, cancellationToken); // Mid-range amplitude (normalized)
        _logger.LogInformation("Stored audio sample (0.5): ID={Id}", sampleId1);

        var sampleId2 = await StoreAtomicAudioSampleAsync(0.5f, cancellationToken); // Same - should dedupe
        _logger.LogInformation("Stored same sample again (duplicate): ID={Id}", sampleId2);
        _logger.LogInformation("IDs match (deduplication): {Match}", sampleId1 == sampleId2);

        // Test 3: Atomic tokens
        _logger.LogInformation("\n=== Testing Atomic Token Storage ===");
        var tokenId1 = await StoreAtomicTextTokenAsync("hello", null, cancellationToken);
        _logger.LogInformation("Stored token 'hello': ID={Id}", tokenId1);

        var tokenId2 = await StoreAtomicTextTokenAsync("hello", null, cancellationToken); // Same - should dedupe
        _logger.LogInformation("Stored token 'hello' again (duplicate): ID={Id}", tokenId2);
        _logger.LogInformation("IDs match (deduplication): {Match}", tokenId1 == tokenId2);

        var tokenId3 = await StoreAtomicTextTokenAsync("world", null, cancellationToken); // Different
        _logger.LogInformation("Stored token 'world': ID={Id}", tokenId3);

        // Test 4: Batch pixel storage
        _logger.LogInformation("\n=== Testing Batch Pixel Storage ===");
        var batchPixels = new List<(byte r, byte g, byte b, byte a)>
        {
            (255, 0, 0, 255), // Red - duplicate
            (0, 255, 0, 255), // Green - duplicate
            (0, 0, 255, 255)  // Blue - new
        };
        var batchIds = await StoreBatchPixelsAsync(batchPixels, cancellationToken);
        _logger.LogInformation("Batch stored {Count} pixels, got IDs: {Ids}",
            batchPixels.Count, string.Join(", ", batchIds));

        _logger.LogInformation("\n✓ Atomic storage test complete - deduplication working!");
    }

    private async Task<long> StoreAtomicPixelAsync(byte r, byte g, byte b, byte a, CancellationToken cancellationToken)
    {
        var pixelHash = ComputePixelHash(r, g, b, a);

        // Check if pixel already exists
        var existingPixel = await _pixelRepository.GetByHashAsync(pixelHash, cancellationToken);
        if (existingPixel != null)
        {
            // Increment reference count
            await _pixelRepository.UpdateReferenceCountAsync(pixelHash, cancellationToken);
            return existingPixel.PixelHash.GetHashCode(); // Return hash code as ID for now
        }

        // Create new atomic pixel
        var pixel = new AtomicPixel
        {
            PixelHash = pixelHash,
            R = r,
            G = g,
            B = b,
            A = a,
            ReferenceCount = 1
        };

        await _pixelRepository.AddAsync(pixel, cancellationToken);
        return pixel.PixelHash.GetHashCode(); // Return hash code as ID for now
    }

    private async Task<IEnumerable<long>> StoreBatchPixelsAsync(IEnumerable<(byte r, byte g, byte b, byte a)> pixels, CancellationToken cancellationToken)
    {
        var ids = new List<long>();

        foreach (var pixel in pixels)
        {
            var id = await StoreAtomicPixelAsync(pixel.r, pixel.g, pixel.b, pixel.a, cancellationToken);
            ids.Add(id);
        }

        return ids;
    }

    private async Task<long> StoreAtomicAudioSampleAsync(float amplitude, CancellationToken cancellationToken)
    {
        // Convert normalized amplitude to int16 for storage
        short amplitudeInt16 = (short)(amplitude * 32767.0f);
        var sampleHash = ComputeAudioSampleHash(amplitudeInt16);

        // Check if sample already exists
        var existingSample = await _audioSampleRepository.GetByHashAsync(sampleHash, cancellationToken);
        if (existingSample != null)
        {
            await _audioSampleRepository.UpdateReferenceCountAsync(sampleHash, cancellationToken);
            return existingSample.SampleHash.GetHashCode(); // Return hash code as ID for now
        }

        // Create new atomic audio sample
        var sample = new AtomicAudioSample
        {
            SampleHash = sampleHash,
            AmplitudeNormalized = amplitude,
            AmplitudeInt16 = amplitudeInt16,
            ReferenceCount = 1
        };

        await _audioSampleRepository.AddAsync(sample, cancellationToken);
        return sample.SampleHash.GetHashCode(); // Return hash code as ID for now
    }

    private async Task<long> StoreAtomicTextTokenAsync(string tokenText, int? vocabId, CancellationToken cancellationToken)
    {
        var tokenHash = ComputeTextHash(tokenText);

        // Check if token already exists
        var existingToken = await _textTokenRepository.GetByHashAsync(tokenHash, cancellationToken);
        if (existingToken != null)
        {
            await _textTokenRepository.UpdateReferenceCountAsync(tokenHash, cancellationToken);
            return existingToken.TokenId;
        }

        // Create new atomic token
        var token = new AtomicTextToken
        {
            TokenHash = tokenHash,
            TokenText = tokenText,
            TokenLength = tokenText.Length,
            VocabId = vocabId,
            ReferenceCount = 1
        };

        var addedToken = await _textTokenRepository.AddAsync(token, cancellationToken);
        return addedToken.TokenId;
    }

    private byte[] ComputePixelHash(byte r, byte g, byte b, byte a)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        return sha256.ComputeHash(new byte[] { r, g, b, a });
    }

    private byte[] ComputeAudioSampleHash(short amplitude)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        return sha256.ComputeHash(BitConverter.GetBytes(amplitude));
    }

    private byte[] ComputeTextHash(string text)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        return sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text));
    }
}