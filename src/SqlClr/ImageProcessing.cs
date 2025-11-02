using System;
using System.Data.SqlTypes;
using System.Text;
using Microsoft.SqlServer.Server;
using Microsoft.SqlServer.Types;

namespace SqlClrFunctions
{
    /// <summary>
    /// Image processing helpers executed inside SQL CLR.
    /// </summary>
    public static class ImageProcessing
    {
        /// <summary>
        /// Convert raw pixel data into a 3D point cloud (x, y, brightness).
        /// The input buffer must be tightly packed in RGB or RGBA order.
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = false)]
        public static SqlGeometry ImageToPointCloud(SqlBytes imageData, SqlInt32 width, SqlInt32 height, SqlInt32 sampleStep)
        {
            if (imageData.IsNull || width.IsNull || height.IsNull)
            {
                return SqlGeometry.Null;
            }

            int w = Math.Max(1, width.Value);
            int h = Math.Max(1, height.Value);
            byte[] pixels = imageData.Value;

            int channels = InferChannelCount(pixels.Length, w, h);
            if (channels == 0)
            {
                return SqlGeometry.Null;
            }

            int step = sampleStep.IsNull || sampleStep.Value <= 0
                ? Math.Max(1, (int)Math.Sqrt((w * h) / 4096.0))
                : sampleStep.Value;

            var builder = new SqlGeometryBuilder();
            builder.SetSrid(0);
            builder.BeginGeometry(OpenGisGeometryType.MultiPoint);

            for (int y = 0; y < h; y += step)
            {
                for (int x = 0; x < w; x += step)
                {
                    int pixelIndex = y * w + x;
                    if (!TryReadPixel(pixels, pixelIndex, channels, out byte r, out byte g, out byte b, out byte a))
                    {
                        continue;
                    }

                    double brightness = ComputeLuminance(r, g, b);
                    double alpha = a / 255.0;
                    double xNorm = (x + 0.5) / w;
                    double yNorm = (y + 0.5) / h;

                    builder.BeginGeometry(OpenGisGeometryType.Point);
                    builder.BeginFigure(xNorm, yNorm, brightness, alpha);
                    builder.EndFigure();
                    builder.EndGeometry();
                }
            }

            builder.EndGeometry();
            return builder.ConstructedGeometry;
        }

        /// <summary>
        /// Compute the average color of an image buffer and return it as hex string #RRGGBB (or #RRGGBBAA when alpha is present).
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = false)]
        public static SqlString ImageAverageColor(SqlBytes imageData, SqlInt32 width, SqlInt32 height)
        {
            if (imageData.IsNull || width.IsNull || height.IsNull)
            {
                return SqlString.Null;
            }

            int w = Math.Max(1, width.Value);
            int h = Math.Max(1, height.Value);
            byte[] pixels = imageData.Value;

            int channels = InferChannelCount(pixels.Length, w, h);
            if (channels == 0)
            {
                return SqlString.Null;
            }

            long sumR = 0, sumG = 0, sumB = 0, sumA = 0;
            int totalPixels = w * h;

            for (int i = 0; i < totalPixels; i++)
            {
                if (!TryReadPixel(pixels, i, channels, out byte r, out byte g, out byte b, out byte a))
                {
                    continue;
                }

                sumR += r;
                sumG += g;
                sumB += b;
                sumA += a;
            }

            if (totalPixels == 0)
            {
                return new SqlString("#000000");
            }

            byte avgR = (byte)(sumR / totalPixels);
            byte avgG = (byte)(sumG / totalPixels);
            byte avgB = (byte)(sumB / totalPixels);
            byte avgA = (byte)(sumA / totalPixels);

            string hex = channels == 4
                ? $"#{avgR:X2}{avgG:X2}{avgB:X2}{avgA:X2}"
                : $"#{avgR:X2}{avgG:X2}{avgB:X2}";

            return new SqlString(hex);
        }

        /// <summary>
        /// Generate a luminance histogram with the requested number of bins, returned as a JSON array string.
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = false)]
        public static SqlString ImageLuminanceHistogram(SqlBytes imageData, SqlInt32 width, SqlInt32 height, SqlInt32 binCount)
        {
            if (imageData.IsNull || width.IsNull || height.IsNull)
            {
                return SqlString.Null;
            }

            int w = Math.Max(1, width.Value);
            int h = Math.Max(1, height.Value);
            int bins = binCount.IsNull || binCount.Value <= 0 ? 32 : Math.Min(512, binCount.Value);

            byte[] pixels = imageData.Value;
            int channels = InferChannelCount(pixels.Length, w, h);
            if (channels == 0)
            {
                return SqlString.Null;
            }

            long[] histogram = new long[bins];
            int totalPixels = w * h;

            for (int i = 0; i < totalPixels; i++)
            {
                if (!TryReadPixel(pixels, i, channels, out byte r, out byte g, out byte b, out byte a))
                {
                    continue;
                }

                double luminance = ComputeLuminance(r, g, b);
                int bin = (int)Math.Min(bins - 1, Math.Floor(luminance * bins));
                histogram[bin]++;
            }

            var builder = new StringBuilder();
            builder.Append('[');
            for (int i = 0; i < bins; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                builder.Append(histogram[i]);
            }
            builder.Append(']');

            return new SqlString(builder.ToString());
        }

        private static int InferChannelCount(int byteLength, int width, int height)
        {
            int totalPixels = width * height;
            if (totalPixels <= 0)
            {
                return 0;
            }

            if (byteLength == totalPixels * 4)
            {
                return 4;
            }

            if (byteLength == totalPixels * 3)
            {
                return 3;
            }

            if (byteLength % totalPixels == 0)
            {
                return byteLength / totalPixels;
            }

            return 0;
        }

        private static bool TryReadPixel(byte[] buffer, int pixelIndex, int channels, out byte r, out byte g, out byte b, out byte a)
        {
            int offset = pixelIndex * channels;
            if (offset + channels > buffer.Length)
            {
                r = g = b = 0;
                a = 255;
                return false;
            }

            switch (channels)
            {
                case 4:
                    r = buffer[offset];
                    g = buffer[offset + 1];
                    b = buffer[offset + 2];
                    a = buffer[offset + 3];
                    return true;
                case 3:
                    r = buffer[offset];
                    g = buffer[offset + 1];
                    b = buffer[offset + 2];
                    a = 255;
                    return true;
                case 1:
                    r = g = b = buffer[offset];
                    a = 255;
                    return true;
                default:
                    r = g = b = 0;
                    a = 255;
                    return false;
            }
        }

        private static double ComputeLuminance(byte r, byte g, byte b)
        {
            return (0.299 * r + 0.587 * g + 0.114 * b) / 255.0;
        }
    }
}
