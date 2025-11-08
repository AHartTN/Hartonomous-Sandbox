using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;

namespace Hartonomous.Infrastructure.ModelFormats;

public interface IOnnxModelLoader
{
    OnnxModelLoadResult Load(string modelPath, CancellationToken cancellationToken = default);
}

public sealed class OnnxModelLoadResult
{
    public OnnxModelLoadResult(
        string modelName,
        string? graphName,
        string? domain,
        string? producerName,
        string? description,
        IReadOnlyList<string> inputs,
        IReadOnlyList<string> outputs,
        IReadOnlyList<OnnxInitializerInfo> initializers)
    {
        ModelName = modelName;
        GraphName = graphName;
        Domain = domain;
        ProducerName = producerName;
        Description = description;
        Inputs = inputs;
        Outputs = outputs;
        Initializers = initializers;
    }

    public string ModelName { get; }
    public string? GraphName { get; }
    public string? Domain { get; }
    public string? ProducerName { get; }
    public string? Description { get; }
    public IReadOnlyList<string> Inputs { get; }
    public IReadOnlyList<string> Outputs { get; }
    public IReadOnlyList<OnnxInitializerInfo> Initializers { get; }
}

public sealed record OnnxInitializerInfo(
    string Name,
    int DataType,
    long[] Dimensions,
    IReadOnlyList<float> FloatData,
    IReadOnlyList<double> DoubleData,
    byte[] RawData,
    string? DocString);

public sealed class OnnxModelLoader : IOnnxModelLoader
{
    private readonly ILogger _logger;

    public OnnxModelLoader(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public OnnxModelLoadResult Load(string modelPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(modelPath);
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException("ONNX model file not found", modelPath);
        }

        var modelBytes = File.ReadAllBytes(modelPath);
        var parseResult = OnnxModelParser.Parse(modelBytes);

        using var session = new InferenceSession(modelPath);
        var metadata = session.ModelMetadata;

        var inputs = session.InputMetadata.Keys.ToArray();
        var outputs = session.OutputMetadata.Keys.ToArray();

        var initializerInfos = parseResult.Initializers
            .Select(i => new OnnxInitializerInfo(
                string.IsNullOrWhiteSpace(i.Name) ? $"initializer_{Guid.NewGuid():N}" : i.Name,
                i.DataType,
                i.Dims.ToArray(),
                i.FloatData.ToArray(),
                i.DoubleData.ToArray(),
                i.RawData,
                i.DocString))
            .ToArray();

        _logger.LogDebug("Parsed {InitializerCount} ONNX initializers from {Path}", initializerInfos.Length, modelPath);

        return new OnnxModelLoadResult(
            modelName: metadata.GraphName ?? Path.GetFileNameWithoutExtension(modelPath),
            graphName: metadata.GraphName,
            domain: metadata.Domain,
            producerName: metadata.ProducerName,
            description: metadata.Description,
            inputs: inputs,
            outputs: outputs,
            initializers: initializerInfos);
    }
}
