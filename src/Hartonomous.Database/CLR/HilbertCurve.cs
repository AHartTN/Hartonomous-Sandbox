using Microsoft.SqlServer.Server;
using System.Data.SqlTypes;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System;

/// <summary>
/// Hilbert Curve spatial indexing for 3D semantic space using NetTopologySuite
/// Provides 1D ordering that preserves spatial locality
/// </summary>
public static partial class SpatialFunctions
{
    private static readonly SqlServerBytesReader _geometryReader = new SqlServerBytesReader();
    private static readonly SqlServerBytesWriter _geometryWriter = new SqlServerBytesWriter();
    private static readonly GeometryFactory _geometryFactory = new GeometryFactory(new PrecisionModel(), 0);

    /// <summary>
    /// Computes Hilbert curve value for a 3D GEOMETRY point
    /// Uses 21-bit precision per dimension (63 total bits fitting in BIGINT)
    /// </summary>
    [SqlFunction(
        IsDeterministic = true, 
        IsPrecise = false, 
        DataAccess = DataAccessKind.None
    )]
    public static SqlInt64 clr_ComputeHilbertValue(SqlBytes spatialKey, SqlInt32 precision)
    {
        if (spatialKey.IsNull || spatialKey.Length == 0)
            return SqlInt64.Null;

        try
        {
            // Read geometry from SQL Server binary format
            var geometry = _geometryReader.Read(spatialKey.Value);
            
            if (geometry == null || geometry.IsEmpty)
                return SqlInt64.Null;

            var coord = geometry.Coordinate;
            double x = coord.X;
            double y = coord.Y;
            double z = double.IsNaN(coord.Z) ? 0 : coord.Z;

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
            ix = Math.Max(0, Math.Min(maxCoord, ix));
            iy = Math.Max(0, Math.Min(maxCoord, iy));
            iz = Math.Max(0, Math.Min(maxCoord, iz));

            return new SqlInt64(Hilbert3D(ix, iy, iz, p));
        }
        catch
        {
            return SqlInt64.Null;
        }
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
    /// </summary>
    [SqlFunction(
        IsDeterministic = true,
        IsPrecise = false,
        DataAccess = DataAccessKind.None
    )]
    public static SqlBytes clr_InverseHilbert(SqlInt64 hilbertValue, SqlInt32 precision)
    {
        if (hilbertValue.IsNull)
            return SqlBytes.Null;

        try
        {
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

            // Create NTS point with Z
            var point = _geometryFactory.CreatePoint(new CoordinateZ(normX, normY, normZ));
            var geometryBytes = _geometryWriter.Write(point);

            return new SqlBytes(geometryBytes);
        }
        catch
        {
            return SqlBytes.Null;
        }
    }

    /// <summary>
    /// Compute Hilbert range for a spatial bounding box
    /// </summary>
    [SqlFunction(
        IsDeterministic = true,
        IsPrecise = false,
        DataAccess = DataAccessKind.None
    )]
    public static SqlInt64 clr_HilbertRangeStart(SqlBytes boundingBox, SqlInt32 precision)
    {
        if (boundingBox.IsNull || boundingBox.Length == 0)
            return SqlInt64.Null;

        try
        {
            var geometry = _geometryReader.Read(boundingBox.Value);
            
            if (geometry == null || geometry.IsEmpty)
                return SqlInt64.Null;

            // Get envelope (bounding box)
            var envelope = geometry.EnvelopeInternal;
            double minX = envelope.MinX;
            double minY = envelope.MinY;
            double minZ = envelope.Centre.Z; // Use center Z for 2D geometries

            // Create minimum corner point
            var minPoint = _geometryFactory.CreatePoint(new CoordinateZ(minX, minY, minZ));
            var minPointBytes = _geometryWriter.Write(minPoint);

            return clr_ComputeHilbertValue(new SqlBytes(minPointBytes), precision);
        }
        catch
        {
            return SqlInt64.Null;
        }
    }
}
