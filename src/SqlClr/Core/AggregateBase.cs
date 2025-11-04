using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlServer.Server;

namespace SqlClrFunctions.Core
{
    /// <summary>
    /// Base class for vector aggregates with common serialization logic.
    /// Implements SOLID principles with separation of concerns.
    /// </summary>
    public abstract class VectorAggregateBase : IBinarySerialize
    {
        protected int Dimension { get; private set; }
        protected List<float[]> Vectors { get; private set; }

        protected VectorAggregateBase()
        {
            Vectors = new List<float[]>();
        }

        protected bool TryAddVector(string vectorJson)
        {
            var vec = VectorParser.ParseVectorJson(vectorJson);
            if (vec == null) return false;

            if (Dimension == 0)
            {
                Dimension = vec.Length;
            }
            else if (vec.Length != Dimension)
            {
                return false;
            }

            Vectors.Add(vec);
            return true;
        }

        public virtual void Read(BinaryReader r)
        {
            Dimension = r.ReadInt32();
            int count = r.ReadInt32();
            Vectors = new List<float[]>(count);

            for (int i = 0; i < count; i++)
            {
                float[] vec = new float[Dimension];
                for (int j = 0; j < Dimension; j++)
                    vec[j] = r.ReadSingle();
                Vectors.Add(vec);
            }
        }

        public virtual void Write(BinaryWriter w)
        {
            w.Write(Dimension);
            w.Write(Vectors.Count);

            foreach (var vec in Vectors)
            {
                for (int j = 0; j < Dimension; j++)
                    w.Write(vec[j]);
            }
        }

        protected static void WriteVector(BinaryWriter w, ReadOnlySpan<float> vector)
        {
            for (int i = 0; i < vector.Length; i++)
                w.Write(vector[i]);
        }

        protected static void ReadVector(BinaryReader r, Span<float> destination)
        {
            for (int i = 0; i < destination.Length; i++)
                destination[i] = r.ReadSingle();
        }
    }

    /// <summary>
    /// Base class for time-series vector aggregates.
    /// </summary>
    public abstract class TimeSeriesVectorAggregateBase : IBinarySerialize
    {
        protected struct TimestampedVector
        {
            public DateTime Timestamp;
            public float[] Vector;

            public TimestampedVector(DateTime timestamp, float[] vector)
            {
                Timestamp = timestamp;
                Vector = vector;
            }
        }

        protected List<TimestampedVector> Series { get; private set; }
        protected int Dimension { get; private set; }

        protected TimeSeriesVectorAggregateBase()
        {
            Series = new List<TimestampedVector>();
        }

        protected bool TryAddTimestampedVector(DateTime timestamp, string vectorJson)
        {
            var vec = VectorParser.ParseVectorJson(vectorJson);
            if (vec == null) return false;

            if (Dimension == 0)
            {
                Dimension = vec.Length;
            }
            else if (vec.Length != Dimension)
            {
                return false;
            }

            Series.Add(new TimestampedVector(timestamp, vec));
            return true;
        }

        protected void SortByTimestamp()
        {
            Series.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
        }

        public virtual void Read(BinaryReader r)
        {
            Dimension = r.ReadInt32();
            int count = r.ReadInt32();
            Series = new List<TimestampedVector>(count);

            for (int i = 0; i < count; i++)
            {
                var timestamp = DateTime.FromBinary(r.ReadInt64());
                float[] vec = new float[Dimension];
                for (int j = 0; j < Dimension; j++)
                    vec[j] = r.ReadSingle();
                Series.Add(new TimestampedVector(timestamp, vec));
            }
        }

        public virtual void Write(BinaryWriter w)
        {
            w.Write(Dimension);
            w.Write(Series.Count);

            foreach (var item in Series)
            {
                w.Write(item.Timestamp.ToBinary());
                for (int j = 0; j < Dimension; j++)
                    w.Write(item.Vector[j]);
            }
        }
    }

    /// <summary>
    /// Base class for graph-aware vector aggregates.
    /// </summary>
    public abstract class GraphVectorAggregateBase : IBinarySerialize
    {
        protected struct GraphNode
        {
            public string NodeId;
            public float[] Vector;
            public Dictionary<string, double> Edges; // TargetNodeId -> Weight

            public GraphNode(string nodeId, float[] vector)
            {
                NodeId = nodeId;
                Vector = vector;
                Edges = new Dictionary<string, double>();
            }
        }

        protected Dictionary<string, GraphNode> Nodes { get; private set; }
        protected int Dimension { get; private set; }

        protected GraphVectorAggregateBase()
        {
            Nodes = new Dictionary<string, GraphNode>();
        }

        protected bool TryAddNode(string nodeId, string vectorJson)
        {
            var vec = VectorParser.ParseVectorJson(vectorJson);
            if (vec == null) return false;

            if (Dimension == 0)
            {
                Dimension = vec.Length;
            }
            else if (vec.Length != Dimension)
            {
                return false;
            }

            if (!Nodes.ContainsKey(nodeId))
            {
                Nodes[nodeId] = new GraphNode(nodeId, vec);
            }

            return true;
        }

        protected void AddEdge(string fromNodeId, string toNodeId, double weight)
        {
            if (Nodes.ContainsKey(fromNodeId))
            {
                var node = Nodes[fromNodeId];
                node.Edges[toNodeId] = weight;
                Nodes[fromNodeId] = node;
            }
        }

        public virtual void Read(BinaryReader r)
        {
            Dimension = r.ReadInt32();
            int nodeCount = r.ReadInt32();
            Nodes = new Dictionary<string, GraphNode>(nodeCount);

            for (int i = 0; i < nodeCount; i++)
            {
                string nodeId = r.ReadString();
                float[] vec = new float[Dimension];
                for (int j = 0; j < Dimension; j++)
                    vec[j] = r.ReadSingle();

                var node = new GraphNode(nodeId, vec);

                int edgeCount = r.ReadInt32();
                for (int j = 0; j < edgeCount; j++)
                {
                    string targetId = r.ReadString();
                    double weight = r.ReadDouble();
                    node.Edges[targetId] = weight;
                }

                Nodes[nodeId] = node;
            }
        }

        public virtual void Write(BinaryWriter w)
        {
            w.Write(Dimension);
            w.Write(Nodes.Count);

            foreach (var kvp in Nodes)
            {
                w.Write(kvp.Key);
                for (int j = 0; j < Dimension; j++)
                    w.Write(kvp.Value.Vector[j]);

                w.Write(kvp.Value.Edges.Count);
                foreach (var edge in kvp.Value.Edges)
                {
                    w.Write(edge.Key);
                    w.Write(edge.Value);
                }
            }
        }
    }

    /// <summary>
    /// Memory pool for reusing float arrays.
    /// Reduces GC pressure in aggregates that process many vectors.
    /// </summary>
    public static class VectorPool
    {
        private static readonly ArrayPool<float> Pool = ArrayPool<float>.Shared;

        public static float[] Rent(int minimumLength)
        {
            return Pool.Rent(minimumLength);
        }

        public static void Return(float[] array, bool clearArray = false)
        {
            Pool.Return(array, clearArray);
        }

        /// <summary>
        /// Get a pooled array and copy data into it.
        /// Caller must return the array when done.
        /// </summary>
        public static float[] RentAndCopy(ReadOnlySpan<float> source)
        {
            var array = Pool.Rent(source.Length);
            source.CopyTo(array);
            return array;
        }
    }
}
