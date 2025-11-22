using System;
using System.IO;
using System.Text;

namespace Hartonomous.Infrastructure.Services.Vision;

/// <summary>
/// Shared binary parsing utilities for media metadata extraction.
/// Handles endianness, safe reads, and common binary patterns.
/// Eliminates code duplication across all extractors.
/// </summary>
public static class BinaryReaderHelper
{
    /// <summary>
    /// Read exact number of bytes from stream, throwing if not enough data available.
    /// </summary>
    public static void ReadExact(Stream stream, byte[] buffer, int offset, int count)
    {
        var totalRead = 0;
        while (totalRead < count)
        {
            var read = stream.Read(buffer, offset + totalRead, count - totalRead);
            if (read == 0)
                throw new EndOfStreamException($"Expected {count} bytes but only read {totalRead}");
            totalRead += read;
        }
    }

    /// <summary>
    /// Read exact number of bytes from stream, returning new array.
    /// </summary>
    public static byte[] ReadBytes(Stream stream, int count)
    {
        var buffer = new byte[count];
        ReadExact(stream, buffer, 0, count);
        return buffer;
    }

    /// <summary>
    /// Read 16-bit unsigned integer with specified endianness.
    /// </summary>
    public static ushort ReadUInt16(byte[] data, int offset, bool littleEndian = true)
    {
        if (offset + 2 > data.Length)
            throw new ArgumentException("Not enough data to read UInt16");

        if (littleEndian)
            return BitConverter.ToUInt16(data, offset);
        else
            return (ushort)((data[offset] << 8) | data[offset + 1]);
    }

    /// <summary>
    /// Read 16-bit unsigned integer from stream with specified endianness.
    /// </summary>
    public static ushort ReadUInt16(Stream stream, bool littleEndian = true)
    {
        var bytes = ReadBytes(stream, 2);
        return ReadUInt16(bytes, 0, littleEndian);
    }

    /// <summary>
    /// Read 32-bit unsigned integer with specified endianness.
    /// </summary>
    public static uint ReadUInt32(byte[] data, int offset, bool littleEndian = true)
    {
        if (offset + 4 > data.Length)
            throw new ArgumentException("Not enough data to read UInt32");

        if (littleEndian)
            return BitConverter.ToUInt32(data, offset);
        else
            return (uint)((data[offset] << 24) | (data[offset + 1] << 16) | 
                         (data[offset + 2] << 8) | data[offset + 3]);
    }

    /// <summary>
    /// Read 32-bit unsigned integer from stream with specified endianness.
    /// </summary>
    public static uint ReadUInt32(Stream stream, bool littleEndian = true)
    {
        var bytes = ReadBytes(stream, 4);
        return ReadUInt32(bytes, 0, littleEndian);
    }

    /// <summary>
    /// Read 64-bit unsigned integer with specified endianness.
    /// </summary>
    public static ulong ReadUInt64(byte[] data, int offset, bool littleEndian = true)
    {
        if (offset + 8 > data.Length)
            throw new ArgumentException("Not enough data to read UInt64");

        if (littleEndian)
            return BitConverter.ToUInt64(data, offset);
        else
            return ((ulong)data[offset] << 56) | ((ulong)data[offset + 1] << 48) |
                   ((ulong)data[offset + 2] << 40) | ((ulong)data[offset + 3] << 32) |
                   ((ulong)data[offset + 4] << 24) | ((ulong)data[offset + 5] << 16) |
                   ((ulong)data[offset + 6] << 8) | data[offset + 7];
    }

    /// <summary>
    /// Read 64-bit unsigned integer from stream with specified endianness.
    /// </summary>
    public static ulong ReadUInt64(Stream stream, bool littleEndian = true)
    {
        var bytes = ReadBytes(stream, 8);
        return ReadUInt64(bytes, 0, littleEndian);
    }

    /// <summary>
    /// Read null-terminated ASCII string with maximum length.
    /// </summary>
    public static string ReadString(byte[] data, int offset, int maxLength)
    {
        if (offset >= data.Length)
            return string.Empty;

        var length = 0;
        while (length < maxLength && offset + length < data.Length && data[offset + length] != 0)
        {
            length++;
        }

        return length > 0 ? Encoding.ASCII.GetString(data, offset, length) : string.Empty;
    }

    /// <summary>
    /// Read fixed-length ASCII string from stream.
    /// </summary>
    public static string ReadFixedString(Stream stream, int length)
    {
        var bytes = ReadBytes(stream, length);
        return Encoding.ASCII.GetString(bytes).TrimEnd('\0');
    }

    /// <summary>
    /// Read rational number (EXIF format: numerator/denominator as two 32-bit values).
    /// </summary>
    public static double ReadRational(byte[] data, int offset, bool littleEndian = true)
    {
        var numerator = ReadUInt32(data, offset, littleEndian);
        var denominator = ReadUInt32(data, offset + 4, littleEndian);
        return denominator > 0 ? (double)numerator / denominator : 0;
    }

    /// <summary>
    /// Read synchsafe integer (ID3v2 format: 7 bits per byte).
    /// Used in ID3v2 tags where the highest bit of each byte is always 0.
    /// </summary>
    public static int ReadSynchsafeInt(byte[] data, int offset, int byteCount)
    {
        var value = 0;
        for (var i = 0; i < byteCount && offset + i < data.Length; i++)
        {
            value = (value << 7) | (data[offset + i] & 0x7F);
        }
        return value;
    }

    /// <summary>
    /// Check if byte array starts with magic bytes (file signature).
    /// </summary>
    public static bool HasMagicBytes(byte[] data, params byte[] magic)
    {
        if (data.Length < magic.Length)
            return false;

        for (var i = 0; i < magic.Length; i++)
        {
            if (data[i] != magic[i])
                return false;
        }

        return true;
    }

    /// <summary>
    /// Check if byte array starts with ASCII string signature.
    /// </summary>
    public static bool HasMagicString(byte[] data, string magic, int offset = 0)
    {
        if (data.Length < offset + magic.Length)
            return false;

        var magicBytes = Encoding.ASCII.GetBytes(magic);
        for (var i = 0; i < magicBytes.Length; i++)
        {
            if (data[offset + i] != magicBytes[i])
                return false;
        }

        return true;
    }

    /// <summary>
    /// Safe peek at byte without advancing stream position.
    /// </summary>
    public static int PeekByte(Stream stream)
    {
        var pos = stream.Position;
        var b = stream.ReadByte();
        stream.Position = pos;
        return b;
    }

    /// <summary>
    /// Skip bytes in stream with validation.
    /// </summary>
    public static void Skip(Stream stream, long count)
    {
        if (count < 0)
            throw new ArgumentException("Cannot skip negative bytes");

        stream.Seek(count, SeekOrigin.Current);
    }

    /// <summary>
    /// Read FourCC code (4-character ASCII identifier used in multimedia formats).
    /// </summary>
    public static string ReadFourCC(Stream stream)
    {
        return ReadFixedString(stream, 4);
    }

    /// <summary>
    /// Read FourCC from byte array at offset.
    /// </summary>
    public static string ReadFourCC(byte[] data, int offset)
    {
        if (offset + 4 > data.Length)
            return string.Empty;

        return Encoding.ASCII.GetString(data, offset, 4);
    }

    /// <summary>
    /// Read protobuf varint (variable-length integer) - 32-bit version.
    /// Used in ONNX, TensorFlow, and other protobuf-based formats.
    /// </summary>
    public static int ReadVarint32(Stream stream)
    {
        int result = 0;
        int shift = 0;
        
        while (true)
        {
            int b = stream.ReadByte();
            if (b == -1)
                throw new EndOfStreamException();
            
            result |= (b & 0x7F) << shift;
            
            if ((b & 0x80) == 0)
                return result;
            
            shift += 7;
            if (shift >= 32)
                throw new InvalidDataException("Varint too large for Int32");
        }
    }

    /// <summary>
    /// Read protobuf varint (variable-length integer) - 64-bit version.
    /// </summary>
    public static long ReadVarint64(Stream stream)
    {
        long result = 0;
        int shift = 0;
        
        while (true)
        {
            int b = stream.ReadByte();
            if (b == -1)
                throw new EndOfStreamException();
            
            result |= (long)(b & 0x7F) << shift;
            
            if ((b & 0x80) == 0)
                return result;
            
            shift += 7;
            if (shift >= 64)
                throw new InvalidDataException("Varint too large for Int64");
        }
    }

    /// <summary>
    /// Read zigzag-encoded signed varint.
    /// Used in protobuf for efficient encoding of signed integers.
    /// </summary>
    public static int ReadSignedVarint32(Stream stream)
    {
        uint value = (uint)ReadVarint32(stream);
        return (int)((value >> 1) ^ -(int)(value & 1));
    }

    /// <summary>
    /// Read zigzag-encoded signed varint - 64-bit version.
    /// </summary>
    public static long ReadSignedVarint64(Stream stream)
    {
        ulong value = (ulong)ReadVarint64(stream);
        return (long)((value >> 1) ^ (ulong)(-(long)(value & 1)));
    }

    /// <summary>
    /// Align stream position to specified byte boundary.
    /// Returns the aligned position.
    /// </summary>
    public static long AlignPosition(Stream stream, int alignment)
    {
        long currentPos = stream.Position;
        long remainder = currentPos % alignment;
        
        if (remainder == 0)
            return currentPos;

        long alignedPos = currentPos + (alignment - remainder);
        stream.Position = alignedPos;
        return alignedPos;
    }

    /// <summary>
    /// Calculate aligned offset without modifying stream position.
    /// </summary>
    public static long AlignOffset(long offset, int alignment)
    {
        long remainder = offset % alignment;
        return remainder == 0 ? offset : offset + (alignment - remainder);
    }

    /// <summary>
    /// Read IEEE 754 half-precision (16-bit) float.
    /// Converts to float32 for C# compatibility.
    /// </summary>
    public static float ReadFloat16(byte[] data, int offset)
    {
        ushort half = ReadUInt16(data, offset, littleEndian: true);
        
        uint sign = (uint)((half >> 15) & 0x1);
        uint exponent = (uint)((half >> 10) & 0x1F);
        uint mantissa = (uint)(half & 0x3FF);
        
        if (exponent == 0)
        {
            if (mantissa == 0)
                return sign == 1 ? -0.0f : 0.0f;
            exponent = 1;
        }
        else if (exponent == 31)
        {
            return mantissa == 0 
                ? (sign == 1 ? float.NegativeInfinity : float.PositiveInfinity)
                : float.NaN;
        }
        
        uint f32 = (sign << 31) | ((exponent + 112) << 23) | (mantissa << 13);
        return BitConverter.ToSingle(BitConverter.GetBytes(f32), 0);
    }

    /// <summary>
    /// Read BFloat16 (Brain Floating Point) format.
    /// Used in ML frameworks like TensorFlow.
    /// </summary>
    public static float ReadBFloat16(byte[] data, int offset)
    {
        ushort bf16 = ReadUInt16(data, offset, littleEndian: true);
        uint f32Bits = (uint)bf16 << 16;
        return BitConverter.ToSingle(BitConverter.GetBytes(f32Bits), 0);
    }

    /// <summary>
    /// Read signed integers with endianness support.
    /// </summary>
    public static short ReadInt16(byte[] data, int offset, bool littleEndian = true)
    {
        return (short)ReadUInt16(data, offset, littleEndian);
    }

    public static int ReadInt32(byte[] data, int offset, bool littleEndian = true)
    {
        return (int)ReadUInt32(data, offset, littleEndian);
    }

    public static long ReadInt64(byte[] data, int offset, bool littleEndian = true)
    {
        return (long)ReadUInt64(data, offset, littleEndian);
    }

    /// <summary>
    /// Read signed rational (EXIF SRATIONAL format).
    /// </summary>
    public static double ReadSignedRational(byte[] data, int offset, bool littleEndian = true)
    {
        var numerator = ReadInt32(data, offset, littleEndian);
        var denominator = ReadInt32(data, offset + 4, littleEndian);
        return denominator != 0 ? (double)numerator / denominator : 0;
    }
}
