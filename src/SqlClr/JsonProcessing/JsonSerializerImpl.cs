using System;
using System.Collections.Generic;
using System.Text.Json;
using SqlClrFunctions.Contracts;

namespace SqlClrFunctions.JsonProcessing
{
    /// <summary>
    /// Enterprise-grade JSON serialization using System.Text.Json.
    /// Replaces ALL manual string parsing in SQL CLR.
    /// </summary>
    public class JsonSerializerImpl : IJsonSerializer
    {
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public List<T> DeserializeArray<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<T>();

            try
            {
                var result = JsonSerializer.Deserialize<List<T>>(json, _options);
                return result ?? new List<T>();
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"Invalid JSON array: {ex.Message}", nameof(json), ex);
            }
        }

        public T? DeserializeObject<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return default;

            try
            {
                return JsonSerializer.Deserialize<T>(json, _options);
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"Invalid JSON object: {ex.Message}", nameof(json), ex);
            }
        }

        public string Serialize<T>(T obj)
        {
            if (obj == null)
                return "null";

            return JsonSerializer.Serialize(obj, _options);
        }

        public float[]? ParseFloatArray(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                return JsonSerializer.Deserialize<float[]>(json, _options);
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"Invalid float array JSON: {ex.Message}", nameof(json), ex);
            }
        }

        public string SerializeFloatArray(float[] values)
        {
            if (values == null || values.Length == 0)
                return "[]";

            return JsonSerializer.Serialize(values);
        }
    }
}
