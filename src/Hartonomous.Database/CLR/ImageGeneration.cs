using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using Microsoft.SqlServer.Types;

using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SqlClrFunctions
{
    /// <summary>
    /// CLR helpers for image diffusion routines executed from T-SQL.
    /// </summary>
    public static class ImageGeneration
    {
        /// <summary>
        /// Rasterizes a SqlGeometry object containing shapes (Polygons, LineStrings) into a PNG image.
        /// This is a "shape-to-content" function for the visual modality.
        /// Color is derived from the Z-coordinate.
        /// Opacity is derived from the M-coordinate.
        /// </summary>
        [SqlFunction(IsDeterministic = true, DataAccess = DataAccessKind.None)]
        public static SqlBytes GenerateImageFromShapes(SqlGeometry shapes, SqlInt32 width, SqlInt32 height)
        {
            if (shapes.IsNull) return SqlBytes.Null;

            int w = width.IsNull || width.Value <= 0 ? 256 : width.Value;
            int h = height.IsNull || height.Value <= 0 ? 256 : height.Value;

            try
            {
                using (var bmp = new Bitmap(w, h))
                using (var g = Graphics.FromImage(bmp))
                using (var ms = new MemoryStream())
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.Clear(Color.Transparent);

                    // Iterate through each geometry in the collection if it's a collection
                    if (shapes.STNumGeometries() > 1)
                    {
                        for (int i = 1; i <= shapes.STNumGeometries(); i++)
                        {
                            DrawGeometry(g, shapes.STGeometryN(i), w, h);
                        }
                    }
                    else
                    {
                        DrawGeometry(g, shapes, w, h);
                    }

                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    return new SqlBytes(ms.ToArray());
                }
            }
            catch (Exception)
            {
                // Could log the error, but for now, just return null on failure.
                return SqlBytes.Null;
            }
        }

        private static void DrawGeometry(Graphics g, SqlGeometry geom, int width, int height)
        {
            if (geom.IsNull) return;

            string geomType = geom.STGeometryType().Value;

            if (geomType == "Polygon")
            {
                var points = new PointF[geom.STNumPoints().Value];
                for (int i = 0; i < points.Length; i++)
                {
                    var p = geom.STPointN(i + 1);
                    points[i] = new PointF((float)p.STX.Value * width, (float)p.STY.Value * height);
                }

                if (points.Length > 2)
                {
                    // Use the Z and M of the first point to define the brush
                    var firstPoint = geom.STPointN(1);
                    Color color = ColorFromZ(firstPoint.Z.Value);
                    int alpha = AlphaFromM(firstPoint.M.Value);
                    using (var brush = new SolidBrush(Color.FromArgb(alpha, color)))
                    {
                        g.FillPolygon(brush, points);
                    }
                }
            }
            else if (geomType == "LineString")
            {
                var points = new PointF[geom.STNumPoints().Value];
                for (int i = 0; i < points.Length; i++)
                {
                    var p = geom.STPointN(i + 1);
                    points[i] = new PointF((float)p.STX.Value * width, (float)p.STY.Value * height);
                }

                if (points.Length > 1)
                {
                    var firstPoint = geom.STPointN(1);
                    Color color = ColorFromZ(firstPoint.Z.Value);
                    int alpha = AlphaFromM(firstPoint.M.Value);
                    using (var pen = new Pen(Color.FromArgb(alpha, color), 2))
                    {
                        g.DrawLines(pen, points);
                    }
                }
            }
        }

        // Helper to map a Z-coordinate (double, typically -1 to 1) to a color.
        private static Color ColorFromZ(double z)
        {
            // Simple HSV to RGB mapping: Z maps to Hue
            double hue = (z + 1) / 2.0 * 360; // Map -1..1 to 0..360
            return HsvToRgb(hue, 0.8, 0.9);
        }

        // Helper to map an M-coordinate (double, typically 0 to 1) to an alpha value.
        private static int AlphaFromM(double m)
        {
            return (int)Math.Max(0, Math.Min(255, m * 255));
        }

        // Standard HSV to RGB color conversion.
        private static Color HsvToRgb(double h, double S, double V)
        {
            int hi = Convert.ToInt32(Math.Floor(h / 60)) % 6;
            double f = h / 60 - Math.Floor(h / 60);

            V = V * 255;
            int v = Convert.ToInt32(V);
            int p = Convert.ToInt32(V * (1 - S));
            int q = Convert.ToInt32(V * (1 - f * S));
            int t = Convert.ToInt32(V * (1 - (1 - f) * S));

            if (hi == 0) return Color.FromArgb(255, v, t, p);
            if (hi == 1) return Color.FromArgb(255, q, v, p);
            if (hi == 2) return Color.FromArgb(255, p, v, t);
            if (hi == 3) return Color.FromArgb(255, p, q, v);
            if (hi == 4) return Color.FromArgb(255, t, p, v);
            return Color.FromArgb(255, v, p, q);
        }

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
