using System;
using System.Collections.Generic;
using Google.Protobuf;

namespace Hartonomous.Infrastructure.Services.ModelFormats;

internal static class OnnxModelParser
{
    private const uint ModelGraphTag = (7u << 3) | 2u;
    private const uint GraphInitializerTag = (5u << 3) | 2u;

    private const uint TensorDimsTag = (1u << 3) | 0u;
    private const uint TensorDataTypeTag = (2u << 3);
    private const uint TensorFloatPackedTag = (4u << 3) | 2u;
    private const uint TensorFloatSingleTag = (4u << 3) | 5u;
    private const uint TensorDoublePackedTag = (13u << 3) | 2u;
    private const uint TensorDoubleSingleTag = (13u << 3) | 1u;
    private const uint TensorRawDataTag = (8u << 3) | 2u;
    private const uint TensorNameTag = (9u << 3) | 2u;
    private const uint TensorDocStringTag = (12u << 3) | 2u;

    public static OnnxModelParseResult Parse(byte[] modelBytes)
    {
        if (modelBytes is null || modelBytes.Length == 0)
        {
            return new OnnxModelParseResult();
        }

        var result = new OnnxModelParseResult();
        var input = new CodedInputStream(modelBytes);

        while (!input.IsAtEnd)
        {
            var tag = input.ReadTag();
            if (tag == 0)
            {
                break;
            }

            if (tag == ModelGraphTag)
            {
                var graphBytes = input.ReadBytes();
                if (!graphBytes.IsEmpty)
                {
                    var graphStream = new CodedInputStream(graphBytes.ToByteArray());
                    ParseGraph(graphStream, result.Initializers);
                }
            }
            else
            {
                input.SkipLastField();
            }
        }

        return result;
    }

    private static void ParseGraph(CodedInputStream input, ICollection<OnnxInitializer> initializers)
    {
        while (!input.IsAtEnd)
        {
            var tag = input.ReadTag();
            if (tag == 0)
            {
                break;
            }

            if (tag == GraphInitializerTag)
            {
                var tensorBytes = input.ReadBytes();
                if (tensorBytes.IsEmpty)
                {
                    continue;
                }

                var tensorStream = new CodedInputStream(tensorBytes.ToByteArray());
                var initializer = ParseTensor(tensorStream);
                if (initializer is not null)
                {
                    initializers.Add(initializer);
                }
            }
            else
            {
                input.SkipLastField();
            }
        }
    }

    private static OnnxInitializer? ParseTensor(CodedInputStream input)
    {
        var initializer = new OnnxInitializer();

        while (!input.IsAtEnd)
        {
            var tag = input.ReadTag();
            if (tag == 0)
            {
                break;
            }

            if (tag == TensorDimsTag)
            {
                initializer.Dims.Add(input.ReadInt64());
                continue;
            }

            switch (tag)
            {
                case TensorDataTypeTag:
                    initializer.DataType = input.ReadInt32();
                    break;
                case TensorFloatPackedTag:
                    {
                        var data = input.ReadBytes();
                        if (!data.IsEmpty)
                        {
                            ReadPackedFloats(data.Span, initializer.FloatData);
                        }
                        break;
                    }
                case TensorFloatSingleTag:
                    initializer.FloatData.Add(input.ReadFloat());
                    break;
                case TensorDoublePackedTag:
                    {
                        var data = input.ReadBytes();
                        if (!data.IsEmpty)
                        {
                            ReadPackedDoubles(data.Span, initializer.DoubleData);
                        }
                        break;
                    }
                case TensorDoubleSingleTag:
                    initializer.DoubleData.Add(input.ReadDouble());
                    break;
                case TensorRawDataTag:
                    initializer.RawData = input.ReadBytes().ToByteArray();
                    break;
                case TensorNameTag:
                    initializer.Name = input.ReadString();
                    break;
                case TensorDocStringTag:
                    initializer.DocString = input.ReadString();
                    break;
                default:
                    input.SkipLastField();
                    break;
            }
        }

        return initializer;
    }

    private static void ReadPackedFloats(ReadOnlySpan<byte> data, IList<float> target)
    {
        if (data.IsEmpty)
        {
            return;
        }

        var count = data.Length / sizeof(float);
        for (var i = 0; i < count; i++)
        {
            var value = BitConverter.ToSingle(data.Slice(i * sizeof(float), sizeof(float)));
            target.Add(value);
        }
    }

    private static void ReadPackedDoubles(ReadOnlySpan<byte> data, IList<double> target)
    {
        if (data.IsEmpty)
        {
            return;
        }

        var count = data.Length / sizeof(double);
        for (var i = 0; i < count; i++)
        {
            var value = BitConverter.ToDouble(data.Slice(i * sizeof(double), sizeof(double)));
            target.Add(value);
        }
    }
}

internal sealed class OnnxModelParseResult
{
    public List<OnnxInitializer> Initializers { get; } = new();
}

internal sealed class OnnxInitializer
{
    public string Name { get; set; } = string.Empty;
    public int DataType { get; set; }
    public List<long> Dims { get; } = new();
    public List<float> FloatData { get; } = new();
    public List<double> DoubleData { get; } = new();
    public byte[] RawData { get; set; } = Array.Empty<byte>();
    public string? DocString { get; set; }
}
