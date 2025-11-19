using System;
using System.Text;
using System.Collections.Generic;
using System.Globalization;

namespace Hartonomous.Clr.Core
{
    /// <summary>
    /// Centralized JSON formatting utilities for CLR aggregates.
    /// Provides consistent, allocation-efficient JSON generation.
    /// </summary>
    internal static class JsonFormatter
    {
        /// <summary>
        /// Format a numeric value for JSON output (6 significant figures).
        /// </summary>
        public static string FormatNumber(double value)
        {
            if (double.IsNaN(value)) return "null";
            if (double.IsInfinity(value)) return "null";
            return value.ToString("G6", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Format an array of floats as JSON array.
        /// </summary>
        public static void AppendFloatArray(StringBuilder sb, float[] array)
        {
            if (array == null || array.Length == 0)
            {
                sb.Append("[]");
                return;
            }

            sb.Append('[');
            for (int i = 0; i < array.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(FormatNumber(array[i]));
            }
            sb.Append(']');
        }

        /// <summary>
        /// Format an array of doubles as JSON array.
        /// </summary>
        public static void AppendDoubleArray(StringBuilder sb, double[] array)
        {
            if (array == null || array.Length == 0)
            {
                sb.Append("[]");
                return;
            }

            sb.Append('[');
            for (int i = 0; i < array.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(FormatNumber(array[i]));
            }
            sb.Append(']');
        }

        /// <summary>
        /// Start a JSON object.
        /// </summary>
        public static void BeginObject(StringBuilder sb) => sb.Append('{');

        /// <summary>
        /// End a JSON object.
        /// </summary>
        public static void EndObject(StringBuilder sb) => sb.Append('}');

        /// <summary>
        /// Start a JSON array.
        /// </summary>
        public static void BeginArray(StringBuilder sb) => sb.Append('[');

        /// <summary>
        /// End a JSON array.
        /// </summary>
        public static void EndArray(StringBuilder sb) => sb.Append(']');

        /// <summary>
        /// Append a JSON property (key-value pair).
        /// </summary>
        public static void AppendProperty(StringBuilder sb, string key, string value, bool isLast = false)
        {
            sb.Append('"').Append(key).Append("\":\"").Append(EscapeString(value)).Append('"');
            if (!isLast) sb.Append(',');
        }

        /// <summary>
        /// Append a numeric JSON property.
        /// </summary>
        public static void AppendProperty(StringBuilder sb, string key, double value, bool isLast = false)
        {
            sb.Append('"').Append(key).Append("\":").Append(FormatNumber(value));
            if (!isLast) sb.Append(',');
        }

        /// <summary>
        /// Append an integer JSON property.
        /// </summary>
        public static void AppendProperty(StringBuilder sb, string key, int value, bool isLast = false)
        {
            sb.Append('"').Append(key).Append("\":").Append(value);
            if (!isLast) sb.Append(',');
        }

        /// <summary>
        /// Append a boolean JSON property.
        /// </summary>
        public static void AppendProperty(StringBuilder sb, string key, bool value, bool isLast = false)
        {
            sb.Append('"').Append(key).Append("\":").Append(value ? "true" : "false");
            if (!isLast) sb.Append(',');
        }

        /// <summary>
        /// Escape string for JSON.
        /// </summary>
        public static string EscapeString(string str)
        {
            if (str == null) return "";
            return str.Replace("\\", "\\\\")
                     .Replace("\"", "\\\"")
                     .Replace("\n", "\\n")
                     .Replace("\r", "\\r")
                     .Replace("\t", "\\t");
        }

        /// <summary>
        /// Create a simple JSON object with key-value pairs.
        /// </summary>
        public static string CreateObject(params (string key, object value)[] properties)
        {
            var sb = new StringBuilder();
            BeginObject(sb);
            
            for (int i = 0; i < properties.Length; i++)
            {
                if (i > 0) sb.Append(',');
                var (key, value) = properties[i];
                
                if (value is int intVal)
                    AppendProperty(sb, key, intVal, true);
                else if (value is double doubleVal)
                    AppendProperty(sb, key, doubleVal, true);
                else if (value is bool boolVal)
                    AppendProperty(sb, key, boolVal, true);
                else if (value is string strVal)
                    AppendProperty(sb, key, strVal, true);
                else
                    AppendProperty(sb, key, value?.ToString() ?? "null", true);
            }
            
            EndObject(sb);
            return sb.ToString();
        }
    }
}
