using System;
using Xunit;
using Hartonomous.Core.Pipelines.Ingestion;

namespace Hartonomous.Core.Tests.Pipelines.Ingestion
{
    public class PerceptualHasherTests
    {
        [Fact]
        public void ComputeHash_WithSimple2x2BMP_ReturnsValidHash()
        {
            // Create minimal 2x2 BMP (will be resized to 32x32)
            byte[] bmp = CreateSimpleBMP(2, 2, new byte[,]
            {
                { 0, 255 },    // Black, White
                { 128, 64 }    // Gray, Dark gray
            });

            var result = PerceptualHasher.ComputeHash(bmp);

            Assert.NotNull(result);
            Assert.NotEqual(0UL, result.Hash);
        }

        [Fact]
        public void ComputeHash_IdenticalImages_ProduceSameHash()
        {
            byte[] bmp = CreateSimpleBMP(4, 4, new byte[,]
            {
                { 0, 50, 100, 150 },
                { 50, 100, 150, 200 },
                { 100, 150, 200, 250 },
                { 150, 200, 250, 255 }
            });

            var hash1 = PerceptualHasher.ComputeHash(bmp);
            var hash2 = PerceptualHasher.ComputeHash(bmp);

            Assert.Equal(hash1.Hash, hash2.Hash);
        }

        [Fact]
        public void ComputeHash_SimilarImages_HaveLowHammingDistance()
        {
            // Image 1: Gradient
            byte[] bmp1 = CreateGradientBMP(8, 8);
            
            // Image 2: Same gradient with slight noise
            byte[,] pixels = new byte[8, 8];
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                    pixels[y, x] = (byte)Math.Clamp((x + y) * 16 + 5, 0, 255); // +5 noise
            byte[] bmp2 = CreateSimpleBMP(8, 8, pixels);

            var hash1 = PerceptualHasher.ComputeHash(bmp1);
            var hash2 = PerceptualHasher.ComputeHash(bmp2);

            int distance = hash1.HammingDistance(hash2);

            // Similar images should have low distance (< 15 bits)
            Assert.InRange(distance, 0, 15);
        }

        [Fact]
        public void ComputeHash_DifferentImages_HaveHighHammingDistance()
        {
            // Image 1: Gradient
            byte[] bmp1 = CreateGradientBMP(8, 8);

            // Image 2: Random noise
            byte[,] pixels = new byte[8, 8];
            Random rnd = new Random(42);
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                    pixels[y, x] = (byte)rnd.Next(256);
            byte[] bmp2 = CreateSimpleBMP(8, 8, pixels);

            var hash1 = PerceptualHasher.ComputeHash(bmp1);
            var hash2 = PerceptualHasher.ComputeHash(bmp2);

            int distance = hash1.HammingDistance(hash2);

            // Different images should have high distance (> 15 bits)
            Assert.InRange(distance, 16, 64);
        }

        [Fact]
        public void ComputeHash_RawGrayscale32x32_WorksDirectly()
        {
            // Create raw 32x32 grayscale (1024 bytes)
            byte[] grayscale = new byte[1024];
            for (int i = 0; i < 1024; i++)
                grayscale[i] = (byte)(i % 256);

            var result = PerceptualHasher.ComputeHash(grayscale);

            Assert.NotNull(result);
            Assert.NotEqual(0UL, result.Hash);
        }

        [Fact]
        public void ImageDecoder_DetectFormat_RecognizesBMP()
        {
            byte[] bmp = CreateSimpleBMP(2, 2, new byte[,] { { 0, 255 }, { 128, 64 } });
            var format = ImageDecoder.DetectFormat(bmp);
            Assert.Equal(ImageDecoder.ImageFormat.BMP, format);
        }

        [Fact]
        public void ImageDecoder_DetectFormat_RecognizesPNG()
        {
            byte[] png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00 };
            var format = ImageDecoder.DetectFormat(png);
            Assert.Equal(ImageDecoder.ImageFormat.PNG, format);
        }

        [Fact]
        public void ImageDecoder_DetectFormat_RecognizesJPEG()
        {
            byte[] jpeg = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00 };
            var format = ImageDecoder.DetectFormat(jpeg);
            Assert.Equal(ImageDecoder.ImageFormat.JPEG, format);
        }

        // Helper: Create minimal BMP file (24-bit RGB, uncompressed)
        private byte[] CreateSimpleBMP(int width, int height, byte[,] grayscalePixels)
        {
            int stride = ((width * 3 + 3) / 4) * 4; // Row padding to 4-byte boundary
            int imageSize = stride * height;
            int fileSize = 54 + imageSize; // Header (14) + DIB header (40) + pixels

            byte[] bmp = new byte[fileSize];
            int offset = 0;

            // BMP file header (14 bytes)
            bmp[offset++] = 0x42; // 'B'
            bmp[offset++] = 0x4D; // 'M'
            WriteUInt32(bmp, ref offset, (uint)fileSize);
            WriteUInt32(bmp, ref offset, 0); // Reserved
            WriteUInt32(bmp, ref offset, 54); // Data offset

            // DIB header (40 bytes - BITMAPINFOHEADER)
            WriteUInt32(bmp, ref offset, 40); // Header size
            WriteInt32(bmp, ref offset, width);
            WriteInt32(bmp, ref offset, height);
            WriteUInt16(bmp, ref offset, 1); // Planes
            WriteUInt16(bmp, ref offset, 24); // Bits per pixel
            WriteUInt32(bmp, ref offset, 0); // Compression (none)
            WriteUInt32(bmp, ref offset, (uint)imageSize);
            WriteInt32(bmp, ref offset, 2835); // Horizontal resolution (72 DPI)
            WriteInt32(bmp, ref offset, 2835); // Vertical resolution (72 DPI)
            WriteUInt32(bmp, ref offset, 0); // Colors in palette
            WriteUInt32(bmp, ref offset, 0); // Important colors

            // Pixel data (BGR, bottom-up)
            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    byte gray = grayscalePixels[y, x];
                    bmp[offset++] = gray; // B
                    bmp[offset++] = gray; // G
                    bmp[offset++] = gray; // R
                }
                // Row padding
                while (offset % 4 != 0)
                    bmp[offset++] = 0;
            }

            return bmp;
        }

        private byte[] CreateGradientBMP(int width, int height)
        {
            byte[,] pixels = new byte[height, width];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    pixels[y, x] = (byte)Math.Clamp((x + y) * 16, 0, 255);
            return CreateSimpleBMP(width, height, pixels);
        }

        private void WriteUInt32(byte[] buffer, ref int offset, uint value)
        {
            buffer[offset++] = (byte)(value & 0xFF);
            buffer[offset++] = (byte)((value >> 8) & 0xFF);
            buffer[offset++] = (byte)((value >> 16) & 0xFF);
            buffer[offset++] = (byte)((value >> 24) & 0xFF);
        }

        private void WriteInt32(byte[] buffer, ref int offset, int value)
        {
            WriteUInt32(buffer, ref offset, (uint)value);
        }

        private void WriteUInt16(byte[] buffer, ref int offset, ushort value)
        {
            buffer[offset++] = (byte)(value & 0xFF);
            buffer[offset++] = (byte)((value >> 8) & 0xFF);
        }
    }
}
