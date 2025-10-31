using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Services;

public class ModelDownloadService
{
    private readonly ILogger<ModelDownloadService> _logger;

    public ModelDownloadService(ILogger<ModelDownloadService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> DownloadFromHuggingFaceAsync(string modelId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Downloading model from Hugging Face: {ModelId}", modelId);

        var modelDir = Path.Combine(Path.GetTempPath(), "hartonomous", "models", modelId.Replace("/", "_"));
        Directory.CreateDirectory(modelDir);

        _logger.LogWarning("HuggingFace download not fully implemented - returning temp directory: {Dir}", modelDir);

        return await Task.FromResult(modelDir);
    }

    public async Task<string> DownloadFromOllamaAsync(string modelName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Downloading model from Ollama: {ModelName}", modelName);

        var modelPath = Path.Combine(Path.GetTempPath(), "hartonomous", "models", $"{modelName}.gguf");
        Directory.CreateDirectory(Path.GetDirectoryName(modelPath)!);

        _logger.LogWarning("Ollama download not fully implemented - returning temp path: {Path}", modelPath);

        return await Task.FromResult(modelPath);
    }

    public async Task<DownloadResult> DownloadAndIngestHuggingFaceAsync(string modelId, CancellationToken cancellationToken = default)
    {
        var modelPath = await DownloadFromHuggingFaceAsync(modelId, cancellationToken);

        return new DownloadResult
        {
            ModelPath = modelPath,
            Success = Directory.Exists(modelPath)
        };
    }
}

public class DownloadResult
{
    public string ModelPath { get; set; } = string.Empty;
    public bool Success { get; set; }
}
