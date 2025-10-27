using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace ModelIngestion
{
    public class ModelRepository
    {
        private readonly string _connectionString;

        public ModelRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task SaveModelAsync(Model model)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // 1. Save the model
                var modelId = await SaveModelInfoAsync(connection, model);

                // 2. Save the layers
                foreach (var layer in model.Layers)
                {
                    await SaveLayerAsync(connection, modelId, layer);
                }
            }
        }

        private async Task<int> SaveModelInfoAsync(SqlConnection connection, Model model)
        {
            var command = new SqlCommand("INSERT INTO dbo.Models (model_name, model_type, architecture) VALUES (@model_name, @model_type, @architecture); SELECT SCOPE_IDENTITY();", connection);
            command.Parameters.AddWithValue("@model_name", model.Name);
            command.Parameters.AddWithValue("@model_type", model.Type);
            command.Parameters.AddWithValue("@architecture", model.Architecture);

            var result = await command.ExecuteScalarAsync();
            return result != null ? (int)(decimal)result : 0;
        }

        private async Task SaveLayerAsync(SqlConnection connection, int modelId, Layer layer)
        {
            var command = new SqlCommand("INSERT INTO dbo.ModelLayers (model_id, layer_idx, layer_name, layer_type, weights) VALUES (@model_id, @layer_idx, @layer_name, @layer_type, @weights);", connection);
            command.Parameters.AddWithValue("@model_id", modelId);
            command.Parameters.AddWithValue("@layer_idx", layer.layer_idx);
            command.Parameters.AddWithValue("@layer_name", layer.Name);
            command.Parameters.AddWithValue("@layer_type", layer.Type);
            command.Parameters.AddWithValue("@weights", layer.Weights);

            await command.ExecuteNonQueryAsync();
        }
    }
}
