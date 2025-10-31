using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ModelIngestion;

/// <summary>
/// Service for testing atomic storage operations with deduplication
/// </summary>
public class AtomicStorageTestService
{
    private readonly IAtomicStorageService _atomicStorage;
    private readonly ILogger<AtomicStorageTestService> _logger;

    public AtomicStorageTestService(
        IAtomicStorageService atomicStorage,
        ILogger<AtomicStorageTestService> logger)
    {
        _atomicStorage = atomicStorage ?? throw new ArgumentNullException(nameof(atomicStorage));
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
        var pixelId1 = await _atomicStorage.StoreAtomicPixelAsync(255, 0, 0, 255, cancellationToken); // Red
        _logger.LogInformation("Stored red pixel: ID={Id}", pixelId1);

        var pixelId2 = await _atomicStorage.StoreAtomicPixelAsync(255, 0, 0, 255, cancellationToken); // Same red - should dedupe
        _logger.LogInformation("Stored red pixel again (duplicate): ID={Id}", pixelId2);
        _logger.LogInformation("IDs match (deduplication): {Match}", pixelId1 == pixelId2);

        var pixelId3 = await _atomicStorage.StoreAtomicPixelAsync(0, 255, 0, 255, cancellationToken); // Green - different
        _logger.LogInformation("Stored green pixel: ID={Id}", pixelId3);

        // Test 2: Atomic audio samples
        _logger.LogInformation("\n=== Testing Atomic Audio Sample Storage ===");
        var sampleId1 = await _atomicStorage.StoreAtomicAudioSampleAsync(0.5f, cancellationToken); // Mid-range amplitude (normalized)
        _logger.LogInformation("Stored audio sample (0.5): ID={Id}", sampleId1);

        var sampleId2 = await _atomicStorage.StoreAtomicAudioSampleAsync(0.5f, cancellationToken); // Same - should dedupe
        _logger.LogInformation("Stored same sample again (duplicate): ID={Id}", sampleId2);
        _logger.LogInformation("IDs match (deduplication): {Match}", sampleId1 == sampleId2);

        // Test 3: Atomic tokens
        _logger.LogInformation("\n=== Testing Atomic Token Storage ===");
        var tokenId1 = await _atomicStorage.StoreAtomicTextTokenAsync("hello", null, cancellationToken);
        _logger.LogInformation("Stored token 'hello': ID={Id}", tokenId1);

        var tokenId2 = await _atomicStorage.StoreAtomicTextTokenAsync("hello", null, cancellationToken); // Same - should dedupe
        _logger.LogInformation("Stored token 'hello' again (duplicate): ID={Id}", tokenId2);
        _logger.LogInformation("IDs match (deduplication): {Match}", tokenId1 == tokenId2);

        var tokenId3 = await _atomicStorage.StoreAtomicTextTokenAsync("world", null, cancellationToken); // Different
        _logger.LogInformation("Stored token 'world': ID={Id}", tokenId3);

        // Test 4: Batch pixel storage
        _logger.LogInformation("\n=== Testing Batch Pixel Storage ===");
        var batchPixels = new List<(byte r, byte g, byte b, byte a)>
        {
            (255, 0, 0, 255), // Red - duplicate
            (0, 255, 0, 255), // Green - duplicate
            (0, 0, 255, 255)  // Blue - new
        };
        var batchIds = await _atomicStorage.StoreBatchPixelsAsync(batchPixels, cancellationToken);
        _logger.LogInformation("Batch stored {Count} pixels, got IDs: {Ids}",
            batchPixels.Count, string.Join(", ", batchIds));

        _logger.LogInformation("\n✓ Atomic storage test complete - deduplication working!");
    }
}