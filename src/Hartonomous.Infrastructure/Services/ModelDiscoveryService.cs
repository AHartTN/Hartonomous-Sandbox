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
    /// <summary>
    /// Logger used to capture warnings, diagnostics, and discovery decisions.
    /// </summary>
    private readonly ILogger<ModelDiscoveryService> _logger;

    /// <summary>
    /// Initializes a new instance of the model discovery service.
    /// </summary>
    /// <param name="logger">Application logger.</param>
    public ModelDiscoveryService(ILogger<ModelDiscoveryService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Detects the format details for a model file or directory by inspecting metadata and contents.
    /// </summary>
    /// <param name="path">Absolute path pointing to a single model file or a model directory.</param>
    /// <param name="cancellationToken">Token used to cancel the detection workflow.</param>
    /// <returns>Populated <see cref="ModelFormatInfo"/> describing the discovered model.</returns>
    public async Task<ModelFormatInfo> DetectFormatAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

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

    /// <summary>
    /// Evaluates an individual model artifact file and determines the format it represents.
    /// </summary>
    /// <param name="filePath">Full path to the model file on disk.</param>
    /// <param name="cancellationToken">Token that signals the operation should terminate early.</param>
    /// <returns>Format information describing the supplied model artifact.</returns>
    private async Task<ModelFormatInfo> DetectSingleFileFormatAsync(string filePath, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var fileName = Path.GetFileName(filePath);

        // Try extension-based detection first (fast path)
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
        }

        // Extension-based detection failed, try magic number detection (content-based)
        _logger.LogDebug("Extension {Extension} not recognized, attempting magic number detection", extension);
        return await DetectByMagicNumberAsync(filePath, fileName, cancellationToken);
    }

    /// <summary>
    /// Reads the first bytes of a file to detect formats that are not identifiable via the file extension.
    /// </summary>
    /// <param name="filePath">Full path to the file being inspected.</param>
    /// <param name="fileName">File name to use in diagnostics and returned metadata.</param>
    /// <param name="cancellationToken">Token to abort the read operation.</param>
    /// <returns>Format information inferred from magic numbers stored in the file header.</returns>
    private async Task<ModelFormatInfo> DetectByMagicNumberAsync(string filePath, string fileName, CancellationToken cancellationToken)
    {
        // Read first 4 bytes to check magic numbers
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        var magicBytes = new byte[4];
        var bytesRead = await fileStream.ReadAsync(magicBytes.AsMemory(0, 4), cancellationToken);

        if (bytesRead < 4)
        {
            throw new NotSupportedException($"Unknown model format: File too small ({bytesRead} bytes)");
        }

        // Check for GGUF magic number: 0x47 0x47 0x55 0x46 ("GGUF" in ASCII)
        // In little-endian uint32: 0x46554747
        if (magicBytes[0] == 0x47 && magicBytes[1] == 0x47 && magicBytes[2] == 0x55 && magicBytes[3] == 0x46)
        {
            _logger.LogInformation("Detected GGUF format via magic number for file: {FileName}", fileName);
            return new ModelFormatInfo
            {
                Format = "GGUF",
                IsMultiFile = false,
                IsSharded = false,
                Extensions = new[] { ".gguf" },
                RequiredFiles = new[] { fileName },
                Confidence = 1.0
            };
        }

        // Check for Safetensors magic (8-byte header length as little-endian uint64, typically < 100MB)
        if (bytesRead >= 8)
        {
            fileStream.Seek(0, SeekOrigin.Begin);
            var headerLengthBytes = new byte[8];
            var headerBytesRead = await fileStream.ReadAsync(headerLengthBytes.AsMemory(0, 8), cancellationToken);

            if (headerBytesRead == 8)
            {
                var headerLength = BitConverter.ToUInt64(headerLengthBytes, 0);

                // Safetensors header is typically < 100MB and contains JSON
                if (headerLength > 0 && headerLength < 100_000_000)
                {
                    _logger.LogInformation("Possible Safetensors format detected for file: {FileName}", fileName);
                    return await DetectSafetensorsFormatAsync(filePath, cancellationToken);
                }
            }
        }

        throw new NotSupportedException($"Unknown model format: No recognized magic number found in {fileName}");
    }

    /// <summary>
    /// Inspects a model directory and infers the overall format by evaluating contained files.
    /// </summary>
    /// <param name="dirPath">Directory that houses a multi-file model.</param>
    /// <param name="cancellationToken">Token to cancel filesystem enumeration.</param>
    /// <returns>Format information that captures the structure of the model directory.</returns>
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

    /// <summary>
    /// Processes a HuggingFace style directory by reading configuration metadata and weight files.
    /// </summary>
    /// <param name="dirPath">Root directory containing config and weight artifacts.</param>
    /// <param name="files">All files discovered under the directory.</param>
    /// <param name="cancellationToken">Token used to cancel configuration parsing.</param>
    /// <returns>Detailed format information including architecture and sharding details.</returns>
    private async Task<ModelFormatInfo> DetectHuggingFaceFormatAsync(string dirPath, string[] files, CancellationToken cancellationToken)
    {
        var configPath = Path.Combine(dirPath, "config.json");
        var configJson = await File.ReadAllTextAsync(configPath, cancellationToken);
        using var document = JsonDocument.Parse(configJson);
        var root = document.RootElement;

        // Determine architecture from config
        string? architecture = null;
        if (root.TryGetProperty("model_type", out var modelType))
        {
            architecture = modelType.GetString();
        }
        else if (root.TryGetProperty("architectures", out var architectures) && architectures.ValueKind == JsonValueKind.Array)
        {
            var firstArch = architectures.EnumerateArray().FirstOrDefault();
            architecture = firstArch.ValueKind == JsonValueKind.String ? firstArch.GetString() : null;
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

        var metadata = new Dictionary<string, object>();
        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in root.EnumerateObject())
            {
                metadata[property.Name] = property.Value.Clone();
            }
        }

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
            Metadata = metadata,
            Confidence = 1.0
        };
    }

    /// <summary>
    /// Determines baseline Safetensors metadata for a single file to support discovery workflows.
    /// </summary>
    /// <param name="filePath">Path to the safetensors file.</param>
    /// <param name="cancellationToken">Token used to cancel asynchronous file reads.</param>
    /// <returns>Format information tailored for safetensors artifacts.</returns>
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

    /// <summary>
    /// Aggregates multiple safetensors shards within a directory to describe the combined model.
    /// </summary>
    /// <param name="dirPath">Directory that contains safetensors shards.</param>
    /// <param name="files">Listing of files discovered within the directory.</param>
    /// <param name="cancellationToken">Token used to cancel metadata collection.</param>
    /// <returns>Format information reflecting the safetensors directory contents.</returns>
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

    /// <summary>
    /// Provides PyTorch format metadata for a single serialized model file.
    /// </summary>
    /// <param name="filePath">Path to the PyTorch artifact being analyzed.</param>
    /// <param name="cancellationToken">Token controlling asynchronous execution.</param>
    /// <returns>Format information describing the PyTorch artifact.</returns>
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

    /// <summary>
    /// Interprets a directory of PyTorch artifacts to determine sharding and required files.
    /// </summary>
    /// <param name="dirPath">Directory that contains PyTorch files.</param>
    /// <param name="files">File list derived from the directory scan.</param>
    /// <returns>Format information covering the PyTorch directory layout.</returns>
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

    /// <summary>
    /// Enumerates the filesystem artifacts that must be present to load the specified model.
    /// </summary>
    /// <param name="modelPath">Path to a model file or the directory containing model assets.</param>
    /// <param name="cancellationToken">Token to cancel interrogation.</param>
    /// <returns>Collection of file paths required to hydrate the model.</returns>
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

    /// <summary>
    /// Validates that the provided path represents a recognizable model format with sufficient metadata.
    /// </summary>
    /// <param name="path">File or directory path to evaluate.</param>
    /// <param name="cancellationToken">Token used to cancel the validation operation.</param>
    /// <returns><see langword="true"/> when the path yields a format with acceptable confidence; otherwise <see langword="false"/>.</returns>
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
