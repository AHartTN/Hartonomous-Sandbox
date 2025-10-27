using Hartonomous.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// Implementation of model format discovery.
/// Detects PyTorch, Safetensors, ONNX, GGUF, and other formats.
/// </summary>
public class ModelDiscoveryService : IModelDiscoveryService
{
    private readonly ILogger<ModelDiscoveryService> _logger;

    public ModelDiscoveryService(ILogger<ModelDiscoveryService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ModelFormatInfo> DetectFormatAsync(string path, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Detecting model format for: {Path}", path);

        // Check if path is directory or file
        var isDirectory = Directory.Exists(path);
        var isFile = File.Exists(path);

        if (!isDirectory && !isFile)
        {
            throw new FileNotFoundException($"Model path not found: {path}");
        }

        // Single file detection
        if (isFile)
        {
            return await DetectSingleFileFormatAsync(path, cancellationToken);
        }

        // Multi-file directory detection
        return await DetectDirectoryFormatAsync(path, cancellationToken);
    }

    private async Task<ModelFormatInfo> DetectSingleFileFormatAsync(string filePath, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var fileName = Path.GetFileName(filePath);

        switch (extension)
        {
            case ".onnx":
                return new ModelFormatInfo
                {
                    Format = "ONNX",
                    IsMultiFile = false,
                    IsSharded = false,
                    Extensions = new[] { ".onnx" },
                    RequiredFiles = new[] { fileName },
                    Confidence = 1.0
                };

            case ".safetensors":
                return await DetectSafetensorsFormatAsync(filePath, cancellationToken);

            case ".gguf":
                return new ModelFormatInfo
                {
                    Format = "GGUF",
                    IsMultiFile = false,
                    IsSharded = false,
                    Extensions = new[] { ".gguf" },
                    RequiredFiles = new[] { fileName },
                    Confidence = 1.0
                };

            case ".bin":
            case ".pth":
            case ".pt":
                return await DetectPyTorchFormatAsync(filePath, cancellationToken);

            default:
                throw new NotSupportedException($"Unknown model format: {extension}");
        }
    }

    private async Task<ModelFormatInfo> DetectDirectoryFormatAsync(string dirPath, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Scanning directory for model files: {Path}", dirPath);

        var files = Directory.GetFiles(dirPath, "*", SearchOption.TopDirectoryOnly);
        var extensions = files.Select(f => Path.GetExtension(f).ToLowerInvariant()).Distinct().ToList();

        // Check for config.json (PyTorch/HuggingFace indicator)
        var configPath = Path.Combine(dirPath, "config.json");
        if (File.Exists(configPath))
        {
            return await DetectHuggingFaceFormatAsync(dirPath, files, cancellationToken);
        }

        // Check for .safetensors files
        if (extensions.Contains(".safetensors"))
        {
            return await DetectSafetensorsDirectoryAsync(dirPath, files, cancellationToken);
        }

        // Check for PyTorch .bin files without config
        if (extensions.Any(e => e == ".bin" || e == ".pth" || e == ".pt"))
        {
            return DetectPyTorchDirectory(dirPath, files);
        }

        throw new NotSupportedException($"Unable to detect model format in directory: {dirPath}");
    }

    private async Task<ModelFormatInfo> DetectHuggingFaceFormatAsync(string dirPath, string[] files, CancellationToken cancellationToken)
    {
        var configPath = Path.Combine(dirPath, "config.json");
        var configJson = await File.ReadAllTextAsync(configPath, cancellationToken);
        var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(configJson);

        // Determine architecture from config
        string? architecture = null;
        if (config != null && config.TryGetValue("model_type", out var modelType))
        {
            architecture = modelType.GetString();
        }
        else if (config != null && config.TryGetValue("architectures", out var architectures) && 
                 architectures.ValueKind == JsonValueKind.Array)
        {
            var firstArch = architectures.EnumerateArray().FirstOrDefault();
            architecture = firstArch.GetString();
        }

        // Check for sharded weights
        var weightFiles = files.Where(f =>
        {
            var name = Path.GetFileName(f).ToLowerInvariant();
            return name.Contains("pytorch_model") || name.Contains("model") && 
                   (f.EndsWith(".bin") || f.EndsWith(".safetensors"));
        }).ToList();

        var isSharded = weightFiles.Any(f => Path.GetFileName(f).Contains("-of-"));
        var shardCount = isSharded ? weightFiles.Count : (int?)null;

        // Determine if using Safetensors or PyTorch format
        var format = weightFiles.Any(f => f.EndsWith(".safetensors")) ? "Safetensors" : "PyTorch";

        return new ModelFormatInfo
        {
            Format = format,
            Architecture = architecture,
            IsMultiFile = true,
            IsSharded = isSharded,
            ShardCount = shardCount,
            Extensions = files.Select(f => Path.GetExtension(f).ToLowerInvariant()).Distinct().ToArray(),
            RequiredFiles = new[] { "config.json" }.Concat(weightFiles.Select(Path.GetFileName)).ToArray()!,
            OptionalFiles = new[] { "tokenizer.json", "tokenizer_config.json", "special_tokens_map.json", "vocab.json" },
            Metadata = config?.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value) ?? new Dictionary<string, object>(),
            Confidence = 1.0
        };
    }

    private async Task<ModelFormatInfo> DetectSafetensorsFormatAsync(string filePath, CancellationToken cancellationToken)
    {
        // TODO: Read safetensors header to extract metadata
        return await Task.FromResult(new ModelFormatInfo
        {
            Format = "Safetensors",
            IsMultiFile = false,
            IsSharded = false,
            Extensions = new[] { ".safetensors" },
            RequiredFiles = new[] { Path.GetFileName(filePath) },
            Confidence = 1.0
        });
    }

    private async Task<ModelFormatInfo> DetectSafetensorsDirectoryAsync(string dirPath, string[] files, CancellationToken cancellationToken)
    {
        var safetensorFiles = files.Where(f => f.EndsWith(".safetensors")).ToArray();
        var isSharded = safetensorFiles.Length > 1;

        return await Task.FromResult(new ModelFormatInfo
        {
            Format = "Safetensors",
            IsMultiFile = isSharded,
            IsSharded = isSharded,
            ShardCount = isSharded ? safetensorFiles.Length : null,
            Extensions = new[] { ".safetensors" },
            RequiredFiles = safetensorFiles.Select(Path.GetFileName).ToArray()!,
            Confidence = 1.0
        });
    }

    private async Task<ModelFormatInfo> DetectPyTorchFormatAsync(string filePath, CancellationToken cancellationToken)
    {
        return await Task.FromResult(new ModelFormatInfo
        {
            Format = "PyTorch",
            IsMultiFile = false,
            IsSharded = false,
            Extensions = new[] { Path.GetExtension(filePath).ToLowerInvariant() },
            RequiredFiles = new[] { Path.GetFileName(filePath) },
            Confidence = 0.8 // Lower confidence without config.json
        });
    }

    private ModelFormatInfo DetectPyTorchDirectory(string dirPath, string[] files)
    {
        var ptFiles = files.Where(f =>
        {
            var ext = Path.GetExtension(f).ToLowerInvariant();
            return ext == ".bin" || ext == ".pth" || ext == ".pt";
        }).ToArray();

        return new ModelFormatInfo
        {
            Format = "PyTorch",
            IsMultiFile = ptFiles.Length > 1,
            IsSharded = ptFiles.Length > 1,
            ShardCount = ptFiles.Length > 1 ? ptFiles.Length : null,
            Extensions = ptFiles.Select(f => Path.GetExtension(f).ToLowerInvariant()).Distinct().ToArray(),
            RequiredFiles = ptFiles.Select(Path.GetFileName).ToArray()!,
            Confidence = 0.7 // Lower confidence without config.json
        };
    }

    public async Task<IEnumerable<string>> GetModelFilesAsync(string modelPath, CancellationToken cancellationToken = default)
    {
        var formatInfo = await DetectFormatAsync(modelPath, cancellationToken);

        if (File.Exists(modelPath))
        {
            // Single file model
            return new[] { modelPath };
        }

        // Directory model - return full paths
        return formatInfo.RequiredFiles.Select(f => Path.Combine(modelPath, f)).ToArray();
    }

    public async Task<bool> IsValidModelAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            var formatInfo = await DetectFormatAsync(path, cancellationToken);
            return formatInfo.Confidence > 0.5;
        }
        catch
        {
            return false;
        }
    }
}
