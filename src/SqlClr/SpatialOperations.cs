using System;
using System.Data.SqlTypes;
using System.Globalization;
using Microsoft.SqlServer.Server;
using Microsoft.SqlServer.Types;

namespace SqlClrFunctions
{
    /// <summary>
    /// Spatial operations for geometric AI reasoning
    /// </summary>
    public class SpatialOperations
    {
        /// <summary>
        /// Create a point cloud from an array of coordinates
        /// Returns a MULTIPOINT geometry
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = false)]
        public static SqlGeometry CreatePointCloud(SqlString coordinates)
        {
            if (coordinates.IsNull)
                return SqlGeometry.Null;

            // Expected format: "x1 y1, x2 y2, x3 y3, ..."
            string[] points = coordinates.Value.Split(',');

            var builder = new SqlGeometryBuilder();
            builder.SetSrid(0);
            builder.BeginGeometry(OpenGisGeometryType.MultiPoint);

            foreach (string point in points)
            {
                string[] coords = point.Trim().Split(' ');
                if (coords.Length >= 2)
                {
                    double x = double.Parse(coords[0], CultureInfo.InvariantCulture);
                    double y = double.Parse(coords[1], CultureInfo.InvariantCulture);
                    double? z = coords.Length >= 3 ? (double?)double.Parse(coords[2], CultureInfo.InvariantCulture) : null;
                    double? m = coords.Length >= 4 ? (double?)double.Parse(coords[3], CultureInfo.InvariantCulture) : null;

                    builder.BeginGeometry(OpenGisGeometryType.Point);
                    if (z.HasValue)
                    {
                        if (m.HasValue)
                        {
                            builder.BeginFigure(x, y, z.Value, m.Value);
                        }
                        else
                        {
                            builder.BeginFigure(x, y, z.Value, null);
                        }
                    }
                    else
                    {
                        builder.BeginFigure(x, y);
                    }
                    builder.EndFigure();
                    builder.EndGeometry();
                }
            }

            builder.EndGeometry();
            return builder.ConstructedGeometry;
        }

        /// <summary>
        /// Compute convex hull of a point cloud (decision boundary)
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = false)]
        public static SqlGeometry ConvexHull(SqlGeometry geometry)
        {
            if (geometry.IsNull)
                return SqlGeometry.Null;

            return geometry.STConvexHull();
        }

        /// <summary>
        /// Check if a point is within a geometric region (classification)
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = false)]
        public static SqlBoolean PointInRegion(SqlGeometry point, SqlGeometry region)
        {
            if (point.IsNull || region.IsNull)
                return SqlBoolean.Null;

            return region.STContains(point);
        }

        /// <summary>
        /// Find intersection area between two regions (feature overlap)
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = false)]
        public static SqlDouble RegionOverlap(SqlGeometry region1, SqlGeometry region2)
        {
            if (region1.IsNull || region2.IsNull)
                return SqlDouble.Null;

            SqlGeometry intersection = region1.STIntersection(region2);
            if (intersection.IsNull)
                return new SqlDouble(0);

            return intersection.STArea();
        }

        /// <summary>
        /// Compute centroid of a geometric region
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = false)]
        public static SqlGeometry Centroid(SqlGeometry geometry)
        {
            if (geometry.IsNull)
                return SqlGeometry.Null;

            return geometry.STCentroid();
        }

        /// <summary>
        /// Create a LINESTRING from a large array of float weights
        /// Bypasses NetTopologySuite serialization for massive tensors (millions of points)
        /// X = index, Y = weight value
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = true)]
        public static SqlGeometry CreateLineStringFromWeights(SqlBytes weightsData, SqlInt32 srid)
        {
            if (weightsData.IsNull)
                return SqlGeometry.Null;

            // Deserialize float array from binary
            byte[] data = weightsData.Value;
            int floatCount = data.Length / 4;

            if (floatCount < 2)
                return SqlGeometry.Null; // LINESTRING needs at least 2 points

            var builder = new SqlGeometryBuilder();
            builder.SetSrid(srid.IsNull ? 0 : srid.Value);
            builder.BeginGeometry(OpenGisGeometryType.LineString);
            builder.BeginFigure(0, BitConverter.ToSingle(data, 0)); // First point

            // Add remaining points
            for (int i = 1; i < floatCount; i++)
            {
                float weight = BitConverter.ToSingle(data, i * 4);
                builder.AddLine(i, weight);
            }

            builder.EndFigure();
            builder.EndGeometry();
            return builder.ConstructedGeometry;
        }

        /// <summary>
        /// Create a MULTILINESTRING from a large array of float weights
        /// Automatically chunks into segments to handle massive tensors
        /// X = index, Y = weight value
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = true)]
        public static SqlGeometry CreateMultiLineStringFromWeights(SqlBytes weightsData, SqlInt32 srid, SqlInt32 pointsPerSegment)
        {
            if (weightsData.IsNull)
                return SqlGeometry.Null;

            byte[] data = weightsData.Value;
            int floatCount = data.Length / 4;

            if (floatCount < 2)
                return SqlGeometry.Null;

            int chunkSize = pointsPerSegment.IsNull ? 100000 : pointsPerSegment.Value;

            var builder = new SqlGeometryBuilder();
            builder.SetSrid(srid.IsNull ? 0 : srid.Value);
            builder.BeginGeometry(OpenGisGeometryType.MultiLineString);

            int currentIndex = 0;
            while (currentIndex < floatCount)
            {
                int endIndex = Math.Min(currentIndex + chunkSize, floatCount);
                
                builder.BeginGeometry(OpenGisGeometryType.LineString);
                
                // Start figure with first point of segment
                float firstWeight = BitConverter.ToSingle(data, currentIndex * 4);
                builder.BeginFigure(currentIndex, firstWeight);
                
                // Add remaining points in this segment
                for (int i = currentIndex + 1; i < endIndex; i++)
                {
                    float weight = BitConverter.ToSingle(data, i * 4);
                    builder.AddLine(i, weight);
                }
                
                builder.EndFigure();
                builder.EndGeometry();
                
                currentIndex = endIndex;
            }

            builder.EndGeometry();
            return builder.ConstructedGeometry;
        }
    }
}
