using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ModelIngestion
{
    /// <summary>
    /// Repository for ingesting models into production schema with VECTOR types
    /// Decomposes models into queryable atomic weights
    /// </summary>
    public class ProductionModelRepository
    {
        private readonly string _connectionString;

        public ProductionModelRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Ingest a transformer model with full decomposition into attention heads
        /// </summary>
        public async Task<int> IngestTransformerModelAsync(
            string modelName,
            string modelType,
            string architecture,
            int inputDim,
            int outputDim,
            int numLayers,
            long numParameters,
            string sourceFramework,
            string sourceUrl,
            List<TransformerLayerData> layers)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Insert model metadata
                var modelId = await InsertModelAsync(connection, transaction,
                    modelName, modelType, architecture, inputDim, outputDim,
                    numLayers, numParameters, sourceFramework, sourceUrl);

                Console.WriteLine($"Created model ID: {modelId}");

                // 2. Insert layers
                foreach (var layerData in layers)
                {
                    var layerId = await InsertLayerAsync(connection, transaction,
                        modelId, layerData);

                    Console.WriteLine($"  Layer {layerData.LayerIdx}: {layerData.LayerType} (ID: {layerId})");

                    // 3. Insert attention weights for this layer
                    if (layerData.AttentionHeads != null && layerData.AttentionHeads.Count > 0)
                    {
                        foreach (var head in layerData.AttentionHeads)
                        {
                            await InsertAttentionWeightsAsync(connection, transaction,
                                layerId, head);
                        }
                        Console.WriteLine($"    Inserted {layerData.AttentionHeads.Count} attention heads");
                    }
                }

                await transaction.CommitAsync();
                Console.WriteLine($"✓ Model '{modelName}' fully ingested with {numParameters:N0} parameters");
                return modelId;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"✗ Error ingesting model: {ex.Message}");
                throw;
            }
        }

        private async Task<int> InsertModelAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            string modelName,
            string modelType,
            string architecture,
            int inputDim,
            int outputDim,
            int numLayers,
            long numParameters,
            string sourceFramework,
            string sourceUrl)
        {
            var sql = @"
                INSERT INTO dbo.Models_Production (
                    model_name, model_type, architecture,
                    input_dim, output_dim, num_layers, num_parameters,
                    source_framework, source_url, is_decomposed
                ) VALUES (
                    @model_name, @model_type, @architecture,
                    @input_dim, @output_dim, @num_layers, @num_parameters,
                    @source_framework, @source_url, 1
                );
                SELECT SCOPE_IDENTITY();
            ";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.Parameters.AddWithValue("@model_name", modelName);
            cmd.Parameters.AddWithValue("@model_type", modelType);
            cmd.Parameters.AddWithValue("@architecture", architecture);
            cmd.Parameters.AddWithValue("@input_dim", inputDim);
            cmd.Parameters.AddWithValue("@output_dim", outputDim);
            cmd.Parameters.AddWithValue("@num_layers", numLayers);
            cmd.Parameters.AddWithValue("@num_parameters", numParameters);
            cmd.Parameters.AddWithValue("@source_framework", (object?)sourceFramework ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@source_url", (object?)sourceUrl ?? DBNull.Value);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        private async Task<long> InsertLayerAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            int modelId,
            TransformerLayerData layerData)
        {
            var sql = @"
                INSERT INTO dbo.TransformerLayers (
                    model_id, layer_idx, layer_type,
                    num_heads, head_dim,
                    avg_activation, sparsity_ratio
                ) VALUES (
                    @model_id, @layer_idx, @layer_type,
                    @num_heads, @head_dim,
                    @avg_activation, @sparsity_ratio
                );
                SELECT SCOPE_IDENTITY();
            ";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.Parameters.AddWithValue("@model_id", modelId);
            cmd.Parameters.AddWithValue("@layer_idx", layerData.LayerIdx);
            cmd.Parameters.AddWithValue("@layer_type", layerData.LayerType);
            cmd.Parameters.AddWithValue("@num_heads", (object?)layerData.NumHeads ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@head_dim", (object?)layerData.HeadDim ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@avg_activation", (object?)layerData.AvgActivation ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@sparsity_ratio", (object?)layerData.SparsityRatio ?? DBNull.Value);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt64(result);
        }

        private async Task InsertAttentionWeightsAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            long layerId,
            AttentionHeadData headData)
        {
            var sql = @"
                INSERT INTO dbo.AttentionWeights (
                    layer_id, head_idx, weight_type,
                    weight_vector, importance_score
                ) VALUES (
                    @layer_id, @head_idx, @weight_type,
                    CAST(@weight_vector AS VECTOR(768)), @importance_score
                );
            ";

            using var cmd = new SqlCommand(sql, connection, transaction);
            cmd.Parameters.AddWithValue("@layer_id", layerId);
            cmd.Parameters.AddWithValue("@head_idx", headData.HeadIdx);
            cmd.Parameters.AddWithValue("@weight_type", headData.WeightType);
            cmd.Parameters.AddWithValue("@weight_vector", FormatVectorString(headData.WeightVector));
            cmd.Parameters.AddWithValue("@importance_score", headData.ImportanceScore);

            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Format float array as SQL Server VECTOR string: '[1.0, 2.0, 3.0]'
        /// </summary>
        private string FormatVectorString(float[] vector)
        {
            return "[" + string.Join(",", vector.Select(v => v.ToString("G"))) + "]";
        }

        /// <summary>
        /// Calculate importance score for a weight vector (e.g., L2 norm)
        /// </summary>
        public static float CalculateImportanceScore(float[] weights)
        {
            double sumSquares = weights.Select(w => w * w).Sum();
            return (float)Math.Sqrt(sumSquares);
        }
    }

    public class TransformerLayerData
    {
        public int LayerIdx { get; set; }
        public string LayerType { get; set; } = ""; // 'embedding', 'attention', 'feedforward'
        public int? NumHeads { get; set; }
        public int? HeadDim { get; set; }
        public float? AvgActivation { get; set; }
        public float? SparsityRatio { get; set; }
        public List<AttentionHeadData> AttentionHeads { get; set; } = new();
    }

    public class AttentionHeadData
    {
        public int HeadIdx { get; set; }
        public string WeightType { get; set; } = ""; // 'Q', 'K', 'V', 'O'
        public float[] WeightVector { get; set; } = Array.Empty<float>();
        public float ImportanceScore { get; set; }
    }
}
