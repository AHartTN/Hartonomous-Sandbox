namespace Hartonomous.Database.CLR.Tests.Core;

/// <summary>
/// Core Hilbert Curve mathematics - extracted for unit testing
/// Based on public domain implementation by John Skilling
/// </summary>
public static class HilbertMath
{
    /// <summary>
    /// Compact 3D Hilbert curve calculation
    /// Converts 3D integer coordinates to 1D Hilbert curve value
    /// </summary>
    /// <param name="x">X coordinate (0 to 2^bits - 1)</param>
    /// <param name="y">Y coordinate (0 to 2^bits - 1)</param>
    /// <param name="z">Z coordinate (0 to 2^bits - 1)</param>
    /// <param name="bits">Precision in bits per dimension (typically 21)</param>
    /// <returns>Hilbert curve value</returns>
    public static long Hilbert3D(long x, long y, long z, int bits)
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
    public static (long x, long y, long z) InverseHilbert3D(long hilbertValue, int bits)
    {
        long x = 0, y = 0, z = 0;

        for (int i = bits - 1; i >= 0; i--)
        {
            long mask = 7L << (i * 3);
            long curBits = (hilbertValue & mask) >> (i * 3);

            long qc = (curBits >> 2) & 1;
            long qd = (curBits >> 1) & 1;
            long qa = curBits & 1;
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

        return (x, y, z);
    }
}
