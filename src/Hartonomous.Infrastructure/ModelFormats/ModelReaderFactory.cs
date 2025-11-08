using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Interfaces.ModelFormats;

namespace Hartonomous.Infrastructure.ModelFormats
{
    /// <summary>
    /// Factory for creating format-specific model readers.
    /// Uses DI to provide dependencies to readers.
    /// Updated to use IModelFormatReader<TMetadata> interface.
    /// </summary>
    public class ModelReaderFactory
    {
        private readonly ILogger<OnnxModelReader> _onnxLogger;
        private readonly ILogger<SafetensorsModelReader> _safetensorsLogger;
        private readonly ILogger<GGUFModelReader> _ggufLogger;
        private readonly IModelLayerRepository _layerRepository;
        private readonly IModelRepository _modelRepository;
        private readonly ILayerTensorSegmentRepository _tensorSegmentRepository;
        private readonly GGUFParser _ggufParser;
        private readonly GGUFDequantizer _ggufDequantizer;
        private readonly GGUFModelBuilder _ggufModelBuilder;
        private readonly GGUFGeometryBuilder _ggufGeometryBuilder;

        public ModelReaderFactory(
            ILogger<OnnxModelReader> onnxLogger,
            ILogger<SafetensorsModelReader> safetensorsLogger,
            ILogger<GGUFModelReader> ggufLogger,
            IModelLayerRepository layerRepository,
            IModelRepository modelRepository,
            ILayerTensorSegmentRepository tensorSegmentRepository,
            GGUFParser ggufParser,
            GGUFDequantizer ggufDequantizer,
            GGUFModelBuilder ggufModelBuilder,
            GGUFGeometryBuilder ggufGeometryBuilder)
        {
            _onnxLogger = onnxLogger ?? throw new ArgumentNullException(nameof(onnxLogger));
            _safetensorsLogger = safetensorsLogger ?? throw new ArgumentNullException(nameof(safetensorsLogger));
            _ggufLogger = ggufLogger ?? throw new ArgumentNullException(nameof(ggufLogger));
            _layerRepository = layerRepository ?? throw new ArgumentNullException(nameof(layerRepository));
            _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
            _tensorSegmentRepository = tensorSegmentRepository ?? throw new ArgumentNullException(nameof(tensorSegmentRepository));
            _ggufParser = ggufParser ?? throw new ArgumentNullException(nameof(ggufParser));
            _ggufDequantizer = ggufDequantizer ?? throw new ArgumentNullException(nameof(ggufDequantizer));
            _ggufModelBuilder = ggufModelBuilder ?? throw new ArgumentNullException(nameof(ggufModelBuilder));
            _ggufGeometryBuilder = ggufGeometryBuilder ?? throw new ArgumentNullException(nameof(ggufGeometryBuilder));
        }

        public IModelFormatReader<OnnxMetadata> GetOnnxReader()
        {
            return new OnnxModelReader(_onnxLogger, _layerRepository);
        }

        public IModelFormatReader<SafetensorsMetadata> GetSafetensorsReader()
        {
            return new SafetensorsModelReader(_safetensorsLogger, _layerRepository);
        }

        public IModelFormatReader<GGUFMetadata> GetGgufReader()
        {
            return new GGUFModelReader(
                _ggufParser,
                _ggufDequantizer,
                _ggufModelBuilder,
                _ggufGeometryBuilder,
                _ggufLogger);
        }

        /// <summary>
        /// Gets the appropriate reader for a file extension.
        /// Returns the reader interface, caller must cast to specific type.
        /// </summary>
        public object GetReader(string modelPath)
        {
            var extension = Path.GetExtension(modelPath).ToLowerInvariant();

            switch (extension)
            {
                case ".onnx":
                    return GetOnnxReader();
                case ".safetensors":
                    return GetSafetensorsReader();
                case ".gguf":
                    return GetGgufReader();
                default:
                    throw new NotSupportedException($"File type not supported: {extension}");
            }
        }
    }
}

