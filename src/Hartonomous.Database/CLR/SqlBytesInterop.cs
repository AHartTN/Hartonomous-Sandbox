using System;
using System.Data.SqlTypes;

namespace Hartonomous.Clr
{
    internal static class SqlBytesInterop
    {
        internal static byte[] GetBuffer(SqlBytes value, out int byteLength)
        {
            if (value.IsNull)
            {
                byteLength = 0;
                return Array.Empty<byte>();
            }

            byte[]? buffer;
            try
            {
                buffer = value.Buffer;
            }
            catch (InvalidOperationException)
            {
                buffer = null;
            }

            buffer = buffer ?? value.Value ?? Array.Empty<byte>();

            long declaredLength = value.Length;
            if (declaredLength >= 0)
            {
                if (declaredLength > int.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "SqlBytes length exceeds supported range.");
                }

                byteLength = (int)declaredLength;
            }
            else
            {
                if (buffer.LongLength > int.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "SqlBytes buffer exceeds supported range.");
                }

                byteLength = buffer.Length;
            }

            if (byteLength > buffer.Length)
            {
                byteLength = buffer.Length;
            }

            return buffer;
        }

        internal static void GetFloatBuffer(SqlBytes value, out byte[] buffer, out int floatCount)
        {
            buffer = GetBuffer(value, out var byteLength);
            if ((byteLength & (sizeof(float) - 1)) != 0)
            {
                throw new ArgumentException("Vector byte length must be a multiple of 4 bytes.", nameof(value));
            }

            floatCount = byteLength / sizeof(float);
        }

        internal static float[] GetFloatArray(SqlBytes value, out int floatCount)
        {
            var buffer = GetBuffer(value, out var byteLength);
            if ((byteLength & (sizeof(float) - 1)) != 0)
            {
                throw new ArgumentException("Vector byte length must be a multiple of 4 bytes.", nameof(value));
            }

            floatCount = byteLength / sizeof(float);
            if (floatCount == 0)
            {
                return Array.Empty<float>();
            }

            var result = new float[floatCount];
            Buffer.BlockCopy(buffer, 0, result, 0, byteLength);
            return result;
        }

        internal static SqlBytes CreateFromFloats(float[] values)
        {
            if (values is null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (values.Length == 0)
            {
                return new SqlBytes(Array.Empty<byte>());
            }

            var bytes = new byte[values.Length * sizeof(float)];
            Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
            return new SqlBytes(bytes);
        }

        internal static SqlBytes CreateFromBytes(byte[] values)
        {
            if (values is null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            return values.Length == 0 ? new SqlBytes(Array.Empty<byte>()) : new SqlBytes(values);
        }
    }
}
