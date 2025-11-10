using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Hartonomous.Infrastructure.Services.ModelFormats.Readers;

/// <summary>
/// Reads GGUF (GPT-Generated Unified Format) quantized models.
/// Specification: https://github.com/ggerganov/ggml/blob/master/docs/gguf.md
///
/// Supports all GGML quantization types:
/// - F32 (0): Full 32-bit floats
/// - F16 (1): 16-bit floats
/// - Q4_0 (2): 4-bit quantization, block size 32
/// - Q4_1 (3): 4-bit quantization with min, block size 32
/// - Q5_0 (6): 5-bit quantization, block size 32
/// - Q5_1 (7): 5-bit quantization with min, block size 32
/// - Q8_0 (8): 8-bit quantization, block size 32
/// - Q2_K (10): 2-bit super-block quantization
/// - Q3_K (11): 3-bit super-block quantization
/// - Q4_K (12): 4-bit super-block quantization
/// - Q5_K (13): 5-bit super-block quantization
/// - Q6_K (14): 6-bit super-block quantization
/// - BF16 (30): Brain Float 16
/// </summary>
public class GGUFModelReader : IModelFormatReader<GGUFMetadata>
{
    private readonly GGUFParser _parser;
    private readonly GGUFDequantizer _dequantizer;
    private readonly GGUFModelBuilder _modelBuilder;
    private readonly GGUFGeometryBuilder _geometryBuilder;
    private readonly ILogger<GGUFModelReader> _logger;

    private const int PreviewPointLimit = 4096;

    public string FormatName => "GGUF";
    public IEnumerable<string> SupportedExtensions => new[] { ".gguf" };

    public GGUFModelReader(
        GGUFParser parser,
        GGUFDequantizer dequantizer,
        GGUFModelBuilder modelBuilder,
        GGUFGeometryBuilder geometryBuilder,
        ILogger<GGUFModelReader> logger)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _dequantizer = dequantizer ?? throw new ArgumentNullException(nameof(dequantizer));
        _modelBuilder = modelBuilder ?? throw new ArgumentNullException(nameof(modelBuilder));
        _geometryBuilder = geometryBuilder ?? throw new ArgumentNullException(nameof(geometryBuilder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Model> ReadAsync(string modelPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Reading GGUF model from: {FilePath}", modelPath);

        using var fileStream = new FileStream(modelPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new BinaryReader(fileStream, Encoding.UTF8, leaveOpen: false);

        // Parse GGUF file structure
        var header = _parser.ReadHeader(reader);
        var metadata = _parser.ReadMetadata(reader, header.MetadataCount);
        var tensorInfos = _parser.ReadTensorInfos(reader, header.TensorCount);

        // Update tensor data lengths
        var dataStartOffset = _parser.CalculateDataStartOffset(fileStream.Position, 32); // Default alignment
        var totalDataSectionLength = (ulong)Math.Max(0, fileStream.Length - (long)dataStartOffset);
        _parser.UpdateTensorDataLengths(tensorInfos, totalDataSectionLength);

        // Create metadata object
        var ggufMetadata = new GGUFMetadata
        {
            FilePath = modelPath,
            FileSize = new FileInfo(modelPath).Length,
            Version = header.Version,
            TensorCount = (int)header.TensorCount,
            MetadataKV = new Dictionary<string, object?>(metadata)
        };

        // Extract key metadata
        if (metadata.TryGetValue("general.architecture", out var arch))
            ggufMetadata.Architecture = arch?.ToString();
        if (metadata.TryGetValue("general.file_type", out var fileType))
            ggufMetadata.FileType = fileType?.ToString();

        // Create model
        var model = await _modelBuilder.CreateModelAsync(ggufMetadata, modelPath, cancellationToken);

        // Create layers and tensor segments
        var layers = await _modelBuilder.CreateLayersAsync(model, tensorInfos, cancellationToken);

        // Update model statistics
        await _modelBuilder.UpdateModelStatisticsAsync(model, layers, cancellationToken);

        // Validate creation
        var isValid = await _modelBuilder.ValidateModelCreationAsync(model, cancellationToken);
        if (!isValid)
        {
            _logger.LogWarning("Model creation validation failed for: {ModelName}", model.ModelName);
        }

        _logger.LogInformation("GGUF model ingestion complete: {ModelName} ({LayerCount} layers, {TotalParams} parameters)",
            model.ModelName, layers.Count, layers.Sum(l => l.ParameterCount));

        return model;
    }

    public async Task<GGUFMetadata> GetMetadataAsync(string modelPath, CancellationToken cancellationToken = default)
    {
        return await _parser.GetMetadataAsync(modelPath, cancellationToken);
    }

    public async Task<bool> ValidateFormatAsync(string modelPath, CancellationToken cancellationToken = default)
    {
        return await _parser.ValidateFormatAsync(modelPath, cancellationToken);
    }
}
