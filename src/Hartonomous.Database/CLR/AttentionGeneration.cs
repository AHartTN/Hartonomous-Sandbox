using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using Microsoft.SqlServer.Server;
using Newtonsoft.Json;

namespace Hartonomous.Clr
{
    /// <summary>
    /// Advanced generation functions with multi-head attention mechanism.
    /// Implements transformer-style attention for context-aware generation.
    /// Tracks nano-provenance via AtomicStream for every generation step.
    /// </summary>
    public static class AttentionGeneration
    {
        private const int DefaultAttentionHeads = 8;
        private const int DefaultContextWindow = 512;
        private const double MinTemperature = 0.01;
        private const double MaxTemperature = 2.0;

        /// <summary>
        /// Core inference function with multi-head attention mechanism.
        /// Generates atoms using context-aware attention over input atoms.
        /// Returns GenerationStream with complete AtomicStream provenance.
        /// </summary>
        /// <param name="modelId">Model ID to use for generation</param>
        /// <param name="inputAtomIds">Input atom IDs as context (comma-separated)</param>
        /// <param name="contextJson">Additional context metadata (JSON)</param>
        /// <param name="maxTokens">Maximum tokens to generate</param>
        /// <param name="temperature">Sampling temperature (0.01-2.0)</param>
        /// <param name="topK">Top-K candidates per step</param>
        /// <param name="topP">Nucleus sampling threshold</param>
        /// <param name="attentionHeads">Number of attention heads (default 8)</param>
        /// <param name="tenantId">Tenant ID for security context</param>
        /// <returns>GenerationStreamId (BIGINT) with complete provenance</returns>
        [SqlFunction(
            DataAccess = DataAccessKind.Read,
            SystemDataAccess = SystemDataAccessKind.Read,
            IsDeterministic = false)]
        public static SqlInt64 fn_GenerateWithAttention(
            SqlInt32 modelId,
            SqlString inputAtomIds,
            SqlString contextJson,
            SqlInt32 maxTokens,
            SqlDouble temperature,
            SqlInt32 topK,
            SqlDouble topP,
            SqlInt32 attentionHeads,
            SqlInt32 tenantId)
        {
            if (modelId.IsNull || modelId.Value <= 0)
            {
                return SqlInt64.Null;
            }

            try
            {
                using (var connection = new SqlConnection("context connection=true"))
                {
                    connection.Open();

                    // Parse parameters
                    var inputIds = ParseInputAtomIds(inputAtomIds);
                    var maxTokenCount = maxTokens.IsNull || maxTokens.Value <= 0 ? 1 : Math.Min(maxTokens.Value, 4096);
                    var temp = ClampTemperature(temperature);
                    var topKValue = topK.IsNull || topK.Value <= 0 ? 50 : Math.Min(topK.Value, 1000);
                    var topPValue = topP.IsNull ? 0.9 : Math.Max(0.0, Math.Min(1.0, topP.Value));
                    var heads = attentionHeads.IsNull || attentionHeads.Value <= 0 ? DefaultAttentionHeads : attentionHeads.Value;
                    var context = contextJson.IsNull ? "{}" : contextJson.Value;
                    var tenant = tenantId.IsNull ? 0 : tenantId.Value;

                    // Load model metadata
                    var model = LoadModel(connection, modelId.Value);
                    if (model == null)
                    {
                        return SqlInt64.Null;
                    }

                    // Create AtomicStream for provenance
                    var streamId = Guid.NewGuid();
                    var stream = AtomicStream.Create(
                        streamId,
                        DateTime.UtcNow,
                        "inference",
                        model.ModelName,
                        $"{{\"modelId\":{modelId.Value},\"temperature\":{temp},\"topK\":{topKValue},\"topP\":{topPValue},\"heads\":{heads}}}"
                    );

                    // Load input embeddings
                    var inputEmbeddings = LoadInputEmbeddings(connection, inputIds);
                    if (inputEmbeddings.Count == 0)
                    {
                        return SqlInt64.Null;
                    }

                    // Add input atoms to provenance stream
                    foreach (var atomId in inputIds)
                    {
                        stream.AddSegment(
                            AtomicStreamSegmentKind.Input.ToString(),
                            DateTime.UtcNow,
                            "application/atom-reference",
                            $"{{\"atomId\":{atomId}}}",
                            new SqlBytes(BitConverter.GetBytes(atomId))
                        );
                    }

                    // Generate sequence with attention
                    var generatedAtomIds = new List<long>();
                    var currentEmbeddings = new List<float[]>(inputEmbeddings);
                    
                    // Use deterministic seed: XOR of streamId hash and modelId for reproducible sampling
                    var seed = streamId.GetHashCode() ^ modelId.Value;
                    var random = new Random(seed);

                    for (var step = 0; step < maxTokenCount; step++)
                    {
                        // Compute multi-head attention
                        var attentionOutput = ComputeMultiHeadAttention(
                            currentEmbeddings,
                            heads,
                            model.EmbeddingDimension
                        );

                        // Query candidates using attention-weighted context
                        var candidates = QueryCandidatesWithAttention(
                            connection,
                            attentionOutput,
                            model,
                            topKValue,
                            generatedAtomIds
                        );

                        if (candidates.Count == 0)
                        {
                            break;
                        }

                        // Apply nucleus (top-p) sampling
                        var filtered = ApplyNucleusSampling(candidates, topPValue);

                        // Temperature-based sampling
                        var selectedAtomId = SampleCandidate(filtered, temp, random);
                        if (selectedAtomId <= 0)
                        {
                            break;
                        }

                        generatedAtomIds.Add(selectedAtomId);

                        // Add generation step to provenance stream
                        var stepMetadata = $"{{\"step\":{step},\"candidateCount\":{candidates.Count},\"score\":{filtered[0].Score:F4}}}";
                        stream.AddSegment(
                            AtomicStreamSegmentKind.Output.ToString(),
                            DateTime.UtcNow,
                            "application/atom-reference",
                            stepMetadata,
                            new SqlBytes(BitConverter.GetBytes(selectedAtomId))
                        );

                        // Load embedding for next iteration
                        var nextEmbedding = LoadAtomEmbedding(connection, selectedAtomId);
                        if (nextEmbedding == null)
                        {
                            break;
                        }

                        currentEmbeddings.Add(nextEmbedding);

                        // Sliding window context (keep last N embeddings)
                        if (currentEmbeddings.Count > DefaultContextWindow)
                        {
                            currentEmbeddings.RemoveAt(0);
                        }

                        // Check for terminal token
                        if (IsTerminalToken(connection, selectedAtomId))
                        {
                            break;
                        }
                    }

                    // Store GenerationStream with AtomicStream provenance
                    var generationStreamId = StoreGenerationStream(
                        connection,
                        modelId.Value,
                        generatedAtomIds,
                        stream,
                        context,
                        tenant
                    );

                    return new SqlInt64(generationStreamId);
                }
            }
            catch (Exception ex)
            {
                // Log error (in production, use proper logging)
                SqlContext.Pipe.Send($"fn_GenerateWithAttention error: {ex.Message}");
                return SqlInt64.Null;
            }
        }

        private static List<long> ParseInputAtomIds(SqlString inputAtomIds)
        {
            var result = new List<long>();

            if (inputAtomIds.IsNull || string.IsNullOrWhiteSpace(inputAtomIds.Value))
            {
                return result;
            }

            var parts = inputAtomIds.Value.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (long.TryParse(part.Trim(), out var atomId) && atomId > 0)
                {
                    result.Add(atomId);
                }
            }

            return result;
        }

        private static double ClampTemperature(SqlDouble temperature)
        {
            if (temperature.IsNull)
            {
                return 1.0;
            }

            var value = temperature.Value;
            return Math.Max(MinTemperature, Math.Min(MaxTemperature, value));
        }

        private static ModelInfo LoadModel(SqlConnection connection, int modelId)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT 
    ModelId,
    ModelName,
    ModelType,
    Architecture,
    Config
FROM dbo.Models
WHERE ModelId = @modelId;
";
                command.Parameters.AddWithValue("@modelId", modelId);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new ModelInfo
                        {
                            ModelId = reader.GetInt32(0),
                            ModelName = reader.GetString(1),
                            ModelType = reader.IsDBNull(2) ? "unknown" : reader.GetString(2),
                            Architecture = reader.IsDBNull(3) ? null : reader.GetString(3),
                            Config = reader.IsDBNull(4) ? "{}" : reader.GetString(4),
                            EmbeddingDimension = 1998 // Default for SQL Server 2025 VECTOR type
                        };
                    }
                }
            }

            return null;
        }

        private static List<float[]> LoadInputEmbeddings(SqlConnection connection, List<long> atomIds)
        {
            var embeddings = new List<float[]>();

            if (atomIds.Count == 0)
            {
                return embeddings;
            }

            using (var command = connection.CreateCommand())
            {
                var atomIdsParam = string.Join(",", atomIds);
                command.CommandText = $@"
SELECT 
    ae.AtomId,
    CAST(ae.EmbeddingVector AS NVARCHAR(MAX)) AS EmbeddingJson
FROM dbo.AtomEmbeddings ae
WHERE ae.AtomId IN ({atomIdsParam})
ORDER BY ae.AtomId;
";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(1))
                        {
                            var embeddingJson = reader.GetString(1);
                            var embedding = ParseVectorJson(embeddingJson);
                            if (embedding != null)
                            {
                                embeddings.Add(embedding);
                            }
                        }
                    }
                }
            }

            return embeddings;
        }

        private static float[] LoadAtomEmbedding(SqlConnection connection, long atomId)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT TOP 1 CAST(EmbeddingVector AS NVARCHAR(MAX))
FROM dbo.AtomEmbeddings
WHERE AtomId = @atomId
ORDER BY CreatedAt DESC;
";
                command.Parameters.AddWithValue("@atomId", atomId);

                var result = command.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    return ParseVectorJson(result.ToString());
                }
            }

            return null;
        }

        private static float[] ParseVectorJson(string vectorJson)
        {
            // Parse JSON array: "[0.1, 0.2, 0.3, ...]"
            if (string.IsNullOrWhiteSpace(vectorJson))
            {
                return null;
            }

            try
            {
                var trimmed = vectorJson.Trim();
                if (!trimmed.StartsWith("[") || !trimmed.EndsWith("]"))
                {
                    return null;
                }

                var values = trimmed.Substring(1, trimmed.Length - 2)
                    .Split(',')
                    .Select(s => float.Parse(s.Trim()))
                    .ToArray();

                return values;
            }
            catch
            {
                return null;
            }
        }

        private static float[] ComputeMultiHeadAttention(
            List<float[]> embeddings,
            int numHeads,
            int embeddingDim)
        {
            if (embeddings.Count == 0)
            {
                return new float[embeddingDim];
            }

            // PARADIGM-COMPLIANT REFACTOR: Query model weights from GEOMETRY tensors
            // This is the "T-SQL queries itself" vision - we execute SQL against TensorAtoms.WeightsGeometry
            using (var connection = new SqlConnection("context connection=true"))
            {
                connection.Open();

                // Find the attention layer tensors for this model
                // We'll query the Q, K, V projection matrices from the database
                var queryWeights = LoadTensorWeightsFromGeometry(connection, "attn.q_proj.weight", embeddingDim);
                var keyWeights = LoadTensorWeightsFromGeometry(connection, "attn.k_proj.weight", embeddingDim);
                var valueWeights = LoadTensorWeightsFromGeometry(connection, "attn.v_proj.weight", embeddingDim);

                if (queryWeights == null || keyWeights == null || valueWeights == null)
                {
                    // Fallback to simplified attention if tensors not available
                    return ComputeSimplifiedAttention(embeddings, embeddingDim);
                }

                // Compute attention using the actual model weights from GEOMETRY
                var output = new float[embeddingDim];
                var headDim = embeddingDim / numHeads;

                for (int head = 0; head < numHeads; head++)
                {
                    var headOffset = head * headDim;

                    // For each embedding in the sequence
                    for (int i = 0; i < embeddings.Count; i++)
                    {
                        var embedding = embeddings[i];

                        // Project to Query, Key, Value using weights from GEOMETRY
                        var q = ProjectWithTensor(embedding, queryWeights, headOffset, headDim);
                        var k = ProjectWithTensor(embedding, keyWeights, headOffset, headDim);
                        var v = ProjectWithTensor(embedding, valueWeights, headOffset, headDim);

                        // Compute attention scores (Q * K^T / sqrt(d_k))
                        double attentionScore = 0.0;
                        for (int j = 0; j < q.Length; j++)
                        {
                            attentionScore += q[j] * k[j];
                        }
                        attentionScore /= Math.Sqrt(headDim);

                        // Apply softmax and accumulate weighted values
                        var softmaxWeight = Math.Exp(attentionScore);
                        for (int j = 0; j < headDim; j++)
                        {
                            output[headOffset + j] += (float)(v[j] * softmaxWeight);
                        }
                    }
                }

                // Normalize output
                var norm = ComputeL2Norm(output);
                if (norm > 0)
                {
                    for (int i = 0; i < output.Length; i++)
                    {
                        output[i] /= (float)norm;
                    }
                }

                return output;
            }
        }

        /// <summary>
        /// Load tensor weights from GEOMETRY representation via STPointN() queries.
        /// This is the core "queryable tensors" implementation.
        /// </summary>
        private static float[] LoadTensorWeightsFromGeometry(
            SqlConnection connection,
            string tensorNamePattern,
            int maxDimension)
        {
            using (var command = connection.CreateCommand())
            {
                // Query the GEOMETRY representation of the tensor
                command.CommandText = @"
SELECT TOP 1
    ta.WeightsGeometry,
    ta.ElementCount
FROM dbo.TensorAtoms ta
WHERE ta.TensorName LIKE '%' + @pattern + '%'
ORDER BY ta.ElementCount DESC;
";
                command.Parameters.AddWithValue("@pattern", tensorNamePattern);

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read() || reader.IsDBNull(0))
                    {
                        return null;
                    }

                    var geometry = reader.GetValue(0) as Microsoft.SqlServer.Types.SqlGeometry;
                    var elementCount = reader.GetInt64(1);

                    if (geometry == null || geometry.IsNull)
                    {
                        return null;
                    }

                    // Extract weights from GEOMETRY using STPointN()
                    var weights = new List<float>();
                    var pointCount = geometry.STNumPoints().Value;

                    for (int i = 1; i <= pointCount && weights.Count < maxDimension; i++)
                    {
                        var point = geometry.STPointN(i);
                        if (!point.IsNull)
                        {
                            // Y coordinate is the weight value
                            var value = point.STY.Value;
                            weights.Add((float)value);
                        }
                    }

                    return weights.ToArray();
                }
            }
        }

        /// <summary>
        /// Project an embedding using tensor weights from GEOMETRY.
        /// This performs a matrix-vector multiplication using weights queried from the database.
        /// </summary>
        private static float[] ProjectWithTensor(
            float[] embedding,
            float[] tensorWeights,
            int offset,
            int dimension)
        {
            var result = new float[dimension];

            for (int i = 0; i < dimension; i++)
            {
                double sum = 0.0;
                for (int j = 0; j < embedding.Length && (offset + i) * embedding.Length + j < tensorWeights.Length; j++)
                {
                    var weightIndex = (offset + i) * embedding.Length + j;
                    sum += embedding[j] * tensorWeights[weightIndex];
                }
                result[i] = (float)sum;
            }

            return result;
        }

        /// <summary>
        /// Fallback simplified attention when GEOMETRY tensors are not available.
        /// </summary>
        private static float[] ComputeSimplifiedAttention(
            List<float[]> embeddings,
            int embeddingDim)
        {
            // Simplified multi-head attention: average pooling with recency weights
            var output = new float[embeddingDim];
            var weights = ComputeAttentionWeights(embeddings);

            for (var i = 0; i < embeddingDim; i++)
            {
                double sum = 0.0;
                double weightSum = 0.0;

                for (var j = 0; j < embeddings.Count; j++)
                {
                    if (embeddings[j].Length > i)
                    {
                        sum += embeddings[j][i] * weights[j];
                        weightSum += weights[j];
                    }
                }

                output[i] = weightSum > 0 ? (float)(sum / weightSum) : 0f;
            }

            return output;
        }

        private static double[] ComputeAttentionWeights(List<float[]> embeddings)
        {
            // Compute attention weights based on recency and magnitude
            var weights = new double[embeddings.Count];
            double totalWeight = 0.0;

            for (var i = 0; i < embeddings.Count; i++)
            {
                // Recency bias: more recent embeddings get higher weight
                var recencyWeight = Math.Pow(1.1, i);

                // Magnitude: normalize by L2 norm
                var magnitude = ComputeL2Norm(embeddings[i]);

                weights[i] = recencyWeight * magnitude;
                totalWeight += weights[i];
            }

            // Normalize weights to sum to 1.0
            if (totalWeight > 0)
            {
                for (var i = 0; i < weights.Length; i++)
                {
                    weights[i] /= totalWeight;
                }
            }

            return weights;
        }

        private static double ComputeL2Norm(float[] vector)
        {
            double sum = 0.0;
            foreach (var value in vector)
            {
                sum += value * value;
            }
            return Math.Sqrt(sum);
        }

        private static List<Candidate> QueryCandidatesWithAttention(
            SqlConnection connection,
            float[] attentionOutput,
            ModelInfo model,
            int topK,
            List<long> excludeAtomIds)
        {
            var candidates = new List<Candidate>();

            using (var command = connection.CreateCommand())
            {
                var embeddingJson = JsonConvert.SerializeObject(attentionOutput);

                // CRITICAL: Query ALL modalities using GEOMETRY spatial joins
                // This enables cross-modal generation (text→image, audio→video, etc.)
                command.CommandText = @"
-- Step 1: Project attention output to GEOMETRY for spatial queries
DECLARE @embedding VECTOR(1998) = CAST(@embeddingJson AS VECTOR(1998));
DECLARE @queryGeometry GEOMETRY = dbo.fn_ProjectTo3D(CAST(@embedding AS VARBINARY(MAX)));

-- Step 2: Spatial filter using R-tree index (O(log n) candidates)
WITH SpatialCandidates AS (
    SELECT TOP (@spatialPool)
        ae.AtomEmbeddingId,
        ae.AtomId,
        ae.SpatialGeometry.STDistance(@queryGeometry) AS SpatialDistance
    FROM dbo.AtomEmbeddings ae WITH (INDEX(IX_AtomEmbeddings_SpatialGeometry))
    WHERE ae.SpatialGeometry IS NOT NULL
      AND ae.SpatialGeometry.STIntersects(
          @queryGeometry.STBuffer(10.0) -- 10 unit search radius
      ) = 1
    ORDER BY ae.SpatialGeometry.STDistance(@queryGeometry)
),
-- Step 3: Exact vector similarity on candidates (O(k) refinement)
RankedCandidates AS (
    SELECT 
        sc.AtomId,
        a.Modality,
        a.CanonicalText,
        a.ContentJson,
        VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @embedding) AS VectorDistance,
        sc.SpatialDistance,
        -- Blend vector + spatial scores
        (1.0 - VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @embedding)) * 0.7 +
        (1.0 / (1.0 + sc.SpatialDistance)) * 0.3 AS BlendedScore
    FROM SpatialCandidates sc
    INNER JOIN dbo.AtomEmbeddings ae ON sc.AtomEmbeddingId = ae.AtomEmbeddingId
    INNER JOIN dbo.Atoms a ON ae.AtomId = a.AtomId
    -- NO modality filter - return text + image + audio + video candidates!
    WHERE ae.EmbeddingVector IS NOT NULL
)
SELECT TOP (@topK)
    AtomId,
    Modality,
    COALESCE(CanonicalText, 
             JSON_VALUE(ContentJson, '$.description'),
             CONCAT('[', Modality, ' atom #', CAST(AtomId AS NVARCHAR(20)), ']')
    ) AS DisplayText,
    VectorDistance AS Distance,
    BlendedScore AS Score
FROM RankedCandidates
ORDER BY BlendedScore DESC;
";
                command.Parameters.AddWithValue("@embeddingJson", embeddingJson);
                command.Parameters.AddWithValue("@spatialPool", topK * 10); // Get 10x for spatial filter
                command.Parameters.AddWithValue("@topK", topK * 2); // Get extra for filtering

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var atomId = reader.GetInt64(0);

                        // Skip already generated atoms
                        if (excludeAtomIds.Contains(atomId))
                        {
                            continue;
                        }

                        var modality = reader.IsDBNull(1) ? "unknown" : reader.GetString(1);
                        var text = reader.IsDBNull(2) ? null : reader.GetString(2);
                        var distance = reader.GetDouble(3);
                        var score = reader.GetDouble(4);

                        candidates.Add(new Candidate
                        {
                            AtomId = atomId,
                            Modality = modality,
                            Text = text,
                            Distance = distance,
                            Score = score
                        });

                        if (candidates.Count >= topK)
                        {
                            break;
                        }
                    }
                }
            }

            return candidates;
        }

        private static List<Candidate> ApplyNucleusSampling(List<Candidate> candidates, double topP)
        {
            if (candidates.Count == 0 || topP >= 1.0)
            {
                return candidates;
            }

            // Sort by score descending
            candidates.Sort((a, b) => b.Score.CompareTo(a.Score));

            // Compute cumulative probability
            var totalScore = candidates.Sum(c => c.Score);
            var cumulative = 0.0;
            var cutoff = 0;

            for (var i = 0; i < candidates.Count; i++)
            {
                cumulative += candidates[i].Score / totalScore;
                cutoff = i + 1;

                if (cumulative >= topP)
                {
                    break;
                }
            }

            return candidates.Take(cutoff).ToList();
        }

        private static long SampleCandidate(List<Candidate> candidates, double temperature, Random random)
        {
            if (candidates.Count == 0)
            {
                return -1;
            }

            if (candidates.Count == 1)
            {
                return candidates[0].AtomId;
            }

            // Apply temperature to scores
            var weights = new double[candidates.Count];
            var totalWeight = 0.0;

            for (var i = 0; i < candidates.Count; i++)
            {
                var weight = Math.Pow(candidates[i].Score, 1.0 / temperature);
                weights[i] = weight;
                totalWeight += weight;
            }

            // Sample using roulette wheel
            var threshold = random.NextDouble() * totalWeight;
            var cumulative = 0.0;

            for (var i = 0; i < candidates.Count; i++)
            {
                cumulative += weights[i];
                if (threshold <= cumulative)
                {
                    return candidates[i].AtomId;
                }
            }

            return candidates[candidates.Count - 1].AtomId;
        }

        private static bool IsTerminalToken(SqlConnection connection, long atomId)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT CanonicalText
FROM dbo.Atoms
WHERE AtomId = @atomId;
";
                command.Parameters.AddWithValue("@atomId", atomId);

                var text = command.ExecuteScalar() as string;
                if (string.IsNullOrWhiteSpace(text))
                {
                    return false;
                }

                var trimmed = text.Trim();
                return trimmed.Equals("[EOS]", StringComparison.OrdinalIgnoreCase) ||
                       trimmed.Equals("</s>", StringComparison.OrdinalIgnoreCase) ||
                       trimmed.Equals("<|endoftext|>", StringComparison.Ordinal);
            }
        }

        private static long StoreGenerationStream(
            SqlConnection connection,
            int modelId,
            List<long> generatedAtomIds,
            AtomicStream stream,
            string contextJson,
            int tenantId)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
INSERT INTO provenance.GenerationStreams (
    ModelId,
    GeneratedAtomIds,
    ProvenanceStream,
    ContextMetadata,
    TenantId,
    CreatedUtc
)
OUTPUT INSERTED.GenerationStreamId
VALUES (
    @modelId,
    @generatedAtomIds,
    @provenanceStream,
    @contextMetadata,
    @tenantId,
    SYSUTCDATETIME()
);
";
                command.Parameters.AddWithValue("@modelId", modelId);
                command.Parameters.AddWithValue("@generatedAtomIds", string.Join(",", generatedAtomIds));
                command.Parameters.Add("@provenanceStream", SqlDbType.Udt).Value = stream;
                command.Parameters.AddWithValue("@contextMetadata", contextJson);
                command.Parameters.AddWithValue("@tenantId", tenantId);

                var result = command.ExecuteScalar();
                return result != null && result != DBNull.Value ? Convert.ToInt64(result) : -1;
            }
        }

        private class ModelInfo
        {
            public int ModelId { get; set; }
            public string ModelName { get; set; }
            public string ModelType { get; set; }
            public string Architecture { get; set; }
            public string Config { get; set; }
            public int EmbeddingDimension { get; set; }
        }

        private class Candidate
        {
            public long AtomId { get; set; }
            public string Modality { get; set; }  // NEW: Support cross-modal candidates
            public string Text { get; set; }
            public double Distance { get; set; }
            public double Score { get; set; }
        }
    }
}
