using Microsoft.SqlServer.Server;
using System;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace Hartonomous.Clr
{
    /// <summary>
    /// Represents a 3D point with a timestamp, used for building trajectories.
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
    /// from a sequence of atoms, ordered by timestamp using NetTopologySuite.
    /// </summary>
    [Serializable]
    [SqlUserDefinedAggregate(
        Format.UserDefined,
        IsInvariantToNulls = true,
        IsInvariantToDuplicates = false,
        IsInvariantToOrder = false,
        MaxByteSize = -1)
    ]
    public struct BuildPathFromAtoms : IBinarySerialize
    {
        private static readonly SqlServerBytesReader _geometryReader = new SqlServerBytesReader();
        private static readonly SqlServerBytesWriter _geometryWriter = new SqlServerBytesWriter();
        private static readonly GeometryFactory _geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
        
        private List<PointWithTimestamp> _points;

        public void Init()
        {
            _points = new List<PointWithTimestamp>();
        }

        /// <summary>
        /// Accumulates data for each row - fetches spatial location from database.
        /// </summary>
        public void Accumulate(SqlInt64 atomId, SqlDateTime timestamp)
        {
            if (atomId.IsNull || timestamp.IsNull)
                return;

            using (SqlConnection conn = new SqlConnection("context connection=true"))
            {
                conn.Open();
                string query = "SELECT TOP 1 SpatialGeometry FROM dbo.AtomEmbeddings WHERE AtomId = @id ORDER BY CreatedAt DESC";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", atomId.Value);
                    object result = cmd.ExecuteScalar();
                    
                    if (result != null && result != DBNull.Value)
                    {
                        try
                        {
                            // SpatialGeometry column stores geometry as SqlBytes (binary format)
                            byte[] geometryBytes = null;
                            
                            if (result is SqlBytes sqlBytes && !sqlBytes.IsNull)
                            {
                                geometryBytes = sqlBytes.Value;
                            }
                            else if (result is byte[] bytes)
                            {
                                geometryBytes = bytes;
                            }
                            
                            if (geometryBytes != null)
                            {
                                var geometry = _geometryReader.Read(geometryBytes);
                                
                                if (geometry != null && !geometry.IsEmpty && geometry is Point point)
                                {
                                    _points.Add(new PointWithTimestamp
                                    {
                                        X = point.X,
                                        Y = point.Y,
                                        Z = double.IsNaN(point.Z) ? 0.0 : point.Z,
                                        Timestamp = timestamp.Value
                                    });
                                }
                            }
                        }
                        catch
                        {
                            // Skip invalid geometries
                        }
                    }
                }
            }
        }

        public void Merge(BuildPathFromAtoms other)
        {
            _points.AddRange(other._points);
        }

        /// <summary>
        /// Returns LineString with Z and M coordinates (M = timestamp as OADate).
        /// </summary>
        public SqlBytes Terminate()
        {
            if (_points.Count < 2)
                return SqlBytes.Null;

            try
            {
                // Sort by timestamp
                _points.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

                // Build coordinates with Z and M values
                var coordinates = new CoordinateM[_points.Count];
                for (int i = 0; i < _points.Count; i++)
                {
                    coordinates[i] = new CoordinateZM(
                        _points[i].X,
                        _points[i].Y,
                        _points[i].Z,
                        _points[i].Timestamp.ToOADate()
                    );
                }

                var lineString = _geometryFactory.CreateLineString(coordinates);
                var geometryBytes = _geometryWriter.Write(lineString);

                return new SqlBytes(geometryBytes);
            }
            catch
            {
                return SqlBytes.Null;
            }
        }

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
