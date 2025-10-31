using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using TorchSharp;

namespace ModelIngestion.ModelFormats
{
    /// <summary>
    /// Describes a single PyTorch parameter extracted from a model.
    /// </summary>
    public sealed record PyTorchParameterInfo(string Name, float[] Weights, long[] Shape, string DType, bool RequiresGrad);

    /// <summary>
    /// Aggregated result of inspecting a PyTorch model.
    /// </summary>
    public sealed class PyTorchModelLoadResult
    {
        public PyTorchModelLoadResult(IReadOnlyList<PyTorchParameterInfo> parameters, Dictionary<string, object> stateDict)
        {
            Parameters = parameters;
            StateDict = stateDict;
        }

        public IReadOnlyList<PyTorchParameterInfo> Parameters { get; }
        public Dictionary<string, object> StateDict { get; }
    }

    /// <summary>
    /// Abstraction that loads a PyTorch model and materialises parameter tensors.
    /// </summary>
    public interface IPyTorchModelLoader
    {
        PyTorchModelLoadResult Load(string modelPath, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// TorchSharp-based implementation of <see cref="IPyTorchModelLoader"/>.
    /// </summary>
    public sealed class TorchSharpModelLoader : IPyTorchModelLoader
    {
        private readonly ILogger? _logger;

        public TorchSharpModelLoader(ILogger? logger = null)
        {
            _logger = logger;
        }

        public PyTorchModelLoadResult Load(string modelPath, CancellationToken cancellationToken = default)
        {
            using var module = torch.jit.load(modelPath);

            var parameters = new List<PyTorchParameterInfo>();
            var stateDict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (var param in module.named_parameters())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var weights = ExtractTensorWeights(param.parameter);
                var shape = param.parameter.shape?.Select(dim => (long)dim).ToArray() ?? Array.Empty<long>();
                var dtype = param.parameter.dtype.ToString();
                var requiresGrad = param.parameter.requires_grad;

                parameters.Add(new PyTorchParameterInfo(param.name, weights ?? Array.Empty<float>(), shape, dtype, requiresGrad));
                stateDict[param.name] = new
                {
                    shape,
                    dtype,
                    requires_grad = requiresGrad
                };
            }

            return new PyTorchModelLoadResult(parameters, stateDict);
        }

        private float[]? ExtractTensorWeights(torch.Tensor tensor)
        {
            try
            {
                using var flatTensor = tensor.flatten();
                var numElements = flatTensor.numel();

                if (numElements <= 0 || numElements > int.MaxValue)
                {
                    _logger?.LogWarning("Tensor too large or empty: {Size} elements", numElements);
                    return null;
                }

                var weights = new float[numElements];
                using var cpuTensor = flatTensor.cpu();
                using var dataPtr = cpuTensor.data<float>();

                for (int i = 0; i < numElements; i++)
                {
                    weights[i] = dataPtr[i];
                }

                return weights;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to extract tensor weights");
                return null;
            }
        }
    }
}
