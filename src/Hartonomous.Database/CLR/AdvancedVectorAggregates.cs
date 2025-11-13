using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.Server;
using Newtonsoft.Json;
using SqlClrFunctions.Core;

namespace SqlClrFunctions
{
    /// <summary>
    /// MIND-BLOWING vector aggregates that exploit SQL Server 2025 capabilities:
    /// - Aggregate VECTORs into centroid/cluster representatives
    /// - Compute GEOMETRY convex hulls of spatial projections
    /// - Build hierarchical clusterings during GROUP BY
    /// - Stream-process embeddings for real-time analytics
    /// </summary>

    /// <summary>
    /// Computes the CENTROID (mean vector) of a collection of VECTORs.
    /// SELECT atom_type, dbo.VectorCentroid(embedding_vector) FROM atoms GROUP BY atom_type
    /// Returns: VECTOR representing the geometric center in embedding space
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = true,
        MaxByteSize = -1)]  // Unlimited for large vector collections
    public struct VectorCentroid : IBinarySerialize
    {
        private long count;
        private int dimension;
        private float[] sum;

        public void Init()
        {
            count = 0;
            dimension = 0;
            sum = null;
        }

        /// <summary>
        /// Accumulate a VECTOR (passed as JSON array string from SQL Server VECTOR type)
        /// </summary>
        public void Accumulate(SqlString vectorJson)
        {
            if (vectorJson.IsNull) return;

            var vec = VectorUtilities.ParseVectorJson(vectorJson.Value);
            if (vec == null) return;

            if (dimension == 0)
            {
                dimension = vec.Length;
                sum = new float[dimension];
            }
            else if (vec.Length != dimension)
                return; // Skip mismatched dimensions

            for (int i = 0; i < dimension; i++)
                sum[i] += vec[i];

            count++;
        }

        public void Merge(VectorCentroid other)
        {
            if (other.count == 0 || other.dimension == 0)
                return;

            if (dimension == 0)
            {
                dimension = other.dimension;
                sum = other.sum == null ? null : (float[])other.sum.Clone();
                count = other.count;
                return;
            }

            if (other.dimension != dimension || sum == null || other.sum == null)
                return;

            for (int i = 0; i < dimension; i++)
                sum[i] += other.sum[i];

            count += other.count;
        }

        /// <summary>
        /// Returns the centroid as VECTOR JSON format: [x, y, z, ...]
        /// </summary>
        public SqlString Terminate()
        {
            if (count == 0 || dimension == 0 || sum == null)
                return SqlString.Null;

            float[] centroid = new float[dimension];
            for (int i = 0; i < dimension; i++)
                centroid[i] = sum[i] / count;

            // Reset state to release references eagerly
            sum = null;
            count = 0;
            dimension = 0;

            return new SqlString(JsonConvert.SerializeObject(centroid));
        }

        public void Read(BinaryReader r)
        {
            dimension = r.ReadInt32();
            count = r.ReadInt64();

            if (dimension > 0 && count > 0)
            {
                sum = new float[dimension];
                for (int i = 0; i < dimension; i++)
                    sum[i] = r.ReadSingle();
            }
            else
            {
                sum = null;
            }
        }

        public void Write(BinaryWriter w)
        {
            w.Write(dimension);
            w.Write(count);
            if (dimension > 0 && sum != null)
            {
                for (int i = 0; i < dimension; i++)
                    w.Write(sum[i]);
            }
        }
    }

    /// <summary>
    /// Aggregates spatial GEOMETRY points into a CONVEX HULL
    /// SELECT region, dbo.SpatialConvexHull(spatial_point) FROM atoms GROUP BY region
    /// Returns: WKT POLYGON representing the convex hull of all points
    /// USE CASE: Visualize cluster boundaries in projected embedding space
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = true,  // Duplicates don't affect convex hull
        IsInvariantToOrder = true,
        MaxByteSize = -1)]
    public struct SpatialConvexHull : IBinarySerialize
    {
        private List<(double X, double Y)> points;

        public void Init()
        {
            points = new List<(double, double)>();
        }

        /// <summary>
        /// Accumulate GEOMETRY::Point(X, Y, 0) as "POINT(X Y)" WKT
        /// </summary>
        public void Accumulate(SqlString pointWkt)
        {
            if (pointWkt.IsNull) return;

            var pt = ParsePointWkt(pointWkt.Value);
            if (pt.HasValue)
                points.Add(pt.Value);
        }

        public void Merge(SpatialConvexHull other)
        {
            if (other.points != null)
                points.AddRange(other.points);
        }

        /// <summary>
        /// Graham scan algorithm for 2D convex hull
        /// </summary>
        public SqlString Terminate()
        {
            if (points.Count < 3)
                return SqlString.Null;

            var hull = ComputeConvexHull(points);
            if (hull.Count < 3)
                return SqlString.Null;

            // Close the polygon
            hull.Add(hull[0]);

            var wkt = "POLYGON((" +
                string.Join(", ", hull.Select(p => $"{p.X:F6} {p.Y:F6}")) +
                "))";
            return new SqlString(wkt);
        }

        public void Read(BinaryReader r)
        {
            int count = r.ReadInt32();
            points = new List<(double, double)>(count);
            for (int i = 0; i < count; i++)
                points.Add((r.ReadDouble(), r.ReadDouble()));
        }

        public void Write(BinaryWriter w)
        {
            w.Write(points.Count);
            foreach (var (x, y) in points)
            {
                w.Write(x);
                w.Write(y);
            }
        }

        private static (double X, double Y)? ParsePointWkt(string wkt)
        {
            try
            {
                // Parse "POINT(X Y)" or "POINT (X Y)"
                var cleaned = wkt.Replace("POINT", "").Replace("(", "").Replace(")", "").Trim();
                var parts = cleaned.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                    return (double.Parse(parts[0]), double.Parse(parts[1]));
            }
            catch { }
            return null;
        }

        private static List<(double X, double Y)> ComputeConvexHull(List<(double X, double Y)> points)
        {
            // Graham scan implementation
            var sorted = points.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
            if (sorted.Count < 3) return sorted;

            // Lower hull
            var lower = new List<(double, double)>();
            foreach (var p in sorted)
            {
                while (lower.Count >= 2 && CrossProduct(lower[lower.Count - 2], lower[lower.Count - 1], p) <= 0)
                    lower.RemoveAt(lower.Count - 1);
                lower.Add(p);
            }

            // Upper hull
            var upper = new List<(double, double)>();
            for (int i = sorted.Count - 1; i >= 0; i--)
            {
                while (upper.Count >= 2 && CrossProduct(upper[upper.Count - 2], upper[upper.Count - 1], sorted[i]) <= 0)
                    upper.RemoveAt(upper.Count - 1);
                upper.Add(sorted[i]);
            }

            // Remove last point of each half because it's repeated
            lower.RemoveAt(lower.Count - 1);
            upper.RemoveAt(upper.Count - 1);

            lower.AddRange(upper);
            return lower;
        }

        private static double CrossProduct((double X, double Y) o, (double X, double Y) a, (double X, double Y) b)
        {
            return (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);
        }
    }

    /// <summary>
    /// K-MEANS CLUSTERING AGGREGATE
    /// Performs streaming k-means on vectors during GROUP BY
    /// SELECT category, dbo.VectorKMeansCluster(embedding_vector, 5) FROM atoms GROUP BY category
    /// Returns: JSON array of K cluster centroids
    /// USE CASE: Discover sub-clusters within semantic categories
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = false,  // Order affects online clustering
        MaxByteSize = -1)]
    public struct VectorKMeansCluster : IBinarySerialize
    {
        private List<float[]> centroids;
        private List<int> counts;
        private int k;
        private int dimension;

        public void Init()
        {
            centroids = new List<float[]>();
            counts = new List<int>();
            k = 0;
            dimension = 0;
        }

        /// <summary>
        /// Accumulate with K parameter
        /// vectorJson: VECTOR as JSON array
        /// kValue: Number of clusters
        /// </summary>
        public void Accumulate(SqlString vectorJson, SqlInt32 kValue)
        {
            if (vectorJson.IsNull || kValue.IsNull) return;

            if (k == 0) k = kValue.Value;

            var vec = VectorUtilities.ParseVectorJson(vectorJson.Value);
            if (vec == null) return;

            if (dimension == 0)
            {
                dimension = vec.Length;
                // Initialize first K vectors as initial centroids
                centroids.Add(vec);
                counts.Add(1);
                return;
            }

            if (vec.Length != dimension) return;

            // Online k-means: assign to nearest centroid and update
            if (centroids.Count < k)
            {
                // Still initializing
                centroids.Add(vec);
                counts.Add(1);
            }
            else
            {
                int nearest = FindNearestCentroid(vec);
                UpdateCentroid(nearest, vec);
            }
        }

        public void Merge(VectorKMeansCluster other)
        {
            // Merge by treating other centroids as new points
            if (other.centroids == null) return;
            for (int i = 0; i < other.centroids.Count; i++)
            {
                int nearest = FindNearestCentroid(other.centroids[i]);
                for (int c = 0; c < other.counts[i]; c++)
                    UpdateCentroid(nearest, other.centroids[i]);
            }
        }

        public SqlString Terminate()
        {
            if (centroids.Count == 0)
                return SqlString.Null;

            var centroidJson = centroids
                .Select(c => JsonConvert.SerializeObject(c))
                .ToList();

            return new SqlString(JsonConvert.SerializeObject(centroidJson));
        }

        public void Read(BinaryReader r)
        {
            k = r.ReadInt32();
            dimension = r.ReadInt32();
            int count = r.ReadInt32();
            centroids = new List<float[]>(count);
            counts = new List<int>(count);
            for (int i = 0; i < count; i++)
            {
                float[] centroid = new float[dimension];
                for (int j = 0; j < dimension; j++)
                    centroid[j] = r.ReadSingle();
                centroids.Add(centroid);
                counts.Add(r.ReadInt32());
            }
        }

        public void Write(BinaryWriter w)
        {
            w.Write(k);
            w.Write(dimension);
            w.Write(centroids.Count);
            for (int i = 0; i < centroids.Count; i++)
            {
                foreach (var val in centroids[i])
                    w.Write(val);
                w.Write(counts[i]);
            }
        }

        private int FindNearestCentroid(float[] vec)
        {
            int nearest = 0;
            double minDist = double.MaxValue;
            for (int i = 0; i < centroids.Count; i++)
            {
                double dist = VectorUtilities.EuclideanDistance(vec, centroids[i]);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = i;
                }
            }
            return nearest;
        }

        private void UpdateCentroid(int index, float[] vec)
        {
            var centroid = centroids[index];
            int count = counts[index];
            for (int i = 0; i < dimension; i++)
            {
                centroid[i] = (centroid[i] * count + vec[i]) / (count + 1);
            }
            counts[index] = count + 1;
        }
    }

    /// <summary>
    /// VECTOR VARIANCE-COVARIANCE MATRIX
    /// Computes full covariance matrix for dimensionality reduction
    /// SELECT dbo.VectorCovariance(embedding_vector) FROM atoms WHERE category = 'images'
    /// Returns: JSON matrix for PCA/compression analysis
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = true,
        MaxByteSize = -1)]
    public struct VectorCovariance : IBinarySerialize
    {
        private List<float[]> vectors;
        private int dimension;

        public void Init()
        {
            vectors = new List<float[]>();
            dimension = 0;
        }

        public void Accumulate(SqlString vectorJson)
        {
            if (vectorJson.IsNull) return;
            var vec = VectorUtilities.ParseVectorJson(vectorJson.Value);
            if (vec == null) return;

            if (dimension == 0)
                dimension = vec.Length;
            else if (vec.Length != dimension)
                return;

            vectors.Add(vec);
        }

        public void Merge(VectorCovariance other)
        {
            if (other.vectors != null)
                vectors.AddRange(other.vectors);
        }

        public SqlString Terminate()
        {
            if (vectors.Count < 2 || dimension == 0)
                return SqlString.Null;

            // Compute mean
            float[] mean = new float[dimension];
            foreach (var vec in vectors)
                for (int i = 0; i < dimension; i++)
                    mean[i] += vec[i];
            for (int i = 0; i < dimension; i++)
                mean[i] /= vectors.Count;

            // Compute covariance matrix (upper triangle only for efficiency)
            var cov = new Dictionary<string, double>();
            foreach (var vec in vectors)
            {
                for (int i = 0; i < dimension; i++)
                {
                    for (int j = i; j < dimension; j++)
                    {
                        string key = $"{i},{j}";
                        if (!cov.ContainsKey(key))
                            cov[key] = 0;
                        cov[key] += (vec[i] - mean[i]) * (vec[j] - mean[j]);
                    }
                }
            }

            // Normalize
            foreach (var key in cov.Keys.ToList())
                cov[key] /= (vectors.Count - 1);

            // Return sparse JSON representation
            var json = "{" + string.Join(",",
                cov.Select(kvp => $"\"{kvp.Key}\":{kvp.Value:G9}")
            ) + "}";
            return new SqlString(json);
        }

        public void Read(BinaryReader r)
        {
            dimension = r.ReadInt32();
            int count = r.ReadInt32();
            vectors = new List<float[]>(count);
            for (int i = 0; i < count; i++)
            {
                float[] vec = new float[dimension];
                for (int j = 0; j < dimension; j++)
                    vec[j] = r.ReadSingle();
                vectors.Add(vec);
            }
        }

        public void Write(BinaryWriter w)
        {
            w.Write(dimension);
            w.Write(vectors.Count);
            foreach (var vec in vectors)
                foreach (var val in vec)
                    w.Write(val);
        }
    }
}
