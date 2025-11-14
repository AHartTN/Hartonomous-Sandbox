using System;
using System.Collections;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;

namespace Hartonomous.SqlClr
{
    /// <summary>
    /// Production-grade CLR table-valued function for extracting RGB pixels from image byte data.
    /// Supports BMP format natively without external dependencies (safe for SQL CLR SAFE permission).
    /// For PNG/JPEG, use BMP-encoded data or implement with ImageSharp in separate assembly.
    /// </summary>
    public static class ImagePixelExtractor
    {
        /// <summary>
        /// Extracts pixels from BMP image data with configurable stride for downsampling.
        /// Returns streaming results via IEnumerable for memory efficiency.
        /// </summary>
        /// <param name="imageData">BMP-encoded image bytes (VARBINARY(MAX))</param>
        /// <param name="strideX">Sample every Nth pixel horizontally (1 = all pixels, 2 = every other, etc.)</param>
        /// <param name="strideY">Sample every Nth pixel vertically (1 = all rows, 2 = every other, etc.)</param>
        /// <returns>Stream of pixel records with X, Y, R, G, B values</returns>
        [SqlFunction(
            FillRowMethodName = "FillPixelRow",
            TableDefinition = "X INT, Y INT, R TINYINT, G TINYINT, B TINYINT",
            DataAccess = DataAccessKind.None,
            IsDeterministic = true)]
        public static IEnumerable ExtractPixels(SqlBytes imageData, SqlInt32 strideX, SqlInt32 strideY)
        {
            // Handle NULL inputs
            if (imageData == null || imageData.IsNull || imageData.Length == 0)
                yield break;

            int xStride = strideX.IsNull ? 1 : strideX.Value;
            int yStride = strideY.IsNull ? 1 : strideY.Value;

            // Validate stride parameters
            if (xStride < 1) xStride = 1;
            if (yStride < 1) yStride = 1;

            byte[] bmpBytes = imageData.Value;

            // Parse BMP header (minimum 54 bytes for standard BMP)
            if (bmpBytes.Length < 54)
                yield break; // Invalid BMP

            // Verify BMP signature (0x42 0x4D = "BM")
            if (bmpBytes[0] != 0x42 || bmpBytes[1] != 0x4D)
                yield break; // Not a BMP file

            // Read BMP header values (little-endian)
            int dataOffset = BitConverter.ToInt32(bmpBytes, 10);
            int headerSize = BitConverter.ToInt32(bmpBytes, 14);

            // DIB header should be at least 40 bytes (BITMAPINFOHEADER)
            if (headerSize < 40)
                yield break;

            int width = BitConverter.ToInt32(bmpBytes, 18);
            int height = BitConverter.ToInt32(bmpBytes, 22);
            short bitsPerPixel = BitConverter.ToInt16(bmpBytes, 28);
            int compression = BitConverter.ToInt32(bmpBytes, 30);

            // Only support uncompressed 24-bit or 32-bit BMPs for simplicity
            if (compression != 0) // BI_RGB = 0
                yield break;

            if (bitsPerPixel != 24 && bitsPerPixel != 32)
                yield break;

            bool isBottomUp = height > 0;
            int absHeight = Math.Abs(height);

            // Calculate row stride (BMP rows are padded to 4-byte boundaries)
            int bytesPerPixel = bitsPerPixel / 8;
            int rowStride = ((width * bytesPerPixel + 3) / 4) * 4;

            // Validate data size
            int expectedDataSize = rowStride * absHeight;
            if (bmpBytes.Length < dataOffset + expectedDataSize)
                yield break;

            // Extract pixels with stride sampling
            for (int y = 0; y < absHeight; y += yStride)
            {
                // BMP rows are stored bottom-up by default
                int actualY = isBottomUp ? (absHeight - 1 - y) : y;
                int rowOffset = dataOffset + (actualY * rowStride);

                for (int x = 0; x < width; x += xStride)
                {
                    int pixelOffset = rowOffset + (x * bytesPerPixel);

                    // BMP stores pixels as BGR (not RGB)
                    byte b = bmpBytes[pixelOffset];
                    byte g = bmpBytes[pixelOffset + 1];
                    byte r = bmpBytes[pixelOffset + 2];

                    yield return new PixelData { X = x, Y = y, R = r, G = g, B = b };
                }
            }
        }

        /// <summary>
        /// FillRow method to populate SqlDataRecord for streaming table output.
        /// Called by SQL Server for each yielded pixel.
        /// </summary>
        public static void FillPixelRow(
            object pixelObj,
            out SqlInt32 x,
            out SqlInt32 y,
            out SqlByte r,
            out SqlByte g,
            out SqlByte b)
        {
            PixelData pixel = (PixelData)pixelObj;
            x = new SqlInt32(pixel.X);
            y = new SqlInt32(pixel.Y);
            r = new SqlByte(pixel.R);
            g = new SqlByte(pixel.G);
            b = new SqlByte(pixel.B);
        }

        /// <summary>
        /// Internal struct for pixel data storage during enumeration.
        /// </summary>
        private struct PixelData
        {
            public int X;
            public int Y;
            public byte R;
            public byte G;
            public byte B;
        }
    }
}
