using Dapper;
using Hartonomous.Api.DTOs.Inference;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Hartonomous.Api.Services
{
    /// <summary>
    /// Service for direct, synchronous execution of inference models in the database.
    /// </summary>
    public class InferenceExecutionService
    {
        private readonly string _connectionString;
        private readonly ILogger<InferenceExecutionService> _logger;

        public InferenceExecutionService(IConfiguration configuration, ILogger<InferenceExecutionService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        /// <summary>
        /// Executes the in-database inference function synchronously.
        /// </summary>
        public async Task<RunInferenceResponse> RunInferenceAsync(RunInferenceRequest request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Running synchronous inference for ModelId {ModelId}", request.ModelId);

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync(cancellationToken);

                var parameters = new
                {
                    modelId = request.ModelId,
                    tokenIdsJson = JsonConvert.SerializeObject(request.TokenIds)
                };

                // Execute the SQL CLR function
                var resultJson = await connection.ExecuteScalarAsync<string>(
                    "SELECT dbo.clr_RunInference(@modelId, @tokenIdsJson)",
                    parameters);

                if (string.IsNullOrEmpty(resultJson))
                {
                    _logger.LogWarning("In-database inference returned null or empty for ModelId {ModelId}", request.ModelId);
                    return null;
                }

                // Check for errors returned from the CLR function
                var errorCheck = JsonConvert.DeserializeObject<JsonError>(resultJson);
                if (errorCheck?.Error != null)
                {
                    _logger.LogError("In-database inference failed for ModelId {ModelId}: {Error}", request.ModelId, errorCheck.Error);
                    throw new InvalidOperationException($"In-database inference failed: {errorCheck.Error}");
                }

                var embedding = JsonConvert.DeserializeObject<float[]>(resultJson);

                return new RunInferenceResponse { Embedding = embedding };
            }
        }

        private class JsonError
        {
            public string Error { get; set; }
        }
    }
}
