using System;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;

namespace Hartonomous.Clr
{
    /// <summary>
    /// Simple binary conversion utilities for atomic value decomposition.
    /// Converts VARBINARY storage to native SQL types.
    /// </summary>
    public static class BinaryConversions
    {
        /// <summary>
        /// Extract a single float32 value from VARBINARY(64) atomic storage.
        /// Reinterprets the first 4 bytes of VARBINARY as IEEE-754 single-precision float.
        /// This enables direct conversion from AtomicValue (VARBINARY) to FLOAT in SQL views.
        /// </summary>
        [SqlFunction(Name = "clr_BinaryToFloat", IsDeterministic = true, IsPrecise = false)]
        public static SqlSingle BinaryToFloat(SqlBytes binaryValue)
        {
            if (binaryValue.IsNull)
                return SqlSingle.Null;

            // This is a simplified stand-in for what would be a more robust
            // interop method to get the buffer without copying if possible.
            var buffer = binaryValue.Buffer;
            
            if (buffer.Length < sizeof(float))
                return SqlSingle.Null;

            // Reinterpret first 4 bytes as float32 using BitConverter
            float result = BitConverter.ToSingle(buffer, 0);

            return new SqlSingle(result);
        }
    }
}
