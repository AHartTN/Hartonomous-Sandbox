using System.Collections.Generic;
using System.Text.Json;

namespace ModelIngestion.Content;

internal static class MetadataUtilities
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false
    };

    public static string? Serialize(IDictionary<string, object?> metadata)
    {
        if (metadata.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(metadata, Options);
    }

    public static string? Serialize(IDictionary<string, string>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(metadata, Options);
    }
}
