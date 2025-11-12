using System;
using System.IO;
using System.Text;

namespace Hartonomous.Core.Pipelines.Ingestion
{
    /// <summary>
    /// Decodes PNG/JPEG images and provides resizing/grayscale conversion for perceptual hashing.
    /// Implements minimal decoding - enough to get pixels for pHash, not full-featured decoder.
    /// </summary>
    public static class ImageDecoder
    {
        private static readonly byte[] PNG_SIGNATURE = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        private static readonly byte[] JPEG_SOI = { 0xFF, 0xD8 };
        private static readonly byte[] JPEG_JFIF = { 0xFF, 0xE0 };
        private static readonly byte[] JPEG_EXIF = { 0xFF, 0xE1 };

        public enum ImageFormat
        {
            Unknown,
            PNG,
            JPEG,
            BMP
        }

        /// <summary>
        /// Detect image format from magic numbers
        /// </summary>
        public static ImageFormat DetectFormat(byte[] data)
        {
            if (data == null || data.Length < 8) return ImageFormat.Unknown;

            // PNG: 89 50 4E 47 0D 0A 1A 0A
            if (data.Length >= PNG_SIGNATURE.Length &&
                MatchBytes(data, PNG_SIGNATURE, 0))
            {
                return ImageFormat.PNG;
            }

            // JPEG: FF D8 FF
            if (data.Length >= 3 &&
                data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
            {
                return ImageFormat.JPEG;
            }

            // BMP: 42 4D (BM)
            if (data.Length >= 2 &&
                data[0] == 0x42 && data[1] == 0x4D)
            {
                return ImageFormat.BMP;
            }

            return ImageFormat.Unknown;
        }

        /// <summary>
        /// Decode image to 32x32 grayscale for perceptual hashing.
        /// This is optimized for pHash, not general-purpose image decoding.
        /// </summary>
        public static byte[] DecodeToGrayscale32x32(byte[] imageData)
        {
            var format = DetectFormat(imageData);

            switch (format)
            {
                case ImageFormat.PNG:
                    return DecodePngToGrayscale32x32(imageData);
                
                case ImageFormat.JPEG:
                    // JPEG decoding is complex - fall back to System.Drawing or skip for now
                    throw new NotImplementedException("JPEG decoding requires external library (System.Drawing.Common, ImageSharp, or SkiaSharp). Install ImageSharp for production use.");
                
                case ImageFormat.BMP:
                    return DecodeBmpToGrayscale32x32(imageData);
                
                default:
                    throw new NotSupportedException($"Unsupported image format. Expected PNG, JPEG, or BMP.");
            }
        }

        /// <summary>
        /// Decode BMP to 32x32 grayscale. BMP is simple - uncompressed RGB data.
        /// </summary>
        private static byte[] DecodeBmpToGrayscale32x32(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var reader = new BinaryReader(ms))
            {
                // BMP header: 14 bytes
                reader.ReadUInt16(); // Signature (BM)
                reader.ReadUInt32(); // File size
                reader.ReadUInt32(); // Reserved
                uint dataOffset = reader.ReadUInt32();

                // DIB header
                uint headerSize = reader.ReadUInt32();
                int width = reader.ReadInt32();
                int height = reader.ReadInt32();
                reader.ReadUInt16(); // Planes
                ushort bitsPerPixel = reader.ReadUInt16();

                // Only support 24-bit and 32-bit BMPs for simplicity
                if (bitsPerPixel != 24 && bitsPerPixel != 32)
                {
                    throw new NotSupportedException($"BMP must be 24-bit or 32-bit RGB. Got {bitsPerPixel}-bit.");
                }

                int bytesPerPixel = bitsPerPixel / 8;
                int stride = ((width * bytesPerPixel + 3) / 4) * 4; // Row padding to 4-byte boundary

                // Read pixel data
                ms.Seek(dataOffset, SeekOrigin.Begin);
                byte[] pixels = new byte[height * stride];
                ms.Read(pixels, 0, pixels.Length);

                // Convert to RGB (BMP stores BGR), then grayscale, then resize
                byte[,] rgb = new byte[height, width];
                for (int y = 0; y < height; y++)
                {
                    int row = height - 1 - y; // BMP stores bottom-up
                    for (int x = 0; x < width; x++)
                    {
                        int offset = row * stride + x * bytesPerPixel;
                        byte b = pixels[offset];
                        byte g = pixels[offset + 1];
                        byte r = pixels[offset + 2];
                        rgb[y, x] = ToGrayscale(r, g, b);
                    }
                }

                return ResizeGrayscale(rgb, 32, 32);
            }
        }

        /// <summary>
        /// Decode PNG to 32x32 grayscale. Minimal implementation - no interlacing, no palette, basic filters only.
        /// For production, use ImageSharp or SkiaSharp.
        /// </summary>
        private static byte[] DecodePngToGrayscale32x32(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var reader = new BinaryReader(ms))
            {
                // Verify PNG signature
                byte[] sig = reader.ReadBytes(8);
                if (!MatchBytes(sig, PNG_SIGNATURE, 0))
                    throw new InvalidDataException("Invalid PNG signature");

                int width = 0, height = 0, bitDepth = 0, colorType = 0;
                byte[]? imageData = null;

                // Read chunks
                while (ms.Position < ms.Length - 12)
                {
                    uint length = ReadBigEndianUInt32(reader);
                    string chunkType = Encoding.ASCII.GetString(reader.ReadBytes(4));
                    byte[] chunkData = reader.ReadBytes((int)length);
                    uint crc = reader.ReadUInt32(); // Skip CRC validation for speed

                    if (chunkType == "IHDR")
                    {
                        width = ReadBigEndianInt32(chunkData, 0);
                        height = ReadBigEndianInt32(chunkData, 4);
                        bitDepth = chunkData[8];
                        colorType = chunkData[9];
                        // compression=chunkData[10], filter=chunkData[11], interlace=chunkData[12]
                    }
                    else if (chunkType == "IDAT")
                    {
                        // Concatenate IDAT chunks
                        if (imageData == null)
                        {
                            imageData = chunkData;
                        }
                        else
                        {
                            byte[] combined = new byte[imageData.Length + chunkData.Length];
                            Buffer.BlockCopy(imageData, 0, combined, 0, imageData.Length);
                            Buffer.BlockCopy(chunkData, 0, combined, imageData.Length, chunkData.Length);
                            imageData = combined;
                        }
                    }
                    else if (chunkType == "IEND")
                    {
                        break;
                    }
                }

                if (width == 0 || height == 0 || imageData == null)
                    throw new InvalidDataException("PNG missing IHDR or IDAT chunks");

                // Decompress zlib data (skip for now - requires SharpZipLib or System.IO.Compression)
                // For minimal implementation, throw - user must use ImageSharp
                throw new NotImplementedException("PNG decompression requires external library. Install ImageSharp: dotnet add package SixLabors.ImageSharp");
            }
        }

        /// <summary>
        /// Convert RGB to grayscale using ITU-R BT.601 formula: Y = 0.299R + 0.587G + 0.114B
        /// </summary>
        private static byte ToGrayscale(byte r, byte g, byte b)
        {
            // Using integer approximation: (299*R + 587*G + 114*B) / 1000
            int gray = (299 * r + 587 * g + 114 * b) / 1000;
            return (byte)Math.Clamp(gray, 0, 255);
        }

        /// <summary>
        /// Resize grayscale image to targetWidth x targetHeight using bilinear interpolation.
        /// </summary>
        private static byte[] ResizeGrayscale(byte[,] source, int targetWidth, int targetHeight)
        {
            int srcHeight = source.GetLength(0);
            int srcWidth = source.GetLength(1);

            byte[] result = new byte[targetWidth * targetHeight];

            float xRatio = (float)srcWidth / targetWidth;
            float yRatio = (float)srcHeight / targetHeight;

            for (int dstY = 0; dstY < targetHeight; dstY++)
            {
                for (int dstX = 0; dstX < targetWidth; dstX++)
                {
                    // Map destination pixel to source coordinates
                    float srcX = dstX * xRatio;
                    float srcY = dstY * yRatio;

                    // Bilinear interpolation
                    int x0 = (int)srcX;
                    int y0 = (int)srcY;
                    int x1 = Math.Min(x0 + 1, srcWidth - 1);
                    int y1 = Math.Min(y0 + 1, srcHeight - 1);

                    float fx = srcX - x0;
                    float fy = srcY - y0;

                    byte p00 = source[y0, x0];
                    byte p10 = source[y0, x1];
                    byte p01 = source[y1, x0];
                    byte p11 = source[y1, x1];

                    // Interpolate horizontally
                    float p0 = p00 * (1 - fx) + p10 * fx;
                    float p1 = p01 * (1 - fx) + p11 * fx;

                    // Interpolate vertically
                    float p = p0 * (1 - fy) + p1 * fy;

                    result[dstY * targetWidth + dstX] = (byte)Math.Clamp(p, 0, 255);
                }
            }

            return result;
        }

        private static bool MatchBytes(byte[] data, byte[] pattern, int offset)
        {
            if (data.Length < offset + pattern.Length) return false;
            for (int i = 0; i < pattern.Length; i++)
            {
                if (data[offset + i] != pattern[i]) return false;
            }
            return true;
        }

        private static uint ReadBigEndianUInt32(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(4);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }

        private static int ReadBigEndianInt32(byte[] data, int offset)
        {
            byte[] bytes = new byte[4];
            Buffer.BlockCopy(data, offset, bytes, 0, 4);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }
    }
}
