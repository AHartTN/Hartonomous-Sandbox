using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// Downloads models from Hugging Face, Ollama, and other sources.
/// </summary>
public class ModelDownloader
{
    /// <summary>
    /// Logger used to record download progress and failures.
    /// </summary>
    private readonly ILogger<ModelDownloader> _logger;

    /// <summary>
    /// HTTP client configured for interacting with model registries.
    /// </summary>
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Root directory where downloaded models are cached.
    /// </summary>
    private readonly string _cacheDirectory;

    /// <summary>
    /// Initializes a new <see cref="ModelDownloader"/> with a named HTTP client and cache directory.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="httpClientFactory">Factory that provides the configured HTTP client.</param>
    public ModelDownloader(ILogger<ModelDownloader> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("ModelDownloader");
        _cacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".hartonomous", "models");
        Directory.CreateDirectory(_cacheDirectory);
    }

    /// <summary>
    /// Download model from Hugging Face.
    /// </summary>
    /// <param name="modelId">Format: "organization/model-name" (e.g., "TinyLlama/TinyLlama-1.1B-Chat-v1.0")</param>
    /// <param name="cancellationToken">Token used to cancel ongoing requests.</param>
    /// <returns>Path to downloaded model directory.</returns>
    public async Task<string> DownloadFromHuggingFaceAsync(string modelId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Downloading model from Hugging Face: {ModelId}", modelId);

        // Parse model ID
        var parts = modelId.Split('/');
        if (parts.Length != 2)
        {
            throw new ArgumentException($"Invalid Hugging Face model ID format. Expected 'org/model', got '{modelId}'");
        }

        var organization = parts[0];
        var modelName = parts[1];
        var modelDir = Path.Combine(_cacheDirectory, "huggingface", organization, modelName);

        // Check if already cached
        if (Directory.Exists(modelDir))
        {
            var existingFiles = Directory.GetFiles(modelDir, "*.safetensors");
            if (existingFiles.Length > 0)
            {
                _logger.LogInformation("Model already cached at: {Path}", modelDir);
                return modelDir;
            }
        }

        Directory.CreateDirectory(modelDir);

        try
        {
            // Get model info from Hugging Face API
            var apiUrl = $"https://huggingface.co/api/models/{modelId}";
            _logger.LogInformation("Fetching model info from: {Url}", apiUrl);

            var modelInfo = await _httpClient.GetFromJsonAsync<HuggingFaceModelInfo>(apiUrl, cancellationToken);
            if (modelInfo?.Siblings == null || modelInfo.Siblings.Count == 0)
            {
                throw new InvalidOperationException($"No files found for model: {modelId}");
            }

            // Download safetensors files (prioritize) or fallback to other formats
            var filesToDownload = modelInfo.Siblings
                .Where(s => s.Rfilename.EndsWith(".safetensors") ||
                           s.Rfilename.EndsWith(".onnx") ||
                           s.Rfilename.EndsWith(".pt") ||
                           s.Rfilename.EndsWith(".bin"))
                .OrderBy(s => s.Rfilename.EndsWith(".safetensors") ? 0 : 1) // Prefer safetensors
                .Take(1) // Download primary model file only
                .ToList();

            if (filesToDownload.Count == 0)
            {
                throw new InvalidOperationException($"No compatible model files (.safetensors, .onnx, .pt, .bin) found for: {modelId}");
            }

            foreach (var file in filesToDownload)
            {
                var fileUrl = $"https://huggingface.co/{modelId}/resolve/main/{file.Rfilename}";
                var localPath = Path.Combine(modelDir, file.Rfilename);

                _logger.LogInformation("Downloading: {FileName} ({Size:N0} bytes)", file.Rfilename, file.Size);
                await DownloadFileWithProgressAsync(fileUrl, localPath, file.Size, cancellationToken);
            }

            // Download config.json if available
            try
            {
                var configUrl = $"https://huggingface.co/{modelId}/resolve/main/config.json";
                var configPath = Path.Combine(modelDir, "config.json");
                await DownloadFileAsync(configUrl, configPath, cancellationToken);
                _logger.LogInformation("Downloaded config.json");
            }
            catch (HttpRequestException)
            {
                _logger.LogWarning("config.json not available (optional)");
            }

            _logger.LogInformation("✓ Model downloaded to: {Path}", modelDir);
            return modelDir;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download model from Hugging Face");

            // Clean up partial download
            if (Directory.Exists(modelDir))
            {
                try { Directory.Delete(modelDir, true); } catch { }
            }

            throw;
        }
    }

    /// <summary>
    /// Download model from Ollama local instance.
    /// </summary>
    /// <param name="modelName">Ollama model name (e.g., "llama3.2:1b")</param>
    /// <param name="cancellationToken">Token used to cancel ongoing requests.</param>
    /// <returns>Path to exported model file.</returns>
    public async Task<string> DownloadFromOllamaAsync(string modelName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting model from Ollama: {ModelName}", modelName);

        var modelDir = Path.Combine(_cacheDirectory, "ollama", modelName.Replace(":", "_"));
        Directory.CreateDirectory(modelDir);

        var exportPath = Path.Combine(modelDir, "model.gguf");

        try
        {
            // Check if Ollama is running
            var ollamaUrl = "http://localhost:11434/api/tags";
            var response = await _httpClient.GetAsync(ollamaUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException("Ollama is not running. Please start Ollama first.");
            }

            // List available models
            var modelsResponse = await _httpClient.GetFromJsonAsync<OllamaModelsResponse>(ollamaUrl, cancellationToken);
            var modelExists = modelsResponse?.Models?.Any(m => m.Name == modelName) ?? false;

            if (!modelExists)
            {
                _logger.LogInformation("Model not found locally. Pulling from Ollama registry...");

                // Pull model via Ollama API
                var pullUrl = "http://localhost:11434/api/pull";
                var pullRequest = new { name = modelName };
                var pullResponse = await _httpClient.PostAsJsonAsync(pullUrl, pullRequest, cancellationToken);
                pullResponse.EnsureSuccessStatusCode();

                _logger.LogInformation("Model pulled successfully");
            }

            // Get model location from Ollama
            var showUrl = $"http://localhost:11434/api/show";
            var showRequest = new { name = modelName };
            var showResponse = await _httpClient.PostAsJsonAsync(showUrl, showRequest, cancellationToken);
            var showResult = await showResponse.Content.ReadFromJsonAsync<OllamaShowResponse>(cancellationToken);

            if (showResult?.ModelInfo != null)
            {
                // Ollama stores models in ~/.ollama/models
                var ollamaModelsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ollama", "models");
                var blobsDir = Path.Combine(ollamaModelsDir, "blobs");

                // Find the GGUF blob
                if (Directory.Exists(blobsDir))
                {
                    var ggufs = Directory.GetFiles(blobsDir, "sha256-*")
                        .Where(f => new FileInfo(f).Length > 1_000_000) // Model files are large
                        .OrderByDescending(f => new FileInfo(f).Length)
                        .FirstOrDefault();

                    if (ggufs != null)
                    {
                        _logger.LogInformation("Copying model from Ollama cache: {Path}", ggufs);
                        File.Copy(ggufs, exportPath, overwrite: true);
                        _logger.LogInformation("✓ Model exported to: {Path}", exportPath);
                        return exportPath;
                    }
                }
            }

            throw new InvalidOperationException($"Could not locate model files for: {modelName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export model from Ollama");
            throw;
        }
    }

    /// <summary>
    /// Get the local cache directory path.
    /// </summary>
    public string GetCacheDirectory() => _cacheDirectory;

    /// <summary>
    /// Downloads a file while logging progress milestones.
    /// </summary>
    /// <param name="url">Remote file location.</param>
    /// <param name="destinationPath">Local file path to write.</param>
    /// <param name="totalSize">Expected file size when known.</param>
    /// <param name="cancellationToken">Token to cancel the download.</param>
    private async Task DownloadFileWithProgressAsync(string url, string destinationPath, long? totalSize, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var contentLength = response.Content.Headers.ContentLength ?? totalSize ?? 0;

        using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        var buffer = new byte[8192];
        long totalRead = 0;
        int lastPercent = -1;

        while (true)
        {
            var bytesRead = await contentStream.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0) break;

            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            totalRead += bytesRead;

            if (contentLength > 0)
            {
                var percent = (int)((totalRead * 100) / contentLength);
                if (percent != lastPercent && percent % 10 == 0)
                {
                    _logger.LogInformation("Progress: {Percent}% ({Downloaded:N0} / {Total:N0} bytes)",
                        percent, totalRead, contentLength);
                    lastPercent = percent;
                }
            }
        }
    }

    /// <summary>
    /// Downloads a file without emitting progress updates.
    /// </summary>
    /// <param name="url">Remote file location.</param>
    /// <param name="destinationPath">Local file path to write.</param>
    /// <param name="cancellationToken">Token to cancel the download.</param>
    private async Task DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await response.Content.CopyToAsync(fileStream, cancellationToken);
    }

    #region DTOs

    /// <summary>
    /// Represents model metadata returned from the Hugging Face API.
    /// </summary>
    private class HuggingFaceModelInfo
    {
        /// <summary>
        /// Files available for download alongside the model.
        /// </summary>
        [JsonPropertyName("siblings")]
        public List<HuggingFaceFile>? Siblings { get; set; }
    }

    /// <summary>
    /// Describes a single file entry in a Hugging Face model response.
    /// </summary>
    private class HuggingFaceFile
    {
        [JsonPropertyName("rfilename")]
        public string Rfilename { get; set; } = string.Empty;

        [JsonPropertyName("size")]
        public long Size { get; set; }
    }

    /// <summary>
    /// Envelope containing Ollama model listings.
    /// </summary>
    private class OllamaModelsResponse
    {
        [JsonPropertyName("models")]
        public List<OllamaModel>? Models { get; set; }
    }

    /// <summary>
    /// Represents an individual model entry in the Ollama tags response.
    /// </summary>
    private class OllamaModel
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response payload returned by the Ollama show endpoint.
    /// </summary>
    private class OllamaShowResponse
    {
        [JsonPropertyName("modelinfo")]
        public string? ModelInfo { get; set; }
    }

    #endregion
}
