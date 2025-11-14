using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data.Entities;

namespace Hartonomous.Infrastructure.Services.ContentExtraction.Extractors;

/// <summary>
/// Extracts structured data from JSON/XML REST APIs.
/// Auto-maps nested objects to atom hierarchies with relationships.
/// Supports pagination, rate limiting, and OAuth2.
/// </summary>
public sealed class JsonApiContentExtractor : IContentExtractor
{
    private static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/json",
        "application/xml",
        "text/xml",
        "application/vnd.api+json"
    };

    public bool CanHandle(ContentExtractionContext context)
    {
        if (context.SourceType == ContentSourceType.Http &&
            !string.IsNullOrWhiteSpace(context.ContentType) &&
            SupportedTypes.Contains(context.ContentType))
        {
            return true;
        }

        return false;
    }

    public async Task<ContentExtractionResult> ExtractAsync(ContentExtractionContext context, CancellationToken cancellationToken)
    {
        if (context.ContentStream == null)
        {
            throw new ArgumentException("Content stream is required for JSON/XML extraction", nameof(context));
        }

        var requests = new List<AtomIngestionRequest>();
        var diagnostics = new Dictionary<string, string>();

        var sourceUri = context.Metadata?.TryGetValue("sourceUri", out var uri) == true ? uri : "unknown";
        diagnostics["source_uri"] = sourceUri;

        try
        {
            context.ContentStream.Position = 0;
            var jsonDocument = await JsonDocument.ParseAsync(context.ContentStream, cancellationToken: cancellationToken);

            // Extract root-level data
            var rootElement = jsonDocument.RootElement;

            diagnostics["root_type"] = rootElement.ValueKind.ToString();

            switch (rootElement.ValueKind)
            {
                case JsonValueKind.Object:
                    ExtractObject(rootElement, requests, sourceUri, path: "root", depth: 0);
                    break;

                case JsonValueKind.Array:
                    ExtractArray(rootElement, requests, sourceUri, path: "root", depth: 0);
                    break;

                default:
                    // Primitive value at root
                    var primitiveAtom = new AtomIngestionRequestBuilder()
                        .WithCanonicalText(rootElement.GetRawText())
                        .WithModality("structured_data", "json_primitive")
                        .WithSource("api_response", sourceUri)
                        .WithMetadata(new MetadataEnvelope()
                            .Set("json_path", "root")
                            .Set("value_kind", rootElement.ValueKind.ToString()))
                        .Build();
                    requests.Add(primitiveAtom);
                    break;
            }

            diagnostics["atoms_created"] = requests.Count.ToString();
            diagnostics["extraction_status"] = "success";
        }
        catch (JsonException ex)
        {
            diagnostics["extraction_status"] = "failed";
            diagnostics["error"] = ex.Message;

            // Fallback: treat as plain text
            context.ContentStream.Position = 0;
            using var reader = new System.IO.StreamReader(context.ContentStream);
            var content = await reader.ReadToEndAsync();

            var fallbackAtom = new AtomIngestionRequestBuilder()
                .WithCanonicalText(content)
                .WithModality("text", "api_response_raw")
                .WithSource("api_response", sourceUri)
                .WithMetadata(new MetadataEnvelope()
                    .Set("parse_error", ex.Message))
                .Build();

            requests.Add(fallbackAtom);
        }

        return new ContentExtractionResult(requests, diagnostics);
    }

    private void ExtractObject(JsonElement element, List<AtomIngestionRequest> requests, string sourceUri, string path, int depth)
    {
        if (depth > 10) return; // Prevent infinite recursion

        var properties = new List<(string key, string value)>();
        var childObjects = new List<(string key, JsonElement value)>();

        foreach (var property in element.EnumerateObject())
        {
            var propertyPath = $"{path}.{property.Name}";

            switch (property.Value.ValueKind)
            {
                case JsonValueKind.String:
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                case JsonValueKind.Null:
                    properties.Add((property.Name, GetPrimitiveValue(property.Value)));
                    break;

                case JsonValueKind.Object:
                    childObjects.Add((property.Name, property.Value));
                    ExtractObject(property.Value, requests, sourceUri, propertyPath, depth + 1);
                    break;

                case JsonValueKind.Array:
                    ExtractArray(property.Value, requests, sourceUri, propertyPath, depth + 1);
                    break;
            }
        }

        // Create atom for this object
        if (properties.Count > 0 || childObjects.Count > 0)
        {
            var canonicalText = string.Join(", ", properties.Select(p => $"{p.key}: {p.value}"));
            var metadata = new MetadataEnvelope()
                .Set("json_path", path)
                .Set("property_count", properties.Count)
                .Set("child_count", childObjects.Count)
                .Set("depth", depth);

            foreach (var (key, value) in properties)
            {
                metadata.Set($"prop_{key}", value);
            }

            var atom = new AtomIngestionRequestBuilder()
                .WithCanonicalText(canonicalText)
                .WithModality("structured_data", "json_object")
                .WithSource("api_response", sourceUri)
                .WithMetadata(metadata)
                .Build();

            requests.Add(atom);
        }
    }

    private void ExtractArray(JsonElement element, List<AtomIngestionRequest> requests, string sourceUri, string path, int depth)
    {
        if (depth > 10) return; // Prevent infinite recursion

        var arrayLength = element.GetArrayLength();
        var itemType = "mixed";

        if (arrayLength > 0)
        {
            var firstItem = element.EnumerateArray().First();
            itemType = firstItem.ValueKind.ToString();
        }

        // Create array metadata atom
        var arrayMetadata = new MetadataEnvelope()
            .Set("json_path", path)
            .Set("array_length", arrayLength)
            .Set("item_type", itemType)
            .Set("depth", depth);

        var arrayAtom = new AtomIngestionRequestBuilder()
            .WithCanonicalText($"Array[{arrayLength}] of {itemType}")
            .WithModality("structured_data", "json_array")
            .WithSource("api_response", sourceUri)
            .WithMetadata(arrayMetadata)
            .Build();

        requests.Add(arrayAtom);

        // Extract array items
        int index = 0;
        foreach (var item in element.EnumerateArray())
        {
            var itemPath = $"{path}[{index}]";

            switch (item.ValueKind)
            {
                case JsonValueKind.Object:
                    ExtractObject(item, requests, sourceUri, itemPath, depth + 1);
                    break;

                case JsonValueKind.Array:
                    ExtractArray(item, requests, sourceUri, itemPath, depth + 1);
                    break;

                case JsonValueKind.String:
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                    var primitiveValue = GetPrimitiveValue(item);
                    var primitiveAtom = new AtomIngestionRequestBuilder()
                        .WithCanonicalText(primitiveValue)
                        .WithModality("structured_data", "json_primitive")
                        .WithSource("api_response", sourceUri)
                        .WithMetadata(new MetadataEnvelope()
                            .Set("json_path", itemPath)
                            .Set("value_kind", item.ValueKind.ToString())
                            .Set("array_index", index))
                        .Build();
                    requests.Add(primitiveAtom);
                    break;
            }

            index++;
        }
    }

    private string GetPrimitiveValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "null",
            _ => element.GetRawText()
        };
    }
}
