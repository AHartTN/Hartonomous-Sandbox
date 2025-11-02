using Microsoft.Extensions.Logging;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Entities;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ModelIngestion
{
    /// <summary>
    /// Service for content-addressable atomic component storage.
    /// NEVER store the same atomic component twice - use hashing for deduplication.
    /// Examples: pixels, audio samples, vector components, tokens, waveform patterns
    /// </summary>
    public class AtomicStorageService : IAtomicStorageService
    {
        private readonly ILogger<AtomicStorageService> _logger;
        private readonly IAtomicPixelRepository _pixelRepository;
        private readonly IAtomicAudioSampleRepository _audioSampleRepository;
        private readonly IAtomicTextTokenRepository _textTokenRepository;

        public AtomicStorageService(
            ILogger<AtomicStorageService> logger,
            IAtomicPixelRepository pixelRepository,
            IAtomicAudioSampleRepository audioSampleRepository,
            IAtomicTextTokenRepository textTokenRepository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pixelRepository = pixelRepository ?? throw new ArgumentNullException(nameof(pixelRepository));
            _audioSampleRepository = audioSampleRepository ?? throw new ArgumentNullException(nameof(audioSampleRepository));
            _textTokenRepository = textTokenRepository ?? throw new ArgumentNullException(nameof(textTokenRepository));
        }

        // =============================================
        // PIXEL STORAGE (Images)
        // =============================================

        /// <summary>
        /// Store atomic pixel with content-addressable deduplication
        /// Returns: pixel_id (existing or newly created)
        /// </summary>
        public async Task<long> StoreAtomicPixelAsync(byte r, byte g, byte b, byte a = 255, CancellationToken cancellationToken = default)
        {
            var pixelHash = ComputePixelHash(r, g, b, a);

            // Check if pixel already exists
            var existingPixel = await _pixelRepository.GetByHashAsync(pixelHash, cancellationToken);
            if (existingPixel != null)
            {
                // Increment reference count
                await _pixelRepository.UpdateReferenceCountAsync(pixelHash, cancellationToken);
                return ConvertHashToKey(existingPixel.PixelHash);
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
            return ConvertHashToKey(pixel.PixelHash);
        }

        /// <summary>
        /// Store a batch of atomic pixels for efficient bulk operations
        /// </summary>
        public async Task<IEnumerable<long>> StoreBatchPixelsAsync(
            IEnumerable<(byte r, byte g, byte b, byte a)> pixels,
            CancellationToken cancellationToken = default)
        {
            var ids = new List<long>();

            foreach (var pixel in pixels)
            {
                var id = await StoreAtomicPixelAsync(pixel.r, pixel.g, pixel.b, pixel.a, cancellationToken);
                ids.Add(id);
            }

            return ids;
        }

        /// <summary>
        /// Store atomic audio sample with deduplication
        /// Interface expects normalized amplitude (-1.0 to 1.0)
        /// </summary>
        public async Task<long> StoreAtomicAudioSampleAsync(float amplitude, CancellationToken cancellationToken = default)
        {
            // Convert normalized amplitude to int16 for storage
            short amplitudeInt16 = (short)(amplitude * 32767.0f);
            var sampleHash = ComputeAudioSampleHash(amplitudeInt16);

            // Check if sample already exists
            var existingSample = await _audioSampleRepository.GetByHashAsync(sampleHash, cancellationToken);
            if (existingSample != null)
            {
                await _audioSampleRepository.UpdateReferenceCountAsync(sampleHash, cancellationToken);
                return ConvertHashToKey(existingSample.SampleHash);
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
            return ConvertHashToKey(sample.SampleHash);
        }

        /// <summary>
        /// Store atomic text token with deduplication
        /// </summary>
        public async Task<long> StoreAtomicTextTokenAsync(string tokenText, int? vocabId = null, CancellationToken cancellationToken = default)
        {
            var tokenHash = ComputeTextHash(tokenText);

            // Check if token already exists
            var existingToken = await _textTokenRepository.GetByHashAsync(tokenHash, cancellationToken);
            if (existingToken != null)
            {
                await _textTokenRepository.UpdateReferenceCountAsync(existingToken.TokenId, cancellationToken);
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

        // =============================================
        // HASHING UTILITIES
        // =============================================

        private static long ConvertHashToKey(byte[] hash)
            => ConvertHashToKey((ReadOnlySpan<byte>)hash);

        private static long ConvertHashToKey(ReadOnlySpan<byte> hash)
        {
            if (hash.Length < sizeof(long))
            {
                throw new ArgumentException("Hash length must be at least 8 bytes", nameof(hash));
            }

            var value = BinaryPrimitives.ReadInt64BigEndian(hash[..sizeof(long)]);
            return value & long.MaxValue;
        }

        private byte[] ComputePixelHash(byte r, byte g, byte b, byte a)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(new byte[] { r, g, b, a });
        }

        private byte[] ComputeAudioSampleHash(short amplitude)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(BitConverter.GetBytes(amplitude));
        }

        private byte[] ComputeTextHash(string text)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
        }
    }
}
