using Microsoft.SqlServer.Server;
using Hartonomous.Clr.Contracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.SqlTypes;

namespace Hartonomous.Clr.TensorOperations
{
    /// <summary>
    /// An implementation of ITensorProvider that loads tensor data from the
    /// atomized storage within the SQL Server database.
    /// PHASE 3 OPTIMIZATION: Static cache for hot model weights in RAM
    /// </summary>
    public class ClrTensorProvider : ITensorProvider
    {
        private readonly int _modelId;

        // PHASE 3: Static weight cache (shared across all instances for same AppDomain)
        private static readonly ConcurrentDictionary<string, float[]> _weightCache 
            = new ConcurrentDictionary<string, float[]>();

        public ClrTensorProvider(int modelId)
        {
            _modelId = modelId;
        }

        /// <summary>
        /// Loads tensor weights by querying the database for the corresponding tensor atom
        /// and retrieving its payload from FILESTREAM storage.
        /// PHASE 3: Implements static caching to avoid repeated FILESTREAM reads
        /// </summary>
        public float[]? LoadWeights(string tensorName, int maxElements)
        {
            try
            {
                // PHASE 3: Check cache first
                string cacheKey = $"{_modelId}:{tensorName}";
                if (_weightCache.TryGetValue(cacheKey, out float[]? cachedWeights))
                {
                    // Cache hit - return immediately
                    return cachedWeights;
                }

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

                // PHASE 3: Store in cache for future requests
                _weightCache.TryAdd(cacheKey, floats);

                return floats;
            }
            catch (Exception ex)
            {
                // Log error via SQL Server context and return null to allow graceful degradation
                SqlContext.Pipe?.Send($"Error loading tensor {tensorName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// PHASE 3: Cache management - clear stale entries
        /// </summary>
        public static void ClearCache()
        {
            _weightCache.Clear();
        }

        /// <summary>
        /// PHASE 3: Cache statistics
        /// </summary>
        public static int GetCacheSize()
        {
            return _weightCache.Count;
        }

        /// <summary>
        /// Loads multiple tensors in a single batch operation.
        /// </summary>
        public System.Collections.Generic.Dictionary<string, float[]> LoadWeightsBatch(System.Collections.Generic.Dictionary<string, string> tensorPatterns)
        {
            var results = new System.Collections.Generic.Dictionary<string, float[]>();

            foreach (var kvp in tensorPatterns)
            {
                var weights = LoadWeights(kvp.Value, int.MaxValue);
                if (weights != null)
                {
                    results[kvp.Key] = weights;
                }
            }

            return results;
        }

        /// <summary>
        /// Gets metadata for a tensor without loading the full weights.
        /// </summary>
        public Hartonomous.Clr.Core.TensorMetadata GetMetadata(string tensorNamePattern)
        {
            try
            {
                using (var conn = new SqlConnection("context connection=true"))
                {
                    conn.Open();

                    var query = @"
                        SELECT TOP 1 ta.TensorName, ta.TensorShape, ta.DataType, ta.ElementCount, ta.ByteSize
                        FROM dbo.TensorAtoms ta
                        WHERE ta.TensorName LIKE '%' + @pattern + '%'
                        ORDER BY ta.ElementCount DESC;";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@pattern", tensorNamePattern);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new Hartonomous.Clr.Core.TensorMetadata
                                {
                                    TensorName = reader.GetString(0),
                                    TensorShape = reader.GetString(1),
                                    DataType = reader.GetString(2),
                                    ElementCount = reader.GetInt64(3),
                                    ByteSize = reader.GetInt64(4)
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SqlContext.Pipe?.Send($"Error getting tensor metadata: {ex.Message}");
            }

            throw new ArgumentException($"Tensor not found matching pattern: {tensorNamePattern}");
        }
    }
}
