using System;

namespace ModelIngestion.ModelFormats;

internal static class Float16Utilities
{
    public static float HalfToFloat(ushort halfBits)
    {
        uint sign = (uint)(halfBits & 0x8000) << 16;
        uint exponent = (uint)(halfBits & 0x7C00) >> 10;
        uint mantissa = (uint)(halfBits & 0x03FF);

        if (exponent == 0)
        {
            if (mantissa == 0)
            {
                return BitConverter.UInt32BitsToSingle(sign);
            }

            while ((mantissa & 0x0400) == 0)
            {
                mantissa <<= 1;
                exponent--;
            }

            mantissa &= 0x03FF;
        }
        else if (exponent == 0x1F)
        {
            return BitConverter.UInt32BitsToSingle(sign | 0x7F800000 | (mantissa << 13));
        }

        exponent += 127 - 15;
        mantissa <<= 13;

        return BitConverter.UInt32BitsToSingle(sign | (exponent << 23) | mantissa);
    }

    public static float BFloat16ToFloat(ushort bfloat16Bits)
    {
        uint floatBits = (uint)bfloat16Bits << 16;
        return BitConverter.UInt32BitsToSingle(floatBits);
    }
}
