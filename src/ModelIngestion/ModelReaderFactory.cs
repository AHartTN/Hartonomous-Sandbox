using System.IO;
using Microsoft.Extensions.Logging;
using Hartonomous.Infrastructure.Repositories;

namespace ModelIngestion
{
    /// <summary>
    /// Factory for creating format-specific model readers.
    /// Uses DI to provide dependencies to readers.
    /// OBSOLETE: This factory will be removed in Phase 6 - use DI to inject specific readers instead.
    /// </summary>
    [System.Obsolete("This legacy factory will be removed. Use DI to inject IModelFormatReader<TMetadata> implementations directly.")]
    public class ModelReaderFactory
    {
        private readonly IModelRepository _modelRepository;
        private readonly ILoggerFactory _loggerFactory;

        public ModelReaderFactory(IModelRepository modelRepository, ILoggerFactory loggerFactory)
        {
            _modelRepository = modelRepository ?? throw new System.ArgumentNullException(nameof(modelRepository));
            _loggerFactory = loggerFactory ?? throw new System.ArgumentNullException(nameof(loggerFactory));
        }

        public object GetReader(string modelPath)
        {
            var extension = Path.GetExtension(modelPath).ToLowerInvariant();

            switch (extension)
            {
                case ".onnx":
                    return new OnnxModelReader(_modelRepository, _loggerFactory.CreateLogger<OnnxModelReader>());
                case ".safetensors":
                    throw new System.NotImplementedException("SafetensorsModelReader not yet refactored to IModelFormatReader<SafetensorsMetadata>");
                default:
                    throw new System.NotSupportedException($"File type not supported: {extension}");
            }
        }
    }
}
