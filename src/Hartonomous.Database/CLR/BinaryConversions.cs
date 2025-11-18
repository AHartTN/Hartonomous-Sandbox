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

        /// <summary>
        /// Parses a JSON array string into a float array.
        /// Used by aggregates that receive vector data as JSON from SQL.
        /// Format: "[1.0, 2.0, 3.0]"
        /// </summary>
        /// <param name="json">JSON array string</param>
        /// <returns>Float array or null if parsing fails</returns>
        public static float[] ParseVectorJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                json = json.Trim();
                if (!json.StartsWith("[") || !json.EndsWith("]"))
                    return null;

                return json.Substring(1, json.Length - 2)
                    .Split(',')
                    .Select(s => float.Parse(s.Trim()))
                    .ToArray();
            }
            catch
            {
                return null;
            }
        }
    }
}
