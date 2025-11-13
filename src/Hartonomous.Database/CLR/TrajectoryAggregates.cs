using Microsoft.SqlServer.Server;
using System;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.Collections.Generic;
using Microsoft.SqlServer.Types;

namespace SqlClrFunctions
{
    /// <summary>
    /// Represents a 3D point with a timestamp, used for building trajectories.
    /// The timestamp is stored to ensure correct ordering before creating the LineString.
    /// </summary>
    [Serializable]
    public struct PointWithTimestamp
    {
        public double X;
        public double Y;
        public double Z;
        public DateTime Timestamp;
    }

    /// <summary>
    /// A SQL CLR user-defined aggregate to construct a geometric path (LineString)
    /// from a sequence of atoms, ordered by timestamp. This is the core of
    /// trajectory and user session analysis.
    ///
    /// T-SQL Usage:
    /// SELECT
    ///     SessionId,
    ///     dbo.agg_BuildPathFromAtoms(AtomId, Timestamp) AS SessionPath
    /// FROM
    ///     dbo.UserInteractions
    /// GROUP BY
    ///     SessionId;
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = false, // Order is handled explicitly by sorting timestamps.
        MaxByteSize = -1)          // MaxByteSize = -1 for large objects.
    ]
    public struct BuildPathFromAtoms : IBinarySerialize
    {
        private List<PointWithTimestamp> _points;

        /// <summary>
        /// Initializes the aggregate state.
        /// </summary>
        public void Init()
        {
            _points = new List<PointWithTimestamp>();
        }

        /// <summary>
        /// Accumulates data for each row in the group.
        /// Connects back to the database to fetch the spatial location for the given AtomId.
        /// </summary>
        public void Accumulate(SqlInt64 atomId, SqlDateTime timestamp)
        {
            if (atomId.IsNull || timestamp.IsNull)
            {
                return;
            }

            SqlGeometry point = SqlGeometry.Null;

            // Use the context connection to execute a query and retrieve the atom's spatial location.
            // This requires the assembly to be deployed with PERMISSION_SET = EXTERNAL_ACCESS or UNSAFE.
            using (SqlConnection conn = new SqlConnection("context connection=true"))
            {
                conn.Open();
                // Note: Using AtomEmbeddings as the source of truth for spatial location.
                string query = "SELECT TOP 1 SpatialGeometry FROM dbo.AtomEmbeddings WHERE AtomId = @id ORDER BY CreatedAt DESC";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", atomId.Value);
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        point = (SqlGeometry)result;
                    }
                }
            }

            if (!point.IsNull && point.STGeometryType().Value == "Point")
            {
                _points.Add(new PointWithTimestamp
                {
                    X = point.STX.Value,
                    Y = point.STY.Value,
                    Z = point.Z.IsNull ? 0.0 : point.Z.Value,
                    Timestamp = timestamp.Value
                });
            }
        }

        /// <summary>
        /// Merges the state of another instance of the aggregate.
        /// </summary>
        public void Merge(BuildPathFromAtoms other)
        {
            _points.AddRange(other._points);
        }

        /// <summary>
        /// Terminates the aggregation and returns the final result.
        /// Constructs a LineStringZM geometry from the collected points.
        /// </summary>
        public SqlGeometry Terminate()
        {
            if (_points.Count < 2)
            {
                return SqlGeometry.Null; // A path requires at least two points.
            }

            // Sort the points by timestamp to ensure the path is in chronological order.
            _points.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

            // Use SqlGeometryBuilder to construct the LineString.
            SqlGeometryBuilder builder = new SqlGeometryBuilder();
            builder.SetSrid(4326); // Assuming WGS 84, should match source data.
            builder.BeginGeometry(OpenGisGeometryType.LineString);
            
            // The M value must be a double. Using ToOADate for conversion.
            builder.BeginFigure(_points[0].X, _points[0].Y, _points[0].Z, _points[0].Timestamp.ToOADate());

            for (int i = 1; i < _points.Count; i++)
            {
                builder.AddLine(_points[i].X, _points[i].Y, _points[i].Z, _points[i].Timestamp.ToOADate());
            }

            builder.EndFigure();
            builder.EndGeometry();

            return builder.ConstructedGeometry;
        }

        // IBinarySerialize implementation for persistence.
        public void Read(BinaryReader r)
        {
            int count = r.ReadInt32();
            _points = new List<PointWithTimestamp>(count);
            for (int i = 0; i < count; i++)
            {
                _points.Add(new PointWithTimestamp
                {
                    X = r.ReadDouble(),
                    Y = r.ReadDouble(),
                    Z = r.ReadDouble(),
                    Timestamp = DateTime.FromBinary(r.ReadInt64())
                });
            }
        }

        public void Write(BinaryWriter w)
        {
            w.Write(_points.Count);
            foreach (var p in _points)
            {
                w.Write(p.X);
                w.Write(p.Y);
                w.Write(p.Z);
                w.Write(p.Timestamp.ToBinary());
            }
        }
    }
}
