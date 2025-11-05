using System.Text.Json;
using Hartonomous.Data;
using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ModelIngestion;

/// <summary>
/// Service for ingesting models from Ollama blob storage into Hartonomous database.
/// Reads manifests from D:\Models\manifests\registry.ollama.ai\library and blob metadata.
/// </summary>
public class OllamaModelIngestionService
{
    private readonly HartonomousDbContext _context;
    private readonly ILogger<OllamaModelIngestionService> _logger;
    private readonly string _ollamaModelsPath;
    private readonly string _ollamaBlobsPath;
    private readonly string _ollamaManifestsPath;

    public OllamaModelIngestionService(
        HartonomousDbContext context,
        ILogger<OllamaModelIngestionService> logger,
        string ollamaModelsPath = @"D:\Models")
    {
        _context = context;
        _logger = logger;
        _ollamaModelsPath = ollamaModelsPath;
        _ollamaBlobsPath = Path.Combine(ollamaModelsPath, "blobs");
        _ollamaManifestsPath = Path.Combine(ollamaModelsPath, "manifests", "registry.ollama.ai", "library");
    }

    public async Task<List<Model>> IngestAllModelsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Ollama model ingestion from {Path}", _ollamaManifestsPath);

        if (!Directory.Exists(_ollamaManifestsPath))
        {
            _logger.LogError("Ollama manifests path not found: {Path}", _ollamaManifestsPath);
            return new List<Model>();
        }

        var ingestedModels = new List<Model>();
        var modelDirs = Directory.GetDirectories(_ollamaManifestsPath);

        foreach (var modelDir in modelDirs)
        {
            var modelName = Path.GetFileName(modelDir);
            try
            {
                var model = await IngestModelAsync(modelName, cancellationToken);
                if (model != null)
                {
                    ingestedModels.Add(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ingest Ollama model: {ModelName}", modelName);
            }
        }

        _logger.LogInformation("Completed Ollama model ingestion. Ingested {Count} models", ingestedModels.Count);
        return ingestedModels;
    }

    public async Task<Model?> IngestModelAsync(string modelName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Ingesting Ollama model: {ModelName}", modelName);

        var modelPath = Path.Combine(_ollamaManifestsPath, modelName);
        if (!Directory.Exists(modelPath))
        {
            _logger.LogWarning("Model directory not found: {Path}", modelPath);
            return null;
        }

        // Find the latest or specific version manifest
        var manifestFiles = Directory.GetFiles(modelPath, "*", SearchOption.AllDirectories);
        if (manifestFiles.Length == 0)
        {
            _logger.LogWarning("No manifest files found for model: {ModelName}", modelName);
            return null;
        }

        var manifestFile = manifestFiles.FirstOrDefault(f => f.EndsWith("latest")) ?? manifestFiles[0];
        var manifestJson = await File.ReadAllTextAsync(manifestFile, cancellationToken);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var manifest = JsonSerializer.Deserialize<OllamaManifest>(manifestJson, options);

        if (manifest == null)
        {
            _logger.LogWarning("Failed to parse manifest for model: {ModelName}", modelName);
            return null;
        }

        // Read config blob
        var configDigest = manifest.Config.Digest.Replace("sha256:", "sha256-");
        var configPath = Path.Combine(_ollamaBlobsPath, configDigest);
        
        if (!File.Exists(configPath))
        {
            _logger.LogWarning("Config blob not found at path: {Path} (digest: {Digest})", configPath, configDigest);
            return null;
        }

        var configJson = await File.ReadAllTextAsync(configPath, cancellationToken);
        var config = JsonSerializer.Deserialize<OllamaModelConfig>(configJson, options);

        if (config == null)
        {
            _logger.LogWarning("Failed to parse config for model: {ModelName}", modelName);
            return null;
        }

        // Check if model already exists
        var existingModel = await _context.Models
            .Include(m => m.Metadata)
            .FirstOrDefaultAsync(m => m.ModelName == modelName, cancellationToken);

        if (existingModel != null)
        {
            _logger.LogInformation("Model already exists, updating: {ModelName}", modelName);
            return await UpdateModelAsync(existingModel, manifest, config, cancellationToken);
        }

        // Create new model
        var model = new Model
        {
            ModelName = modelName,
            ModelType = DetermineModelType(config.ModelFamily),
            Architecture = config.Architecture ?? "unknown",
            Config = JsonSerializer.Serialize(config),
            ParameterCount = ParseParameterCount(config.ModelType),
            IngestionDate = DateTime.UtcNow,
            Metadata = new ModelMetadata
            {
                SupportedTasks = DetermineSupportedTasks(config.ModelFamily),
                SupportedModalities = "[\"text\"]",
                MaxInputLength = DetermineContextLength(modelName),
                MaxOutputLength = 4096,
                EmbeddingDimension = null,
                PerformanceMetrics = JsonSerializer.Serialize(new { quantization = config.FileType })
            }
        };

        _context.Models.Add(model);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully ingested model: {ModelName} ({Parameters}B parameters)",
            modelName, model.ParameterCount / 1_000_000_000.0);

        return model;
    }

    private async Task<Model> UpdateModelAsync(
        Model existingModel,
        OllamaManifest manifest,
        OllamaModelConfig config,
        CancellationToken cancellationToken)
    {
        existingModel.Config = JsonSerializer.Serialize(config);
        
        if (existingModel.Metadata != null)
        {
            existingModel.Metadata.PerformanceMetrics = JsonSerializer.Serialize(new { quantization = config.FileType });
        }

        await _context.SaveChangesAsync(cancellationToken);
        return existingModel;
    }

    private static string DetermineModelType(string? modelFamily)
    {
        return modelFamily?.ToLowerInvariant() switch
        {
            "llama" or "llama4" or "llama3" or "llama2" => "transformer",
            "qwen" or "qwen3" or "qwen2" => "transformer",
            "mistral" => "transformer",
            "phi" or "phi3" => "transformer",
            "codellama" => "transformer",
            "stable-diffusion" => "diffusion",
            "clip" => "multimodal",
            _ => "transformer"
        };
    }

    private static string DetermineSupportedTasks(string? modelFamily)
    {
        return modelFamily?.ToLowerInvariant() switch
        {
            "llama" or "llama4" or "llama3" or "llama2" => "[\"text-generation\", \"text-embedding\"]",
            "qwen" or "qwen3" or "qwen2" when modelFamily.Contains("coder") => "[\"code-generation\", \"text-generation\"]",
            "qwen" or "qwen3" or "qwen2" => "[\"text-generation\"]",
            "codellama" => "[\"code-generation\", \"text-generation\"]",
            "stable-diffusion" => "[\"image-generation\"]",
            "clip" => "[\"image-embedding\", \"text-embedding\"]",
            _ => "[\"text-generation\"]"
        };
    }

    private static int DetermineContextLength(string modelName)
    {
        // Default context lengths for known models
        if (modelName.Contains("llama4")) return 128000;
        if (modelName.Contains("llama3")) return 8192;
        if (modelName.Contains("qwen3")) return 32768;
        return 4096;
    }

    private static long ParseParameterCount(string? modelType)
    {
        if (string.IsNullOrEmpty(modelType))
            return 0;

        // Parse formats like "108.6B", "30B", "7B"
        var cleaned = modelType.Replace("B", "").Replace("b", "").Trim();
        if (double.TryParse(cleaned, out var billions))
        {
            return (long)(billions * 1_000_000_000);
        }

        return 0;
    }
}

// DTOs for Ollama manifest format
public class OllamaManifest
{
    public int SchemaVersion { get; set; }
    public string MediaType { get; set; } = "";
    public OllamaManifestConfig Config { get; set; } = new();
    public List<OllamaManifestLayer> Layers { get; set; } = new();
}

public class OllamaManifestConfig
{
    public string MediaType { get; set; } = "";
    public string Digest { get; set; } = "";
    public long Size { get; set; }
}

public class OllamaManifestLayer
{
    public string MediaType { get; set; } = "";
    public string Digest { get; set; } = "";
    public long Size { get; set; }
    public string? From { get; set; }
}

public class OllamaModelConfig
{
    [System.Text.Json.Serialization.JsonPropertyName("model_format")]
    public string? ModelFormat { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("model_family")]
    public string? ModelFamily { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("model_families")]
    public List<string>? ModelFamilies { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("model_type")]
    public string? ModelType { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("file_type")]
    public string? FileType { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("architecture")]
    public string? Architecture { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("os")]
    public string? Os { get; set; }
}
