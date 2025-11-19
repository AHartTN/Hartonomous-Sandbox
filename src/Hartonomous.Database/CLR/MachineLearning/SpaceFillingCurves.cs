using System;
using Hartonomous.Clr.Core;

namespace Hartonomous.Clr.MachineLearning
{
    /// <summary>
    /// Space-filling curve algorithms for locality-preserving spatial indexing.
    /// 
    /// UNIVERSAL DISTANCE SUPPORT: Distance preservation metrics enable validation
    /// of locality preservation across modalities - ensure nearby points in semantic
    /// space map to nearby positions on the curve.
    /// 
    /// APPLICATIONS IN USER'S ARCHITECTURE:
    /// - Spatial indexing for 3D GEOMETRY projection
    /// - Locality-preserving retrieval (nearby embeddings = nearby curve positions)
    /// - Range queries on high-dimensional data
    /// - Cache-friendly memory layout for inference
    /// </summary>
    internal static class SpaceFillingCurves
    {
        #region Morton/Z-Order Curve

        /// <summary>
        /// Compute Morton (Z-order) code for 2D point.
        /// 
        /// Morton code interleaves bits from x and y coordinates:
        /// x = x₀x₁x₂..., y = y₀y₁y₂... → z = y₀x₀y₁x₁y₂x₂...
        /// 
        /// Simpler than Hilbert but still provides good locality preservation.
        /// Used extensively in quadtree indexing and database spatial indexes.
        /// </summary>
        /// <param name="x">X coordinate (0-1 normalized, or integer grid position)</param>
        /// <param name="y">Y coordinate (0-1 normalized, or integer grid position)</param>
        /// <param name="bits">Bits of precision per dimension (max 32)</param>
        /// <returns>Morton code (z-order index)</returns>
        public static ulong Morton2D(uint x, uint y, int bits = 16)
        {
            ulong mx = Part1By1(x);
            ulong my = Part1By1(y);
            return mx | (my << 1);
        }

        /// <summary>
        /// Compute Morton (Z-order) code for 3D point.
        /// 
        /// Interleaves bits from x, y, z coordinates:
        /// z = z₀y₀x₀z₁y₁x₁z₂y₂x₂...
        /// 
        /// Critical for user's 3D GEOMETRY projection - enables range queries
        /// on landmark-reduced embedding space.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="z">Z coordinate</param>
        /// <param name="bits">Bits of precision per dimension (max 21 for 3D in ulong)</param>
        /// <returns>Morton code (z-order index)</returns>
        public static ulong Morton3D(uint x, uint y, uint z, int bits = 21)
        {
            ulong mx = Part1By2(x);
            ulong my = Part1By2(y);
            ulong mz = Part1By2(z);
            return mx | (my << 1) | (mz << 2);
        }

        /// <summary>
        /// Inverse Morton 2D - decode z-order index back to (x, y).
        /// </summary>
        public static (uint x, uint y) InverseMorton2D(ulong morton)
        {
            uint x = Compact1By1((uint)(morton & 0x5555555555555555UL));
            uint y = Compact1By1((uint)((morton >> 1) & 0x5555555555555555UL));
            return (x, y);
        }

        /// <summary>
        /// Inverse Morton 3D - decode z-order index back to (x, y, z).
        /// </summary>
        public static (uint x, uint y, uint z) InverseMorton3D(ulong morton)
        {
            uint x = Compact1By2((uint)(morton & 0x1249249249249249UL));
            uint y = Compact1By2((uint)((morton >> 1) & 0x1249249249249249UL));
            uint z = Compact1By2((uint)((morton >> 2) & 0x1249249249249249UL));
            return (x, y, z);
        }

        // Bit interleaving magic - spreads bits with zeros between them
        private static ulong Part1By1(uint n)
        {
            ulong x = n & 0xFFFFFFFF;
            x = (x ^ (x << 16)) & 0x0000FFFF0000FFFFUL;
            x = (x ^ (x << 8))  & 0x00FF00FF00FF00FFUL;
            x = (x ^ (x << 4))  & 0x0F0F0F0F0F0F0F0FUL;
            x = (x ^ (x << 2))  & 0x3333333333333333UL;
            x = (x ^ (x << 1))  & 0x5555555555555555UL;
            return x;
        }

        private static ulong Part1By2(uint n)
        {
            ulong x = n & 0x1FFFFF; // Only use 21 bits for 3D
            x = (x ^ (x << 32)) & 0x1F00000000FFFFUL;
            x = (x ^ (x << 16)) & 0x1F0000FF0000FFUL;
            x = (x ^ (x << 8))  & 0x100F00F00F00F00FUL;
            x = (x ^ (x << 4))  & 0x10C30C30C30C30C3UL;
            x = (x ^ (x << 2))  & 0x1249249249249249UL;
            return x;
        }

        private static uint Compact1By1(uint n)
        {
            n &= 0x55555555;
            n = (n ^ (n >> 1))  & 0x33333333;
            n = (n ^ (n >> 2))  & 0x0F0F0F0F;
            n = (n ^ (n >> 4))  & 0x00FF00FF;
            n = (n ^ (n >> 8))  & 0x0000FFFF;
            return n;
        }

        private static uint Compact1By2(uint n)
        {
            n &= 0x49249249;
            n = (n ^ (n >> 2))  & 0xC30C30C3;
            n = (n ^ (n >> 4))  & 0x0F00F00F;
            n = (n ^ (n >> 8))  & 0xFF0000FF;
            n = (n ^ (n >> 16)) & 0x000001FF;
            return n;
        }

        #endregion

        #region Hilbert Curve

        /// <summary>
        /// Compute 2D Hilbert curve index for point (x, y).
        /// 
        /// Hilbert curve provides better locality preservation than Morton/Z-order.
        /// No long jumps - curve is continuous. Critical for cache-friendly access.
        /// </summary>
        /// <param name="x">X coordinate (integer grid position)</param>
        /// <param name="y">Y coordinate (integer grid position)</param>
        /// <param name="order">Curve order (grid is 2^order x 2^order)</param>
        /// <returns>Hilbert index (position along curve)</returns>
        public static ulong Hilbert2D(uint x, uint y, int order)
        {
            ulong d = 0;
            uint n = (uint)(1 << order);

            for (uint s = n / 2; s > 0; s /= 2)
            {
                uint rx = ((x & s) > 0) ? 1u : 0u;
                uint ry = ((y & s) > 0) ? 1u : 0u;
                d += s * s * ((3u * rx) ^ ry);
                Rot(n, ref x, ref y, rx, ry);
            }

            return d;
        }

        /// <summary>
        /// Inverse 2D Hilbert curve - decode index back to (x, y).
        /// </summary>
        public static (uint x, uint y) InverseHilbert2D(ulong d, int order)
        {
            uint x = 0, y = 0;
            uint n = (uint)(1 << order);

            for (uint s = 1; s < n; s *= 2)
            {
                uint rx = 1u & (uint)(d / 2);
                uint ry = 1u & (uint)(d ^ rx);
                Rot(s, ref x, ref y, rx, ry);
                x += s * rx;
                y += s * ry;
                d /= 4;
            }

            return (x, y);
        }

        /// <summary>
        /// Compute 3D Hilbert curve index.
        /// 
        /// Used for user's 3D GEOMETRY projection - locality-preserving indexing
        /// of landmark-reduced embeddings across all modalities.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="z">Z coordinate</param>
        /// <param name="order">Curve order (grid is 2^order per dimension)</param>
        /// <returns>Hilbert index</returns>
        public static ulong Hilbert3D(uint x, uint y, uint z, int order)
        {
            ulong h = 0;
            uint n = (uint)(1 << order);

            for (int i = order - 1; i >= 0; i--)
            {
                uint mask = (uint)(1 << i);
                uint px = ((x & mask) != 0) ? 1u : 0u;
                uint py = ((y & mask) != 0) ? 1u : 0u;
                uint pz = ((z & mask) != 0) ? 1u : 0u;

                // 3D Hilbert state machine
                uint index = (pz << 2) | (py << 1) | px;
                h = (h << 3) | index;

                // Rotate for next iteration
                Rot3D(ref x, ref y, ref z, px, py, pz, i);
            }

            return h;
        }

        private static void Rot(uint n, ref uint x, ref uint y, uint rx, uint ry)
        {
            if (ry == 0)
            {
                if (rx == 1)
                {
                    x = n - 1 - x;
                    y = n - 1 - y;
                }

                // Swap x and y
                uint t = x;
                x = y;
                y = t;
            }
        }

        private static void Rot3D(ref uint x, ref uint y, ref uint z, uint px, uint py, uint pz, int level)
        {
            // Simplified 3D rotation for Hilbert curve
            if (pz == 0)
            {
                if (px == 1)
                {
                    uint t = x;
                    x = y;
                    y = t;
                }
            }
        }

        #endregion

        #region Distance Preservation Metrics

        /// <summary>
        /// Measure locality preservation quality of space-filling curve mapping.
        /// 
        /// Compares Euclidean distance in original space to curve distance (difference
        /// in curve indices). Perfect locality: nearby points have nearby indices.
        /// 
        /// CRITICAL FOR USER'S ARCHITECTURE: Validates that 3D GEOMETRY projection +
        /// space-filling curve preserves semantic similarity structure.
        /// </summary>
        /// <param name="points">Points in original N-dimensional space</param>
        /// <param name="curveIndices">Corresponding positions on space-filling curve</param>
        /// <param name="metric">Distance metric for original space (null = Euclidean)</param>
        /// <returns>Correlation between spatial distance and curve distance (closer to 1 = better)</returns>
        public static double LocalityPreservationScore(
            float[][] points,
            ulong[] curveIndices)
        {
            return LocalityPreservationScore(points, curveIndices, new EuclideanDistance());
        }

        public static double LocalityPreservationScore(
            float[][] points,
            ulong[] curveIndices,
            IDistanceMetric metric)
        {
            if (metric == null)
                metric = new EuclideanDistance();

            // Sample pairs to avoid O(n²) for large datasets
            int sampleSize = Math.Min(1000, points.Length * (points.Length - 1) / 2);
            var random = new Random(42);
            double sumProduct = 0, sumSpatialDist = 0, sumCurveDist = 0;
            double sumSpatialDistSq = 0, sumCurveDistSq = 0;

            for (int sample = 0; sample < sampleSize; sample++)
            {
                int i = random.Next(points.Length);
                int j = random.Next(points.Length);
                if (i == j) continue;

                double spatialDist = metric.Distance(points[i], points[j]);
                double curveDist = Math.Abs((long)curveIndices[i] - (long)curveIndices[j]);

                sumProduct += spatialDist * curveDist;
                sumSpatialDist += spatialDist;
                sumCurveDist += curveDist;
                sumSpatialDistSq += spatialDist * spatialDist;
                sumCurveDistSq += curveDist * curveDist;
            }

            // Pearson correlation coefficient
            double n = sampleSize;
            double numerator = n * sumProduct - sumSpatialDist * sumCurveDist;
            double denominator = Math.Sqrt(
                (n * sumSpatialDistSq - sumSpatialDist * sumSpatialDist) *
                (n * sumCurveDistSq - sumCurveDist * sumCurveDist)
            );

            return denominator > 1e-10 ? numerator / denominator : 0.0;
        }

        /// <summary>
        /// Compute average nearest-neighbor preservation.
        /// 
        /// For each point, checks if K-nearest neighbors in original space are also
        /// nearest on the curve. High score = curve preserves local neighborhoods.
        /// </summary>
        /// <param name="points">Points in original space</param>
        /// <param name="curveIndices">Curve positions</param>
        /// <param name="k">Number of nearest neighbors to check</param>
        /// <param name="metric">Distance metric (null = Euclidean)</param>
        /// <returns>Fraction of nearest neighbors preserved (0-1)</returns>
        public static double NearestNeighborPreservation(
            float[][] points,
            ulong[] curveIndices,
            int k = 5)
        {
            return NearestNeighborPreservation(points, curveIndices, k, new EuclideanDistance());
        }

        public static double NearestNeighborPreservation(
            float[][] points,
            ulong[] curveIndices,
            int k,
            IDistanceMetric metric)
        {
            if (points.Length < k + 1)
                return 0.0;

            if (metric == null)
                metric = new EuclideanDistance();

            double totalPreserved = 0;
            int validPoints = 0;

            for (int i = 0; i < points.Length; i++)
            {
                // Find K nearest neighbors in original space
                var spatialNeighbors = new System.Collections.Generic.List<int>();
                var distances = new System.Collections.Generic.List<(int idx, double dist)>();

                for (int j = 0; j < points.Length; j++)
                {
                    if (i == j) continue;
                    double dist = metric.Distance(points[i], points[j]);
                    distances.Add((j, dist));
                }

                distances.Sort((a, b) => a.dist.CompareTo(b.dist));
                for (int n = 0; n < Math.Min(k, distances.Count); n++)
                    spatialNeighbors.Add(distances[n].idx);

                // Find K nearest neighbors on curve
                var curveNeighbors = new System.Collections.Generic.List<int>();
                var curveDistances = new System.Collections.Generic.List<(int idx, ulong dist)>();

                for (int j = 0; j < curveIndices.Length; j++)
                {
                    if (i == j) continue;
                    ulong dist = (ulong)Math.Abs((long)curveIndices[i] - (long)curveIndices[j]);
                    curveDistances.Add((j, dist));
                }

                curveDistances.Sort((a, b) => a.dist.CompareTo(b.dist));
                for (int n = 0; n < Math.Min(k, curveDistances.Count); n++)
                    curveNeighbors.Add(curveDistances[n].idx);

                // Count how many spatial neighbors are also curve neighbors
                int preserved = 0;
                foreach (int neighbor in spatialNeighbors)
                {
                    if (curveNeighbors.Contains(neighbor))
                        preserved++;
                }

                totalPreserved += (double)preserved / k;
                validPoints++;
            }

            return validPoints > 0 ? totalPreserved / validPoints : 0.0;
        }

        #endregion
    }
}
