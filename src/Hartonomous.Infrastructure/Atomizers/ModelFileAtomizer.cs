using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces.Ingestion;

namespace Hartonomous.Infrastructure.Atomizers;

/// <summary>
/// Atomizes machine learning model files (ONNX, TensorFlow, PyTorch, etc.) by extracting architecture, 
/// layer information, weights metadata, and computational graph structure.
/// </summary>
public class ModelFileAtomizer : IAtomizer<byte[]>
{
    private const int MaxAtomSize = 64;
    public int Priority => 50;

    public bool CanHandle(string contentType, string? fileExtension)
    {
        // AI model content types
        if (contentType == "application/x-onnx" || 
            contentType == "application/x-tensorflow" ||
            contentType == "application/x-pytorch" ||
            contentType == "application/octet-stream")
        {
            // Need to check extension for octet-stream
        }

        var modelExtensions = new[] { 
            // GGUF/GGML (llama.cpp, Ollama, LM Studio)
            "gguf", "ggml",
            
            // ONNX (Microsoft, cross-platform)
            "onnx",
            
            // TensorFlow formats
            "pb",            // TensorFlow SavedModel/GraphDef
            "tflite",        // TensorFlow Lite
            "tfjs",          // TensorFlow.js
            "tfrec",         // TensorFlow Records
            "ckpt",          // TensorFlow checkpoint
            
            // PyTorch formats
            "pth", "pt",     // PyTorch model state
            "bin",           // PyTorch binary (also used by Hugging Face)
            "pkl", "pickle", // Pickled PyTorch models
            
            // Hugging Face / Transformers
            "safetensors",   // SafeTensors (Hugging Face standard)
            
            // Keras
            "h5", "hdf5",    // Keras/HDF5 format
            "keras",         // Keras native format
            
            // Core ML (Apple)
            "mlmodel",       // Core ML model
            "mlpackage",     // Core ML package
            
            // NNEF (Neural Network Exchange Format)
            "nnef",
            
            // Caffe
            "caffemodel",    // Caffe model
            "prototxt",      // Caffe model definition
            
            // MXNet
            "params",        // MXNet parameters
            "json",          // MXNet symbol (if in model context)
            
            // PaddlePaddle
            "pdmodel",       // PaddlePaddle model
            "pdparams",      // PaddlePaddle parameters
            "pdopt",         // PaddlePaddle optimizer
            
            // OpenVINO
            "xml",           // OpenVINO IR (if in model context)
            
            // Darknet (YOLO)
            "weights",       // Darknet weights
            "cfg",           // Darknet config
            
            // JAX/Flax
            "msgpack",       // JAX serialized models
            
            // NCNN (Tencent)
            "param",         // NCNN parameter
            
            // TNN (Tencent)
            "tnnproto",      // TNN model proto
            "tnnmodel",      // TNN model
            
            // Model Optimizer / Quantized
            "ot",            // ONNX quantized
            "q8",            // 8-bit quantized
            "q4",            // 4-bit quantized
            "awq",           // Activation-aware Weight Quantization
            "gptq",          // GPTQ quantized models
            
            // LLM-specific formats
            "ggjtv3", "ggjtv2", "ggjtv1",  // Legacy GGML formats
            "llamafile",     // Mozilla llamafile
            "exl2",          // ExLlamaV2 format
            "marlin",        // Marlin quantization format
            
            // Misc
            "mar",           // MXNet model archive
            "bmodel",        // Sophon model
            "tmfile",        // Tengine model
            "tflite",        // TensorFlow Lite
            "dlc"            // Qualcomm DLC
        };
        
        return fileExtension != null && modelExtensions.Contains(fileExtension.ToLowerInvariant());
    }

    public async Task<AtomizationResult> AtomizeAsync(byte[] input, SourceMetadata source, CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var atoms = new List<AtomData>();
        var compositions = new List<AtomComposition>();
        var warnings = new List<string>();

        try
        {
            // Create parent atom for the model file
            var modelHash = SHA256.HashData(input);
            var modelType = DetectModelType(source.ContentType, source.FileName, input);
            var modelMetadataBytes = Encoding.UTF8.GetBytes($"model:{modelType}:{source.FileName}:{input.Length}");
            
            var modelAtom = new AtomData
            {
                AtomicValue = modelMetadataBytes.Length <= MaxAtomSize ? modelMetadataBytes : modelMetadataBytes.Take(MaxAtomSize).ToArray(),
                ContentHash = modelHash,
                Modality = "ml-model",
                Subtype = $"{modelType}-model",
                ContentType = source.ContentType ?? "application/octet-stream",
                CanonicalText = $"{source.FileName ?? "model"} ({input.Length:N0} bytes)",
                Metadata = BuildModelMetadata(input, modelType, source.FileName)
            };
            atoms.Add(modelAtom);

            // Extract model structure based on type
            switch (modelType)
            {
                case "gguf":
                    await ExtractGgufStructureAsync(input, modelHash, atoms, compositions, warnings, cancellationToken);
                    break;
                case "ggml":
                    ExtractGgmlStructure(input, modelHash, atoms, compositions, warnings);
                    break;
                case "onnx":
                    await ExtractOnnxStructureAsync(input, modelHash, atoms, compositions, warnings, cancellationToken);
                    break;
                case "tensorflow":
                    ExtractTensorFlowStructure(input, modelHash, atoms, compositions, warnings);
                    break;
                case "pytorch":
                    ExtractPyTorchStructure(input, modelHash, atoms, compositions, warnings);
                    break;
                case "h5":
                case "keras":
                    ExtractKerasStructure(input, modelHash, atoms, compositions, warnings);
                    break;
                case "safetensors":
                    ExtractSafeTensorsStructure(input, modelHash, atoms, compositions, warnings);
                    break;
                case "coreml":
                    ExtractCoreMLStructure(input, modelHash, atoms, compositions, warnings);
                    break;
                case "tflite":
                    ExtractTFLiteStructure(input, modelHash, atoms, compositions, warnings);
                    break;
                case "caffe":
                    ExtractCaffeStructure(input, modelHash, atoms, compositions, warnings);
                    break;
                case "mxnet":
                    ExtractMXNetStructure(input, modelHash, atoms, compositions, warnings);
                    break;
                case "openvino":
                    ExtractOpenVINOStructure(input, modelHash, atoms, compositions, warnings);
                    break;
                default:
                    warnings.Add($"Model type '{modelType}' structure extraction not yet implemented");
                    ExtractGenericBinaryMetadata(input, modelHash, atoms, compositions);
                    break;
            }

            sw.Stop();

            var uniqueHashes = atoms.Select(a => Convert.ToBase64String(a.ContentHash)).Distinct().Count();

            return new AtomizationResult
            {
                Atoms = atoms,
                Compositions = compositions,
                ProcessingInfo = new ProcessingMetadata
                {
                    TotalAtoms = atoms.Count,
                    UniqueAtoms = uniqueHashes,
                    DurationMs = sw.ElapsedMilliseconds,
                    AtomizerType = nameof(ModelFileAtomizer),
                    DetectedFormat = modelType.ToUpperInvariant(),
                    Warnings = warnings.Count > 0 ? warnings : null
                }
            };
        }
        catch (Exception ex)
        {
            warnings.Add($"Model atomization failed: {ex.Message}");
            throw;
        }
    }

    private string DetectModelType(string? contentType, string? fileName, byte[] data)
    {
        // Check file extension first
        if (!string.IsNullOrEmpty(fileName))
        {
            var ext = System.IO.Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
            
            // GGUF/GGML (llama.cpp ecosystem)
            if (ext == "gguf") return "gguf";
            if (ext == "ggml" || ext.StartsWith("ggjtv")) return "ggml";
            
            // ONNX
            if (ext == "onnx") return "onnx";
            
            // TensorFlow family
            if (ext == "pb") return "tensorflow";
            if (ext == "tflite") return "tflite";
            
            // PyTorch
            if (ext == "pth" || ext == "pt" || ext == "bin") return "pytorch";
            
            // Hugging Face / SafeTensors
            if (ext == "safetensors") return "safetensors";
            
            // Keras
            if (ext == "h5" || ext == "hdf5" || ext == "keras") return "h5";
            
            // Core ML
            if (ext == "mlmodel" || ext == "mlpackage") return "coreml";
            
            // Caffe
            if (ext == "caffemodel") return "caffe";
            
            // MXNet
            if (ext == "params" || ext == "mar") return "mxnet";
            
            // PaddlePaddle
            if (ext == "pdmodel" || ext == "pdparams") return "paddle";
            
            // OpenVINO
            if (fileName.Contains(".xml") && fileName.Contains("openvino", StringComparison.OrdinalIgnoreCase)) 
                return "openvino";
            
            // Darknet (YOLO)
            if (ext == "weights") return "darknet";
            
            // Quantized formats
            if (ext == "q4" || ext == "q8" || ext == "awq" || ext == "gptq") return "quantized";
            if (ext == "exl2") return "exllama";
        }

        // Check magic bytes for definitive identification
        if (data.Length >= 8)
        {
            // GGUF magic: "GGUF" (0x47 0x47 0x55 0x46)
            if (data[0] == 0x47 && data[1] == 0x47 && data[2] == 0x55 && data[3] == 0x46)
                return "gguf";
            
            // GGML magic: "ggml" or "ggjt"
            if (data[0] == 0x67 && data[1] == 0x67)
            {
                if (data[2] == 0x6d && data[3] == 0x6c) return "ggml";
                if (data[2] == 0x6a && data[3] == 0x74) return "ggml"; // ggjt variants
            }
            
            // HDF5 signature: \x89HDF\r\n\x1a\n
            if (data[0] == 0x89 && data[1] == 0x48 && data[2] == 0x44 && data[3] == 0x46)
                return "h5";
            
            // PyTorch pickle format
            if (data[0] == 0x80 && data.Length > 10)
            {
                var header = Encoding.ASCII.GetString(data.Take(Math.Min(200, data.Length)).ToArray());
                if (header.Contains("torch", StringComparison.OrdinalIgnoreCase))
                    return "pytorch";
            }
            
            // SafeTensors: starts with 8-byte header size
            var headerSize = BitConverter.ToInt64(data, 0);
            if (headerSize > 0 && headerSize < data.Length && headerSize < 1000000)
            {
                // Likely SafeTensors if header size is reasonable
                if (data.Length > headerSize + 8 && data[8] == '{')
                    return "safetensors";
            }
            
            // ONNX protobuf signature
            if (data[0] == 0x08 && data.Length > 100)
            {
                var header = Encoding.ASCII.GetString(data.Take(Math.Min(2048, data.Length)).ToArray());
                if (header.Contains("onnx", StringComparison.OrdinalIgnoreCase) ||
                    header.Contains("ir_version", StringComparison.OrdinalIgnoreCase))
                    return "onnx";
            }
            
            // TensorFlow Lite FlatBuffer magic: "TFL3"
            if (data.Length >= 8)
            {
                var magic = Encoding.ASCII.GetString(data, 4, Math.Min(4, data.Length - 4));
                if (magic == "TFL3") return "tflite";
            }
            
            // Core ML: ZIP archive with specific structure
            if (data[0] == 0x50 && data[1] == 0x4B && data.Length > 100)
            {
                var header = Encoding.ASCII.GetString(data.Take(Math.Min(1000, data.Length)).ToArray());
                if (header.Contains("coreml", StringComparison.OrdinalIgnoreCase))
                    return "coreml";
            }
        }

        return "unknown";
    }

    private string BuildModelMetadata(byte[] input, string modelType, string? fileName)
    {
        var metadata = new StringBuilder();
        metadata.Append("{");
        metadata.Append($"\"type\":\"{modelType}\",");
        metadata.Append($"\"size\":{input.Length},");
        metadata.Append($"\"fileName\":\"{fileName ?? "unknown"}\"");
        
        // Calculate entropy (indicates compression/encryption)
        var entropy = CalculateEntropy(input.Take(Math.Min(8192, input.Length)).ToArray());
        metadata.Append($",\"entropy\":{entropy:F2}");
        
        // Compressed models have higher entropy
        if (entropy > 7.5)
            metadata.Append(",\"compressed\":true");
        
        metadata.Append("}");
        return metadata.ToString();
    }

    private async Task ExtractOnnxStructureAsync(
        byte[] input,
        byte[] modelHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        // ONNX models are Protocol Buffer format
        // Basic structure: ModelProto -> GraphProto -> Nodes, Initializers, Inputs, Outputs
        
        try
        {
            // Look for node definitions (simplified heuristic)
            var text = Encoding.UTF8.GetString(input, 0, Math.Min(input.Length, 100000));
            
            // Count layer types (Conv, Gemm, Relu, etc.)
            var layerTypes = new[] { "Conv", "Gemm", "Relu", "MaxPool", "BatchNormalization", "Add", "Softmax" };
            var layerCounts = new Dictionary<string, int>();
            
            foreach (var layerType in layerTypes)
            {
                var count = CountOccurrences(text, layerType);
                if (count > 0)
                    layerCounts[layerType] = count;
            }

            // Create atoms for each layer type found
            int layerIndex = 0;
            foreach (var (layerType, count) in layerCounts)
            {
                var layerBytes = Encoding.UTF8.GetBytes($"layer:{layerType}:{count}");
                var layerHash = SHA256.HashData(layerBytes);
                
                var layerAtom = new AtomData
                {
                    AtomicValue = layerBytes,
                    ContentHash = layerHash,
                    Modality = "ml-model",
                    Subtype = "onnx-layer-type",
                    ContentType = "application/x-onnx",
                    CanonicalText = $"{layerType} (×{count})",
                    Metadata = $"{{\"layerType\":\"{layerType}\",\"count\":{count}}}"
                };
                
                atoms.Add(layerAtom);
                
                compositions.Add(new AtomComposition
                {
                    ParentAtomHash = modelHash,
                    ComponentAtomHash = layerHash,
                    SequenceIndex = layerIndex++,
                    Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
                });
            }
            
            warnings.Add("ONNX parsing requires ONNX Runtime or protobuf library for production use");
        }
        catch (Exception ex)
        {
            warnings.Add($"ONNX structure extraction failed: {ex.Message}");
        }
        
        await Task.CompletedTask;
    }

    private async Task ExtractGgufStructureAsync(
        byte[] input,
        byte[] modelHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        // GGUF format: https://github.com/ggerganov/ggml/blob/master/docs/gguf.md
        // Header: magic (4 bytes) + version (4 bytes) + tensor_count (8 bytes) + metadata_kv_count (8 bytes)
        
        try
        {
            if (input.Length < 24)
            {
                warnings.Add("GGUF file too small to parse header");
                return;
            }

            // Read version
            var version = BitConverter.ToUInt32(input, 4);
            
            // Read counts
            var tensorCount = BitConverter.ToUInt64(input, 8);
            var metadataCount = BitConverter.ToUInt64(input, 16);
            
            // Create metadata atom
            var metadataBytes = Encoding.UTF8.GetBytes($"gguf:v{version}:tensors:{tensorCount}:metadata:{metadataCount}");
            var metadataHash = SHA256.HashData(metadataBytes);
            
            var metadataAtom = new AtomData
            {
                AtomicValue = metadataBytes,
                ContentHash = metadataHash,
                Modality = "ml-model",
                Subtype = "gguf-metadata",
                ContentType = "application/x-gguf",
                CanonicalText = $"GGUF v{version} ({tensorCount} tensors)",
                Metadata = $"{{\"version\":{version},\"tensorCount\":{tensorCount},\"metadataCount\":{metadataCount}}}"
            };
            
            atoms.Add(metadataAtom);
            
            compositions.Add(new AtomComposition
            {
                ParentAtomHash = modelHash,
                ComponentAtomHash = metadataHash,
                SequenceIndex = 0,
                Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
            });
            
            // Parse metadata key-value pairs (simplified - full parsing needs proper KV type handling)
            int offset = 24;
            for (ulong i = 0; i < Math.Min(metadataCount, 50); i++) // Limit to first 50 for performance
            {
                if (offset + 8 > input.Length) break;
                
                // Read key length and key string (simplified)
                var keyLen = BitConverter.ToUInt64(input, offset);
                if (keyLen > 256 || offset + 8 + (int)keyLen > input.Length) break;
                
                var key = Encoding.UTF8.GetString(input, offset + 8, (int)keyLen);
                
                // Create atom for important metadata keys
                if (key.Contains("model") || key.Contains("architecture") || key.Contains("context") || key.Contains("quantization"))
                {
                    var kvBytes = Encoding.UTF8.GetBytes($"gguf:kv:{key}");
                    var kvHash = SHA256.HashData(kvBytes);
                    
                    var kvAtom = new AtomData
                    {
                        AtomicValue = kvBytes,
                        ContentHash = kvHash,
                        Modality = "ml-model",
                        Subtype = "gguf-metadata-kv",
                        ContentType = "application/x-gguf",
                        CanonicalText = key,
                        Metadata = $"{{\"key\":\"{key}\"}}"
                    };
                    
                    if (!atoms.Any(a => a.ContentHash.SequenceEqual(kvHash)))
                    {
                        atoms.Add(kvAtom);
                    }
                    
                    compositions.Add(new AtomComposition
                    {
                        ParentAtomHash = modelHash,
                        ComponentAtomHash = kvHash,
                        SequenceIndex = (int)i,
                        Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
                    });
                }
                
                // Skip to next KV pair (rough estimate - proper implementation needs type-aware parsing)
                offset += 8 + (int)keyLen + 16; // Approximate
            }
            
            warnings.Add("GGUF parsing is basic; production use should employ ggml library for full structure");
        }
        catch (Exception ex)
        {
            warnings.Add($"GGUF structure extraction failed: {ex.Message}");
        }
        
        await Task.CompletedTask;
    }

    private void ExtractGgmlStructure(
        byte[] input,
        byte[] modelHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings)
    {
        // Legacy GGML format (pre-GGUF)
        // Header: magic (4 bytes) + version (4 bytes) + n_vocab (4 bytes) + n_embd (4 bytes) + ...
        
        try
        {
            if (input.Length < 16)
            {
                warnings.Add("GGML file too small to parse header");
                return;
            }

            var magic = Encoding.ASCII.GetString(input, 0, 4);
            var version = BitConverter.ToUInt32(input, 4);
            
            var metadataBytes = Encoding.UTF8.GetBytes($"ggml:{magic}:v{version}");
            var metadataHash = SHA256.HashData(metadataBytes);
            
            var metadataAtom = new AtomData
            {
                AtomicValue = metadataBytes,
                ContentHash = metadataHash,
                Modality = "ml-model",
                Subtype = "ggml-metadata",
                ContentType = "application/x-ggml",
                CanonicalText = $"GGML {magic} v{version}",
                Metadata = $"{{\"magic\":\"{magic}\",\"version\":{version}}}"
            };
            
            atoms.Add(metadataAtom);
            
            compositions.Add(new AtomComposition
            {
                ParentAtomHash = modelHash,
                ComponentAtomHash = metadataHash,
                SequenceIndex = 0,
                Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
            });
            
            warnings.Add("Legacy GGML format; consider converting to GGUF for better metadata support");
        }
        catch (Exception ex)
        {
            warnings.Add($"GGML structure extraction failed: {ex.Message}");
        }
    }

    private void ExtractTFLiteStructure(
        byte[] input,
        byte[] modelHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings)
    {
        // TensorFlow Lite uses FlatBuffers format
        try
        {
            // TFLite FlatBuffer has schema with operators, tensors, buffers
            var header = Encoding.UTF8.GetString(input.Take(Math.Min(10000, input.Length)).ToArray());
            
            // Count common operators
            var tfliteOps = new[] { "CONV_2D", "DEPTHWISE_CONV_2D", "FULLY_CONNECTED", "ADD", "MUL", "RELU", "SOFTMAX", "RESHAPE" };
            var opCounts = new Dictionary<string, int>();
            
            foreach (var op in tfliteOps)
            {
                var count = CountOccurrences(header, op);
                if (count > 0)
                    opCounts[op] = count;
            }

            int opIndex = 0;
            foreach (var (opType, count) in opCounts)
            {
                var opBytes = Encoding.UTF8.GetBytes($"tflite:{opType}:{count}");
                var opHash = SHA256.HashData(opBytes);
                
                var opAtom = new AtomData
                {
                    AtomicValue = opBytes,
                    ContentHash = opHash,
                    Modality = "ml-model",
                    Subtype = "tflite-op",
                    ContentType = "application/x-tflite",
                    CanonicalText = $"{opType} (×{count})",
                    Metadata = $"{{\"opType\":\"{opType}\",\"count\":{count}}}"
                };
                
                atoms.Add(opAtom);
                
                compositions.Add(new AtomComposition
                {
                    ParentAtomHash = modelHash,
                    ComponentAtomHash = opHash,
                    SequenceIndex = opIndex++,
                    Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
                });
            }
            
            warnings.Add("TFLite parsing requires TensorFlow Lite FlatBuffers schema for production use");
        }
        catch (Exception ex)
        {
            warnings.Add($"TFLite structure extraction failed: {ex.Message}");
        }
    }

    private void ExtractCoreMLStructure(
        byte[] input,
        byte[] modelHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings)
    {
        // Core ML models are protobuf-based (.mlmodel) or package directories (.mlpackage)
        try
        {
            // Check if it's a ZIP (mlpackage is a package/directory structure)
            if (input.Length > 4 && input[0] == 0x50 && input[1] == 0x4B)
            {
                warnings.Add("Core ML package format detected (ZIP-based)");
                
                using var ms = new System.IO.MemoryStream(input);
                using var archive = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Read);
                
                // Look for model.mlmodel or similar
                foreach (var entry in archive.Entries.Take(20))
                {
                    if (entry.FullName.EndsWith(".mlmodel", StringComparison.OrdinalIgnoreCase))
                    {
                        var entryBytes = Encoding.UTF8.GetBytes($"coreml:{entry.FullName}");
                        var entryHash = SHA256.HashData(entryBytes);
                        
                        var entryAtom = new AtomData
                        {
                            AtomicValue = entryBytes,
                            ContentHash = entryHash,
                            Modality = "ml-model",
                            Subtype = "coreml-entry",
                            ContentType = "application/x-coreml",
                            CanonicalText = entry.FullName,
                            Metadata = $"{{\"entry\":\"{entry.FullName}\",\"size\":{entry.Length}}}"
                        };
                        
                        atoms.Add(entryAtom);
                        
                        compositions.Add(new AtomComposition
                        {
                            ParentAtomHash = modelHash,
                            ComponentAtomHash = entryHash,
                            SequenceIndex = 0,
                            Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
                        });
                    }
                }
            }
            else
            {
                // Direct .mlmodel file (protobuf)
                var header = Encoding.UTF8.GetString(input.Take(Math.Min(5000, input.Length)).ToArray());
                
                // Look for layer types
                if (header.Contains("neuralNetwork"))
                {
                    var metadataBytes = Encoding.UTF8.GetBytes("coreml:neuralNetwork");
                    var metadataHash = SHA256.HashData(metadataBytes);
                    
                    var metadataAtom = new AtomData
                    {
                        AtomicValue = metadataBytes,
                        ContentHash = metadataHash,
                        Modality = "ml-model",
                        Subtype = "coreml-type",
                        ContentType = "application/x-coreml",
                        CanonicalText = "Neural Network",
                        Metadata = "{\"modelType\":\"neuralNetwork\"}"
                    };
                    
                    atoms.Add(metadataAtom);
                    
                    compositions.Add(new AtomComposition
                    {
                        ParentAtomHash = modelHash,
                        ComponentAtomHash = metadataHash,
                        SequenceIndex = 0,
                        Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
                    });
                }
            }
            
            warnings.Add("Core ML parsing requires Core ML protobuf schema for production use");
        }
        catch (Exception ex)
        {
            warnings.Add($"Core ML structure extraction failed: {ex.Message}");
        }
    }

    private void ExtractCaffeStructure(
        byte[] input,
        byte[] modelHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings)
    {
        // Caffe models: .caffemodel (binary protobuf) and .prototxt (text proto)
        try
        {
            var text = Encoding.UTF8.GetString(input.Take(Math.Min(50000, input.Length)).ToArray());
            
            // Look for layer definitions
            var layerTypes = new[] { "Convolution", "InnerProduct", "ReLU", "Pooling", "Dropout", "Softmax", "Data" };
            var layerCounts = new Dictionary<string, int>();
            
            foreach (var layerType in layerTypes)
            {
                var count = CountOccurrences(text, $"type: \"{layerType}\"");
                if (count == 0)
                    count = CountOccurrences(text, $"type: '{layerType}'");
                if (count > 0)
                    layerCounts[layerType] = count;
            }

            int layerIndex = 0;
            foreach (var (layerType, count) in layerCounts)
            {
                var layerBytes = Encoding.UTF8.GetBytes($"caffe:{layerType}:{count}");
                var layerHash = SHA256.HashData(layerBytes);
                
                var layerAtom = new AtomData
                {
                    AtomicValue = layerBytes,
                    ContentHash = layerHash,
                    Modality = "ml-model",
                    Subtype = "caffe-layer",
                    ContentType = "application/x-caffe",
                    CanonicalText = $"{layerType} (×{count})",
                    Metadata = $"{{\"layerType\":\"{layerType}\",\"count\":{count}}}"
                };
                
                atoms.Add(layerAtom);
                
                compositions.Add(new AtomComposition
                {
                    ParentAtomHash = modelHash,
                    ComponentAtomHash = layerHash,
                    SequenceIndex = layerIndex++,
                    Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
                });
            }
            
            warnings.Add("Caffe model parsing requires Caffe protobuf library for production use");
        }
        catch (Exception ex)
        {
            warnings.Add($"Caffe structure extraction failed: {ex.Message}");
        }
    }

    private void ExtractMXNetStructure(
        byte[] input,
        byte[] modelHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings)
    {
        // MXNet models: .params (NDArray) and .json (symbol graph)
        try
        {
            var text = Encoding.UTF8.GetString(input.Take(Math.Min(50000, input.Length)).ToArray());
            
            // Check if it's JSON (symbol file)
            if (text.TrimStart().StartsWith("{"))
            {
                // Count operator types
                var ops = new[] { "Convolution", "FullyConnected", "Activation", "Pooling", "BatchNorm", "Dropout", "SoftmaxOutput" };
                var opCounts = new Dictionary<string, int>();
                
                foreach (var op in ops)
                {
                    var count = CountOccurrences(text, $"\"op\": \"{op}\"");
                    if (count > 0)
                        opCounts[op] = count;
                }

                int opIndex = 0;
                foreach (var (opType, count) in opCounts)
                {
                    var opBytes = Encoding.UTF8.GetBytes($"mxnet:{opType}:{count}");
                    var opHash = SHA256.HashData(opBytes);
                    
                    var opAtom = new AtomData
                    {
                        AtomicValue = opBytes,
                        ContentHash = opHash,
                        Modality = "ml-model",
                        Subtype = "mxnet-op",
                        ContentType = "application/x-mxnet",
                        CanonicalText = $"{opType} (×{count})",
                        Metadata = $"{{\"opType\":\"{opType}\",\"count\":{count}}}"
                    };
                    
                    atoms.Add(opAtom);
                    
                    compositions.Add(new AtomComposition
                    {
                        ParentAtomHash = modelHash,
                        ComponentAtomHash = opHash,
                        SequenceIndex = opIndex++,
                        Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
                    });
                }
            }
            else
            {
                // Binary .params file
                var metadataBytes = Encoding.UTF8.GetBytes("mxnet:params");
                var metadataHash = SHA256.HashData(metadataBytes);
                
                var metadataAtom = new AtomData
                {
                    AtomicValue = metadataBytes,
                    ContentHash = metadataHash,
                    Modality = "ml-model",
                    Subtype = "mxnet-params",
                    ContentType = "application/x-mxnet",
                    CanonicalText = "MXNet Parameters",
                    Metadata = "{\"type\":\"params\"}"
                };
                
                atoms.Add(metadataAtom);
                
                compositions.Add(new AtomComposition
                {
                    ParentAtomHash = modelHash,
                    ComponentAtomHash = metadataHash,
                    SequenceIndex = 0,
                    Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
                });
            }
            
            warnings.Add("MXNet model parsing requires MXNet library for production use");
        }
        catch (Exception ex)
        {
            warnings.Add($"MXNet structure extraction failed: {ex.Message}");
        }
    }

    private void ExtractOpenVINOStructure(
        byte[] input,
        byte[] modelHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings)
    {
        // OpenVINO IR: .xml (model architecture) and .bin (weights)
        try
        {
            var xml = Encoding.UTF8.GetString(input);
            
            // Parse XML for layer types
            var layerTypes = new[] { "Convolution", "FullyConnected", "Pooling", "ReLU", "Softmax", "Concat", "Reshape" };
            var layerCounts = new Dictionary<string, int>();
            
            foreach (var layerType in layerTypes)
            {
                var count = CountOccurrences(xml, $"type=\"{layerType}\"");
                if (count > 0)
                    layerCounts[layerType] = count;
            }

            int layerIndex = 0;
            foreach (var (layerType, count) in layerCounts)
            {
                var layerBytes = Encoding.UTF8.GetBytes($"openvino:{layerType}:{count}");
                var layerHash = SHA256.HashData(layerBytes);
                
                var layerAtom = new AtomData
                {
                    AtomicValue = layerBytes,
                    ContentHash = layerHash,
                    Modality = "ml-model",
                    Subtype = "openvino-layer",
                    ContentType = "application/x-openvino",
                    CanonicalText = $"{layerType} (×{count})",
                    Metadata = $"{{\"layerType\":\"{layerType}\",\"count\":{count}}}"
                };
                
                atoms.Add(layerAtom);
                
                compositions.Add(new AtomComposition
                {
                    ParentAtomHash = modelHash,
                    ComponentAtomHash = layerHash,
                    SequenceIndex = layerIndex++,
                    Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
                });
            }
            
            warnings.Add("OpenVINO IR parsing is XML-based; production use should employ OpenVINO toolkit");
        }
        catch (Exception ex)
        {
            warnings.Add($"OpenVINO structure extraction failed: {ex.Message}");
        }
    }

    private void ExtractTensorFlowStructure(
        byte[] input,
        byte[] modelHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings)
    {
        // TensorFlow SavedModel or GraphDef format (protobuf-based)
        try
        {
            var text = Encoding.UTF8.GetString(input, 0, Math.Min(input.Length, 50000));
            
            // Look for common TF ops
            var tfOps = new[] { "Conv2D", "MatMul", "Relu", "Add", "BiasAdd", "Softmax", "Placeholder" };
            var opCounts = new Dictionary<string, int>();
            
            foreach (var op in tfOps)
            {
                var count = CountOccurrences(text, op);
                if (count > 0)
                    opCounts[op] = count;
            }

            int opIndex = 0;
            foreach (var (opType, count) in opCounts)
            {
                var opBytes = Encoding.UTF8.GetBytes($"tfop:{opType}:{count}");
                var opHash = SHA256.HashData(opBytes);
                
                var opAtom = new AtomData
                {
                    AtomicValue = opBytes,
                    ContentHash = opHash,
                    Modality = "ml-model",
                    Subtype = "tensorflow-op",
                    ContentType = "application/x-tensorflow",
                    CanonicalText = $"{opType} (×{count})",
                    Metadata = $"{{\"opType\":\"{opType}\",\"count\":{count}}}"
                };
                
                atoms.Add(opAtom);
                
                compositions.Add(new AtomComposition
                {
                    ParentAtomHash = modelHash,
                    ComponentAtomHash = opHash,
                    SequenceIndex = opIndex++,
                    Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
                });
            }
            
            warnings.Add("TensorFlow parsing requires TensorFlow .NET or protobuf library for production use");
        }
        catch (Exception ex)
        {
            warnings.Add($"TensorFlow structure extraction failed: {ex.Message}");
        }
    }

    private void ExtractPyTorchStructure(
        byte[] input,
        byte[] modelHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings)
    {
        // PyTorch models are pickled Python objects
        warnings.Add("PyTorch model parsing requires PyTorch or pickle deserialization (security risk)");
        
        // Basic metadata extraction from pickle structure
        var header = Encoding.UTF8.GetString(input.Take(Math.Min(1000, input.Length)).ToArray());
        
        if (header.Contains("torch"))
        {
            var metadataBytes = Encoding.UTF8.GetBytes("pytorch:model");
            var metadataHash = SHA256.HashData(metadataBytes);
            
            var metadataAtom = new AtomData
            {
                AtomicValue = metadataBytes,
                ContentHash = metadataHash,
                Modality = "ml-model",
                Subtype = "pytorch-metadata",
                ContentType = "application/x-pytorch",
                CanonicalText = "PyTorch Model",
                Metadata = "{\"framework\":\"pytorch\"}"
            };
            
            atoms.Add(metadataAtom);
            
            compositions.Add(new AtomComposition
            {
                ParentAtomHash = modelHash,
                ComponentAtomHash = metadataHash,
                SequenceIndex = 0,
                Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
            });
        }
    }

    private void ExtractKerasStructure(
        byte[] input,
        byte[] modelHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings)
    {
        // Keras H5 models use HDF5 format
        warnings.Add("Keras H5 parsing requires HDF5 library for production use");
        
        // HDF5 has structured hierarchy - groups and datasets
        // For now, just note that it's an H5 file
        var metadataBytes = Encoding.UTF8.GetBytes("keras:h5:model");
        var metadataHash = SHA256.HashData(metadataBytes);
        
        var metadataAtom = new AtomData
        {
            AtomicValue = metadataBytes,
            ContentHash = metadataHash,
            Modality = "ml-model",
            Subtype = "keras-h5",
            ContentType = "application/x-hdf5",
            CanonicalText = "Keras H5 Model",
            Metadata = "{\"framework\":\"keras\",\"format\":\"h5\"}"
        };
        
        atoms.Add(metadataAtom);
        
        compositions.Add(new AtomComposition
        {
            ParentAtomHash = modelHash,
            ComponentAtomHash = metadataHash,
            SequenceIndex = 0,
            Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
        });
    }

    private void ExtractSafeTensorsStructure(
        byte[] input,
        byte[] modelHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings)
    {
        // SafeTensors format: header (JSON) + tensor data
        // Header size is stored in first 8 bytes (little-endian)
        
        if (input.Length < 8)
        {
            warnings.Add("SafeTensors file too small");
            return;
        }

        try
        {
            var headerSize = BitConverter.ToInt64(input, 0);
            
            if (headerSize > 0 && headerSize < input.Length)
            {
                var headerJson = Encoding.UTF8.GetString(input, 8, (int)headerSize);
                
                // Parse basic structure (tensor names and shapes)
                var tensorCount = CountOccurrences(headerJson, "\"dtype\"");
                
                var metadataBytes = Encoding.UTF8.GetBytes($"safetensors:{tensorCount}");
                var metadataHash = SHA256.HashData(metadataBytes);
                
                var metadataAtom = new AtomData
                {
                    AtomicValue = metadataBytes,
                    ContentHash = metadataHash,
                    Modality = "ml-model",
                    Subtype = "safetensors-metadata",
                    ContentType = "application/safetensors",
                    CanonicalText = $"SafeTensors ({tensorCount} tensors)",
                    Metadata = $"{{\"format\":\"safetensors\",\"tensorCount\":{tensorCount},\"headerSize\":{headerSize}}}"
                };
                
                atoms.Add(metadataAtom);
                
                compositions.Add(new AtomComposition
                {
                    ParentAtomHash = modelHash,
                    ComponentAtomHash = metadataHash,
                    SequenceIndex = 0,
                    Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
                });
            }
        }
        catch (Exception ex)
        {
            warnings.Add($"SafeTensors parsing failed: {ex.Message}");
        }
    }

    private void ExtractGenericBinaryMetadata(
        byte[] input,
        byte[] modelHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions)
    {
        // Generic binary analysis: entropy, size, magic bytes
        var entropy = CalculateEntropy(input.Take(Math.Min(8192, input.Length)).ToArray());
        
        var metadataBytes = Encoding.UTF8.GetBytes($"binary:entropy:{entropy:F2}");
        var metadataHash = SHA256.HashData(metadataBytes);
        
        var metadataAtom = new AtomData
        {
            AtomicValue = metadataBytes,
            ContentHash = metadataHash,
            Modality = "binary",
            Subtype = "entropy-analysis",
            ContentType = "application/octet-stream",
            CanonicalText = $"Entropy: {entropy:F2}",
            Metadata = $"{{\"entropy\":{entropy:F2},\"sampleSize\":{Math.Min(8192, input.Length)}}}"
        };
        
        atoms.Add(metadataAtom);
        
        compositions.Add(new AtomComposition
        {
            ParentAtomHash = modelHash,
            ComponentAtomHash = metadataHash,
            SequenceIndex = 0,
            Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
        });
    }

    private double CalculateEntropy(byte[] data)
    {
        if (data.Length == 0) return 0;
        
        var freq = new int[256];
        foreach (var b in data)
            freq[b]++;
        
        double entropy = 0;
        foreach (var count in freq)
        {
            if (count == 0) continue;
            double p = (double)count / data.Length;
            entropy -= p * Math.Log2(p);
        }
        
        return entropy;
    }

    private int CountOccurrences(string text, string pattern)
    {
        int count = 0;
        int pos = 0;
        while ((pos = text.IndexOf(pattern, pos, StringComparison.Ordinal)) != -1)
        {
            count++;
            pos += pattern.Length;
        }
        return count;
    }
}
