using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Server;
using SqlClrFunctions.Core;

namespace SqlClrFunctions
{
    /// <summary>
    /// GRAPH + VECTOR + GEOMETRY hybrid aggregates
    /// These exploit the multi-modal database architecture for revolutionary capabilities
    /// </summary>

    /// <summary>
    /// HIERARCHICAL VECTOR CLUSTERING for GRAPH traversal results
    /// 
    /// SELECT path_depth, 
    ///        dbo.GraphPathVectorSummary($node_id, embedding_vector, spatial_point)
    /// FROM graph_traversal_results
    /// GROUP BY path_depth
    /// 
    /// Returns: JSON with {centroid, spatial_hull, diameter, count} for each path level
    /// USE CASE: Summarize semantic drift as you traverse the knowledge graph
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = false,
        MaxByteSize = -1)]
    public struct GraphPathVectorSummary : IBinarySerialize
    {
        private List<string> nodeIds;
        private List<float[]> vectors;
        private List<(double X, double Y)> spatialPoints;

        public void Init()
        {
            nodeIds = new List<string>();
            vectors = new List<float[]>();
            spatialPoints = new List<(double, double)>();
        }

        public void Accumulate(SqlString nodeId, SqlString vectorJson, SqlString pointWkt)
        {
            if (!nodeId.IsNull)
                nodeIds.Add(nodeId.Value);

            if (!vectorJson.IsNull)
            {
                var vec = VectorUtilities.ParseVectorJson(vectorJson.Value);
                if (vec != null)
                    vectors.Add(vec);
            }

            if (!pointWkt.IsNull)
            {
                var pt = VectorUtilities.ParsePointWkt(pointWkt.Value);
                if (pt.HasValue)
                    spatialPoints.Add(pt.Value);
            }
        }

        public void Merge(GraphPathVectorSummary other)
        {
            if (other.nodeIds != null) nodeIds.AddRange(other.nodeIds);
            if (other.vectors != null) vectors.AddRange(other.vectors);
            if (other.spatialPoints != null) spatialPoints.AddRange(other.spatialPoints);
        }

        public SqlString Terminate()
        {
            var result = new StringBuilder();
            result.Append("{");
            
            result.Append($"\"node_count\":{nodeIds.Count},");
            
            // Vector centroid
            if (vectors.Count > 0)
            {
                int dim = vectors[0].Length;
                float[] centroid = new float[dim];
                foreach (var vec in vectors)
                    for (int i = 0; i < dim; i++)
                        centroid[i] += vec[i];
                for (int i = 0; i < dim; i++)
                    centroid[i] /= vectors.Count;

                var serializer = new SqlClrFunctions.JsonProcessing.JsonSerializerImpl();
                result.Append("\"centroid\":");
                result.Append(serializer.SerializeFloatArray(centroid.Take(10).ToArray()));
                result.Append(",");

                // Vector diameter (max distance between any two vectors)
                double maxDist = 0;
                for (int i = 0; i < vectors.Count; i++)
                {
                    for (int j = i + 1; j < vectors.Count; j++)
                    {
                        double dist = VectorUtilities.EuclideanDistance(vectors[i], vectors[j]);
                        if (dist > maxDist) maxDist = dist;
                    }
                }
                result.Append($"\"diameter\":{maxDist:G6},");
            }

            // Spatial extent (bounding box)
            if (spatialPoints.Count > 0)
            {
                double minX = spatialPoints.Min(p => p.X);
                double maxX = spatialPoints.Max(p => p.X);
                double minY = spatialPoints.Min(p => p.Y);
                double maxY = spatialPoints.Max(p => p.Y);

                result.Append($"\"spatial_extent\":{{");
                result.Append($"\"min_x\":{minX:G6},\"max_x\":{maxX:G6},");
                result.Append($"\"min_y\":{minY:G6},\"max_y\":{maxY:G6},");
                result.Append($"\"area\":{(maxX - minX) * (maxY - minY):G6}");
                result.Append("},");
            }

            result.Append($"\"unique_nodes\":{nodeIds.Distinct().Count()}");
            result.Append("}");

            return new SqlString(result.ToString());
        }

        public void Read(BinaryReader r)
        {
            int nodeCount = r.ReadInt32();
            nodeIds = new List<string>(nodeCount);
            for (int i = 0; i < nodeCount; i++)
                nodeIds.Add(r.ReadString());

            int vecCount = r.ReadInt32();
            vectors = new List<float[]>(vecCount);
            if (vecCount > 0)
            {
                int dim = r.ReadInt32();
                for (int i = 0; i < vecCount; i++)
                {
                    float[] vec = new float[dim];
                    for (int j = 0; j < dim; j++)
                        vec[j] = r.ReadSingle();
                    vectors.Add(vec);
                }
            }

            int ptCount = r.ReadInt32();
            spatialPoints = new List<(double, double)>(ptCount);
            for (int i = 0; i < ptCount; i++)
                spatialPoints.Add((r.ReadDouble(), r.ReadDouble()));
        }

        public void Write(BinaryWriter w)
        {
            w.Write(nodeIds.Count);
            foreach (var id in nodeIds)
                w.Write(id);

            w.Write(vectors.Count);
            if (vectors.Count > 0)
            {
                w.Write(vectors[0].Length);
                foreach (var vec in vectors)
                    foreach (var val in vec)
                        w.Write(val);
            }

            w.Write(spatialPoints.Count);
            foreach (var (x, y) in spatialPoints)
            {
                w.Write(x);
                w.Write(y);
            }
        }
    }

    /// <summary>
    /// EDGE WEIGHT AGGREGATOR with vector similarity
    /// 
    /// Computes aggregate edge weights across graph paths, weighted by vector similarity
    /// 
    /// SELECT source_type, target_type,
    ///        dbo.EdgeWeightedByVectorSimilarity(edge_weight, from_vector, to_vector)
    /// FROM graph_edges
    /// GROUP BY source_type, target_type
    /// 
    /// Returns: Average edge weight, scaled by cosine similarity between connected nodes
    /// USE CASE: Find strongest semantic relationships in the knowledge graph
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = true,
        MaxByteSize = -1)]
    public struct EdgeWeightedByVectorSimilarity : IBinarySerialize
    {
        private double totalWeight;
        private double totalSimilarity;
        private int count;

        public void Init()
        {
            totalWeight = 0;
            totalSimilarity = 0;
            count = 0;
        }

        public void Accumulate(SqlDouble edgeWeight, SqlString fromVector, SqlString toVector)
        {
            if (edgeWeight.IsNull || fromVector.IsNull || toVector.IsNull)
                return;

            var vecFrom = VectorUtilities.ParseVectorJson(fromVector.Value);
            var vecTo = VectorUtilities.ParseVectorJson(toVector.Value);
            if (vecFrom == null || vecTo == null || vecFrom.Length != vecTo.Length)
                return;

            double similarity = VectorUtilities.CosineSimilarity(vecFrom, vecTo);
            double weight = edgeWeight.Value;

            // Weight the edge by similarity
            totalWeight += weight * similarity;
            totalSimilarity += similarity;
            count++;
        }

        public void Merge(EdgeWeightedByVectorSimilarity other)
        {
            totalWeight += other.totalWeight;
            totalSimilarity += other.totalSimilarity;
            count += other.count;
        }

        public SqlDouble Terminate()
        {
            if (count == 0 || totalSimilarity == 0)
                return SqlDouble.Null;

            return new SqlDouble(totalWeight / totalSimilarity);
        }

        public void Read(BinaryReader r)
        {
            totalWeight = r.ReadDouble();
            totalSimilarity = r.ReadDouble();
            count = r.ReadInt32();
        }

        public void Write(BinaryWriter w)
        {
            w.Write(totalWeight);
            w.Write(totalSimilarity);
            w.Write(count);
        }
    }

    /// <summary>
    /// SPATIAL DENSITY HEATMAP from triangulated embedding space
    /// 
    /// SELECT grid_x, grid_y, dbo.SpatialDensityGrid(spatial_point, 10, 10)
    /// FROM atom_embeddings
    /// GROUP BY grid_x, grid_y
    /// 
    /// Returns: Density count for spatial grid cell
    /// USE CASE: Create heatmaps of semantic space for visualization
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = true,
        MaxByteSize = 8000)]
    public struct SpatialDensityGrid : IBinarySerialize
    {
        private int count;
        private double sumX;
        private double sumY;

        public void Init()
        {
            count = 0;
            sumX = 0;
            sumY = 0;
        }

        public void Accumulate(SqlString pointWkt)
        {
            if (pointWkt.IsNull) return;

            var pt = ParsePointWkt(pointWkt.Value);
            if (pt.HasValue)
            {
                count++;
                sumX += pt.Value.X;
                sumY += pt.Value.Y;
            }
        }

        public void Merge(SpatialDensityGrid other)
        {
            count += other.count;
            sumX += other.sumX;
            sumY += other.sumY;
        }

        public SqlString Terminate()
        {
            if (count == 0)
                return SqlString.Null;

            return new SqlString($"{{\"count\":{count},\"density\":{count},\"center_x\":{sumX / count:F6},\"center_y\":{sumY / count:F6}}}");
        }

        public void Read(BinaryReader r)
        {
            count = r.ReadInt32();
            sumX = r.ReadDouble();
            sumY = r.ReadDouble();
        }

        public void Write(BinaryWriter w)
        {
            w.Write(count);
            w.Write(sumX);
            w.Write(sumY);
        }

        private static (double X, double Y)? ParsePointWkt(string wkt)
        {
            try
            {
                var cleaned = wkt.Replace("POINT", "").Replace("(", "").Replace(")", "").Trim();
                var parts = cleaned.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                    return (double.Parse(parts[0]), double.Parse(parts[1]));
            }
            catch { }
            return null;
        }
    }

    /// <summary>
    /// TEMPORAL VECTOR DRIFT ANALYZER
    /// 
    /// Tracks how vector embeddings change over time in a knowledge graph
    /// 
    /// SELECT atom_id, dbo.VectorDriftOverTime(timestamp, embedding_vector)
    /// FROM atom_history
    /// GROUP BY atom_id
    /// ORDER BY drift_magnitude DESC
    /// 
    /// Returns: JSON with drift_magnitude, drift_direction (first 10 dims), velocity
    /// USE CASE: Detect concept drift, identify evolving knowledge, track learning
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = false,  // Order matters for temporal analysis!
        MaxByteSize = -1)]
    public struct VectorDriftOverTime : IBinarySerialize
    {
        private List<(DateTime Timestamp, float[] Vector)> snapshots;
        private int dimension;

        public void Init()
        {
            snapshots = new List<(DateTime, float[])>();
            dimension = 0;
        }

        public void Accumulate(SqlDateTime timestamp, SqlString vectorJson)
        {
            if (timestamp.IsNull || vectorJson.IsNull) return;

            var vec = VectorUtilities.ParseVectorJson(vectorJson.Value);
            if (vec == null) return;

            if (dimension == 0)
                dimension = vec.Length;
            else if (vec.Length != dimension)
                return;

            snapshots.Add((timestamp.Value, vec));
        }

        public void Merge(VectorDriftOverTime other)
        {
            if (other.snapshots != null)
                snapshots.AddRange(other.snapshots);
        }

        public SqlString Terminate()
        {
            if (snapshots.Count < 2)
                return SqlString.Null;

            // Sort by timestamp
            snapshots.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

            // Compute total drift from first to last
            var first = snapshots.First().Vector;
            var last = snapshots.Last().Vector;
            var timeDelta = (snapshots.Last().Timestamp - snapshots.First().Timestamp).TotalSeconds;

            if (timeDelta <= 0)
                return SqlString.Null;

            float[] driftVector = new float[dimension];
            double magnitude = 0;
            for (int i = 0; i < dimension; i++)
            {
                driftVector[i] = last[i] - first[i];
                magnitude += driftVector[i] * driftVector[i];
            }
            magnitude = Math.Sqrt(magnitude);

            // Velocity: drift per second
            double velocity = magnitude / timeDelta;

            var serializer = new SqlClrFunctions.JsonProcessing.JsonSerializerImpl();
            var result = new
            {
                drift_magnitude = magnitude,
                velocity,
                time_span_seconds = timeDelta,
                snapshots = snapshots.Count,
                drift_direction = driftVector.Take(10).ToArray()
            };
            return new SqlString(serializer.Serialize(result));
        }

        public void Read(BinaryReader r)
        {
            dimension = r.ReadInt32();
            int count = r.ReadInt32();
            snapshots = new List<(DateTime, float[])>(count);
            for (int i = 0; i < count; i++)
            {
                var timestamp = DateTime.FromBinary(r.ReadInt64());
                float[] vec = new float[dimension];
                for (int j = 0; j < dimension; j++)
                    vec[j] = r.ReadSingle();
                snapshots.Add((timestamp, vec));
            }
        }

        public void Write(BinaryWriter w)
        {
            w.Write(dimension);
            w.Write(snapshots.Count);
            foreach (var (timestamp, vec) in snapshots)
            {
                w.Write(timestamp.ToBinary());
                foreach (var val in vec)
                    w.Write(val);
            }
        }
    }
}
