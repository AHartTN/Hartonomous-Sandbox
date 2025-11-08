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
            var pixels = SqlBytesInterop.GetBuffer(imageData, out var bufferLength);

            int channels = InferChannelCount(bufferLength, w, h);
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
                    if (!TryReadPixel(pixels, bufferLength, pixelIndex, channels, out byte r, out byte g, out byte b, out byte a))
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
            var pixels = SqlBytesInterop.GetBuffer(imageData, out var bufferLength);

            int channels = InferChannelCount(bufferLength, w, h);
            if (channels == 0)
            {
                return SqlString.Null;
            }

            long sumR = 0, sumG = 0, sumB = 0, sumA = 0;
            int totalPixels = w * h;
            int samples = 0;

            for (int i = 0; i < totalPixels; i++)
            {
                if (!TryReadPixel(pixels, bufferLength, i, channels, out byte r, out byte g, out byte b, out byte a))
                {
                    continue;
                }

                sumR += r;
                sumG += g;
                sumB += b;
                sumA += a;
                samples++;
            }

            if (samples == 0)
            {
                return new SqlString("#000000");
            }

            byte avgR = (byte)(sumR / samples);
            byte avgG = (byte)(sumG / samples);
            byte avgB = (byte)(sumB / samples);
            byte avgA = (byte)(sumA / samples);

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

            var pixels = SqlBytesInterop.GetBuffer(imageData, out var bufferLength);
            int channels = InferChannelCount(bufferLength, w, h);
            if (channels == 0)
            {
                return SqlString.Null;
            }

            long[] histogram = new long[bins];
            int totalPixels = w * h;

            for (int i = 0; i < totalPixels; i++)
            {
                if (!TryReadPixel(pixels, bufferLength, i, channels, out byte r, out byte g, out byte b, out byte a))
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

        private static bool TryReadPixel(byte[] buffer, int bufferLength, int pixelIndex, int channels, out byte r, out byte g, out byte b, out byte a)
        {
            int offset = pixelIndex * channels;
            if (offset + channels > bufferLength)
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

        /// <summary>
        /// Generate spatial image patches for diffusion-guided image synthesis.
        /// Returns table of patches with spatial coordinates (x, y, z) and GEOMETRY regions.
        /// </summary>
        [SqlFunction(FillRowMethodName = "FillImagePatchRow", TableDefinition = "patch_x INT, patch_y INT, spatial_x FLOAT, spatial_y FLOAT, spatial_z FLOAT, patch GEOMETRY")]
        public static System.Collections.IEnumerable GenerateImagePatches(
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
            if (width.IsNull || height.IsNull || patchSize.IsNull)
            {
                yield break;
            }

            int w = Math.Max(1, width.Value);
            int h = Math.Max(1, height.Value);
            int pSize = Math.Max(1, patchSize.Value);
            int diffusionSteps = steps.IsNull ? 32 : Math.Max(1, steps.Value);
            double guidance = guidanceScale.IsNull ? 6.5 : guidanceScale.Value;
            double gx = guideX.IsNull ? 0.0 : guideX.Value;
            double gy = guideY.IsNull ? 0.0 : guideY.Value;
            double gz = guideZ.IsNull ? 0.0 : guideZ.Value;
            int rngSeed = seed.IsNull ? 42 : seed.Value;

            var random = new Random(rngSeed);

            int patchesX = (w + pSize - 1) / pSize;
            int patchesY = (h + pSize - 1) / pSize;

            for (int py = 0; py < patchesY; py++)
            {
                for (int px = 0; px < patchesX; px++)
                {
                    // Diffusion process: start with noise, iteratively denoise towards guide
                    double noiseX = random.NextDouble() * 2.0 - 1.0;
                    double noiseY = random.NextDouble() * 2.0 - 1.0;
                    double noiseZ = random.NextDouble() * 2.0 - 1.0;

                    double spatialX = noiseX;
                    double spatialY = noiseY;
                    double spatialZ = noiseZ;

                    // Diffusion process: linear interpolation from noise towards guide point
                    // LERP is appropriate for spatial placement in GEOMETRY context
                    for (int step = 0; step < diffusionSteps; step++)
                    {
                        double alpha = (step + 1.0) / diffusionSteps; // Interpolation factor
                        spatialX = noiseX * (1.0 - alpha) + gx * alpha * guidance;
                        spatialY = noiseY * (1.0 - alpha) + gy * alpha * guidance;
                        spatialZ = noiseZ * (1.0 - alpha) + gz * alpha * guidance;
                    }

                    // Create GEOMETRY patch region (rectangular polygon)
                    int x1 = px * pSize;
                    int y1 = py * pSize;
                    int x2 = Math.Min((px + 1) * pSize, w);
                    int y2 = Math.Min((py + 1) * pSize, h);

                    var builder = new SqlGeometryBuilder();
                    builder.SetSrid(0);
                    builder.BeginGeometry(OpenGisGeometryType.Polygon);
                    builder.BeginFigure(x1, y1, spatialZ, null);
                    builder.AddLine(x2, y1, spatialZ, null);
                    builder.AddLine(x2, y2, spatialZ, null);
                    builder.AddLine(x1, y2, spatialZ, null);
                    builder.AddLine(x1, y1, spatialZ, null); // Close polygon
                    builder.EndFigure();
                    builder.EndGeometry();

                    yield return new ImagePatchRow
                    {
                        PatchX = px,
                        PatchY = py,
                        SpatialX = spatialX,
                        SpatialY = spatialY,
                        SpatialZ = spatialZ,
                        Patch = builder.ConstructedGeometry
                    };
                }
            }
        }

        /// <summary>
        /// Generate full image GEOMETRY representing synthesized image structure.
        /// Returns single GEOMETRY (MULTIPOLYGON) representing entire generated image.
        /// </summary>
        [SqlFunction(IsDeterministic = false, IsPrecise = false)]
        public static SqlGeometry GenerateImageGeometry(
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
            if (width.IsNull || height.IsNull)
            {
                return SqlGeometry.Null;
            }

            int w = Math.Max(1, width.Value);
            int h = Math.Max(1, height.Value);
            int pSize = patchSize.IsNull ? 32 : Math.Max(1, patchSize.Value);

            var builder = new SqlGeometryBuilder();
            builder.SetSrid(0);
            builder.BeginGeometry(OpenGisGeometryType.MultiPolygon);

            // Generate patches and combine into MultiPolygon
            var patches = GenerateImagePatches(width, height, patchSize, steps, guidanceScale, guideX, guideY, guideZ, seed);
            foreach (ImagePatchRow patch in patches)
            {
                builder.BeginGeometry(OpenGisGeometryType.Polygon);
                builder.BeginFigure(patch.PatchX * pSize, patch.PatchY * pSize, patch.SpatialZ, null);
                builder.AddLine((patch.PatchX + 1) * pSize, patch.PatchY * pSize, patch.SpatialZ, null);
                builder.AddLine((patch.PatchX + 1) * pSize, (patch.PatchY + 1) * pSize, patch.SpatialZ, null);
                builder.AddLine(patch.PatchX * pSize, (patch.PatchY + 1) * pSize, patch.SpatialZ, null);
                builder.AddLine(patch.PatchX * pSize, patch.PatchY * pSize, patch.SpatialZ, null);
                builder.EndFigure();
                builder.EndGeometry();
            }

            builder.EndGeometry();
            return builder.ConstructedGeometry;
        }

        public static void FillImagePatchRow(object obj, out SqlInt32 patchX, out SqlInt32 patchY, out SqlDouble spatialX, out SqlDouble spatialY, out SqlDouble spatialZ, out SqlGeometry patch)
        {
            var row = (ImagePatchRow)obj;
            patchX = row.PatchX;
            patchY = row.PatchY;
            spatialX = row.SpatialX;
            spatialY = row.SpatialY;
            spatialZ = row.SpatialZ;
            patch = row.Patch;
        }

        private class ImagePatchRow
        {
            public int PatchX { get; set; }
            public int PatchY { get; set; }
            public double SpatialX { get; set; }
            public double SpatialY { get; set; }
            public double SpatialZ { get; set; }
            public SqlGeometry Patch { get; set; }
        }

        /// <summary>
        /// Deconstructs an image into a grid of patches, returning a table of patch metadata including statistical features.
        /// This is a high-performance replacement for the T-SQL WHILE loops in sp_AtomizeImage.
        /// </summary>
        [SqlFunction(
            FillRowMethodName = "FillDeconstructedImagePatchRow",
            TableDefinition = "PatchIndex INT, RowIndex INT, ColIndex INT, PatchX INT, PatchY INT, PatchWidth INT, PatchHeight INT, PatchGeometry GEOMETRY, MeanR FLOAT, MeanG FLOAT, MeanB FLOAT, Variance FLOAT"
        )]
        public static System.Collections.IEnumerable DeconstructImageToPatches(
            SqlBytes rawImage,
            SqlInt32 imageWidth,
            SqlInt32 imageHeight,
            SqlInt32 patchSize,
            SqlInt32 strideSize)
        {
            if (rawImage.IsNull || imageWidth.IsNull || imageHeight.IsNull || patchSize.IsNull || strideSize.IsNull)
            {
                yield break;
            }

            int imgW = imageWidth.Value;
            int imgH = imageHeight.Value;
            int pSize = patchSize.Value;
            int sSize = strideSize.Value;

            var pixels = SqlBytesInterop.GetBuffer(rawImage, out var bufferLength);
            int channels = InferChannelCount(bufferLength, imgW, imgH);
            if (channels == 0) yield break;

            int patchesX = (imgW - pSize + sSize) / sSize;
            int patchesY = (imgH - pSize + sSize) / sSize;
            int patchIndex = 0;

            for (int y = 0; y < patchesY; y++)
            {
                for (int x = 0; x < patchesX; x++)
                {
                    int pixelX = x * sSize;
                    int pixelY = y * sSize;

                    int actualPatchWidth = Math.Min(pSize, imgW - pixelX);
                    int actualPatchHeight = Math.Min(pSize, imgH - pixelY);

                    if (actualPatchWidth <= 0 || actualPatchHeight <= 0) continue;

                    // --- Feature Extraction Logic ---
                    long sumR = 0, sumG = 0, sumB = 0;
                    double sumLuminance = 0;
                    double sumLuminanceSq = 0;
                    int patchPixelCount = 0;

                    for (int py_local = 0; py_local < actualPatchHeight; py_local++)
                    {
                        for (int px_local = 0; px_local < actualPatchWidth; px_local++)
                        {
                            int globalPixelIndex = (pixelY + py_local) * imgW + (pixelX + px_local);
                            if (TryReadPixel(pixels, bufferLength, globalPixelIndex, channels, out byte r, out byte g, out byte b, out byte a))
                            {
                                sumR += r;
                                sumG += g;
                                sumB += b;
                                double luminance = ComputeLuminance(r, g, b);
                                sumLuminance += luminance;
                                sumLuminanceSq += luminance * luminance;
                                patchPixelCount++;
                            }
                        }
                    }

                    if (patchPixelCount == 0) continue;

                    double meanR = (double)sumR / patchPixelCount;
                    double meanG = (double)sumG / patchPixelCount;
                    double meanB = (double)sumB / patchPixelCount;
                    double meanLuminance = sumLuminance / patchPixelCount;
                    double variance = (sumLuminanceSq / patchPixelCount) - (meanLuminance * meanLuminance);
                    // --- End Feature Extraction ---


                    var builder = new SqlGeometryBuilder();
                    builder.SetSrid(0);
                    builder.BeginGeometry(OpenGisGeometryType.Polygon);
                    builder.BeginFigure(pixelX, pixelY);
                    builder.AddLine(pixelX + actualPatchWidth, pixelY);
                    builder.AddLine(pixelX + actualPatchWidth, pixelY + actualPatchHeight);
                    builder.AddLine(pixelX, pixelY + actualPatchHeight);
                    builder.AddLine(pixelX, pixelY);
                    builder.EndFigure();
                    builder.EndGeometry();

                    yield return new ImageDeconstructionPatchRow
                    {
                        PatchIndex = patchIndex,
                        RowIndex = y,
                        ColIndex = x,
                        PatchX = pixelX,
                        PatchY = pixelY,
                        PatchWidth = actualPatchWidth,
                        PatchHeight = actualPatchHeight,
                        PatchGeometry = builder.ConstructedGeometry,
                        MeanR = meanR,
                        MeanG = meanG,
                        MeanB = meanB,
                        Variance = variance
                    };

                    patchIndex++;
                }
            }
        }

        public static void FillDeconstructedImagePatchRow(
            object obj,
            out SqlInt32 patchIndex,
            out SqlInt32 rowIndex,
            out SqlInt32 colIndex,
            out SqlInt32 patchX,
            out SqlInt32 patchY,
            out SqlInt32 patchWidth,
            out SqlInt32 patchHeight,
            out SqlGeometry patchGeometry,
            out SqlDouble meanR,
            out SqlDouble meanG,
            out SqlDouble meanB,
            out SqlDouble variance)
        {
            var row = (ImageDeconstructionPatchRow)obj;
            patchIndex = row.PatchIndex;
            rowIndex = row.RowIndex;
            colIndex = row.ColIndex;
            patchX = row.PatchX;
            patchY = row.PatchY;
            patchWidth = row.PatchWidth;
            patchHeight = row.PatchHeight;
            patchGeometry = row.PatchGeometry;
            meanR = row.MeanR;
            meanG = row.MeanG;
            meanB = row.MeanB;
            variance = row.Variance;
        }

        private class ImageDeconstructionPatchRow
        {
            public int PatchIndex { get; set; }
            public int RowIndex { get; set; }
            public int ColIndex { get; set; }
            public int PatchX { get; set; }
            public int PatchY { get; set; }
            public int PatchWidth { get; set; }
            public int PatchHeight { get; set; }
            public SqlGeometry PatchGeometry { get; set; }
            public double MeanR { get; set; }
            public double MeanG { get; set; }
            public double MeanB { get; set; }
            public double Variance { get; set; }
        }
    }
}
