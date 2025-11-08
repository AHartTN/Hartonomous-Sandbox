using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.ModelFormats;

internal static class TensorDataReader
{
    public static float[] Read(BinaryReader reader, string dtype, int elementCount, long availableBytes, ILogger logger)
    {
        if (elementCount <= 0)
        {
            return Array.Empty<float>();
        }

        var result = new float[elementCount];
        var normalized = dtype?.ToLowerInvariant();
        switch (normalized)
        {
            case "f32":
            case "float32":
                FillFloat32(reader, result, availableBytes);
                break;
            case "f16":
            case "float16":
                FillFloat16(reader, result, availableBytes);
                break;
            case "bf16":
            case "bfloat16":
                FillBFloat16(reader, result, availableBytes);
                break;
            default:
                logger.LogWarning("Unsupported dtype: {DType}, skipping tensor data", dtype);
                Array.Clear(result);
                break;
        }

        return result;
    }

    public static float[] Read(BinaryReader reader, string dtype, int elementCount, long availableBytes)
        => Read(reader, dtype, elementCount, availableBytes, NullLogger.Instance);

    private static void FillFloat32(BinaryReader reader, float[] destination, long availableBytes)
    {
        int limit = Math.Min(destination.Length, (int)(availableBytes / sizeof(float)));
        for (int i = 0; i < limit; i++)
        {
            destination[i] = reader.ReadSingle();
        }
    }

    private static void FillFloat16(BinaryReader reader, float[] destination, long availableBytes)
    {
        int limit = Math.Min(destination.Length, (int)(availableBytes / sizeof(ushort)));
        for (int i = 0; i < limit; i++)
        {
            var halfBits = reader.ReadUInt16();
            destination[i] = Float16Utilities.HalfToFloat(halfBits);
        }
    }

    private static void FillBFloat16(BinaryReader reader, float[] destination, long availableBytes)
    {
        int limit = Math.Min(destination.Length, (int)(availableBytes / sizeof(ushort)));
        for (int i = 0; i < limit; i++)
        {
            var bfloat16Bits = reader.ReadUInt16();
            destination[i] = Float16Utilities.BFloat16ToFloat(bfloat16Bits);
        }
    }

    private sealed class NullLogger : ILogger
    {
        public static readonly NullLogger Instance = new();

        private NullLogger() { }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => Disposable.Instance;

        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
        }

        private sealed class Disposable : IDisposable
        {
            public static readonly Disposable Instance = new();
            private Disposable() { }
            public void Dispose() { }
        }
    }
}
