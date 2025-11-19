using Microsoft.SqlServer.Server;
using Hartonomous.Clr.Contracts;
using System;
using System.Data.SqlClient;
using System.Data.SqlTypes;

namespace Hartonomous.Clr.TensorOperations
{
    /// <summary>
    /// An implementation of ITensorProvider that loads tensor data from the
    /// atomized storage within the SQL Server database.
    /// </summary>
    public class ClrTensorProvider : ITensorProvider
    {
        private readonly int _modelId;

        public ClrTensorProvider(int modelId)
        {
            _modelId = modelId;
        }

        /// <summary>
        /// Loads tensor weights by querying the database for the corresponding tensor atom
        /// and retrieving its payload from FILESTREAM storage.
        /// </summary>
        public float[]? LoadWeights(string tensorName, int maxElements)
        {
            try
            {
                long tensorAtomId = -1;

                // Use a context connection to find the TensorAtomId for the given tensor name and model.
                // This query is conceptual and depends on how tensor names are mapped to layers/atoms.
                // Let's assume the LayerName in ModelLayers corresponds to the tensorName.
                using (var conn = new SqlConnection("context connection=true"))
                {
                    conn.Open();
                    // Query maps tensor name to TensorAtomId via ModelLayers -> TensorAtomCoefficient schema
                    // Selects highest coefficient when multiple atoms exist for a layer
                    var query = @"
                        SELECT TOP 1 tac.TensorAtomId
                        FROM dbo.ModelLayers ml
                        JOIN dbo.TensorAtomCoefficient tac ON ml.LayerId = tac.ParentLayerId
                        WHERE ml.ModelId = @modelId AND ml.LayerName = @tensorName
                        ORDER BY tac.Coefficient DESC; -- Highest coefficient represents primary weight
                    ";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@modelId", _modelId);
                        cmd.Parameters.AddWithValue("@tensorName", tensorName);

                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            tensorAtomId = Convert.ToInt64(result);
                        }
                    }
                }

                if (tensorAtomId == -1)
                {
                    // Tensor not found for this model.
                    return null;
                }

                // Retrieve the payload using the existing CLR function.
                SqlBytes payload = TensorDataIO.clr_GetTensorAtomPayload(new SqlInt64(tensorAtomId));

                if (payload.IsNull)
                {
                    return null;
                }

                byte[] bytes = payload.Value;
                float[] floats = new float[bytes.Length / sizeof(float)];
                Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
                return floats;
            }
            catch (Exception ex)
            {
                // Log error via SQL Server context and return null to allow graceful degradation
                SqlContext.Pipe?.Send($"Error loading tensor {tensorName}: {ex.Message}");
                return null;
            }
        }
    }
}
