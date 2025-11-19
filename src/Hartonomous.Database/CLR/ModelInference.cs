using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using Microsoft.SqlServer.Server;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;

namespace Hartonomous.Clr
{
    /// <summary>
    /// Model inference engine using reconstructed TensorAtom graph
    /// Executes forward pass through model layers stored in LayerTensorSegments
    /// NO external dependencies - pure SQL CLR tensor operations
    /// </summary>
    public static class ModelInference
    {
        /// <summary>
        /// Execute model inference using TensorAtom graph
        /// Loads model layers from database, runs forward pass, returns prediction
        /// </summary>
        [SqlFunction(IsDeterministic = false, IsPrecise = false, DataAccess = DataAccessKind.Read)]
        public static SqlString ExecuteModelInference(SqlInt32 modelId, SqlBytes embeddingVector)
        {
            if (modelId.IsNull || embeddingVector.IsNull)
                return SqlString.Null;

            try
            {
                using (var conn = new SqlConnection("context connection=true"))
                {
                    conn.Open();

                    // Load model architecture
                    var architecture = LoadModelArchitecture(conn, modelId.Value);
                    if (architecture == null)
                        return new SqlString("{\"error\": \"Model architecture not found\"}");

                    // Convert input embedding to vector
                    var inputVec = BytesToVector(embeddingVector.Value);
                    if (inputVec == null || inputVec.Length == 0)
                        return new SqlString("{\"error\": \"Invalid embedding vector\"}");

                    // Run forward pass through all layers
                    var currentActivation = inputVec;
                    
                    foreach (var layer in architecture.Layers.OrderBy(l => l.LayerIndex))
                    {
                        currentActivation = ExecuteLayer(conn, layer, currentActivation);
                    }

                    // Output layer: compute softmax and get predicted class
                    var probabilities = Softmax(currentActivation);
                    int predictedClass = ArgMax(probabilities);
                    float score = probabilities[predictedClass];

                    // Return JSON result
                    return new SqlString($"{{\"score\": {score:F6}, \"label\": \"class_{predictedClass}\", \"class_id\": {predictedClass}}}");
                }
            }
            catch (Exception ex)
            {
                return new SqlString($"{{\"error\": \"{ex.Message.Replace("\"", "\\\"")}\", \"stack\": \"{ex.StackTrace?.Replace("\"", "\\\"")}\"}}");
            }
        }

        private static ModelArchitecture? LoadModelArchitecture(SqlConnection conn, int modelId)
        {
            var query = @"
                SELECT 
                    ml.LayerId,
                    ml.LayerIndex,
                    ml.LayerType,
                    ml.InputDimension,
                    ml.OutputDimension,
                    ml.ActivationFunction,
                    ml.Config
                FROM dbo.ModelLayers ml
                WHERE ml.ModelId = @ModelId
                ORDER BY ml.LayerIndex";

            var architecture = new ModelArchitecture { Layers = new List<LayerDefinition>() };

            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ModelId", modelId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        architecture.Layers.Add(new LayerDefinition
                        {
                            LayerId = reader.GetInt32(0),
                            LayerIndex = reader.GetInt32(1),
                            LayerType = reader.GetString(2),
                            InputDimension = reader.GetInt32(3),
                            OutputDimension = reader.GetInt32(4),
                            ActivationFunction = reader.IsDBNull(5) ? null : reader.GetString(5),
                            Config = reader.IsDBNull(6) ? null : reader.GetString(6)
                        });
                    }
                }
            }

            return architecture.Layers.Count > 0 ? architecture : null;
        }

        private static float[] ExecuteLayer(SqlConnection conn, LayerDefinition layer, float[] input)
        {
            switch (layer.LayerType.ToLower())
            {
                case "linear":
                case "dense":
                case "fully_connected":
                    return ExecuteLinearLayer(conn, layer, input);

                case "layernorm":
                case "layer_norm":
                    return ExecuteLayerNorm(conn, layer, input);

                case "dropout":
                    // Dropout disabled during inference
                    return input;

                case "embedding":
                    // Embedding layers handled separately during tokenization
                    return input;

                default:
                    // Unknown layer type, pass through
                    return input;
            }
        }

        private static float[] ExecuteLinearLayer(SqlConnection conn, LayerDefinition layer, float[] input)
        {
            // Load weight matrix and bias vector from LayerTensorSegments
            var weights = LoadLayerWeights(conn, layer.LayerId, layer.InputDimension, layer.OutputDimension);
            var bias = LoadLayerBias(conn, layer.LayerId, layer.OutputDimension);

            if (weights == null)
                return input; // Fallback: identity mapping

            // Matrix multiplication: output = input × W + b
            var output = new float[layer.OutputDimension];
            
            for (int outIdx = 0; outIdx < layer.OutputDimension; outIdx++)
            {
                float sum = 0f;
                for (int inIdx = 0; inIdx < layer.InputDimension; inIdx++)
                {
                    sum += input[inIdx] * weights[inIdx, outIdx];
                }
                output[outIdx] = sum + (bias != null ? bias[outIdx] : 0f);
            }

            // Apply activation function
            if (!string.IsNullOrEmpty(layer.ActivationFunction))
            {
                var activation = layer.ActivationFunction;
                if (activation != null)
                    output = ApplyActivation(output, activation);
            }

            return output;
        }

        private static float[] ExecuteLayerNorm(SqlConnection conn, LayerDefinition layer, float[] input)
        {
            // Load gamma (scale) and beta (shift) parameters
            var gamma = LoadLayerParameter(conn, layer.LayerId, "gamma", layer.InputDimension);
            var beta = LoadLayerParameter(conn, layer.LayerId, "beta", layer.InputDimension);

            if (gamma == null)
                gamma = Enumerable.Repeat(1.0f, layer.InputDimension).ToArray();
            if (beta == null)
                beta = Enumerable.Repeat(0.0f, layer.InputDimension).ToArray();

            // Compute mean
            float mean = input.Average();

            // Compute variance
            float variance = input.Select(x => (x - mean) * (x - mean)).Average();

            // Normalize: (x - mean) / sqrt(variance + epsilon)
            float epsilon = 1e-5f;
            float stdDev = (float)Math.Sqrt(variance + epsilon);

            var output = new float[input.Length];
            for (int i = 0; i < input.Length; i++)
            {
                float normalized = (input[i] - mean) / stdDev;
                output[i] = normalized * gamma[i] + beta[i];
            }

            return output;
        }

        private static float[,]? LoadLayerWeights(SqlConnection conn, int layerId, int inputDim, int outputDim)
        {
            // Query LayerTensorSegments for weight matrix
            var query = @"
                SELECT 
                    lts.RawPayload,
                    lts.SegmentOrdinal,
                    lts.QuantizationType
                FROM dbo.LayerTensorSegments lts
                WHERE lts.LayerId = @LayerId
                  AND lts.TensorName = 'weight'
                ORDER BY lts.SegmentOrdinal";

            var segments = new List<(byte[] Data, int Ordinal, string QuantType)>();

            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@LayerId", layerId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var data = reader.GetSqlBytes(0).Value;
                        var ordinal = reader.GetInt32(1);
                        var quantType = reader.IsDBNull(2) ? "F32" : reader.GetString(2);

                        segments.Add((data, ordinal, quantType));
                    }
                }
            }

            if (segments.Count == 0)
                return null;

            // Dequantize and reconstruct weight matrix
            var flatWeights = new List<float>();
            
            foreach (var segment in segments.OrderBy(s => s.Ordinal))
            {
                var dequantized = DequantizeSegment(segment.Data, segment.QuantType);
                flatWeights.AddRange(dequantized);
            }

            // Reshape to [inputDim, outputDim] matrix
            var weights = new float[inputDim, outputDim];
            int idx = 0;
            for (int i = 0; i < inputDim && idx < flatWeights.Count; i++)
            {
                for (int j = 0; j < outputDim && idx < flatWeights.Count; j++)
                {
                    weights[i, j] = flatWeights[idx++];
                }
            }

            return weights;
        }

        private static float[]? LoadLayerBias(SqlConnection conn, int layerId, int outputDim)
        {
            return LoadLayerParameter(conn, layerId, "bias", outputDim);
        }

        private static float[]? LoadLayerParameter(SqlConnection conn, int layerId, string tensorName, int expectedDim)
        {
            var query = @"
                SELECT 
                    lts.RawPayload,
                    lts.QuantizationType
                FROM dbo.LayerTensorSegments lts
                WHERE lts.LayerId = @LayerId
                  AND lts.TensorName = @TensorName
                ORDER BY lts.SegmentOrdinal";

            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@LayerId", layerId);
                cmd.Parameters.AddWithValue("@TensorName", tensorName);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var data = reader.GetSqlBytes(0).Value;
                        var quantType = reader.IsDBNull(1) ? "F32" : reader.GetString(1);

                        return DequantizeSegment(data, quantType);
                    }
                }
            }

            return null;
        }

        private static float[] DequantizeSegment(byte[] data, string quantizationType)
        {
            switch (quantizationType.ToUpper())
            {
                case "F32":
                case "FLOAT32":
                    return BytesToFloatArray(data);

                case "F16":
                case "FLOAT16":
                    return DequantizeFloat16(data);

                case "Q8_0":
                    return DequantizeQ8_0(data);

                case "Q4_K":
                    return DequantizeQ4_K(data);

                default:
                    // Assume F32 by default
                    return BytesToFloatArray(data);
            }
        }

        private static float[] BytesToFloatArray(byte[] data)
        {
            if (data.Length % 4 != 0)
                return new float[0];

            var result = new float[data.Length / 4];
            Buffer.BlockCopy(data, 0, result, 0, data.Length);
            return result;
        }

        private static float[] DequantizeFloat16(byte[] data)
        {
            // FP16 to FP32 conversion
            var result = new float[data.Length / 2];
            for (int i = 0; i < result.Length; i++)
            {
                ushort half = BitConverter.ToUInt16(data, i * 2);
                result[i] = HalfToFloat(half);
            }
            return result;
        }

        private static float HalfToFloat(ushort half)
        {
            int sign = (half >> 15) & 0x1;
            int exponent = (half >> 10) & 0x1F;
            int mantissa = half & 0x3FF;

            float result;
            if (exponent == 0)
            {
                result = (sign == 1 ? -1f : 1f) * (float)Math.Pow(2, -14) * (mantissa / 1024f);
            }
            else if (exponent == 31)
            {
                result = mantissa == 0 ? (sign == 1 ? float.NegativeInfinity : float.PositiveInfinity) : float.NaN;
            }
            else
            {
                result = (sign == 1 ? -1f : 1f) * (float)Math.Pow(2, exponent - 15) * (1 + mantissa / 1024f);
            }

            return result;
        }

        private static float[] DequantizeQ8_0(byte[] data)
        {
            // Q8_0: 8-bit quantization with per-block scaling
            // Block structure: [float32 scale][32 × int8 values]
            const int blockSize = 32;
            const int blockBytes = 4 + blockSize; // 4 bytes scale + 32 bytes values

            int numBlocks = data.Length / blockBytes;
            var result = new float[numBlocks * blockSize];

            for (int block = 0; block < numBlocks; block++)
            {
                int blockOffset = block * blockBytes;
                float scale = BitConverter.ToSingle(data, blockOffset);

                for (int i = 0; i < blockSize; i++)
                {
                    sbyte quantized = (sbyte)data[blockOffset + 4 + i];
                    result[block * blockSize + i] = quantized * scale;
                }
            }

            return result;
        }

        private static float[] DequantizeQ4_K(byte[] data)
        {
            // Q4_K: 4-bit quantization with per-block scaling
            // Block structure: [float16 scale][float16 min][128 × 4-bit values packed]
            const int blockSize = 128;
            const int blockBytes = 4 + (blockSize / 2); // 2×2 bytes (scale+min) + 64 bytes (packed 4-bit)

            int numBlocks = data.Length / blockBytes;
            var result = new float[numBlocks * blockSize];

            for (int block = 0; block < numBlocks; block++)
            {
                int blockOffset = block * blockBytes;
                
                ushort scaleHalf = BitConverter.ToUInt16(data, blockOffset);
                ushort minHalf = BitConverter.ToUInt16(data, blockOffset + 2);
                
                float scale = HalfToFloat(scaleHalf);
                float min = HalfToFloat(minHalf);

                for (int i = 0; i < blockSize / 2; i++)
                {
                    byte packed = data[blockOffset + 4 + i];
                    
                    // Extract two 4-bit values
                    int val1 = packed & 0x0F;
                    int val2 = (packed >> 4) & 0x0F;

                    result[block * blockSize + i * 2] = val1 * scale + min;
                    result[block * blockSize + i * 2 + 1] = val2 * scale + min;
                }
            }

            return result;
        }

        private static float[] ApplyActivation(float[] input, string activationFunction)
        {
            var output = new float[input.Length];

            switch (activationFunction.ToLower())
            {
                case "relu":
                    for (int i = 0; i < input.Length; i++)
                        output[i] = Math.Max(0, input[i]);
                    break;

                case "gelu":
                    for (int i = 0; i < input.Length; i++)
                        output[i] = Gelu(input[i]);
                    break;

                case "tanh":
                    for (int i = 0; i < input.Length; i++)
                        output[i] = (float)Math.Tanh(input[i]);
                    break;

                case "sigmoid":
                    for (int i = 0; i < input.Length; i++)
                        output[i] = 1f / (1f + (float)Math.Exp(-input[i]));
                    break;

                case "silu":
                case "swish":
                    for (int i = 0; i < input.Length; i++)
                        output[i] = input[i] / (1f + (float)Math.Exp(-input[i]));
                    break;

                default:
                    // No activation or unknown: identity
                    return input;
            }

            return output;
        }

        private static float Gelu(float x)
        {
            // GELU approximation: 0.5 * x * (1 + tanh(sqrt(2/π) * (x + 0.044715 * x^3)))
            const float sqrt2OverPi = 0.7978845608f;
            float x3 = x * x * x;
            return 0.5f * x * (1f + (float)Math.Tanh(sqrt2OverPi * (x + 0.044715f * x3)));
        }

        private static float[] Softmax(float[] logits)
        {
            float max = logits.Max();
            var exps = logits.Select(x => (float)Math.Exp(x - max)).ToArray();
            float sum = exps.Sum();
            return exps.Select(x => x / sum).ToArray();
        }

        private static int ArgMax(float[] values)
        {
            int maxIdx = 0;
            float maxVal = values[0];

            for (int i = 1; i < values.Length; i++)
            {
                if (values[i] > maxVal)
                {
                    maxVal = values[i];
                    maxIdx = i;
                }
            }

            return maxIdx;
        }

        private static float[]? BytesToVector(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0 || bytes.Length % 4 != 0)
                return null;

            var vector = new float[bytes.Length / 4];
            Buffer.BlockCopy(bytes, 0, vector, 0, bytes.Length);
            return vector;
        }

        private class ModelArchitecture
        {
            public List<LayerDefinition> Layers { get; set; } = null!;
        }

        private class LayerDefinition
        {
            public int LayerId { get; set; }
            public int LayerIndex { get; set; }
            public string LayerType { get; set; } = string.Empty;
            public int InputDimension { get; set; }
            public int OutputDimension { get; set; }
            public string? ActivationFunction { get; set; }
            public string? Config { get; set; }
        }
    }
}

