using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces;
using Hartonomous.Testing.Common;
using Microsoft.Extensions.Logging;

namespace Hartonomous.UnitTests.ModelIngestion;

public sealed class AtomicStorageTestServiceTests
{
    [Fact]
    public async Task TestAtomicStorageAsync_VerifiesDeduplicationViaIds()
    {
        var storage = new StubAtomicStorageService();
        var logger = TestLogger.Create<global::ModelIngestion.AtomicStorageTestService>();
        var service = new global::ModelIngestion.AtomicStorageTestService(storage, logger);

        await service.TestAtomicStorageAsync(CancellationToken.None);

        Assert.True(storage.PixelIds.Count >= 3);
        Assert.Equal(storage.PixelIds[0], storage.PixelIds[1]);
        Assert.NotEqual(storage.PixelIds[0], storage.PixelIds[2]);
        Assert.Equal(storage.AudioSamples[0], storage.AudioSamples[1]);
        Assert.Equal(storage.TokenIds[0], storage.TokenIds[1]);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Information && entry.Message.Contains("Atomic storage test complete", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class StubAtomicStorageService : IAtomicStorageService
    {
        private readonly Dictionary<string, long> _pixelHashes = new();
        private readonly Dictionary<float, long> _audioSamples = new();
        private readonly Dictionary<string, long> _tokens = new();
        private long _nextId = 1;

        public List<long> PixelIds { get; } = new();
        public List<long> AudioSamples { get; } = new();
        public List<long> TokenIds { get; } = new();

        public Task<long> StoreAtomicPixelAsync(byte r, byte g, byte b, byte a, CancellationToken cancellationToken = default)
        {
            var key = $"{r:X2}{g:X2}{b:X2}{a:X2}";
            if (!_pixelHashes.TryGetValue(key, out var id))
            {
                id = _nextId++;
                _pixelHashes[key] = id;
            }

            PixelIds.Add(id);
            return Task.FromResult(id);
        }

        public Task<long> StoreAtomicAudioSampleAsync(float amplitude, CancellationToken cancellationToken = default)
        {
            if (!_audioSamples.TryGetValue(amplitude, out var id))
            {
                id = _nextId++;
                _audioSamples[amplitude] = id;
            }

            AudioSamples.Add(id);
            return Task.FromResult(id);
        }

        public Task<long> StoreAtomicTextTokenAsync(string tokenText, int? vocabId = null, CancellationToken cancellationToken = default)
        {
            var key = tokenText + ":" + vocabId?.ToString() ?? string.Empty;
            if (!_tokens.TryGetValue(key, out var id))
            {
                id = _nextId++;
                _tokens[key] = id;
            }

            TokenIds.Add(id);
            return Task.FromResult(id);
        }

        public Task<IEnumerable<long>> StoreBatchPixelsAsync(IEnumerable<(byte r, byte g, byte b, byte a)> pixels, CancellationToken cancellationToken = default)
        {
            var result = new List<long>();
            foreach (var (r, g, b, a) in pixels)
            {
                var id = StoreAtomicPixelAsync(r, g, b, a, cancellationToken).GetAwaiter().GetResult();
                result.Add(id);
            }

            return Task.FromResult<IEnumerable<long>>(result);
        }
    }
}
