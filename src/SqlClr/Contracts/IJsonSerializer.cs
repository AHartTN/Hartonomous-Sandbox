using System;
using System.Collections.Generic;

namespace SqlClrFunctions.Contracts
{
    /// <summary>
    /// Interface for robust JSON serialization using System.Text.Json.
    /// Replaces all manual string parsing with proper library implementation.
    /// </summary>
    public interface IJsonSerializer
    {
        /// <summary>
        /// Deserialize JSON array to strongly-typed list.
        /// </summary>
        List<T> DeserializeArray<T>(string json);

        /// <summary>
        /// Deserialize JSON object to strongly-typed instance.
        /// </summary>
        T? DeserializeObject<T>(string json);

        /// <summary>
        /// Serialize object to JSON string.
        /// </summary>
        string Serialize<T>(T obj);

        /// <summary>
        /// Parse JSON array of floats (for vectors).
        /// More efficient than generic deserialization.
        /// </summary>
        float[]? ParseFloatArray(string json);

        /// <summary>
        /// Serialize float array to JSON (for vector returns).
        /// </summary>
        string SerializeFloatArray(float[] values);
    }
}
