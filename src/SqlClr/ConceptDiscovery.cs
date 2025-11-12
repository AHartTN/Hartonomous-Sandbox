using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using Microsoft.SqlServer.Server;

namespace SqlClrFunctions
{
    /// <summary>
    /// Unsupervised concept discovery via clustering
    /// Phase 1: Detect emergent patterns in embedding space
    /// Uses spatial bucketing + density-based clustering (DBSCAN)
    /// </summary>
    public static class ConceptDiscovery
    {
        /// <summary>
        /// Discover concepts from embedding clusters
        /// Returns table of ConceptId, Centroid, AtomCount, Coherence
        /// </summary>
        [SqlFunction(
            FillRowMethodName = "FillConceptRow",
            TableDefinition = "ConceptId UNIQUEIDENTIFIER, Centroid VARBINARY(MAX), AtomCount INT, Coherence FLOAT, SpatialBucket INT",
            IsDeterministic = false,
            IsPrecise = false,
            DataAccess = DataAccessKind.Read
        )]
        public static System.Collections.IEnumerable fn_DiscoverConcepts(
            SqlInt32 minClusterSize,
            SqlDouble coherenceThreshold,
            SqlInt32 maxConcepts,
            SqlInt32 tenantId)
        {
            int minSize = minClusterSize.IsNull ? 10 : minClusterSize.Value;
            double minCoherence = coherenceThreshold.IsNull ? 0.7 : coherenceThreshold.Value;
            int maxResults = maxConcepts.IsNull ? 100 : maxConcepts.Value;
            int tenant = tenantId.IsNull ? 0 : tenantId.Value;

            var concepts = new List<ConceptCandidate>();

            using (var conn = new System.Data.SqlClient.SqlConnection("context connection=true"))
            {
                conn.Open();

                // Step 1: Load spatial bucket statistics filtered by tenant via TenantAtoms junction
                var bucketQuery = @"
                    SELECT 
                        ae.SpatialBucket,
                        COUNT(*) AS AtomCount,
                        AVG(CAST(ae.EmbeddingVector AS FLOAT)) AS AvgMagnitude
                    FROM dbo.AtomEmbeddings ae
                    INNER JOIN dbo.TenantAtoms ta ON ae.AtomId = ta.AtomId
                    WHERE ae.SpatialBucket IS NOT NULL AND ta.TenantId = @TenantId
                    GROUP BY ae.SpatialBucket
                    HAVING COUNT(*) >= @MinClusterSize
                    ORDER BY COUNT(*) DESC";

                using (var cmd = new System.Data.SqlClient.SqlCommand(bucketQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@MinClusterSize", minSize);
                    cmd.Parameters.AddWithValue("@TenantId", tenant);

                    using (var reader = cmd.ExecuteReader())
                    {
                        var buckets = new List<(int Bucket, int Count, double AvgMag)>();
                        while (reader.Read())
                        {
                            buckets.Add((
                                reader.GetInt32(0),
                                reader.GetInt32(1),
                                reader.IsDBNull(2) ? 0 : reader.GetDouble(2)
                            ));
                        }

                        // Step 2: Density-based clustering (DBSCAN-like)
                        var clusters = PerformDBSCAN(buckets, minSize);

                        reader.Close();

                        // Step 3: For each cluster, compute centroid and coherence
                        foreach (var cluster in clusters.Take(maxResults))
                        {
                            var centroid = ComputeClusterCentroid(conn, cluster, tenant);
                            double coherence = ComputeClusterCoherence(conn, cluster, centroid, tenant);

                            if (coherence >= minCoherence)
                            {
                                concepts.Add(new ConceptCandidate
                                {
                                    ConceptId = Guid.NewGuid(),
                                    Centroid = centroid,
                                    AtomCount = cluster.Sum(b => b.Count),
                                    Coherence = coherence,
                                    SpatialBucket = cluster[0].Bucket // Representative bucket
                                });
                            }
                        }
                    }
                }
            }

            return concepts;
        }

        public static void FillConceptRow(
            object obj,
            out SqlGuid conceptId,
            out SqlBytes centroid,
            out SqlInt32 atomCount,
            out SqlDouble coherence,
            out SqlInt32 spatialBucket)
        {
            var concept = (ConceptCandidate)obj;
            conceptId = new SqlGuid(concept.ConceptId);
            centroid = new SqlBytes(concept.Centroid);
            atomCount = new SqlInt32(concept.AtomCount);
            coherence = new SqlDouble(concept.Coherence);
            spatialBucket = new SqlInt32(concept.SpatialBucket);
        }

        /// <summary>
        /// DBSCAN-like clustering on spatial buckets
        /// Groups adjacent buckets with high density
        /// </summary>
        private static List<List<(int Bucket, int Count, double AvgMag)>> PerformDBSCAN(
            List<(int Bucket, int Count, double AvgMag)> buckets,
            int minClusterSize)
        {
            var clusters = new List<List<(int Bucket, int Count, double AvgMag)>>();
            var visited = new HashSet<int>();

            foreach (var bucket in buckets)
            {
                if (visited.Contains(bucket.Bucket))
                    continue;

                // Find neighbors (adjacent spatial buckets within epsilon distance)
                var cluster = new List<(int Bucket, int Count, double AvgMag)> { bucket };
                visited.Add(bucket.Bucket);

                // Expand cluster by finding spatial neighbors
                var neighbors = buckets.Where(b =>
                    !visited.Contains(b.Bucket) &&
                    Math.Abs(b.Bucket - bucket.Bucket) <= 10 && // Spatial proximity
                    Math.Abs(b.AvgMag - bucket.AvgMag) <= 0.2    // Magnitude similarity
                ).ToList();

                foreach (var neighbor in neighbors)
                {
                    if (!visited.Contains(neighbor.Bucket))
                    {
                        cluster.Add(neighbor);
                        visited.Add(neighbor.Bucket);
                    }
                }

                if (cluster.Count >= 2) // Minimum cluster size
                {
                    clusters.Add(cluster);
                }
            }

            return clusters;
        }

        /// <summary>
        /// Compute centroid embedding for a cluster
        /// Averages all embeddings in the cluster's spatial buckets
        /// </summary>
        private static byte[] ComputeClusterCentroid(
            System.Data.SqlClient.SqlConnection conn,
            List<(int Bucket, int Count, double AvgMag)> cluster,
            int tenantId)
        {
            var bucketIds = string.Join(",", cluster.Select(c => c.Bucket));

            var centroidQuery = $@"
                SELECT TOP 1 ae.EmbeddingVector
                FROM dbo.AtomEmbeddings ae
                INNER JOIN dbo.TenantAtoms ta ON ae.AtomId = ta.AtomId
                WHERE ae.SpatialBucket IN ({bucketIds}) AND ta.TenantId = @TenantId
                ORDER BY NEWID()"; // Random representative (could use actual centroid computation)

            using (var cmd = new System.Data.SqlClient.SqlCommand(centroidQuery, conn))
            {
                cmd.Parameters.AddWithValue("@TenantId", tenantId);
                var result = cmd.ExecuteScalar();
                return result as byte[] ?? Array.Empty<byte>();
            }
        }

        /// <summary>
        /// Compute cluster coherence (average cosine similarity to centroid)
        /// Higher coherence = tighter cluster = better concept
        /// </summary>
        private static double ComputeClusterCoherence(
            System.Data.SqlClient.SqlConnection conn,
            List<(int Bucket, int Count, double AvgMag)> cluster,
            byte[] centroid,
            int tenantId)
        {
            if (centroid.Length == 0)
                return 0.0;

            var bucketIds = string.Join(",", cluster.Select(c => c.Bucket));

            var coherenceQuery = $@"
                SELECT AVG(
                    1.0 - VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @Centroid)
                ) AS Coherence
                FROM (
                    SELECT TOP 100 ae.EmbeddingVector
                    FROM dbo.AtomEmbeddings ae
                    INNER JOIN dbo.TenantAtoms ta ON ae.AtomId = ta.AtomId
                    WHERE ae.SpatialBucket IN ({bucketIds}) AND ta.TenantId = @TenantId
                    ORDER BY NEWID()
                ) AS Sample";

            using (var cmd = new System.Data.SqlClient.SqlCommand(coherenceQuery, conn))
            {
                cmd.Parameters.Add("@Centroid", System.Data.SqlDbType.VarBinary).Value = centroid;
                cmd.Parameters.AddWithValue("@TenantId", tenantId);

                var result = cmd.ExecuteScalar();
                return result == DBNull.Value ? 0.0 : Convert.ToDouble(result);
            }
        }

        private class ConceptCandidate
        {
            public Guid ConceptId { get; set; }
            public byte[] Centroid { get; set; }
            public int AtomCount { get; set; }
            public double Coherence { get; set; }
            public int SpatialBucket { get; set; }
        }
    }

    /// <summary>
    /// Concept binding: Associate atoms with discovered concepts
    /// Phase 2: Multi-label classification via similarity thresholding
    /// </summary>
    public static class ConceptBinding
    {
        /// <summary>
        /// Bind atoms to concepts based on embedding similarity
        /// Returns table of AtomId, ConceptId, Similarity
        /// </summary>
        [SqlFunction(
            FillRowMethodName = "FillBindingRow",
            TableDefinition = "AtomId BIGINT, ConceptId UNIQUEIDENTIFIER, Similarity FLOAT, IsPrimary BIT",
            IsDeterministic = false,
            IsPrecise = false,
            DataAccess = DataAccessKind.Read
        )]
        public static System.Collections.IEnumerable fn_BindConcepts(
            SqlInt64 atomId,
            SqlDouble similarityThreshold,
            SqlInt32 maxConceptsPerAtom,
            SqlInt32 tenantId)
        {
            long atom = atomId.Value;
            double threshold = similarityThreshold.IsNull ? 0.6 : similarityThreshold.Value;
            int maxConcepts = maxConceptsPerAtom.IsNull ? 5 : maxConceptsPerAtom.Value;
            int tenant = tenantId.IsNull ? 0 : tenantId.Value;

            var bindings = new List<BindingResult>();

            using (var conn = new System.Data.SqlClient.SqlConnection("context connection=true"))
            {
                conn.Open();

                // Step 1: Get atom embedding
                byte[] atomEmbedding = null;
                var getEmbeddingQuery = @"
                    SELECT EmbeddingVector
                    FROM dbo.AtomEmbeddings
                    WHERE AtomId = @AtomId AND TenantId = @TenantId";

                using (var cmd = new System.Data.SqlClient.SqlCommand(getEmbeddingQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@AtomId", atom);
                    cmd.Parameters.AddWithValue("@TenantId", tenant);
                    var result = cmd.ExecuteScalar();
                    if (result == null || result == DBNull.Value)
                        return bindings; // No embedding, no bindings

                    atomEmbedding = (byte[])result;
                }

                // Step 2: Find matching concepts (assuming Concepts table exists)
                var findConceptsQuery = @"
                    SELECT TOP (@MaxConcepts)
                        ConceptId,
                        1.0 - VECTOR_DISTANCE('cosine', Centroid, @AtomEmbedding) AS Similarity
                    FROM provenance.Concepts
                    WHERE TenantId = @TenantId
                    ORDER BY Similarity DESC";

                using (var cmd = new System.Data.SqlClient.SqlCommand(findConceptsQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@MaxConcepts", maxConcepts);
                    cmd.Parameters.Add("@AtomEmbedding", System.Data.SqlDbType.VarBinary).Value = atomEmbedding;
                    cmd.Parameters.AddWithValue("@TenantId", tenant);

                    using (var reader = cmd.ExecuteReader())
                    {
                        bool isFirst = true;
                        while (reader.Read())
                        {
                            Guid conceptId = reader.GetGuid(0);
                            double similarity = reader.GetDouble(1);

                            if (similarity >= threshold)
                            {
                                bindings.Add(new BindingResult
                                {
                                    AtomId = atom,
                                    ConceptId = conceptId,
                                    Similarity = similarity,
                                    IsPrimary = isFirst
                                });
                                isFirst = false;
                            }
                        }
                    }
                }
            }

            return bindings;
        }

        public static void FillBindingRow(
            object obj,
            out SqlInt64 atomId,
            out SqlGuid conceptId,
            out SqlDouble similarity,
            out SqlBoolean isPrimary)
        {
            var binding = (BindingResult)obj;
            atomId = new SqlInt64(binding.AtomId);
            conceptId = new SqlGuid(binding.ConceptId);
            similarity = new SqlDouble(binding.Similarity);
            isPrimary = new SqlBoolean(binding.IsPrimary);
        }

        private class BindingResult
        {
            public long AtomId { get; set; }
            public Guid ConceptId { get; set; }
            public double Similarity { get; set; }
            public bool IsPrimary { get; set; }
        }
    }
}
