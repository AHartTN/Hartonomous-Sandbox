using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Hartonomous.Clr
{
    /// <summary>
    /// CLR helpers for image diffusion routines using NetTopologySuite.
    /// </summary>
    public static class ImageGeneration
    {
        private static readonly SqlServerBytesReader _geometryReader = new SqlServerBytesReader();
        private static readonly SqlServerBytesWriter _geometryWriter = new SqlServerBytesWriter();
        private static readonly GeometryFactory _geometryFactory = new GeometryFactory(new PrecisionModel(), 0);

        /// <summary>
        /// Rasterizes geometry shapes into a PNG image.
        /// Color from Z-coordinate, opacity from M-coordinate.
        /// </summary>
        [SqlFunction(IsDeterministic = true, DataAccess = DataAccessKind.None)]
        public static SqlBytes GenerateImageFromShapes(SqlBytes shapes, SqlInt32 width, SqlInt32 height)
        {
            if (shapes.IsNull || shapes.Length == 0)
                return SqlBytes.Null;

            int w = width.IsNull || width.Value <= 0 ? 256 : width.Value;
            int h = height.IsNull || height.Value <= 0 ? 256 : height.Value;

            try
            {
                var geometry = _geometryReader.Read(shapes.Value);
                if (geometry == null || geometry.IsEmpty)
                    return SqlBytes.Null;

                using (var bmp = new Bitmap(w, h))
                using (var g = Graphics.FromImage(bmp))
                using (var ms = new MemoryStream())
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.Clear(Color.Transparent);

                    if (geometry is GeometryCollection collection)
                    {
                        foreach (var geom in collection.Geometries)
                        {
                            DrawGeometry(g, geom, w, h);
                        }
                    }
                    else
                    {
                        DrawGeometry(g, geometry, w, h);
                    }

                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    return new SqlBytes(ms.ToArray());
                }
            }
            catch
            {
                return SqlBytes.Null;
            }
        }

        private static void DrawGeometry(Graphics g, Geometry geom, int width, int height)
        {
            if (geom == null || geom.IsEmpty)
                return;

            if (geom is Polygon polygon)
            {
                var coords = polygon.ExteriorRing.Coordinates;
                var points = new PointF[coords.Length];
                
                for (int i = 0; i < coords.Length; i++)
                {
                    points[i] = new PointF((float)coords[i].X * width, (float)coords[i].Y * height);
                }

                if (points.Length > 2)
                {
                    var firstCoord = coords[0];
                    Color color = ColorFromZ(double.IsNaN(firstCoord.Z) ? 0.5 : firstCoord.Z);
                    int alpha = AlphaFromM(firstCoord.M);
                    
                    using (var brush = new SolidBrush(Color.FromArgb(alpha, color)))
                    {
                        g.FillPolygon(brush, points);
                    }
                }
            }
            else if (geom is LineString lineString)
            {
                var coords = lineString.Coordinates;
                var points = new PointF[coords.Length];
                
                for (int i = 0; i < coords.Length; i++)
                {
                    points[i] = new PointF((float)coords[i].X * width, (float)coords[i].Y * height);
                }

                if (points.Length > 1)
                {
                    var firstCoord = coords[0];
                    Color color = ColorFromZ(double.IsNaN(firstCoord.Z) ? 0.5 : firstCoord.Z);
                    int alpha = AlphaFromM(firstCoord.M);
                    
                    using (var pen = new Pen(Color.FromArgb(alpha, color), 2.0f))
                    {
                        g.DrawLines(pen, points);
                    }
                }
            }
            else if (geom is Point point)
            {
                var coord = point.Coordinate;
                float x = (float)coord.X * width;
                float y = (float)coord.Y * height;
                
                Color color = ColorFromZ(double.IsNaN(coord.Z) ? 0.5 : coord.Z);
                int alpha = AlphaFromM(coord.M);
                
                using (var brush = new SolidBrush(Color.FromArgb(alpha, color)))
                {
                    g.FillEllipse(brush, x - 2, y - 2, 4, 4);
                }
            }
        }

        private static Color ColorFromZ(double z)
        {
            double normalized = Math.Max(0.0, Math.Min(1.0, z));
            
            if (normalized < 0.5)
            {
                double t = normalized * 2.0;
                int r = (int)(255 * t);
                int g = (int)(128 * t);
                return Color.FromArgb(r, g, 0);
            }
            else
            {
                double t = (normalized - 0.5) * 2.0;
                int r = 255;
                int g = (int)(128 + 127 * t);
                int b = (int)(255 * t);
                return Color.FromArgb(r, g, b);
            }
        }

        private static int AlphaFromM(double m)
        {
            if (double.IsNaN(m))
                return 255;
            
            double normalized = Math.Max(0.0, Math.Min(1.0, m));
            return (int)(255 * normalized);
        }
    }
}
