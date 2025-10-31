using System.IO;
using Microsoft.Extensions.Logging;
using Hartonomous.Core.Interfaces;

namespace ModelIngestion.ModelFormats
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
    private readonly IModelLayerRepository _layerRepository;

        public ModelReaderFactory(
            ILogger<OnnxModelReader> onnxLogger,
            ILogger<SafetensorsModelReader> safetensorsLogger,
            IModelLayerRepository layerRepository)
        {
            _onnxLogger = onnxLogger ?? throw new System.ArgumentNullException(nameof(onnxLogger));
            _safetensorsLogger = safetensorsLogger ?? throw new System.ArgumentNullException(nameof(safetensorsLogger));
            _layerRepository = layerRepository ?? throw new System.ArgumentNullException(nameof(layerRepository));
        }

        public IModelFormatReader<OnnxMetadata> GetOnnxReader()
        {
            return new OnnxModelReader(_onnxLogger, _layerRepository);
        }

        public IModelFormatReader<SafetensorsMetadata> GetSafetensorsReader()
        {
            return new SafetensorsModelReader(_safetensorsLogger, _layerRepository);
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
                default:
                    throw new System.NotSupportedException($"File type not supported: {extension}");
            }
        }
    }
}
