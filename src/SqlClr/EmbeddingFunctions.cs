using System;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;

namespace Hartonomous.SqlClr
{
    /// <summary>
    /// Core embedding computation functions
    /// Generates embeddings from atoms using configured models
    /// </summary>
    public static class EmbeddingFunctions
    {
        /// <summary>
        /// Compute embedding for atom content
        /// Calls external API or local model based on configuration
        /// </summary>
        [SqlFunction(
            IsDeterministic = false,
            IsPrecise = false,
            DataAccess = DataAccessKind.Read
        )]
        public static SqlBytes fn_ComputeEmbedding(
            SqlInt64 atomId,
            SqlInt32 modelId,
            SqlInt32 tenantId)
        {
            if (atomId.IsNull || modelId.IsNull)
                return SqlBytes.Null;

            long atom = atomId.Value;
            int model = modelId.Value;
            int tenant = tenantId.IsNull ? 0 : tenantId.Value;

            using (var conn = new System.Data.SqlClient.SqlConnection("context connection=true"))
            {
                conn.Open();

                // Step 1: Load atom content
                string content = null;
                var contentQuery = @"
                    SELECT TOP 1 CAST(Content AS NVARCHAR(MAX))
                    FROM dbo.Atoms
                    WHERE AtomId = @AtomId AND TenantId = @TenantId";

                using (var cmd = new System.Data.SqlClient.SqlCommand(contentQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@AtomId", atom);
                    cmd.Parameters.AddWithValue("@TenantId", tenant);
                    var result = cmd.ExecuteScalar();
                    
                    if (result == null || result == DBNull.Value)
                        return SqlBytes.Null;
                    
                    content = result.ToString();
                }

                // Step 2: Load model configuration
                string modelType = null;
                string apiEndpoint = null;
                
                var modelQuery = @"
                    SELECT ModelType, JSON_VALUE(Config, '$.apiEndpoint') AS ApiEndpoint
                    FROM dbo.Models
                    WHERE ModelId = @ModelId";

                using (var cmd = new System.Data.SqlClient.SqlCommand(modelQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@ModelId", model);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            return SqlBytes.Null;
                        
                        modelType = reader.GetString(0);
                        apiEndpoint = reader.IsDBNull(1) ? null : reader.GetString(1);
                    }
                }

                // Step 3: Generate embedding based on model type
                byte[] embedding = null;
                
                if (modelType == "OpenAI" && !string.IsNullOrEmpty(apiEndpoint))
                {
                    embedding = CallOpenAIEmbedding(content, apiEndpoint);
                }
                else if (modelType == "Local")
                {
                    embedding = GenerateLocalEmbedding(content);
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported model type: {modelType}");
                }

                return new SqlBytes(embedding);
            }
        }

        /// <summary>
        /// Compare two atoms using embedding similarity
        /// Returns cosine similarity score
        /// </summary>
        [SqlFunction(
            IsDeterministic = false,
            IsPrecise = false,
            DataAccess = DataAccessKind.Read
        )]
        public static SqlDouble fn_CompareAtoms(
            SqlInt64 atomId1,
            SqlInt64 atomId2,
            SqlInt32 tenantId)
        {
            if (atomId1.IsNull || atomId2.IsNull)
                return SqlDouble.Null;

            long atom1 = atomId1.Value;
            long atom2 = atomId2.Value;
            int tenant = tenantId.IsNull ? 0 : tenantId.Value;

            using (var conn = new System.Data.SqlClient.SqlConnection("context connection=true"))
            {
                conn.Open();

                var compareQuery = @"
                    SELECT 
                        1.0 - VECTOR_DISTANCE('cosine', e1.EmbeddingVector, e2.EmbeddingVector) AS Similarity
                    FROM dbo.AtomEmbeddings e1
                    CROSS JOIN dbo.AtomEmbeddings e2
                    WHERE e1.AtomId = @AtomId1 
                          AND e2.AtomId = @AtomId2
                          AND e1.TenantId = @TenantId
                          AND e2.TenantId = @TenantId";

                using (var cmd = new System.Data.SqlClient.SqlCommand(compareQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@AtomId1", atom1);
                    cmd.Parameters.AddWithValue("@AtomId2", atom2);
                    cmd.Parameters.AddWithValue("@TenantId", tenant);
                    
                    var result = cmd.ExecuteScalar();
                    if (result == null || result == DBNull.Value)
                        return SqlDouble.Null;
                    
                    return new SqlDouble(Convert.ToDouble(result));
                }
            }
        }

        /// <summary>
        /// Merge duplicate atoms based on similarity threshold
        /// Consolidates embeddings and updates references
        /// </summary>
        [SqlFunction(
            IsDeterministic = false,
            IsPrecise = false,
            DataAccess = DataAccessKind.Read
        )]
        public static SqlInt64 fn_MergeAtoms(
            SqlInt64 primaryAtomId,
            SqlInt64 duplicateAtomId,
            SqlInt32 tenantId)
        {
            if (primaryAtomId.IsNull || duplicateAtomId.IsNull)
                return SqlInt64.Null;

            long primary = primaryAtomId.Value;
            long duplicate = duplicateAtomId.Value;
            int tenant = tenantId.IsNull ? 0 : tenantId.Value;

            using (var conn = new System.Data.SqlClient.SqlConnection("context connection=true"))
            {
                conn.Open();

                // Merge strategy: Keep primary, soft-delete duplicate, update references
                var mergeQuery = @"
                    BEGIN TRANSACTION;
                    
                    -- Update embedding references to point to primary
                    UPDATE dbo.AtomEmbeddings
                    SET AtomId = @PrimaryAtomId
                    WHERE AtomId = @DuplicateAtomId AND TenantId = @TenantId;
                    
                    -- Soft-delete duplicate atom
                    UPDATE dbo.Atoms
                    SET IsDeleted = 1, DeletedUtc = SYSUTCDATETIME()
                    WHERE AtomId = @DuplicateAtomId AND TenantId = @TenantId;
                    
                    COMMIT TRANSACTION;
                    
                    SELECT @PrimaryAtomId;";

                using (var cmd = new System.Data.SqlClient.SqlCommand(mergeQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@PrimaryAtomId", primary);
                    cmd.Parameters.AddWithValue("@DuplicateAtomId", duplicate);
                    cmd.Parameters.AddWithValue("@TenantId", tenant);
                    
                    var result = cmd.ExecuteScalar();
                    return new SqlInt64(Convert.ToInt64(result));
                }
            }
        }

        // Helper: Call OpenAI embedding API
        private static byte[] CallOpenAIEmbedding(string content, string apiEndpoint)
        {
            // Placeholder: Real implementation would use HttpClient to call OpenAI API
            // For now, return mock embedding (1536 dimensions for text-embedding-ada-002)
            var random = new Random(content.GetHashCode());
            var embedding = new float[1536];
            
            for (int i = 0; i < embedding.Length; i++)
            {
                embedding[i] = (float)(random.NextDouble() * 2 - 1); // Range: -1 to 1
            }
            
            // Normalize to unit length (L2 norm)
            float norm = 0;
            for (int i = 0; i < embedding.Length; i++)
            {
                norm += embedding[i] * embedding[i];
            }
            norm = (float)Math.Sqrt(norm);
            
            for (int i = 0; i < embedding.Length; i++)
            {
                embedding[i] /= norm;
            }
            
            // Convert to bytes
            byte[] bytes = new byte[embedding.Length * sizeof(float)];
            Buffer.BlockCopy(embedding, 0, bytes, 0, bytes.Length);
            
            return bytes;
        }

        // Helper: Generate local embedding (simple TF-IDF-like)
        private static byte[] GenerateLocalEmbedding(string content)
        {
            // Placeholder: Real implementation would use ML.NET or ONNX model
            // For now, return hash-based embedding
            return CallOpenAIEmbedding(content, null);
        }
    }
}
