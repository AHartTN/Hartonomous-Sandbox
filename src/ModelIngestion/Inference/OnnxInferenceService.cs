using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace ModelIngestion.Inference;

/// <summary>
/// Runs ONNX model inference using weights stored in YOUR TensorAtoms table.
/// No external API calls - all inference runs on YOUR ingested model weights.
/// </summary>
public sealed class OnnxInferenceService
{
    private readonly string _connectionString;

    public OnnxInferenceService(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    /// <summary>
    /// Runs object detection inference using YOUR ingested YOLO/etc model weights.
    /// Outputs bounding boxes as GEOMETRY atoms.
    /// </summary>
    public async Task<List<DetectionResult>> DetectObjectsAsync(
        byte[] imageBytes,
        string modelIdentifier,
        float confidenceThreshold = 0.5f,
        CancellationToken cancellationToken = default)
    {
        // Query TensorAtoms for model weights
        var modelWeights = await LoadModelWeightsAsync(modelIdentifier, cancellationToken);
        
        if (modelWeights == null || modelWeights.Length == 0)
        {
            throw new InvalidOperationException($"Model '{modelIdentifier}' not found in TensorAtoms. Ingest the model first.");
        }

        // Create ONNX session from YOUR weights
        using var session = new InferenceSession(modelWeights);

        // Prepare input tensor from image
        var inputTensor = PrepareImageTensor(imageBytes);
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(session.InputMetadata.Keys.First(), inputTensor)
        };

        // Run inference
        using var results = session.Run(inputs);
        var outputTensor = results.First().AsEnumerable<float>().ToArray();

        // Parse detections (YOLO format: [batch, boxes, classes+confidence+bbox])
        var detections = ParseYoloOutput(outputTensor, confidenceThreshold);

        return detections;
    }

    /// <summary>
    /// Runs embedding inference using YOUR ingested model weights.
    /// Returns vector embeddings for downstream tasks.
    /// </summary>
    public async Task<float[]> GenerateEmbeddingAsync(
        string text,
        string modelIdentifier,
        CancellationToken cancellationToken = default)
    {
        var modelWeights = await LoadModelWeightsAsync(modelIdentifier, cancellationToken);
        
        if (modelWeights == null || modelWeights.Length == 0)
        {
            throw new InvalidOperationException($"Model '{modelIdentifier}' not found in TensorAtoms.");
        }

        using var session = new InferenceSession(modelWeights);

        // Tokenize text (simplified - real implementation would use proper tokenizer)
        var tokens = TokenizeText(text);
        var inputTensor = new DenseTensor<long>(tokens, new[] { 1, tokens.Length });

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(session.InputMetadata.Keys.First(), inputTensor)
        };

        using var results = session.Run(inputs);
        var embedding = results.First().AsEnumerable<float>().ToArray();

        return embedding;
    }

    private async Task<byte[]?> LoadModelWeightsAsync(string modelIdentifier, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Query TensorAtoms for model weights (stored as binary in YourColumn)
        // Adjust query based on actual TensorAtoms schema
        const string query = @"
            SELECT TOP 1 ModelWeights
            FROM TensorAtoms
            WHERE ModelIdentifier = @ModelIdentifier
            ORDER BY IngestionTimestamp DESC";

        await using var command = new SqlCommand(query, connection);
        command.Parameters.AddWithValue("@ModelIdentifier", modelIdentifier);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        
        return result as byte[];
    }

    private DenseTensor<float> PrepareImageTensor(byte[] imageBytes)
    {
        // Simplified: Real implementation would use ImageSharp to decode and resize
        // For now, assume 640x640x3 input (YOLO standard)
        const int width = 640;
        const int height = 640;
        const int channels = 3;

        var tensor = new DenseTensor<float>(new[] { 1, channels, height, width });

        // TODO: Decode image bytes, resize to 640x640, normalize pixels to [0,1]
        // This would use ImageSharp (already in ModelIngestion dependencies)

        return tensor;
    }

    private long[] TokenizeText(string text)
    {
        // Simplified tokenization (real implementation would use BPE tokenizer from YOUR model)
        // For demonstration: convert chars to ASCII values
        return text.Take(512).Select(c => (long)c).ToArray();
    }

    private List<DetectionResult> ParseYoloOutput(float[] output, float confidenceThreshold)
    {
        var detections = new List<DetectionResult>();

        // YOLO output format: [batch, num_boxes, (x, y, w, h, confidence, class_probs...)]
        // Simplified parsing - adjust based on actual model output
        int numBoxes = output.Length / 85; // Assuming 80 classes + 5 bbox params

        for (int i = 0; i < numBoxes; i++)
        {
            int offset = i * 85;
            float confidence = output[offset + 4];

            if (confidence < confidenceThreshold)
                continue;

            float x = output[offset + 0];
            float y = output[offset + 1];
            float w = output[offset + 2];
            float h = output[offset + 3];

            // Find max class probability
            float maxClassProb = 0;
            int maxClassIndex = 0;
            for (int c = 0; c < 80; c++)
            {
                float classProb = output[offset + 5 + c];
                if (classProb > maxClassProb)
                {
                    maxClassProb = classProb;
                    maxClassIndex = c;
                }
            }

            detections.Add(new DetectionResult
            {
                BoundingBox = new BoundingBox { X = x, Y = y, Width = w, Height = h },
                Confidence = confidence * maxClassProb,
                ClassIndex = maxClassIndex,
                ClassName = GetCocoClassName(maxClassIndex)
            });
        }

        return detections;
    }

    private string GetCocoClassName(int index)
    {
        // COCO class names (80 classes)
        var classNames = new[]
        {
            "person", "bicycle", "car", "motorcycle", "airplane", "bus", "train", "truck", "boat",
            "traffic light", "fire hydrant", "stop sign", "parking meter", "bench", "bird", "cat",
            "dog", "horse", "sheep", "cow", "elephant", "bear", "zebra", "giraffe", "backpack",
            "umbrella", "handbag", "tie", "suitcase", "frisbee", "skis", "snowboard", "sports ball",
            "kite", "baseball bat", "baseball glove", "skateboard", "surfboard", "tennis racket",
            "bottle", "wine glass", "cup", "fork", "knife", "spoon", "bowl", "banana", "apple",
            "sandwich", "orange", "broccoli", "carrot", "hot dog", "pizza", "donut", "cake",
            "chair", "couch", "potted plant", "bed", "dining table", "toilet", "tv", "laptop",
            "mouse", "remote", "keyboard", "cell phone", "microwave", "oven", "toaster", "sink",
            "refrigerator", "book", "clock", "vase", "scissors", "teddy bear", "hair drier",
            "toothbrush"
        };

        return index >= 0 && index < classNames.Length ? classNames[index] : "unknown";
    }
}

public sealed class DetectionResult
{
    public required BoundingBox BoundingBox { get; init; }
    public required float Confidence { get; init; }
    public required int ClassIndex { get; init; }
    public required string ClassName { get; init; }
}

public sealed class BoundingBox
{
    public required float X { get; init; }
    public required float Y { get; init; }
    public required float Width { get; init; }
    public required float Height { get; init; }

    /// <summary>
    /// Converts bounding box to SQL Server GEOMETRY (POLYGON).
    /// </summary>
    public string ToGeometryWkt()
    {
        var x1 = X;
        var y1 = Y;
        var x2 = X + Width;
        var y2 = Y + Height;

        return $"POLYGON(({x1} {y1}, {x2} {y1}, {x2} {y2}, {x1} {y2}, {x1} {y1}))";
    }
}
