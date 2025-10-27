using System.IO;

namespace ModelIngestion
{
    public class ModelReaderFactory
    {
        public IModelReader GetReader(string modelPath)
        {
            var extension = Path.GetExtension(modelPath);

            switch (extension)
            {
                case ".onnx":
                    return new OnnxModelReader();
                case ".safetensors":
                    return new SafetensorsModelReader();
                // Add cases for other file types here
                default:
                    throw new NotSupportedException($"File type not supported: {extension}");
            }
        }
    }
}
