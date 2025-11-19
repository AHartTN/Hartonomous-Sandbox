using System;
using Hartonomous.Clr.Contracts;
using Hartonomous.Clr.Enums;

namespace Hartonomous.Clr.Core
{
    /// <summary>
    /// Unified dequantization helpers for all quantization types.
    /// Consolidates scattered dequantization logic from ModelInference, GGUFParser, etc.
    /// </summary>
    public static class Dequantizers
    {
        /// <summary>
        /// Dequantizes quantized byte array to float array based on quantization type.
        /// </summary>
        public static float[] Dequantize(byte[] data, QuantizationType type)
        {
            return type switch
            {
                QuantizationType.None or QuantizationType.F32 => DequantizeF32(data),
                QuantizationType.F16 => DequantizeF16(data),
                QuantizationType.Q8_0 => DequantizeQ8_0(data),
                QuantizationType.Q4_0 => DequantizeQ4_0(data),
                QuantizationType.Q4_1 => DequantizeQ4_1(data),
                QuantizationType.Q5_0 => DequantizeQ5_0(data),
                QuantizationType.Q5_1 => DequantizeQ5_1(data),
                QuantizationType.Q4_K => DequantizeQ4_K(data),
                QuantizationType.Q5_K => DequantizeQ5_K(data),
                QuantizationType.Q6_K => DequantizeQ6_K(data),
                _ => DequantizeF32(data) // Default to F32
            };
        }

        #region F32/F16 Dequantization

        /// <summary>
        /// Dequantizes F32 (no quantization, direct binary copy).
        /// </summary>
        public static float[] DequantizeF32(byte[] data)
        {
            if (data.Length % 4 != 0)
                return Array.Empty<float>();

            var result = new float[data.Length / 4];
            Buffer.BlockCopy(data, 0, result, 0, data.Length);
            return result;
        }

        /// <summary>
        /// Dequantizes F16 (half-precision float to F32).
        /// </summary>
        public static float[] DequantizeF16(byte[] data)
        {
            var result = new float[data.Length / 2];
            for (int i = 0; i < result.Length; i++)
            {
                ushort half = BitConverter.ToUInt16(data, i * 2);
                result[i] = HalfToFloat(half);
            }
            return result;
        }

        /// <summary>
        /// Converts IEEE 754 half-precision (16-bit) to single-precision (32-bit).
        /// </summary>
        public static float HalfToFloat(ushort half)
        {
            int sign = (half >> 15) & 0x1;
            int exponent = (half >> 10) & 0x1F;
            int mantissa = half & 0x3FF;

            if (exponent == 0)
            {
                // Subnormal number
                return (sign == 1 ? -1f : 1f) * (float)Math.Pow(2, -14) * (mantissa / 1024f);
            }
            else if (exponent == 31)
            {
                // Infinity or NaN
                return mantissa == 0 ? (sign == 1 ? float.NegativeInfinity : float.PositiveInfinity) : float.NaN;
            }
            else
            {
                // Normal number
                return (sign == 1 ? -1f : 1f) * (float)Math.Pow(2, exponent - 15) * (1 + mantissa / 1024f);
            }
        }

        #endregion

        #region Q8_0 Dequantization

        /// <summary>
        /// Dequantizes Q8_0: 8-bit quantization with per-block scaling.
        /// Block structure: [float32 scale][32 × int8 values]
        /// </summary>
        public static float[] DequantizeQ8_0(byte[] data)
        {
            const int blockSize = 32;
            const int blockBytes = 4 + blockSize; // 4 bytes scale + 32 bytes values

            int numBlocks = data.Length / blockBytes;
            var result = new float[numBlocks * blockSize];

            for (int block = 0; block < numBlocks; block++)
            {
                int blockOffset = block * blockBytes;
                float scale = BitConverter.ToSingle(data, blockOffset);

                for (int i = 0; i < blockSize; i++)
                {
                    sbyte quantized = (sbyte)data[blockOffset + 4 + i];
                    result[block * blockSize + i] = quantized * scale;
                }
            }

            return result;
        }

        #endregion

        #region Q4_0/Q4_1 Dequantization

        /// <summary>
        /// Dequantizes Q4_0: 4-bit quantization with per-block scaling.
        /// Block structure: [float16 scale][32 × 4-bit values packed]
        /// </summary>
        public static float[] DequantizeQ4_0(byte[] data)
        {
            const int blockSize = 32;
            const int blockBytes = 2 + (blockSize / 2); // 2 bytes scale + 16 bytes (packed 4-bit)

            int numBlocks = data.Length / blockBytes;
            var result = new float[numBlocks * blockSize];

            for (int block = 0; block < numBlocks; block++)
            {
                int blockOffset = block * blockBytes;
                ushort scaleHalf = BitConverter.ToUInt16(data, blockOffset);
                float scale = HalfToFloat(scaleHalf);

                for (int i = 0; i < blockSize / 2; i++)
                {
                    byte packed = data[blockOffset + 2 + i];
                    
                    // Extract two 4-bit values (stored as unsigned, convert to signed -8..7)
                    int val1 = (packed & 0x0F) - 8;
                    int val2 = ((packed >> 4) & 0x0F) - 8;

                    result[block * blockSize + i * 2] = val1 * scale;
                    result[block * blockSize + i * 2 + 1] = val2 * scale;
                }
            }

            return result;
        }

        /// <summary>
        /// Dequantizes Q4_1: 4-bit quantization with per-block scale and bias.
        /// Block structure: [float16 scale][float16 min][32 × 4-bit values packed]
        /// </summary>
        public static float[] DequantizeQ4_1(byte[] data)
        {
            const int blockSize = 32;
            const int blockBytes = 4 + (blockSize / 2); // 2×2 bytes (scale+min) + 16 bytes (packed 4-bit)

            int numBlocks = data.Length / blockBytes;
            var result = new float[numBlocks * blockSize];

            for (int block = 0; block < numBlocks; block++)
            {
                int blockOffset = block * blockBytes;
                
                ushort scaleHalf = BitConverter.ToUInt16(data, blockOffset);
                ushort minHalf = BitConverter.ToUInt16(data, blockOffset + 2);
                
                float scale = HalfToFloat(scaleHalf);
                float min = HalfToFloat(minHalf);

                for (int i = 0; i < blockSize / 2; i++)
                {
                    byte packed = data[blockOffset + 4 + i];
                    
                    // Extract two 4-bit values (unsigned 0..15)
                    int val1 = packed & 0x0F;
                    int val2 = (packed >> 4) & 0x0F;

                    result[block * blockSize + i * 2] = val1 * scale + min;
                    result[block * blockSize + i * 2 + 1] = val2 * scale + min;
                }
            }

            return result;
        }

        #endregion

        #region Q5_0/Q5_1 Dequantization

        /// <summary>
        /// Dequantizes Q5_0: 5-bit quantization with per-block scaling.
        /// Block structure: [float16 scale][4 bytes high bits][32 × 4-bit low values]
        /// </summary>
        public static float[] DequantizeQ5_0(byte[] data)
        {
            const int blockSize = 32;
            const int blockBytes = 2 + 4 + (blockSize / 2); // scale + high bits + packed low bits

            int numBlocks = data.Length / blockBytes;
            var result = new float[numBlocks * blockSize];

            for (int block = 0; block < numBlocks; block++)
            {
                int blockOffset = block * blockBytes;
                ushort scaleHalf = BitConverter.ToUInt16(data, blockOffset);
                float scale = HalfToFloat(scaleHalf);

                uint highBits = BitConverter.ToUInt32(data, blockOffset + 2);

                for (int i = 0; i < blockSize / 2; i++)
                {
                    byte packed = data[blockOffset + 6 + i];
                    
                    // Extract two 4-bit low values
                    int val1Low = packed & 0x0F;
                    int val2Low = (packed >> 4) & 0x0F;

                    // Extract corresponding high bits
                    int val1High = (int)((highBits >> (i * 2)) & 1);
                    int val2High = (int)((highBits >> (i * 2 + 1)) & 1);

                    // Combine to 5-bit values (signed -16..15)
                    int val1 = (val1High << 4) | val1Low;
                    int val2 = (val2High << 4) | val2Low;
                    val1 -= 16;
                    val2 -= 16;

                    result[block * blockSize + i * 2] = val1 * scale;
                    result[block * blockSize + i * 2 + 1] = val2 * scale;
                }
            }

            return result;
        }

        /// <summary>
        /// Dequantizes Q5_1: 5-bit quantization with per-block scale and bias.
        /// </summary>
        public static float[] DequantizeQ5_1(byte[] data)
        {
            const int blockSize = 32;
            const int blockBytes = 4 + 4 + (blockSize / 2); // scale + min + high bits + packed low bits

            int numBlocks = data.Length / blockBytes;
            var result = new float[numBlocks * blockSize];

            for (int block = 0; block < numBlocks; block++)
            {
                int blockOffset = block * blockBytes;
                
                ushort scaleHalf = BitConverter.ToUInt16(data, blockOffset);
                ushort minHalf = BitConverter.ToUInt16(data, blockOffset + 2);
                
                float scale = HalfToFloat(scaleHalf);
                float min = HalfToFloat(minHalf);

                uint highBits = BitConverter.ToUInt32(data, blockOffset + 4);

                for (int i = 0; i < blockSize / 2; i++)
                {
                    byte packed = data[blockOffset + 8 + i];
                    
                    int val1Low = packed & 0x0F;
                    int val2Low = (packed >> 4) & 0x0F;

                    int val1High = (int)((highBits >> (i * 2)) & 1);
                    int val2High = (int)((highBits >> (i * 2 + 1)) & 1);

                    int val1 = (val1High << 4) | val1Low;
                    int val2 = (val2High << 4) | val2Low;

                    result[block * blockSize + i * 2] = val1 * scale + min;
                    result[block * blockSize + i * 2 + 1] = val2 * scale + min;
                }
            }

            return result;
        }

        #endregion

        #region K-Quantization (Q4_K, Q5_K, Q6_K)

        /// <summary>
        /// Dequantizes Q4_K: 4-bit K-quantization (optimized variant with better quality).
        /// Block structure: [float16 scale][float16 min][128 × 4-bit values packed]
        /// </summary>
        public static float[] DequantizeQ4_K(byte[] data)
        {
            const int blockSize = 128;
            const int blockBytes = 4 + (blockSize / 2); // 2×2 bytes (scale+min) + 64 bytes (packed 4-bit)

            int numBlocks = data.Length / blockBytes;
            var result = new float[numBlocks * blockSize];

            for (int block = 0; block < numBlocks; block++)
            {
                int blockOffset = block * blockBytes;
                
                ushort scaleHalf = BitConverter.ToUInt16(data, blockOffset);
                ushort minHalf = BitConverter.ToUInt16(data, blockOffset + 2);
                
                float scale = HalfToFloat(scaleHalf);
                float min = HalfToFloat(minHalf);

                for (int i = 0; i < blockSize / 2; i++)
                {
                    byte packed = data[blockOffset + 4 + i];
                    
                    int val1 = packed & 0x0F;
                    int val2 = (packed >> 4) & 0x0F;

                    result[block * blockSize + i * 2] = val1 * scale + min;
                    result[block * blockSize + i * 2 + 1] = val2 * scale + min;
                }
            }

            return result;
        }

        /// <summary>
        /// Dequantizes Q5_K: 5-bit K-quantization.
        /// Similar structure to Q5_0 but with larger blocks.
        /// </summary>
        public static float[] DequantizeQ5_K(byte[] data)
        {
            const int blockSize = 128;
            const int blockBytes = 2 + 16 + (blockSize / 2); // scale + high bits (16 bytes) + packed low bits

            int numBlocks = data.Length / blockBytes;
            var result = new float[numBlocks * blockSize];

            for (int block = 0; block < numBlocks; block++)
            {
                int blockOffset = block * blockBytes;
                ushort scaleHalf = BitConverter.ToUInt16(data, blockOffset);
                float scale = HalfToFloat(scaleHalf);

                // Read 16 bytes (128 bits) of high bits
                byte[] highBytes = new byte[16];
                Array.Copy(data, blockOffset + 2, highBytes, 0, 16);

                for (int i = 0; i < blockSize / 2; i++)
                {
                    byte packed = data[blockOffset + 18 + i];
                    
                    int val1Low = packed & 0x0F;
                    int val2Low = (packed >> 4) & 0x0F;

                    // Extract high bits
                    int byteIdx = (i * 2) / 8;
                    int bitIdx = (i * 2) % 8;
                    int val1High = (highBytes[byteIdx] >> bitIdx) & 1;
                    int val2High = (highBytes[byteIdx] >> (bitIdx + 1)) & 1;

                    int val1 = ((val1High << 4) | val1Low) - 16;
                    int val2 = ((val2High << 4) | val2Low) - 16;

                    result[block * blockSize + i * 2] = val1 * scale;
                    result[block * blockSize + i * 2 + 1] = val2 * scale;
                }
            }

            return result;
        }

        /// <summary>
        /// Dequantizes Q6_K: 6-bit K-quantization.
        /// Highest quality K-quantization variant.
        /// </summary>
        public static float[] DequantizeQ6_K(byte[] data)
        {
            const int blockSize = 128;
            const int blockBytes = 2 + (blockSize * 6 / 8); // scale + 96 bytes (6 bits × 128 / 8)

            int numBlocks = data.Length / blockBytes;
            var result = new float[numBlocks * blockSize];

            for (int block = 0; block < numBlocks; block++)
            {
                int blockOffset = block * blockBytes;
                ushort scaleHalf = BitConverter.ToUInt16(data, blockOffset);
                float scale = HalfToFloat(scaleHalf);

                // Unpack 6-bit values (3 values per 2 bytes)
                for (int i = 0; i < blockSize / 4; i++)
                {
                    int bytePos = blockOffset + 2 + i * 3;
                    byte b0 = data[bytePos];
                    byte b1 = data[bytePos + 1];
                    byte b2 = data[bytePos + 2];

                    // Extract four 6-bit values
                    int val0 = b0 & 0x3F;
                    int val1 = ((b0 >> 6) | ((b1 & 0x0F) << 2)) & 0x3F;
                    int val2 = ((b1 >> 4) | ((b2 & 0x03) << 4)) & 0x3F;
                    int val3 = (b2 >> 2) & 0x3F;

                    // Convert to signed (-32..31)
                    result[block * blockSize + i * 4] = (val0 - 32) * scale;
                    result[block * blockSize + i * 4 + 1] = (val1 - 32) * scale;
                    result[block * blockSize + i * 4 + 2] = (val2 - 32) * scale;
                    result[block * blockSize + i * 4 + 3] = (val3 - 32) * scale;
                }
            }

            return result;
        }

        #endregion
    }
}
