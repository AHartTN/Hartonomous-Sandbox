using SqlClrFunctions.Contracts;
using System;
using System.Data.SqlClient;
using System.Data.SqlTypes;

namespace SqlClrFunctions.TensorOperations
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
        public byte[] LoadWeights(string tensorName, int expectedSizeInBytes)
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
                    // This query needs to be adapted to the actual schema for mapping a tensor name to a TensorAtomId.
                    // For now, we assume a direct mapping via ModelLayers and a one-to-one link in TensorAtomCoefficient.
                    var query = @"
                        SELECT TOP 1 tac.TensorAtomId
                        FROM dbo.ModelLayers ml
                        JOIN dbo.TensorAtomCoefficient tac ON ml.LayerId = tac.ParentLayerId
                        WHERE ml.ModelId = @modelId AND ml.LayerName = @tensorName
                        ORDER BY tac.Coefficient DESC; -- Assuming the main weight has the highest coefficient
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

                return payload.Value;
            }
            catch (Exception)
            {
                // In a production system, log this error. For now, we return null on failure.
                return null;
            }
        }
    }
}
