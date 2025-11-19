using Microsoft.SqlServer.Server;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Types;
using Hartonomous.Clr.MachineLearning;

/// <summary>
/// Hilbert Curve spatial indexing for 3D semantic space
/// SQL Server UDF wrappers for SpaceFillingCurves.cs algorithms
/// Provides 1D ordering that preserves spatial locality
/// </summary>
public static partial class SpatialFunctions
{
    /// <summary>
    /// Computes Hilbert curve value for a 3D GEOMETRY point
    /// Uses 21-bit precision per dimension (63 total bits fitting in BIGINT)
    /// Wraps SpaceFillingCurves.Hilbert3D for SQL Server
    /// </summary>
    [SqlFunction(
        IsDeterministic = true, 
        IsPrecise = false, 
        DataAccess = DataAccessKind.None
    )]
    public static SqlInt64 clr_ComputeHilbertValue(SqlGeometry spatialKey, SqlInt32 precision)
    {
        if (spatialKey.IsNull || spatialKey.STIsEmpty().Value)
            return SqlInt64.Null;

        // Extract coordinates
        double x = spatialKey.STX.IsNull ? 0 : spatialKey.STX.Value;
        double y = spatialKey.STY.IsNull ? 0 : spatialKey.STY.Value;
        double z = spatialKey.Z.IsNull ? 0 : spatialKey.Z.Value;

        // Normalization parameters (in production, query from dbo.SpatialLandmarks)
        // For now, assume normalized [0, 1] space
        double minX = 0, minY = 0, minZ = 0;
        double rangeX = 1, rangeY = 1, rangeZ = 1;

        int order = precision.IsNull ? 21 : precision.Value;
        uint maxCoord = (1u << order) - 1;

        // Normalize and scale to integer grid
        uint ix = (uint)System.Math.Round(((x - minX) / rangeX) * maxCoord);
        uint iy = (uint)System.Math.Round(((y - minY) / rangeY) * maxCoord);
        uint iz = (uint)System.Math.Round(((z - minZ) / rangeZ) * maxCoord);

        // Clamp to valid range
        ix = System.Math.Min(maxCoord, ix);
        iy = System.Math.Min(maxCoord, iy);
        iz = System.Math.Min(maxCoord, iz);

        // Call pure SpaceFillingCurves algorithm
        ulong hilbertIndex = SpaceFillingCurves.Hilbert3D(ix, iy, iz, order);

        return new SqlInt64((long)hilbertIndex);
    }

    /// <summary>
    /// Computes Morton (Z-order) curve value for a 3D GEOMETRY point
    /// Uses 21-bit precision per dimension (63 total bits fitting in BIGINT)
    /// Morton curves are simpler than Hilbert but have slightly worse locality preservation
    /// </summary>
    [SqlFunction(
        IsDeterministic = true, 
        IsPrecise = false, 
        DataAccess = DataAccessKind.None
    )]
    public static SqlInt64 clr_ComputeMortonValue(SqlGeometry spatialKey, SqlInt32 precision)
    {
        if (spatialKey.IsNull || spatialKey.STIsEmpty().Value)
            return SqlInt64.Null;

        // Extract coordinates
        double x = spatialKey.STX.IsNull ? 0 : spatialKey.STX.Value;
        double y = spatialKey.STY.IsNull ? 0 : spatialKey.STY.Value;
        double z = spatialKey.Z.IsNull ? 0 : spatialKey.Z.Value;

        // Normalization parameters
        double minX = 0, minY = 0, minZ = 0;
        double rangeX = 1, rangeY = 1, rangeZ = 1;

        int bits = precision.IsNull ? 21 : precision.Value;
        uint maxCoord = (1u << bits) - 1;

        // Normalize and scale to integer grid
        uint ix = (uint)System.Math.Round(((x - minX) / rangeX) * maxCoord);
        uint iy = (uint)System.Math.Round(((y - minY) / rangeY) * maxCoord);
        uint iz = (uint)System.Math.Round(((z - minZ) / rangeZ) * maxCoord);

        // Clamp to valid range
        ix = System.Math.Min(maxCoord, ix);
        iy = System.Math.Min(maxCoord, iy);
        iz = System.Math.Min(maxCoord, iz);

        // Call pure SpaceFillingCurves algorithm
        ulong mortonIndex = SpaceFillingCurves.Morton3D(ix, iy, iz, bits);

        return new SqlInt64((long)mortonIndex);
    }

    /// <summary>
    /// Inverse Morton - convert 1D Morton value back to 3D coordinates
    /// </summary>
    [SqlFunction(
        IsDeterministic = true,
        IsPrecise = false,
        DataAccess = DataAccessKind.None
    )]
    public static SqlGeometry clr_InverseMorton(SqlInt64 mortonValue, SqlInt32 precision)
    {
        if (mortonValue.IsNull)
            return SqlGeometry.Null;

        ulong morton = (ulong)mortonValue.Value;
        
        // Decode Morton value to coordinates
        var (ix, iy, iz) = SpaceFillingCurves.InverseMorton3D(morton);

        int bits = precision.IsNull ? 21 : precision.Value;
        uint maxCoord = (1u << bits) - 1;

        // Normalize back to [0, 1] space
        double normX = (double)ix / maxCoord;
        double normY = (double)iy / maxCoord;
        double normZ = (double)iz / maxCoord;

        // Return as GEOMETRY point
        var builder = new SqlGeometryBuilder();
        builder.SetSrid(0);
        builder.BeginGeometry(OpenGisGeometryType.Point);
        builder.BeginFigure(normX, normY, normZ, null);
        builder.EndFigure();
        builder.EndGeometry();

        return builder.ConstructedGeometry;
    }

    /// <summary>
    /// Inverse Hilbert curve - convert 1D value back to 3D coordinates
    /// NOTE: Currently uses SpaceFillingCurves.InverseHilbert2D (3D inverse not yet implemented)
    /// TODO: Implement InverseHilbert3D in SpaceFillingCurves.cs for full round-trip
    /// </summary>
    [SqlFunction(
        IsDeterministic = true,
        IsPrecise = false,
        DataAccess = DataAccessKind.None
    )]
    public static SqlGeometry clr_InverseHilbert(SqlInt64 hilbertValue, SqlInt32 precision)
    {
        if (hilbertValue.IsNull)
            return SqlGeometry.Null;

        // TODO: This currently only works for 2D - need InverseHilbert3D
        // For now, fallback to original algorithm until SpaceFillingCurves.InverseHilbert3D is added
        int p = precision.IsNull ? 21 : precision.Value;
        long h = hilbertValue.Value;

        long x = 0, y = 0, z = 0;

        for (int i = p - 1; i >= 0; i--)
        {
            long mask = 7L << (i * 3);
            long bits = (h & mask) >> (i * 3);

            long qc = (bits >> 2) & 1;
            long qd = (bits >> 1) & 1;
            long qa = bits & 1;
            long qb = qa ^ qd;

            if (qd == 1)
            {
                x = x ^ ((1L << (i + 1)) - 1);
                z = z ^ ((1L << (i + 1)) - 1);
            }

            if (qc == 1)
            {
                long temp = x;
                x = y;
                y = temp;
            }

            long q = 1L << i;
            if (qa == 1) x |= q;
            if (qb == 1) y |= q;
            if (qc == 1) z |= q;
        }

        long maxCoord = (1L << p) - 1;
        double normX = (double)x / maxCoord;
        double normY = (double)y / maxCoord;
        double normZ = (double)z / maxCoord;

        // Return as GEOMETRY point
        var builder = new SqlGeometryBuilder();
        builder.SetSrid(0);
        builder.BeginGeometry(OpenGisGeometryType.Point);
        builder.BeginFigure(normX, normY, normZ, null);
        builder.EndFigure();
        builder.EndGeometry();

        return builder.ConstructedGeometry;
    }

    /// <summary>
    /// Compute Hilbert range for a spatial bounding box
    /// Useful for range queries on the Hilbert index
    /// </summary>
    [SqlFunction(
        IsDeterministic = true,
        IsPrecise = false,
        DataAccess = DataAccessKind.None
    )]
    public static SqlInt64 clr_HilbertRangeStart(SqlGeometry boundingBox, SqlInt32 precision)
    {
        if (boundingBox.IsNull || boundingBox.STIsEmpty().Value)
            return SqlInt64.Null;

        // Get minimum corner of bounding box
        double minX = boundingBox.STEnvelope().STPointN(1).STX.Value;
        double minY = boundingBox.STEnvelope().STPointN(1).STY.Value;
        double minZ = boundingBox.STEnvelope().STPointN(1).Z.IsNull ? 0 : boundingBox.STEnvelope().STPointN(1).Z.Value;

        // Create 3D point using WKT - SqlGeometry.Z is read-only
        var wkt = $"POINT ({minX} {minY} {minZ})";
        var minPoint = SqlGeometry.STGeomFromText(new SqlChars(wkt), 0);

        return clr_ComputeHilbertValue(minPoint, precision);
    }
}
