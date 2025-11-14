using Microsoft.SqlServer.Server;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Types;

/// <summary>
/// Hilbert Curve spatial indexing for 3D semantic space
/// Provides 1D ordering that preserves spatial locality
/// </summary>
public static partial class SpatialFunctions
{
    /// <summary>
    /// Computes Hilbert curve value for a 3D GEOMETRY point
    /// Uses 21-bit precision per dimension (63 total bits fitting in BIGINT)
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

        int p = precision.IsNull ? 21 : precision.Value;
        long maxCoord = (1L << p) - 1;

        // Normalize and scale to integer grid
        long ix = (long)(((x - minX) / rangeX) * maxCoord);
        long iy = (long)(((y - minY) / rangeY) * maxCoord);
        long iz = (long)(((z - minZ) / rangeZ) * maxCoord);

        // Clamp to valid range
        ix = System.Math.Max(0, System.Math.Min(maxCoord, ix));
        iy = System.Math.Max(0, System.Math.Min(maxCoord, iy));
        iz = System.Math.Max(0, System.Math.Min(maxCoord, iz));

        return new SqlInt64(Hilbert3D(ix, iy, iz, p));
    }

    /// <summary>
    /// Compact 3D Hilbert curve calculation
    /// Based on public domain implementation by John Skilling
    /// </summary>
    private static long Hilbert3D(long x, long y, long z, int bits)
    {
        long h = 0;
        
        for (int i = bits - 1; i >= 0; i--)
        {
            long q = 1L << i;
            
            long qa = (x & q) != 0 ? 1L : 0L;
            long qb = (y & q) != 0 ? 1L : 0L;
            long qc = (z & q) != 0 ? 1L : 0L;

            // Hilbert curve state machine
            long qd = qa ^ qb;
            
            h = (h << 3) | ((qc << 2) | (qd << 1) | (qa ^ qd ^ qc));

            // Rotate coordinates for next iteration
            if (qc == 1)
            {
                long temp = x;
                x = y;
                y = temp;
            }
            
            if (qd == 1)
            {
                x = x ^ ((1L << (i + 1)) - 1);
                z = z ^ ((1L << (i + 1)) - 1);
            }
        }
        
        return h;
    }

    /// <summary>
    /// Inverse Hilbert curve - convert 1D value back to 3D coordinates
    /// Useful for visualization and debugging
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

        var minPoint = SqlGeometry.Point(minX, minY, 0);
        minPoint.Z = new SqlDouble(minZ);

        return clr_ComputeHilbertValue(minPoint, precision);
    }
}
