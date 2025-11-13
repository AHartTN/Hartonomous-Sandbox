using System;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using Microsoft.SqlServer.Types;
using Newtonsoft.Json;
using SqlClrFunctions.Core;

namespace SqlClrFunctions
{
    /// <summary>
    /// CLR functions for spatial operations and projections.
    /// </summary>
    public static class SpatialOperations
    {
        /// <summary>
        /// Projects a high-dimensional vector to a 3D GEOMETRY point using a landmark-based projection.
        /// This is the correct, mathematically sound replacement for the placeholder logic.
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = false, DataAccess = DataAccessKind.None)]
        public static SqlGeometry fn_ProjectTo3D(SqlBytes vector)
        {
            if (vector.IsNull)
            {
                return SqlGeometry.Null;
            }

            try
            {
                // Convert SqlBytes to float array using the existing interop helper
                var vectorFloats = SqlBytesInterop.GetFloatArray(vector, out _);

                // Call the projection logic from the bridge library
                var (x, y, z) = LandmarkProjection.ProjectTo3D(vectorFloats);

                // Build the GEOMETRY point
                var builder = new SqlGeometryBuilder();
                builder.SetSrid(0); // Use the default spatial reference identifier
                builder.BeginGeometry(OpenGisGeometryType.Point);
                builder.BeginFigure(x, y, z, null);
                builder.EndFigure();
                builder.EndGeometry();

                return builder.ConstructedGeometry;
            }
            catch (Exception e)
            {
                // Log the error and return null. In a real system, you might write to an error log table.
                SqlContext.Pipe.Send($"Error in fn_ProjectTo3D: {e.Message}");
                return SqlGeometry.Null;
            }
        }
    }
}
