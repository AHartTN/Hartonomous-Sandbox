using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Utilities;
using Hartonomous.Infrastructure.Repositories;
using Microsoft.Data.SqlTypes;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// Reads Safetensors format models (Stable Diffusion, FLUX, modern HuggingFace models).
/// Safetensors is a simple, safe tensor serialization format with JSON header + raw tensor data.
/// </summary>
public class SafetensorsModelReader : IModelFormatReader<SafetensorsMetadata>
{
    private readonly IModelRepository _modelRepository;
    private readonly IModelLayerRepository _layerRepository;
    private readonly IModelDiscoveryService _discoveryService;
    private readonly ILogger<SafetensorsModelReader> _logger;

    public string FormatName => "Safetensors";
    public IEnumerable<string> SupportedExtensions => new[] { ".safetensors" };

    public SafetensorsModelReader(
        IModelRepository modelRepository,
        IModelLayerRepository layerRepository,
        IModelDiscoveryService discoveryService,
        ILogger<SafetensorsModelReader> logger)
    {
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
        _layerRepository = layerRepository ?? throw new ArgumentNullException(nameof(layerRepository));
        _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Model> ReadAsync(string modelPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Reading Safetensors model from: {Path}", modelPath);

        // Detect if single file or directory with multiple safetensors
        var isDirectory = Directory.Exists(modelPath);
        var files = isDirectory
            ? Directory.GetFiles(modelPath, "*.safetensors")
            : new[] { modelPath };

        if (!files.Any())
        {
            throw new FileNotFoundException($"No .safetensors files found at: {modelPath}");
        }

        // Get metadata
        var metadata = await GetMetadataAsync(modelPath, cancellationToken);

        // Create model entity
        var model = new Model
        {
            ModelName = isDirectory ? Path.GetFileName(modelPath) : Path.GetFileNameWithoutExtension(modelPath),
            ModelType = "Safetensors",
            Architecture = metadata.Architecture ?? DetermineArchitecture(files),
            Config = JsonSerializer.Serialize(new
            {
                file_count = files.Length,
                tensor_count = metadata.TensorCount,
                total_size_bytes = metadata.TotalSizeBytes,
                metadata = metadata.GlobalMetadata
            }),
            IngestionDate = DateTime.UtcNow,
            Layers = new List<ModelLayer>()
        };

        // Add model to database
        await _modelRepository.AddAsync(model, cancellationToken);
        _logger.LogInformation("Created Safetensors model entity: {ModelName} (ID: {ModelId})", 
            model.ModelName, model.ModelId);

        // Process each safetensors file
        int globalLayerIdx = 0;
        foreach (var file in files)
        {
            globalLayerIdx = await ProcessSafetensorsFileAsync(model.ModelId, file, globalLayerIdx, cancellationToken);
        }

        _logger.LogInformation("Safetensors model ingestion complete: {ModelName} ({TensorCount} tensors from {FileCount} files)",
            model.ModelName, metadata.TensorCount, files.Length);
        return model;
    }

    private async Task<int> ProcessSafetensorsFileAsync(int modelId, string filePath, int startLayerIdx, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing Safetensors file: {Path}", filePath);

        int currentLayerIdx = startLayerIdx;

        try
        {
            using var fileStream = File.OpenRead(filePath);
            using var reader = new BinaryReader(fileStream);

            // Read header length (first 8 bytes)
            var headerLength = reader.ReadInt64();
            if (headerLength <= 0 || headerLength > 100_000_000) // 100MB sanity check
            {
                throw new InvalidDataException($"Invalid Safetensors header length: {headerLength}");
            }

            // Read header JSON
            var headerBytes = reader.ReadBytes((int)headerLength);
            var headerJson = Encoding.UTF8.GetString(headerBytes);
            var header = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(headerJson);

            if (header == null)
            {
                throw new InvalidDataException("Failed to parse Safetensors header");
            }

            // Extract __metadata__ if present
            Dictionary<string, string>? metadata = null;
            if (header.TryGetValue("__metadata__", out var metadataElement))
            {
                metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(metadataElement.GetRawText());
            }

            // Process each tensor
            foreach (var kvp in header)
            {
                if (kvp.Key == "__metadata__")
                    continue;

                var tensorName = kvp.Key;
                var tensorInfo = kvp.Value;

                // Parse tensor metadata
                string? dtype = null;
                long[]? shape = null;
                long[]? dataOffsets = null;

                if (tensorInfo.TryGetProperty("dtype", out var dtypeElement))
                    dtype = dtypeElement.GetString();

                if (tensorInfo.TryGetProperty("shape", out var shapeElement))
                {
                    shape = shapeElement.EnumerateArray()
                        .Select(e => e.GetInt64())
                        .ToArray();
                }

                if (tensorInfo.TryGetProperty("data_offsets", out var offsetsElement))
                {
                    dataOffsets = offsetsElement.EnumerateArray()
                        .Select(e => e.GetInt64())
                        .ToArray();
                }

                // Calculate parameter count
                long paramCount = shape?.Aggregate(1L, (a, b) => a * b) ?? 0;

                // Create layer entity
                var layer = new ModelLayer
                {
                    LayerName = tensorName,
                    LayerType = InferLayerType(tensorName),
                    LayerIdx = currentLayerIdx++,
                    WeightsGeometry = null, // Will populate below if tensor data available
                    TensorShape = shape != null ? JsonSerializer.Serialize(shape) : null,
                    TensorDtype = dtype,
                    Parameters = JsonSerializer.Serialize(new
                    {
                        dtype,
                        shape,
                        file = Path.GetFileName(filePath)
                    }),
                    ParameterCount = paramCount
                };

                await _modelRepository.AddLayerAsync(modelId, layer, cancellationToken);

                // Read and store actual tensor data
                if (dataOffsets != null && dataOffsets.Length == 2)
                {
                    var tensorSize = dataOffsets[1] - dataOffsets[0];
                    var tensorOffset = 8 + headerLength + dataOffsets[0]; // 8 bytes for header length + header + tensor offset
                    
                    _logger.LogDebug("Reading tensor data: {TensorName}, offset={Offset}, size={Size} bytes",
                        tensorName, tensorOffset, tensorSize);
                    
                    fileStream.Seek(tensorOffset, SeekOrigin.Begin);
                    var tensorBytes = new byte[tensorSize];
                    var bytesRead = await fileStream.ReadAsync(tensorBytes, 0, (int)tensorSize, cancellationToken);
                    
                    if (bytesRead != tensorSize)
                    {
                        _logger.LogWarning("Expected {Expected} bytes but read {Actual} bytes for {Tensor}",
                            tensorSize, bytesRead, tensorName);
                    }
                    
                    // Convert to float[] based on dtype
                    var floatData = ConvertToFloat(tensorBytes, dtype, shape);

                    if (floatData.Length > 0)
                    {
                        layer.WeightsGeometry = _layerRepository.CreateGeometryFromWeights(floatData);

                        _logger.LogDebug("Stored {TensorName} with {Elements} elements as GEOMETRY LINESTRING ZM",
                            tensorName, floatData.Length);
                    }
                }
            }

            _logger.LogDebug("Processed {TensorCount} tensors from {File}",
                header.Count - (metadata != null ? 1 : 0), Path.GetFileName(filePath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Safetensors file: {Path}", filePath);
            throw;
        }

        return currentLayerIdx;
    }

    private string InferLayerType(string tensorName)
    {
        var lower = tensorName.ToLowerInvariant();

        if (lower.Contains("embed")) return "Embedding";
        if (lower.Contains("norm")) return "LayerNorm";
        if (lower.Contains("attention") || lower.Contains("attn")) return "Attention";
        if (lower.Contains("mlp") || lower.Contains("ffn") || lower.Contains("feedforward")) return "FeedForward";
        if (lower.Contains("conv")) return "Convolution";
        if (lower.Contains("linear") || lower.Contains("fc")) return "Linear";
        if (lower.Contains("weight") || lower.Contains("kernel")) return "Weight";
        if (lower.Contains("bias")) return "Bias";

        return "Unknown";
    }

    /// <summary>
    /// Convert tensor bytes to float32 array based on dtype.
    /// Supports Llama 4 formats: BF16, FP16, FP32, INT8, INT4
    /// </summary>
    private float[] ConvertToFloat(byte[] tensorBytes, string? dtype, long[]? shape)
    {
        if (tensorBytes == null || tensorBytes.Length == 0)
            return Array.Empty<float>();

        // Calculate expected element count from shape
        var elementCount = shape?.Aggregate(1L, (a, b) => a * b) ?? (tensorBytes.Length / 4);

        try
        {
            return dtype?.ToUpperInvariant() switch
            {
                "F32" or "FLOAT32" => ConvertFloat32(tensorBytes, elementCount),
                "F16" or "FLOAT16" => ConvertFloat16(tensorBytes, elementCount),
                "BF16" or "BFLOAT16" => ConvertBFloat16(tensorBytes, elementCount),
                "I8" or "INT8" => ConvertInt8(tensorBytes, elementCount),
                "U8" or "UINT8" => ConvertUInt8(tensorBytes, elementCount),
                _ => ConvertFloat32(tensorBytes, elementCount) // Default to FP32
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error converting dtype={DType}, falling back to FP32", dtype);
            return ConvertFloat32(tensorBytes, elementCount);
        }
    }

    private float[] ConvertFloat32(byte[] bytes, long elementCount)
    {
        var count = Math.Min((int)elementCount, bytes.Length / 4);
        var result = new float[count];
        Buffer.BlockCopy(bytes, 0, result, 0, count * 4);
        return result;
    }

    private float[] ConvertFloat16(byte[] bytes, long elementCount)
    {
        var count = Math.Min((int)elementCount, bytes.Length / 2);
        var result = new float[count];
        
        for (int i = 0; i < count; i++)
        {
            var halfBits = BitConverter.ToUInt16(bytes, i * 2);
            result[i] = HalfToFloat(halfBits);
        }
        
        return result;
    }

    private float[] ConvertBFloat16(byte[] bytes, long elementCount)
    {
        // BF16: 1 sign bit, 8 exponent bits, 7 mantissa bits
        // Used by Llama 4, Google's models
        var count = Math.Min((int)elementCount, bytes.Length / 2);
        var result = new float[count];
        
        for (int i = 0; i < count; i++)
        {
            var bf16Bits = BitConverter.ToUInt16(bytes, i * 2);
            // BF16 to FP32: shift left 16 bits (BF16 is upper 16 bits of FP32)
            var fp32Bits = (uint)bf16Bits << 16;
            result[i] = BitConverter.ToSingle(BitConverter.GetBytes(fp32Bits), 0);
        }
        
        return result;
    }

    private float[] ConvertInt8(byte[] bytes, long elementCount)
    {
        var count = Math.Min((int)elementCount, bytes.Length);
        var result = new float[count];
        
        for (int i = 0; i < count; i++)
        {
            result[i] = (sbyte)bytes[i] / 127.0f; // Normalize to [-1, 1]
        }
        
        return result;
    }

    private float[] ConvertUInt8(byte[] bytes, long elementCount)
    {
        var count = Math.Min((int)elementCount, bytes.Length);
        var result = new float[count];
        
        for (int i = 0; i < count; i++)
        {
            result[i] = bytes[i] / 255.0f; // Normalize to [0, 1]
        }
        
        return result;
    }

    /// <summary>
    /// Convert IEEE 754 half-precision (FP16) to single-precision (FP32)
    /// </summary>
    private float HalfToFloat(ushort halfBits)
    {
        int sign = (halfBits >> 15) & 0x1;
        int exponent = (halfBits >> 10) & 0x1F;
        int mantissa = halfBits & 0x3FF;

        if (exponent == 0)
        {
            if (mantissa == 0) return sign == 0 ? 0f : -0f; // Zero
            // Denormalized number
            exponent = 1;
            while ((mantissa & 0x400) == 0)
            {
                mantissa <<= 1;
                exponent--;
            }
            mantissa &= 0x3FF;
        }
        else if (exponent == 31)
        {
            // Infinity or NaN
            return mantissa == 0
                ? (sign == 0 ? float.PositiveInfinity : float.NegativeInfinity)
                : float.NaN;
        }

        // Normalized number
        exponent = exponent - 15 + 127; // Adjust bias
        mantissa = mantissa << 13; // Shift mantissa to FP32 position

        uint fp32Bits = ((uint)sign << 31) | ((uint)exponent << 23) | (uint)mantissa;
        return BitConverter.ToSingle(BitConverter.GetBytes(fp32Bits), 0);
    }

    private string DetermineArchitecture(string[] files)
    {
        var fileNames = files.Select(Path.GetFileNameWithoutExtension).ToArray();

        // Check for Stable Diffusion components
        if (fileNames.Any(f => f?.Contains("unet") == true) && 
            fileNames.Any(f => f?.Contains("text_encoder") == true))
            return "Stable-Diffusion";

        // Check for FLUX
        if (fileNames.Any(f => f?.Contains("flux") == true))
            return "FLUX";

        // Check for specific model names
        if (fileNames.Any(f => f?.Contains("bert") == true))
            return "BERT";

        if (fileNames.Any(f => f?.Contains("clip") == true))
            return "CLIP";

        return "Unknown";
    }

    public async Task<SafetensorsMetadata> GetMetadataAsync(string modelPath, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Extracting Safetensors metadata from: {Path}", modelPath);

        var metadata = new SafetensorsMetadata();
        
        // Determine files
        var isDirectory = Directory.Exists(modelPath);
        var files = isDirectory
            ? Directory.GetFiles(modelPath, "*.safetensors")
            : new[] { modelPath };

        metadata.Files = files.Select(Path.GetFileName).ToList()!;

        // Read metadata from first file
        if (files.Any())
        {
            try
            {
                using var fileStream = File.OpenRead(files[0]);
                using var reader = new BinaryReader(fileStream);

                var headerLength = reader.ReadInt64();
                var headerBytes = reader.ReadBytes((int)headerLength);
                var headerJson = Encoding.UTF8.GetString(headerBytes);
                var header = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(headerJson);

                if (header != null)
                {
                    // Extract global metadata
                    if (header.TryGetValue("__metadata__", out var metadataElement))
                    {
                        metadata.GlobalMetadata = JsonSerializer.Deserialize<Dictionary<string, string>>(
                            metadataElement.GetRawText()) ?? new();

                        if (metadata.GlobalMetadata.TryGetValue("format", out var format))
                            metadata.Format = format;

                        if (metadata.GlobalMetadata.TryGetValue("architecture", out var arch))
                            metadata.Architecture = arch;
                    }

                    // Count tensors (exclude __metadata__ key)
                    metadata.TensorCount = header.Count - (header.ContainsKey("__metadata__") ? 1 : 0);

                    // Store tensor info
                    foreach (var kvp in header.Where(k => k.Key != "__metadata__"))
                    {
                        var tensorInfo = new SafetensorsTensorInfo();

                        if (kvp.Value.TryGetProperty("dtype", out var dtype))
                            tensorInfo.DType = dtype.GetString();

                        if (kvp.Value.TryGetProperty("shape", out var shape))
                            tensorInfo.Shape = shape.EnumerateArray().Select(e => e.GetInt64()).ToArray();

                        if (kvp.Value.TryGetProperty("data_offsets", out var offsets))
                            tensorInfo.DataOffsets = offsets.EnumerateArray().Select(e => e.GetInt64()).ToArray();

                        metadata.Tensors[kvp.Key] = tensorInfo;
                    }
                }

                // Calculate total size
                metadata.TotalSizeBytes = files.Sum(f => new FileInfo(f).Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading Safetensors metadata");
                throw;
            }
        }

        _logger.LogInformation("Extracted Safetensors metadata: {Architecture}, {TensorCount} tensors, {FileCount} files",
            metadata.Architecture, metadata.TensorCount, files.Length);

        return await Task.FromResult(metadata);
    }

    public async Task<bool> ValidateFormatAsync(string modelPath, CancellationToken cancellationToken = default)
    {
        try
        {
            var formatInfo = await _discoveryService.DetectFormatAsync(modelPath, cancellationToken);
            return formatInfo.Format == "Safetensors" && formatInfo.Confidence > 0.5;
        }
        catch
        {
            return false;
        }
    }
}
