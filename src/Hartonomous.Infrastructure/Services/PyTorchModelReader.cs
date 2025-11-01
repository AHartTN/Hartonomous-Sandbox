using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Infrastructure.Repositories;
using Microsoft.Data.SqlTypes;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// Reads PyTorch models (Llama 4, GPT, etc.) with multi-file shard support.
/// </summary>
public class PyTorchModelReader : IModelFormatReader<PyTorchMetadata>
{
    private readonly IModelRepository _modelRepository;
    private readonly IModelLayerRepository _layerRepository;
    private readonly IModelDiscoveryService _discoveryService;
    private readonly ILogger<PyTorchModelReader> _logger;

    public string FormatName => "PyTorch";
    public IEnumerable<string> SupportedExtensions => new[] { ".bin", ".pth", ".pt" };

    public PyTorchModelReader(
        IModelRepository modelRepository,
        IModelLayerRepository layerRepository,
        IModelDiscoveryService discoveryService,
        ILogger<PyTorchModelReader> logger)
    {
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
        _layerRepository = layerRepository ?? throw new ArgumentNullException(nameof(layerRepository));
        _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Model> ReadAsync(string modelPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Reading PyTorch model from: {Path}", modelPath);

        // Detect if single file or directory
        var isDirectory = Directory.Exists(modelPath);
        var baseDir = isDirectory ? modelPath : Path.GetDirectoryName(modelPath) ?? modelPath;

        // Get metadata
        var metadata = await GetMetadataAsync(modelPath, cancellationToken);

        // Create model entity
        var model = new Model
        {
            ModelName = Path.GetFileNameWithoutExtension(modelPath),
            ModelType = metadata.ModelType ?? "PyTorch",
            Architecture = metadata.Architecture ?? "Unknown",
            Config = JsonSerializer.Serialize(metadata.RawConfig),
            IngestionDate = DateTime.UtcNow,
            Layers = new List<ModelLayer>()
        };

        // Add model to database
        await _modelRepository.AddAsync(model, cancellationToken);
        _logger.LogInformation("Created model entity: {ModelName} (ID: {ModelId})", model.ModelName, model.ModelId);

        // Process weight files
        if (metadata.ShardFiles.Any())
        {
            _logger.LogInformation("Processing {Count} shard files", metadata.ShardFiles.Count);

            for (int i = 0; i < metadata.ShardFiles.Count; i++)
            {
                var shardPath = Path.Combine(baseDir, metadata.ShardFiles[i]);
                await ProcessShardFileAsync(model.ModelId, shardPath, i, cancellationToken);
            }
        }
        else
        {
            // Single .bin/.pth/.pt file
            var filePath = isDirectory
                ? Directory.GetFiles(baseDir, "*.bin").FirstOrDefault()
                    ?? Directory.GetFiles(baseDir, "*.pth").FirstOrDefault()
                    ?? Directory.GetFiles(baseDir, "*.pt").FirstOrDefault()
                : modelPath;

            if (filePath != null)
            {
                await ProcessShardFileAsync(model.ModelId, filePath, 0, cancellationToken);
            }
        }

        _logger.LogInformation("PyTorch model ingestion complete: {ModelName}", model.ModelName);
        return model;
    }

    private async Task ProcessShardFileAsync(int modelId, string shardPath, int shardIndex, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing shard file: {ShardPath} (index {Index})", shardPath, shardIndex);

        try
        {
            // NOTE: This is a placeholder for actual PyTorch binary parsing.
            // In production, you would use:
            // 1. TorchSharp library for .NET PyTorch interop
            // 2. Or custom binary reader for PyTorch pickle format
            // 3. Extract tensor names, shapes, and data

            // For now, create a placeholder layer for the shard
            var layer = new ModelLayer
            {
                LayerName = $"shard_{shardIndex:D5}",
                LayerType = "Weight",
                LayerIdx = shardIndex,
                WeightsGeometry = null, // Would populate from actual tensor data
                Parameters = "{}",
                ParameterCount = 0
            };

            await _modelRepository.AddLayerAsync(modelId, layer, cancellationToken);
            _logger.LogDebug("Created layer for shard {Index}: {LayerName}", shardIndex, layer.LayerName);

            // TODO: Actual PyTorch tensor extraction would happen here:
            // using var torch = Torch.Load(shardPath);
            // foreach (var (name, tensor) in torch.StateDict())
            // {
            //     var weights = tensor.data<float>().ToArray();
            //     var weightsGeometry = GeometryConverter.ToLineStringZM(weights, 1.0f, layerIndex);
            //     await _modelRepository.AddLayerAsync(modelId, new ModelLayer
            //     {
            //         LayerName = name,
            //         LayerType = InferLayerType(name),
            //         LayerIdx = layerIndex++,
            //         WeightsGeometry = weightsGeometry,
            //         Parameters = JsonSerializer.Serialize(new { shape = tensor.shape }),
            //         ParameterCount = tensor.numel()
            //     }, cancellationToken);
            // }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing shard file: {ShardPath}", shardPath);
            throw;
        }
    }

    public async Task<PyTorchMetadata> GetMetadataAsync(string modelPath, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Extracting PyTorch metadata from: {Path}", modelPath);

        var metadata = new PyTorchMetadata();

        // Determine base directory
        var isDirectory = Directory.Exists(modelPath);
        var baseDir = isDirectory ? modelPath : Path.GetDirectoryName(modelPath) ?? modelPath;

        // Check for config.json (HuggingFace format)
        var configPath = Path.Combine(baseDir, "config.json");
        if (File.Exists(configPath))
        {
            var configJson = await File.ReadAllTextAsync(configPath, cancellationToken);
            var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(configJson);

            if (config != null)
            {
                metadata.RawConfig = config.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);

                // Extract common fields
                if (config.TryGetValue("model_type", out var modelType))
                    metadata.ModelType = modelType.GetString();

                if (config.TryGetValue("architectures", out var arch) && arch.ValueKind == JsonValueKind.Array)
                    metadata.Architecture = arch.EnumerateArray().FirstOrDefault().GetString();

                if (config.TryGetValue("num_hidden_layers", out var numLayers))
                    metadata.NumLayers = numLayers.GetInt32();

                if (config.TryGetValue("hidden_size", out var hiddenSize))
                    metadata.HiddenSize = hiddenSize.GetInt32();

                if (config.TryGetValue("intermediate_size", out var intermediateSize))
                    metadata.IntermediateSize = intermediateSize.GetInt32();

                if (config.TryGetValue("num_attention_heads", out var numHeads))
                    metadata.NumAttentionHeads = numHeads.GetInt32();

                if (config.TryGetValue("num_key_value_heads", out var numKVHeads))
                    metadata.NumKeyValueHeads = numKVHeads.GetInt32();

                if (config.TryGetValue("vocab_size", out var vocabSize))
                    metadata.VocabSize = vocabSize.GetInt32();

                if (config.TryGetValue("max_position_embeddings", out var maxPos))
                    metadata.MaxPositionEmbeddings = maxPos.GetInt32();

                if (config.TryGetValue("hidden_act", out var activation))
                    metadata.ActivationFunction = activation.GetString();

                if (config.TryGetValue("rms_norm_eps", out var rmsNorm))
                    metadata.RmsNormEps = (float)rmsNorm.GetDouble();
            }
        }

        // Find weight files (shards)
        var weightFiles = Directory.GetFiles(baseDir, "pytorch_model-*-of-*.bin")
            .Concat(Directory.GetFiles(baseDir, "model-*-of-*.safetensors"))
            .Concat(Directory.GetFiles(baseDir, "pytorch_model.bin"))
            .Select(Path.GetFileName)
            .Where(f => f != null)
            .ToList();

        metadata.ShardFiles = weightFiles!;

        // Check for tokenizer
        metadata.HasTokenizer = File.Exists(Path.Combine(baseDir, "tokenizer.json")) ||
                                File.Exists(Path.Combine(baseDir, "tokenizer_config.json"));

        _logger.LogInformation("Extracted metadata: {Architecture}, {Layers} layers, {Shards} shards",
            metadata.Architecture, metadata.NumLayers, metadata.ShardFiles.Count);

        return metadata;
    }

    public async Task<bool> ValidateFormatAsync(string modelPath, CancellationToken cancellationToken = default)
    {
        try
        {
            var formatInfo = await _discoveryService.DetectFormatAsync(modelPath, cancellationToken);
            return formatInfo.Format == "PyTorch" && formatInfo.Confidence > 0.5;
        }
        catch
        {
            return false;
        }
    }
}
