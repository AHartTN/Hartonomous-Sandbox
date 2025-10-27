using System;
using System.Data.SqlTypes;
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
                    double x = double.Parse(coords[0]);
                    double y = double.Parse(coords[1]);

                    builder.BeginGeometry(OpenGisGeometryType.Point);
                    builder.BeginFigure(x, y);
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
    }
}
