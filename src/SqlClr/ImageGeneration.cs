using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using Microsoft.SqlServer.Types;

namespace SqlClrFunctions
{
    /// <summary>
    /// CLR helpers for image diffusion routines executed from T-SQL.
    /// </summary>
    public static class ImageGeneration
    {
        private sealed class Patch
        {
            public Patch(int x, int y, double spatialX, double spatialY, double spatialZ, SqlGeometry geometry)
            {
                X = x;
                Y = y;
                SpatialX = spatialX;
                SpatialY = spatialY;
                SpatialZ = spatialZ;
                Geometry = geometry;
            }

            public int X { get; }
            public int Y { get; }
            public double SpatialX { get; }
            public double SpatialY { get; }
            public double SpatialZ { get; }
            public SqlGeometry Geometry { get; }
        }

        [SqlFunction(
            DataAccess = DataAccessKind.None,
            IsDeterministic = false,
            IsPrecise = false,
            TableDefinition = "patch_x INT, patch_y INT, spatial_x FLOAT, spatial_y FLOAT, spatial_z FLOAT, patch geometry",
            FillRowMethodName = nameof(FillPatchRow))]
        public static IEnumerable GenerateGuidedPatches(
            SqlInt32 width,
            SqlInt32 height,
            SqlInt32 patchSize,
            SqlInt32 steps,
            SqlDouble guidanceScale,
            SqlDouble guideX,
            SqlDouble guideY,
            SqlDouble guideZ,
            SqlInt32 seed)
        {
            int w = width.IsNull || width.Value <= 0 ? 64 : width.Value;
            int h = height.IsNull || height.Value <= 0 ? 64 : height.Value;
            int stride = patchSize.IsNull || patchSize.Value <= 0 ? 16 : patchSize.Value;
            int totalSteps = steps.IsNull || steps.Value < 0 ? 0 : steps.Value;
            double scale = guidanceScale.IsNull ? 7.5 : guidanceScale.Value;
            double gx = guideX.IsNull ? 0 : guideX.Value;
            double gy = guideY.IsNull ? 0 : guideY.Value;
            double gz = guideZ.IsNull ? 0 : guideZ.Value;
            int randomSeed = seed.IsNull ? Environment.TickCount : seed.Value;

            var random = new Random(randomSeed);
            var patches = new List<Patch>();

            for (int x = 0; x < w; x += stride)
            {
                for (int y = 0; y < h; y += stride)
                {
                    double px = RandomRange(random);
                    double py = RandomRange(random);
                    double pz = RandomRange(random);

                    for (int step = 0; step < totalSteps; step++)
                    {
                        px += scale * (gx - px);
                        py += scale * (gy - py);
                        pz += scale * (gz - pz);
                    }

                    // Create geometry point (normalized coordinates for stability)
                    double normalizedX = (x + 0.5) / w;
                    double normalizedY = (y + 0.5) / h;

                    var builder = new SqlGeometryBuilder();
                    builder.SetSrid(0);
                    builder.BeginGeometry(OpenGisGeometryType.Point);
                    builder.BeginFigure(normalizedX, normalizedY, pz, null);
                    builder.EndFigure();
                    builder.EndGeometry();

                    var patch = new Patch(
                        x,
                        y,
                        normalizedX,
                        normalizedY,
                        pz,
                        builder.ConstructedGeometry);

                    patches.Add(patch);
                }
            }

            return patches;
        }

        [SqlFunction(IsDeterministic = false, IsPrecise = false)]
        public static SqlGeometry GenerateGuidedGeometry(
            SqlInt32 width,
            SqlInt32 height,
            SqlInt32 patchSize,
            SqlInt32 steps,
            SqlDouble guidanceScale,
            SqlDouble guideX,
            SqlDouble guideY,
            SqlDouble guideZ,
            SqlInt32 seed)
        {
            var patches = GenerateGuidedPatches(width, height, patchSize, steps, guidanceScale, guideX, guideY, guideZ, seed);

            var builder = new SqlGeometryBuilder();
            builder.SetSrid(0);
            builder.BeginGeometry(OpenGisGeometryType.GeometryCollection);

            foreach (Patch patch in patches)
            {
                builder.BeginGeometry(OpenGisGeometryType.Point);
                builder.BeginFigure(patch.SpatialX, patch.SpatialY, patch.SpatialZ, null);
                builder.EndFigure();
                builder.EndGeometry();
            }

            builder.EndGeometry();
            return builder.ConstructedGeometry;
        }

        public static void FillPatchRow(
            object obj,
            out SqlInt32 patchX,
            out SqlInt32 patchY,
            out SqlDouble spatialX,
            out SqlDouble spatialY,
            out SqlDouble spatialZ,
            out SqlGeometry patchGeometry)
        {
            var patch = (Patch)obj;
            patchX = new SqlInt32(patch.X);
            patchY = new SqlInt32(patch.Y);
            spatialX = new SqlDouble(patch.SpatialX);
            spatialY = new SqlDouble(patch.SpatialY);
            spatialZ = new SqlDouble(patch.SpatialZ);
            patchGeometry = patch.Geometry;
        }

        private static double RandomRange(Random random)
        {
            return random.NextDouble() * 2 - 1;
        }
    }
}
