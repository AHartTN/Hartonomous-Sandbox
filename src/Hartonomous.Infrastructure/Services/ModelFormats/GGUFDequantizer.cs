using System.Runtime.InteropServices;

namespace Hartonomous.Infrastructure.Services.ModelFormats;

/// <summary>
/// Handles dequantization of GGUF tensor data from various quantization formats to float32.
/// </summary>
public class GGUFDequantizer
{
    private const int QK_K = 256; // Super-block size for K-quantizations
    private const int QK4_0 = 32; // Block size for Q4_0
    private const int QK4_1 = 32; // Block size for Q4_1
    private const int QK5_0 = 32; // Block size for Q5_0
    private const int QK5_1 = 32; // Block size for Q5_1
    private const int QK8_0 = 32; // Block size for Q8_0

    /// <summary>
    /// Dequantizes a tensor from GGUF format to float32 array.
    /// </summary>
    public float[]? DequantizeTensor(BinaryReader reader, GGUFTensorInfo tensorInfo, int previewLimit)
    {
        var limit = Math.Min(previewLimit, tensorInfo.ElementCount > int.MaxValue ? int.MaxValue : (int)tensorInfo.ElementCount);
        if (limit <= 0)
        {
            return Array.Empty<float>();
        }

        return tensorInfo.Type switch
        {
            GGMLType.F32 => DequantizeF32(reader, tensorInfo.ElementCount, limit),
            GGMLType.F16 => DequantizeF16(reader, tensorInfo.ElementCount, limit),
            GGMLType.BF16 => DequantizeBF16(reader, tensorInfo.ElementCount, limit),
            GGMLType.Q4_0 => DequantizeQ4_0(reader, tensorInfo.ElementCount, limit),
            GGMLType.Q4_1 => DequantizeQ4_1(reader, tensorInfo.ElementCount, limit),
            GGMLType.Q5_0 => DequantizeQ5_0(reader, tensorInfo.ElementCount, limit),
            GGMLType.Q5_1 => DequantizeQ5_1(reader, tensorInfo.ElementCount, limit),
            GGMLType.Q8_0 => DequantizeQ8_0(reader, tensorInfo.ElementCount, limit),
            GGMLType.Q2_K => DequantizeQ2_K(reader, tensorInfo.ElementCount, limit),
            GGMLType.Q3_K => DequantizeQ3_K(reader, tensorInfo.ElementCount, limit),
            GGMLType.Q4_K => DequantizeQ4_K(reader, tensorInfo.ElementCount, limit),
            GGMLType.Q5_K => DequantizeQ5_K(reader, tensorInfo.ElementCount, limit),
            GGMLType.Q6_K => DequantizeQ6_K(reader, tensorInfo.ElementCount, limit),
            _ => null // Unsupported type
        };
    }

    private float[] DequantizeF32(BinaryReader reader, long elementCount, int previewLimit)
    {
        var limit = Math.Min(previewLimit, elementCount > int.MaxValue ? int.MaxValue : (int)elementCount);
        if (limit <= 0)
        {
            return Array.Empty<float>();
        }

        var result = new float[limit];
        for (long i = 0; i < elementCount; i++)
        {
            var value = reader.ReadSingle();
            if (i < limit)
            {
                result[(int)i] = value;
            }
        }

        return result;
    }

    private float[] DequantizeF16(BinaryReader reader, long elementCount, int previewLimit)
    {
        var limit = Math.Min(previewLimit, elementCount > int.MaxValue ? int.MaxValue : (int)elementCount);
        if (limit <= 0)
        {
            return Array.Empty<float>();
        }

        var result = new float[limit];
        for (long i = 0; i < elementCount; i++)
        {
            var u16 = reader.ReadUInt16();
            if (i < limit)
            {
                result[(int)i] = HalfToFloat(u16);
            }
        }

        return result;
    }

    private float[] DequantizeBF16(BinaryReader reader, long elementCount, int previewLimit)
    {
        var limit = Math.Min(previewLimit, elementCount > int.MaxValue ? int.MaxValue : (int)elementCount);
        if (limit <= 0)
        {
            return Array.Empty<float>();
        }

        var result = new float[limit];
        for (long i = 0; i < elementCount; i++)
        {
            var bf16 = reader.ReadUInt16();
            var f32bits = (uint)bf16 << 16;
            if (i < limit)
            {
                result[(int)i] = BitConverter.ToSingle(BitConverter.GetBytes(f32bits), 0);
            }
        }

        return result;
    }

    private float[] DequantizeQ4_0(BinaryReader reader, long elementCount, int previewLimit)
    {
        // Q4_0: 4-bit quantization, block size 32
        // Each block: delta (FP16, 2 bytes) + 16 bytes of 4-bit quantized values
        var numBlocks = (int)((elementCount + QK4_0 - 1) / QK4_0);
        var limit = Math.Min(previewLimit, elementCount > int.MaxValue ? int.MaxValue : (int)elementCount);
        if (limit <= 0)
        {
            return Array.Empty<float>();
        }

        var result = new float[limit];
        long globalIndex = 0;

        for (int b = 0; b < numBlocks && globalIndex < elementCount; b++)
        {
            var delta = HalfToFloat(reader.ReadUInt16());
            var quants = reader.ReadBytes(16); // 32 values @ 4 bits = 16 bytes

            for (int i = 0; i < 16 && globalIndex < elementCount; i++)
            {
                byte b8 = quants[i];

                // Low 4 bits
                int qLow = (b8 & 0x0F) - 8;
                if (globalIndex < limit)
                {
                    result[(int)globalIndex] = qLow * delta;
                }
                globalIndex++;
                if (globalIndex >= limit)
                {
                    return result;
                }
                if (globalIndex >= elementCount)
                {
                    break;
                }

                // High 4 bits
                int qHigh = ((b8 >> 4) & 0x0F) - 8;
                if (globalIndex < limit)
                {
                    result[(int)globalIndex] = qHigh * delta;
                }
                globalIndex++;
                if (globalIndex >= limit)
                {
                    return result;
                }
            }
        }

        return result;
    }

    private float[] DequantizeQ4_1(BinaryReader reader, long elementCount, int previewLimit)
    {
        // Q4_1: 4-bit quantization with min, block size 32
        // Each block: delta (FP16) + min (FP16) + 16 bytes of 4-bit values
        var numBlocks = (int)((elementCount + QK4_1 - 1) / QK4_1);
        var limit = Math.Min(previewLimit, elementCount > int.MaxValue ? int.MaxValue : (int)elementCount);
        if (limit <= 0)
        {
            return Array.Empty<float>();
        }

        var result = new float[limit];
        long globalIndex = 0;

        for (int b = 0; b < numBlocks && globalIndex < elementCount; b++)
        {
            var delta = HalfToFloat(reader.ReadUInt16());
            var min = HalfToFloat(reader.ReadUInt16());
            var quants = reader.ReadBytes(16);

            for (int i = 0; i < 16 && globalIndex < elementCount; i++)
            {
                byte b8 = quants[i];

                int qLow = b8 & 0x0F;
                if (globalIndex < limit)
                {
                    result[(int)globalIndex] = qLow * delta + min;
                }
                globalIndex++;
                if (globalIndex >= limit)
                {
                    return result;
                }
                if (globalIndex >= elementCount)
                {
                    break;
                }

                int qHigh = (b8 >> 4) & 0x0F;
                if (globalIndex < limit)
                {
                    result[(int)globalIndex] = qHigh * delta + min;
                }
                globalIndex++;
                if (globalIndex >= limit)
                {
                    return result;
                }
            }
        }

        return result;
    }

    private float[] DequantizeQ5_0(BinaryReader reader, long elementCount, int previewLimit)
    {
        // Q5_0: 5-bit quantization, block size 32
        // Each block: delta (FP16, 2 bytes) + qh (4 bytes, high bits) + qs (16 bytes, low 4 bits)
        var numBlocks = (int)((elementCount + QK5_0 - 1) / QK5_0);
        var limit = Math.Min(previewLimit, elementCount > int.MaxValue ? int.MaxValue : (int)elementCount);
        if (limit <= 0)
        {
            return Array.Empty<float>();
        }

        var result = new float[limit];
        long globalIndex = 0;

        for (int b = 0; b < numBlocks && globalIndex < elementCount; b++)
        {
            var delta = HalfToFloat(reader.ReadUInt16());
            var qh = reader.ReadUInt32(); // High bits packed
            var qs = reader.ReadBytes(16); // Low 4 bits

            for (int i = 0; i < 32 && globalIndex < elementCount; i++)
            {
                int byteIdx = i / 2;
                int shift = (i % 2) * 4;

                // Get low 4 bits
                int lowBits = (qs[byteIdx] >> shift) & 0x0F;

                // Get high bit from qh
                int highBit = (int)((qh >> i) & 1);

                // Combine to 5-bit value
                int q = (highBit << 4) | lowBits;
                q -= 16; // Center at 0

                if (globalIndex < limit)
                {
                    result[(int)globalIndex] = q * delta;
                }
                globalIndex++;
                if (globalIndex >= limit)
                {
                    return result;
                }
            }
        }

        return result;
    }

    private float[] DequantizeQ5_1(BinaryReader reader, long elementCount, int previewLimit)
    {
        // Q5_1: 5-bit quantization with min, block size 32
        var numBlocks = (int)((elementCount + QK5_1 - 1) / QK5_1);
        var limit = Math.Min(previewLimit, elementCount > int.MaxValue ? int.MaxValue : (int)elementCount);
        if (limit <= 0)
        {
            return Array.Empty<float>();
        }

        var result = new float[limit];
        long globalIndex = 0;

        for (int b = 0; b < numBlocks && globalIndex < elementCount; b++)
        {
            var delta = HalfToFloat(reader.ReadUInt16());
            var min = HalfToFloat(reader.ReadUInt16());
            var qh = reader.ReadUInt32();
            var qs = reader.ReadBytes(16);

            for (int i = 0; i < 32 && globalIndex < elementCount; i++)
            {
                int byteIdx = i / 2;
                int shift = (i % 2) * 4;

                int lowBits = (qs[byteIdx] >> shift) & 0x0F;
                int highBit = (int)((qh >> i) & 1);
                int q = (highBit << 4) | lowBits;

                if (globalIndex < limit)
                {
                    result[(int)globalIndex] = q * delta + min;
                }
                globalIndex++;
                if (globalIndex >= limit)
                {
                    return result;
                }
            }
        }

        return result;
    }

    private float[] DequantizeQ8_0(BinaryReader reader, long elementCount, int previewLimit)
    {
        // Q8_0: 8-bit quantization, block size 32
        // Each block: delta (FP16, 2 bytes) + 32 bytes of int8 values
        var numBlocks = (int)((elementCount + QK8_0 - 1) / QK8_0);
        var limit = Math.Min(previewLimit, elementCount > int.MaxValue ? int.MaxValue : (int)elementCount);
        if (limit <= 0)
        {
            return Array.Empty<float>();
        }

        var result = new float[limit];
        long globalIndex = 0;

        for (int b = 0; b < numBlocks && globalIndex < elementCount; b++)
        {
            var delta = HalfToFloat(reader.ReadUInt16());

            for (int i = 0; i < 32 && globalIndex < elementCount; i++)
            {
                sbyte q = reader.ReadSByte();
                if (globalIndex < limit)
                {
                    result[(int)globalIndex] = q * delta;
                }
                globalIndex++;
                if (globalIndex >= limit)
                {
                    return result;
                }
            }
        }

        return result;
    }

    private float[] DequantizeQ2_K(BinaryReader reader, long elementCount, int previewLimit)
    {
        // Q2_K: 2-bit super-block quantization (256 elements per super-block)
        // Complex structure - for production use, this is a simplified version
        // Real implementation needs to match ggml's block_q2_K structure
        var numBlocks = (int)((elementCount + QK_K - 1) / QK_K);
        var limit = Math.Min(previewLimit, elementCount > int.MaxValue ? int.MaxValue : (int)elementCount);
        if (limit <= 0)
        {
            return Array.Empty<float>();
        }

        var result = new float[limit];
        long globalIndex = 0;

        for (int b = 0; b < numBlocks && globalIndex < elementCount; b++)
        {
            // Simplified: read scales and quantized values
            // Real Q2_K structure: scales (16 bytes), qs (64 bytes)
            var scales = new byte[16];
            reader.Read(scales, 0, 16);
            var qs = new byte[64];
            reader.Read(qs, 0, 64);

            for (int i = 0; i < QK_K && globalIndex < elementCount; i++)
            {
                int scaleIdx = i / 16;
                float scale = (scales[scaleIdx] - 128) / 64.0f;
                int qIdx = i / 4;
                int shift = (i % 4) * 2;

                int q = (qs[qIdx] >> shift) & 0x03; // 2 bits
                q -= 2; // Center

                if (globalIndex < limit)
                {
                    result[(int)globalIndex] = q * scale;
                }
                globalIndex++;
                if (globalIndex >= limit)
                {
                    return result;
                }
            }
        }

        return result;
    }

    private float[] DequantizeQ3_K(BinaryReader reader, long elementCount, int previewLimit)
    {
        // Q3_K: 3-bit super-block quantization
        // Simplified implementation
        var numBlocks = (int)((elementCount + QK_K - 1) / QK_K);
        var limit = Math.Min(previewLimit, elementCount > int.MaxValue ? int.MaxValue : (int)elementCount);
        if (limit <= 0)
        {
            return Array.Empty<float>();
        }

        var result = new float[limit];
        long globalIndex = 0;

        for (int b = 0; b < numBlocks && globalIndex < elementCount; b++)
        {
            // Q3_K structure: hmask (32 bytes), qs (96 bytes), scales (12 bytes)
            var hmask = new byte[32];
            reader.Read(hmask, 0, 32);
            var qs = new byte[96];
            reader.Read(qs, 0, 96);
            var scales = new byte[12];
            reader.Read(scales, 0, 12);

            for (int i = 0; i < QK_K && globalIndex < elementCount; i++)
            {
                int scaleIdx = i / 21;
                float scale = (scales[scaleIdx % 12] - 32) / 16.0f;

                // Extract 3-bit value (simplified)
                int qIdx = i / 8 * 3;
                int q = qs[qIdx % 96] & 0x07;
                q -= 4; // Center at 0

                if (globalIndex < limit)
                {
                    result[(int)globalIndex] = q * scale;
                }
                globalIndex++;
                if (globalIndex >= limit)
                {
                    return result;
                }
            }
        }

        return result;
    }

    private float[] DequantizeQ4_K(BinaryReader reader, long elementCount, int previewLimit)
    {
        // Q4_K: 4-bit super-block quantization
        // Structure: d (FP16, 2 bytes) + dmin (FP16, 2 bytes) + scales (12 bytes) + qs (128 bytes)
        var numBlocks = (int)((elementCount + QK_K - 1) / QK_K);
        var limit = Math.Min(previewLimit, elementCount > int.MaxValue ? int.MaxValue : (int)elementCount);
        if (limit <= 0)
        {
            return Array.Empty<float>();
        }

        var result = new float[limit];
        long globalIndex = 0;

        for (int b = 0; b < numBlocks && globalIndex < elementCount; b++)
        {
            var d = HalfToFloat(reader.ReadUInt16());
            var dmin = HalfToFloat(reader.ReadUInt16());
            var scales = new byte[12];
            reader.Read(scales, 0, 12);
            var qs = new byte[128];
            reader.Read(qs, 0, 128);

            for (int i = 0; i < QK_K && globalIndex < elementCount; i++)
            {
                int scaleIdx = i / 32;
                float scale = ((scales[scaleIdx] & 0x0F) * d) - dmin;

                int qIdx = i / 2;
                int shift = (i % 2) * 4;
                int q = (qs[qIdx] >> shift) & 0x0F;

                if (globalIndex < limit)
                {
                    result[(int)globalIndex] = q * scale;
                }
                globalIndex++;
                if (globalIndex >= limit)
                {
                    return result;
                }
            }
        }

        return result;
    }

    private float[] DequantizeQ5_K(BinaryReader reader, long elementCount, int previewLimit)
    {
        // Q5_K: 5-bit super-block quantization
        // Structure: d (FP16) + dmin (FP16) + scales (12 bytes) + qh (32 bytes) + qs (128 bytes)
        var numBlocks = (int)((elementCount + QK_K - 1) / QK_K);
        var limit = Math.Min(previewLimit, elementCount > int.MaxValue ? int.MaxValue : (int)elementCount);
        if (limit <= 0)
        {
            return Array.Empty<float>();
        }

        var result = new float[limit];
        long globalIndex = 0;

        for (int b = 0; b < numBlocks && globalIndex < elementCount; b++)
        {
            var d = HalfToFloat(reader.ReadUInt16());
            var dmin = HalfToFloat(reader.ReadUInt16());
            var scales = new byte[12];
            reader.Read(scales, 0, 12);
            var qh = new byte[32];
            reader.Read(qh, 0, 32);
            var qs = new byte[128];
            reader.Read(qs, 0, 128);

            for (int i = 0; i < QK_K && globalIndex < elementCount; i++)
            {
                int scaleIdx = i / 32;
                float scale = ((scales[scaleIdx] & 0x0F) * d) - dmin;

                int qIdx = i / 2;
                int shift = (i % 2) * 4;
                int lowBits = (qs[qIdx] >> shift) & 0x0F;

                int highBitIdx = i / 8;
                int highBitShift = i % 8;
                int highBit = (qh[highBitIdx] >> highBitShift) & 1;

                int q = (highBit << 4) | lowBits;

                if (globalIndex < limit)
                {
                    result[(int)globalIndex] = q * scale;
                }
                globalIndex++;
                if (globalIndex >= limit)
                {
                    return result;
                }
            }
        }

        return result;
    }

    private float[] DequantizeQ6_K(BinaryReader reader, long elementCount, int previewLimit)
    {
        // Q6_K: 6-bit super-block quantization
        // Structure: ql (128 bytes) + qh (64 bytes) + scales (16 bytes) + d (FP16)
        var numBlocks = (int)((elementCount + QK_K - 1) / QK_K);
        var limit = Math.Min(previewLimit, elementCount > int.MaxValue ? int.MaxValue : (int)elementCount);
        if (limit <= 0)
        {
            return Array.Empty<float>();
        }

        var result = new float[limit];
        long globalIndex = 0;

        for (int b = 0; b < numBlocks && globalIndex < elementCount; b++)
        {
            var ql = new byte[128];
            reader.Read(ql, 0, 128);
            var qh = new byte[64];
            reader.Read(qh, 0, 64);
            var scales = new sbyte[16];
            for (int i = 0; i < 16; i++)
                scales[i] = reader.ReadSByte();
            var d = HalfToFloat(reader.ReadUInt16());

            for (int i = 0; i < QK_K && globalIndex < elementCount; i++)
            {
                int scaleIdx = i / 16;
                float scale = scales[scaleIdx] * d;

                // Fix: Reconstruct 6-bit value correctly - ql contains 4 bits per element (packed 2 per byte)
                int qlIdx = i / 2;  // 2 values per byte
                int qlShift = (i % 2) * 4;  // 0 or 4 bit offset
                int lowBits = (ql[qlIdx] >> qlShift) & 0x0F;  // Extract 4 bits

                // qh contains 2 high bits per element (packed 4 per byte)
                int qhIdx = i / 4;  // 4 values per byte
                int qhShift = (i % 4) * 2;  // 0, 2, 4, or 6 bit offset
                int highBits = (qh[qhIdx] >> qhShift) & 0x03;  // Extract 2 bits

                int q = lowBits | (highBits << 4);  // Combine into 6-bit value
                q -= 32; // Center at 0

                if (globalIndex < limit)
                {
                    result[(int)globalIndex] = q * scale;
                }
                globalIndex++;
                if (globalIndex >= limit)
                {
                    return result;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Converts IEEE 754 half-precision (FP16) to single-precision float.
    /// </summary>
    private static float HalfToFloat(ushort half)
    {
        uint sign = (uint)(half >> 15) << 31;
        uint exponent = (uint)((half >> 10) & 0x1F);
        uint mantissa = (uint)(half & 0x3FF);

        if (exponent == 0)
        {
            if (mantissa == 0)
                return BitConverter.ToSingle(BitConverter.GetBytes(sign), 0);

            // Denormalized number
            while ((mantissa & 0x400) == 0)
            {
                mantissa <<= 1;
                exponent--;
            }
            exponent++;
            mantissa &= 0x3FF;
        }
        else if (exponent == 31)
        {
            // Infinity or NaN
            return BitConverter.ToSingle(BitConverter.GetBytes(sign | 0x7F800000 | (mantissa << 13)), 0);
        }

        exponent = exponent + (127 - 15);
        mantissa = mantissa << 13;

        uint result = sign | (exponent << 23) | mantissa;
        return BitConverter.ToSingle(BitConverter.GetBytes(result), 0);
    }
}
