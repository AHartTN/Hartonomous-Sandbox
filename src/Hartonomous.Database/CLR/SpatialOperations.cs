using System;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Hartonomous.Clr.Core;

namespace Hartonomous.Clr
{
    /// <summary>
    /// CLR functions for spatial operations and projections using NetTopologySuite.
    /// </summary>
    public static class SpatialOperations
    {
        private static readonly GeometryFactory _geometryFactory = new GeometryFactory(new PrecisionModel(), 0);
        private static readonly SqlServerBytesWriter _geometryWriter = new SqlServerBytesWriter();

        /// <summary>
        /// Projects a high-dimensional vector to a 3D GEOMETRY point using a landmark-based projection.
        /// Returns SQL Server geometry type compatible with built-in geometry.
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = false, DataAccess = DataAccessKind.None)]
        public static SqlBytes fn_ProjectTo3D(SqlBytes vector)
        {
            if (vector.IsNull)
            {
                return SqlBytes.Null;
            }

            try
            {
                // Convert SqlBytes to float array
                var vectorFloats = SqlBytesInterop.GetFloatArray(vector, out _);

                // Call the projection logic
                var (x, y, z) = LandmarkProjection.ProjectTo3D(vectorFloats);

                // Create NetTopologySuite Point with Z coordinate
                var coordinate = new CoordinateZ(x, y, z);
                var point = _geometryFactory.CreatePoint(coordinate);

                // Convert to SQL Server binary format
                var geometryBytes = _geometryWriter.Write(point);
                
                return new SqlBytes(geometryBytes);
            }
            catch (Exception e)
            {
                SqlContext.Pipe.Send($"Error in fn_ProjectTo3D: {e.Message}");
                return SqlBytes.Null;
            }
        }
    }
}
