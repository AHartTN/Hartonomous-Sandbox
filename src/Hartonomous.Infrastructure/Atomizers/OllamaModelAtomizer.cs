using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces.Ingestion;

namespace Hartonomous.Infrastructure.Atomizers;

/// <summary>
/// Atomizes AI models from Ollama local model registry or Ollama Library.
/// Supports pulling model metadata, layer information, and embeddings ingestion.
/// Input: model name (e.g., "llama3.2", "mistral:7b-instruct") or full Ollama model identifier.
/// </summary>
public class OllamaModelAtomizer : IAtomizer<string>
{
    private readonly HttpClient _httpClient;
    private const int MaxAtomSize = 64;
    public int Priority => 60;

    // Ollama default endpoint
    private const string DefaultOllamaEndpoint = "http://localhost:11434";

    public OllamaModelAtomizer(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public bool CanHandle(string contentType, string? fileExtension)
    {
        // This atomizer handles model identifiers, not file content types
        // Will be invoked explicitly via API
        return false;
    }

    /// <summary>
    /// Atomize an Ollama model by name.
    /// Input format: "model:tag" or just "model" (e.g., "llama3.2", "mistral:7b-instruct")
    /// </summary>
    public async Task<AtomizationResult> AtomizeAsync(string modelIdentifier, SourceMetadata source, CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var atoms = new List<AtomData>();
        var compositions = new List<AtomComposition>();
        var warnings = new List<string>();

        try
        {
            // Parse model identifier
            var (modelName, modelTag) = ParseModelIdentifier(modelIdentifier);
            var fullIdentifier = string.IsNullOrEmpty(modelTag) ? modelName : $"{modelName}:{modelTag}";

            // Use custom endpoint from source metadata if provided
            var ollamaEndpoint = source.Metadata?.Contains("ollamaEndpoint") == true 
                ? ExtractOllamaEndpoint(source.Metadata)
                : DefaultOllamaEndpoint;

            // Create parent atom for the model
            var modelBytes = Encoding.UTF8.GetBytes($"ollama:{fullIdentifier}");
            var modelHash = SHA256.HashData(modelBytes);
            
            var modelAtom = new AtomData
            {
                AtomicValue = modelBytes,
                ContentHash = modelHash,
                Modality = "ai-model",
                Subtype = "ollama-model",
                ContentType = "application/x-ollama",
                CanonicalText = fullIdentifier,
                Metadata = $"{{\"modelName\":\"{modelName}\",\"tag\":\"{modelTag ?? "latest"}\",\"source\":\"ollama\"}}"
            };
            atoms.Add(modelAtom);

            // Fetch model information from Ollama API
            await FetchModelInfoAsync(ollamaEndpoint, fullIdentifier, modelHash, atoms, compositions, warnings, cancellationToken);

            // Fetch model layers/blobs
            await FetchModelLayersAsync(ollamaEndpoint, fullIdentifier, modelHash, atoms, compositions, warnings, cancellationToken);

            sw.Stop();

            var uniqueHashes = atoms.Select(a => Convert.ToBase64String(a.ContentHash)).Distinct().Count();

            return new AtomizationResult
            {
                Atoms = atoms,
                Compositions = compositions,
                ProcessingInfo = new ProcessingMetadata
                {
                    TotalAtoms = atoms.Count,
                    UniqueAtoms = uniqueHashes,
                    DurationMs = sw.ElapsedMilliseconds,
                    AtomizerType = nameof(OllamaModelAtomizer),
                    DetectedFormat = "Ollama Model",
                    Warnings = warnings.Count > 0 ? warnings : null
                }
            };
        }
        catch (HttpRequestException ex)
        {
            warnings.Add($"Failed to connect to Ollama: {ex.Message}. Ensure Ollama is running.");
            throw;
        }
        catch (Exception ex)
        {
            warnings.Add($"Ollama model atomization failed: {ex.Message}");
            throw;
        }
    }

    private (string name, string? tag) ParseModelIdentifier(string identifier)
    {
        var parts = identifier.Split(':', 2);
        return parts.Length == 2 ? (parts[0], parts[1]) : (parts[0], null);
    }

    private string ExtractOllamaEndpoint(string metadata)
    {
        try
        {
            var json = JsonDocument.Parse(metadata);
            if (json.RootElement.TryGetProperty("ollamaEndpoint", out var endpoint))
                return endpoint.GetString() ?? DefaultOllamaEndpoint;
        }
        catch { }
        return DefaultOllamaEndpoint;
    }

    private async Task FetchModelInfoAsync(
        string endpoint,
        string modelIdentifier,
        byte[] modelHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        try
        {
            // Call Ollama API: POST /api/show
            var showRequest = new { name = modelIdentifier };
            var response = await _httpClient.PostAsJsonAsync($"{endpoint}/api/show", showRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                warnings.Add($"Failed to fetch model info: {response.StatusCode}");
                return;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var modelInfo = JsonDocument.Parse(content);

            // Extract model metadata
            var infoBytes = Encoding.UTF8.GetBytes(content.Length > 4096 ? content[..4096] : content);
            var infoHash = SHA256.HashData(infoBytes);

            var infoAtom = new AtomData
            {
                AtomicValue = infoBytes.Length <= MaxAtomSize ? infoBytes : infoBytes.Take(MaxAtomSize).ToArray(),
                ContentHash = infoHash,
                Modality = "ai-model",
                Subtype = "ollama-metadata",
                ContentType = "application/json",
                CanonicalText = $"Ollama model info: {modelIdentifier}",
                Metadata = content.Length > 1024 ? content[..1024] : content
            };

            atoms.Add(infoAtom);

            compositions.Add(new AtomComposition
            {
                ParentAtomHash = modelHash,
                ComponentAtomHash = infoHash,
                SequenceIndex = 0,
                Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
            });

            // Extract key properties
            ExtractModelProperties(modelInfo.RootElement, modelHash, atoms, compositions);
        }
        catch (Exception ex)
        {
            warnings.Add($"Model info fetch failed: {ex.Message}");
        }
    }

    private void ExtractModelProperties(
        JsonElement modelInfo,
        byte[] modelHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions)
    {
        int propIndex = 1;

        // Extract model file (Modelfile)
        if (modelInfo.TryGetProperty("modelfile", out var modelfile))
        {
            var modelfileText = modelfile.GetString();
            if (!string.IsNullOrEmpty(modelfileText))
            {
                var modelfileBytes = Encoding.UTF8.GetBytes(modelfileText);
                var modelfileHash = SHA256.HashData(modelfileBytes);

                var modelfileAtom = new AtomData
                {
                    AtomicValue = modelfileBytes.Length <= MaxAtomSize ? modelfileBytes : modelfileBytes.Take(MaxAtomSize).ToArray(),
                    ContentHash = modelfileHash,
                    Modality = "text",
                    Subtype = "ollama-modelfile",
                    ContentType = "text/plain",
                    CanonicalText = modelfileText.Length <= 200 ? modelfileText : modelfileText[..200] + "...",
                    Metadata = "{\"type\":\"modelfile\"}"
                };

                atoms.Add(modelfileAtom);

                compositions.Add(new AtomComposition
                {
                    ParentAtomHash = modelHash,
                    ComponentAtomHash = modelfileHash,
                    SequenceIndex = propIndex++,
                    Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
                });
            }
        }

        // Extract parameters
        if (modelInfo.TryGetProperty("parameters", out var parameters))
        {
            var paramsText = parameters.GetString();
            if (!string.IsNullOrEmpty(paramsText))
            {
                var paramsBytes = Encoding.UTF8.GetBytes(paramsText);
                var paramsHash = SHA256.HashData(paramsBytes);

                var paramsAtom = new AtomData
                {
                    AtomicValue = paramsBytes,
                    ContentHash = paramsHash,
                    Modality = "ai-model",
                    Subtype = "ollama-parameters",
                    ContentType = "text/plain",
                    CanonicalText = paramsText,
                    Metadata = "{\"type\":\"parameters\"}"
                };

                if (!atoms.Any(a => a.ContentHash.SequenceEqual(paramsHash)))
                {
                    atoms.Add(paramsAtom);
                }

                compositions.Add(new AtomComposition
                {
                    ParentAtomHash = modelHash,
                    ComponentAtomHash = paramsHash,
                    SequenceIndex = propIndex++,
                    Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
                });
            }
        }

        // Extract template
        if (modelInfo.TryGetProperty("template", out var template))
        {
            var templateText = template.GetString();
            if (!string.IsNullOrEmpty(templateText))
            {
                var templateBytes = Encoding.UTF8.GetBytes(templateText);
                var templateHash = SHA256.HashData(templateBytes);

                var templateAtom = new AtomData
                {
                    AtomicValue = templateBytes.Length <= MaxAtomSize ? templateBytes : templateBytes.Take(MaxAtomSize).ToArray(),
                    ContentHash = templateHash,
                    Modality = "text",
                    Subtype = "ollama-template",
                    ContentType = "text/plain",
                    CanonicalText = templateText.Length <= 200 ? templateText : templateText[..200] + "...",
                    Metadata = "{\"type\":\"prompt-template\"}"
                };

                atoms.Add(templateAtom);

                compositions.Add(new AtomComposition
                {
                    ParentAtomHash = modelHash,
                    ComponentAtomHash = templateHash,
                    SequenceIndex = propIndex++,
                    Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
                });
            }
        }
    }

    private async Task FetchModelLayersAsync(
        string endpoint,
        string modelIdentifier,
        byte[] modelHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        try
        {
            // List local models to get details
            var response = await _httpClient.GetAsync($"{endpoint}/api/tags", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                warnings.Add($"Failed to fetch model list: {response.StatusCode}");
                return;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var modelList = JsonDocument.Parse(content);

            if (!modelList.RootElement.TryGetProperty("models", out var models))
                return;

            // Find our model
            foreach (var model in models.EnumerateArray())
            {
                if (!model.TryGetProperty("name", out var name))
                    continue;

                var modelName = name.GetString();
                if (modelName != modelIdentifier)
                    continue;

                // Extract size
                if (model.TryGetProperty("size", out var size))
                {
                    var sizeValue = size.GetInt64();
                    var sizeBytes = Encoding.UTF8.GetBytes($"size:{sizeValue}");
                    var sizeHash = SHA256.HashData(sizeBytes);

                    var sizeAtom = new AtomData
                    {
                        AtomicValue = sizeBytes,
                        ContentHash = sizeHash,
                        Modality = "ai-model",
                        Subtype = "ollama-size",
                        ContentType = "text/plain",
                        CanonicalText = $"{sizeValue:N0} bytes",
                        Metadata = $"{{\"size\":{sizeValue}}}"
                    };

                    atoms.Add(sizeAtom);

                    compositions.Add(new AtomComposition
                    {
                        ParentAtomHash = modelHash,
                        ComponentAtomHash = sizeHash,
                        SequenceIndex = 100,
                        Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
                    });
                }

                // Extract digest (model version identifier)
                if (model.TryGetProperty("digest", out var digest))
                {
                    var digestValue = digest.GetString();
                    if (!string.IsNullOrEmpty(digestValue))
                    {
                        var digestBytes = Encoding.UTF8.GetBytes(digestValue);
                        var digestHash = SHA256.HashData(digestBytes);

                        var digestAtom = new AtomData
                        {
                            AtomicValue = digestBytes.Length <= MaxAtomSize ? digestBytes : digestBytes.Take(MaxAtomSize).ToArray(),
                            ContentHash = digestHash,
                            Modality = "ai-model",
                            Subtype = "ollama-digest",
                            ContentType = "text/plain",
                            CanonicalText = digestValue,
                            Metadata = "{\"type\":\"digest\"}"
                        };

                        atoms.Add(digestAtom);

                        compositions.Add(new AtomComposition
                        {
                            ParentAtomHash = modelHash,
                            ComponentAtomHash = digestHash,
                            SequenceIndex = 101,
                            Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
                        });
                    }
                }

                // Extract model details (architecture, parameter count, quantization)
                if (model.TryGetProperty("details", out var details))
                {
                    ExtractModelDetails(details, modelHash, atoms, compositions);
                }

                break;
            }
        }
        catch (Exception ex)
        {
            warnings.Add($"Model layers fetch failed: {ex.Message}");
        }
    }

    private void ExtractModelDetails(
        JsonElement details,
        byte[] modelHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions)
    {
        int detailIndex = 200;

        // Parameter count
        if (details.TryGetProperty("parameter_size", out var paramSize))
        {
            var paramSizeText = paramSize.GetString();
            if (!string.IsNullOrEmpty(paramSizeText))
            {
                var paramBytes = Encoding.UTF8.GetBytes($"params:{paramSizeText}");
                var paramHash = SHA256.HashData(paramBytes);

                var paramAtom = new AtomData
                {
                    AtomicValue = paramBytes,
                    ContentHash = paramHash,
                    Modality = "ai-model",
                    Subtype = "ollama-param-size",
                    ContentType = "text/plain",
                    CanonicalText = paramSizeText,
                    Metadata = $"{{\"parameterSize\":\"{paramSizeText}\"}}"
                };

                atoms.Add(paramAtom);

                compositions.Add(new AtomComposition
                {
                    ParentAtomHash = modelHash,
                    ComponentAtomHash = paramHash,
                    SequenceIndex = detailIndex++,
                    Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
                });
            }
        }

        // Quantization level
        if (details.TryGetProperty("quantization_level", out var quantLevel))
        {
            var quantLevelText = quantLevel.GetString();
            if (!string.IsNullOrEmpty(quantLevelText))
            {
                var quantBytes = Encoding.UTF8.GetBytes($"quant:{quantLevelText}");
                var quantHash = SHA256.HashData(quantBytes);

                var quantAtom = new AtomData
                {
                    AtomicValue = quantBytes,
                    ContentHash = quantHash,
                    Modality = "ai-model",
                    Subtype = "ollama-quantization",
                    ContentType = "text/plain",
                    CanonicalText = quantLevelText,
                    Metadata = $"{{\"quantization\":\"{quantLevelText}\"}}"
                };

                atoms.Add(quantAtom);

                compositions.Add(new AtomComposition
                {
                    ParentAtomHash = modelHash,
                    ComponentAtomHash = quantHash,
                    SequenceIndex = detailIndex++,
                    Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
                });
            }
        }

        // Model family/architecture
        if (details.TryGetProperty("family", out var family))
        {
            var familyText = family.GetString();
            if (!string.IsNullOrEmpty(familyText))
            {
                var familyBytes = Encoding.UTF8.GetBytes($"family:{familyText}");
                var familyHash = SHA256.HashData(familyBytes);

                var familyAtom = new AtomData
                {
                    AtomicValue = familyBytes,
                    ContentHash = familyHash,
                    Modality = "ai-model",
                    Subtype = "ollama-family",
                    ContentType = "text/plain",
                    CanonicalText = familyText,
                    Metadata = $"{{\"family\":\"{familyText}\"}}"
                };

                if (!atoms.Any(a => a.ContentHash.SequenceEqual(familyHash)))
                {
                    atoms.Add(familyAtom);
                }

                compositions.Add(new AtomComposition
                {
                    ParentAtomHash = modelHash,
                    ComponentAtomHash = familyHash,
                    SequenceIndex = detailIndex++,
                    Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
                });
            }
        }
    }
}
