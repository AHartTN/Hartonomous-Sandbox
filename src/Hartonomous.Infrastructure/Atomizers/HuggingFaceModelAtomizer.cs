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
/// Atomizes AI models from Hugging Face Hub.
/// Supports fetching model metadata, config, tokenizer, and model files (safetensors, pytorch_model.bin, etc.).
/// Input: Hugging Face model identifier (e.g., "meta-llama/Llama-3.2-1B", "mistralai/Mistral-7B-v0.1").
/// </summary>
public class HuggingFaceModelAtomizer : IAtomizer<string>
{
    private readonly HttpClient _httpClient;
    private readonly IEnumerable<IAtomizer<byte[]>> _byteAtomizers;
    private const int MaxAtomSize = 64;
    public int Priority => 60;

    // Hugging Face Hub API endpoint
    private const string HfApiBase = "https://huggingface.co/api";
    private const string HfRawBase = "https://huggingface.co";

    public HuggingFaceModelAtomizer(
        HttpClient httpClient,
        IEnumerable<IAtomizer<byte[]>> byteAtomizers)
    {
        _httpClient = httpClient;
        _byteAtomizers = byteAtomizers;
    }

    public bool CanHandle(string contentType, string? fileExtension)
    {
        // This atomizer handles model identifiers, not file content types
        // Will be invoked explicitly via API
        return false;
    }

    /// <summary>
    /// Atomize a Hugging Face model by repository ID.
    /// Input format: "organization/model-name" or "username/model-name" (e.g., "meta-llama/Llama-3.2-1B")
    /// </summary>
    public async Task<AtomizationResult> AtomizeAsync(string modelId, SourceMetadata source, CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var atoms = new List<AtomData>();
        var compositions = new List<AtomComposition>();
        var childSources = new List<ChildSource>();
        var warnings = new List<string>();

        try
        {
            // Validate model ID format
            if (!modelId.Contains('/'))
            {
                throw new ArgumentException($"Invalid Hugging Face model ID format. Expected 'org/model', got: {modelId}");
            }

            // Extract optional HF token from source metadata
            var hfToken = ExtractHfToken(source.Metadata);

            // Create parent atom for the model
            var modelBytes = Encoding.UTF8.GetBytes($"huggingface:{modelId}");
            var modelHash = SHA256.HashData(modelBytes);
            
            var modelAtom = new AtomData
            {
                AtomicValue = modelBytes,
                ContentHash = modelHash,
                Modality = "ai-model",
                Subtype = "huggingface-model",
                ContentType = "application/x-huggingface",
                CanonicalText = modelId,
                Metadata = $"{{\"modelId\":\"{modelId}\",\"source\":\"huggingface\"}}"
            };
            atoms.Add(modelAtom);

            // Fetch model info from HF API
            await FetchModelInfoAsync(modelId, hfToken, modelHash, atoms, compositions, warnings, cancellationToken);

            // Fetch model files list
            await FetchModelFilesAsync(modelId, hfToken, modelHash, atoms, compositions, childSources, warnings, cancellationToken);

            // Fetch key configuration files (config.json, tokenizer_config.json)
            await FetchConfigFilesAsync(modelId, hfToken, modelHash, atoms, compositions, warnings, cancellationToken);

            sw.Stop();

            var uniqueHashes = atoms.Select(a => Convert.ToBase64String(a.ContentHash)).Distinct().Count();

            return new AtomizationResult
            {
                Atoms = atoms,
                Compositions = compositions,
                ChildSources = childSources,
                ProcessingInfo = new ProcessingMetadata
                {
                    TotalAtoms = atoms.Count,
                    UniqueAtoms = uniqueHashes,
                    DurationMs = sw.ElapsedMilliseconds,
                    AtomizerType = nameof(HuggingFaceModelAtomizer),
                    DetectedFormat = "Hugging Face Model",
                    Warnings = warnings.Count > 0 ? warnings : null
                }
            };
        }
        catch (HttpRequestException ex)
        {
            warnings.Add($"Failed to fetch from Hugging Face: {ex.Message}. Check model ID and authentication.");
            throw;
        }
        catch (Exception ex)
        {
            warnings.Add($"Hugging Face model atomization failed: {ex.Message}");
            throw;
        }
    }

    private string? ExtractHfToken(string? metadata)
    {
        if (string.IsNullOrEmpty(metadata))
            return null;

        try
        {
            var json = JsonDocument.Parse(metadata);
            if (json.RootElement.TryGetProperty("hfToken", out var token))
                return token.GetString();
        }
        catch { }
        return null;
    }

    private async Task FetchModelInfoAsync(
        string modelId,
        string? hfToken,
        byte[] modelHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        try
        {
            // Call HF API: GET /api/models/{model_id}
            var request = new HttpRequestMessage(HttpMethod.Get, $"{HfApiBase}/models/{modelId}");
            if (!string.IsNullOrEmpty(hfToken))
                request.Headers.Add("Authorization", $"Bearer {hfToken}");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                warnings.Add($"Failed to fetch model info: {response.StatusCode}");
                return;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var modelInfo = JsonDocument.Parse(content);

            // Create metadata atom
            var infoBytes = Encoding.UTF8.GetBytes(content.Length > 8192 ? content[..8192] : content);
            var infoHash = SHA256.HashData(infoBytes);

            var infoAtom = new AtomData
            {
                AtomicValue = infoBytes.Length <= MaxAtomSize ? infoBytes : infoBytes.Take(MaxAtomSize).ToArray(),
                ContentHash = infoHash,
                Modality = "ai-model",
                Subtype = "huggingface-metadata",
                ContentType = "application/json",
                CanonicalText = $"Hugging Face model info: {modelId}",
                Metadata = content.Length > 2048 ? content[..2048] : content
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

        // Model card (description)
        if (modelInfo.TryGetProperty("cardData", out var cardData))
        {
            if (cardData.TryGetProperty("language", out var languages))
            {
                var langText = languages.ToString();
                var langBytes = Encoding.UTF8.GetBytes(langText);
                var langHash = SHA256.HashData(langBytes);

                var langAtom = new AtomData
                {
                    AtomicValue = langBytes.Length <= MaxAtomSize ? langBytes : langBytes.Take(MaxAtomSize).ToArray(),
                    ContentHash = langHash,
                    Modality = "text",
                    Subtype = "hf-languages",
                    ContentType = "application/json",
                    CanonicalText = langText,
                    Metadata = "{\"type\":\"languages\"}"
                };

                atoms.Add(langAtom);

                compositions.Add(new AtomComposition
                {
                    ParentAtomHash = modelHash,
                    ComponentAtomHash = langHash,
                    SequenceIndex = propIndex++,
                    Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
                });
            }

            // License
            if (cardData.TryGetProperty("license", out var license))
            {
                var licenseText = license.GetString();
                if (!string.IsNullOrEmpty(licenseText))
                {
                    var licenseBytes = Encoding.UTF8.GetBytes(licenseText);
                    var licenseHash = SHA256.HashData(licenseBytes);

                    var licenseAtom = new AtomData
                    {
                        AtomicValue = licenseBytes,
                        ContentHash = licenseHash,
                        Modality = "text",
                        Subtype = "hf-license",
                        ContentType = "text/plain",
                        CanonicalText = licenseText,
                        Metadata = $"{{\"license\":\"{licenseText}\"}}"
                    };

                    if (!atoms.Any(a => a.ContentHash.SequenceEqual(licenseHash)))
                    {
                        atoms.Add(licenseAtom);
                    }

                    compositions.Add(new AtomComposition
                    {
                        ParentAtomHash = modelHash,
                        ComponentAtomHash = licenseHash,
                        SequenceIndex = propIndex++,
                        Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
                    });
                }
            }

            // Tags
            if (cardData.TryGetProperty("tags", out var tags))
            {
                var tagsText = tags.ToString();
                var tagsBytes = Encoding.UTF8.GetBytes(tagsText);
                var tagsHash = SHA256.HashData(tagsBytes);

                var tagsAtom = new AtomData
                {
                    AtomicValue = tagsBytes.Length <= MaxAtomSize ? tagsBytes : tagsBytes.Take(MaxAtomSize).ToArray(),
                    ContentHash = tagsHash,
                    Modality = "text",
                    Subtype = "hf-tags",
                    ContentType = "application/json",
                    CanonicalText = tagsText,
                    Metadata = "{\"type\":\"tags\"}"
                };

                atoms.Add(tagsAtom);

                compositions.Add(new AtomComposition
                {
                    ParentAtomHash = modelHash,
                    ComponentAtomHash = tagsHash,
                    SequenceIndex = propIndex++,
                    Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
                });
            }
        }

        // Pipeline tag (task type)
        if (modelInfo.TryGetProperty("pipeline_tag", out var pipelineTag))
        {
            var pipelineText = pipelineTag.GetString();
            if (!string.IsNullOrEmpty(pipelineText))
            {
                var pipelineBytes = Encoding.UTF8.GetBytes(pipelineText);
                var pipelineHash = SHA256.HashData(pipelineBytes);

                var pipelineAtom = new AtomData
                {
                    AtomicValue = pipelineBytes,
                    ContentHash = pipelineHash,
                    Modality = "ai-model",
                    Subtype = "hf-pipeline-tag",
                    ContentType = "text/plain",
                    CanonicalText = pipelineText,
                    Metadata = $"{{\"task\":\"{pipelineText}\"}}"
                };

                if (!atoms.Any(a => a.ContentHash.SequenceEqual(pipelineHash)))
                {
                    atoms.Add(pipelineAtom);
                }

                compositions.Add(new AtomComposition
                {
                    ParentAtomHash = modelHash,
                    ComponentAtomHash = pipelineHash,
                    SequenceIndex = propIndex++,
                    Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
                });
            }
        }

        // Downloads count
        if (modelInfo.TryGetProperty("downloads", out var downloads))
        {
            var downloadCount = downloads.GetInt64();
            var downloadBytes = Encoding.UTF8.GetBytes($"downloads:{downloadCount}");
            var downloadHash = SHA256.HashData(downloadBytes);

            var downloadAtom = new AtomData
            {
                AtomicValue = downloadBytes,
                ContentHash = downloadHash,
                Modality = "ai-model",
                Subtype = "hf-downloads",
                ContentType = "text/plain",
                CanonicalText = $"{downloadCount:N0} downloads",
                Metadata = $"{{\"downloads\":{downloadCount}}}"
            };

            atoms.Add(downloadAtom);

            compositions.Add(new AtomComposition
            {
                ParentAtomHash = modelHash,
                ComponentAtomHash = downloadHash,
                SequenceIndex = propIndex++,
                Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
            });
        }
    }

    private async Task FetchModelFilesAsync(
        string modelId,
        string? hfToken,
        byte[] modelHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<ChildSource> childSources,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get file tree from HF API
            var request = new HttpRequestMessage(HttpMethod.Get, $"{HfApiBase}/models/{modelId}/tree/main");
            if (!string.IsNullOrEmpty(hfToken))
                request.Headers.Add("Authorization", $"Bearer {hfToken}");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                warnings.Add($"Failed to fetch model files: {response.StatusCode}");
                return;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var fileTree = JsonDocument.Parse(content);

            if (!fileTree.RootElement.EnumerateArray().Any())
                return;

            int fileIndex = 100;
            foreach (var file in fileTree.RootElement.EnumerateArray())
            {
                if (!file.TryGetProperty("path", out var pathProp))
                    continue;

                var filePath = pathProp.GetString();
                if (string.IsNullOrEmpty(filePath))
                    continue;

                // Create atom for important files (model weights, configs)
                if (IsImportantModelFile(filePath))
                {
                    var fileBytes = Encoding.UTF8.GetBytes(filePath);
                    var fileHash = SHA256.HashData(fileBytes);

                    var fileAtom = new AtomData
                    {
                        AtomicValue = fileBytes,
                        ContentHash = fileHash,
                        Modality = "ai-model",
                        Subtype = "hf-file-ref",
                        ContentType = "text/plain",
                        CanonicalText = filePath,
                        Metadata = $"{{\"path\":\"{filePath}\"}}"
                    };

                    atoms.Add(fileAtom);

                    compositions.Add(new AtomComposition
                    {
                        ParentAtomHash = modelHash,
                        ComponentAtomHash = fileHash,
                        SequenceIndex = fileIndex++,
                        Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
                    });

                    // For model weight files, create child source for deferred atomization
                    if (IsModelWeightFile(filePath))
                    {
                        // Note: We would need to download the file first to get the content bytes
                        // For now, we'll skip creating child sources and instead reference the file
                        // A future enhancement could download and create ChildSource with actual bytes
                        warnings.Add($"Model weight file {filePath} referenced but not downloaded. Implement download logic for full atomization.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            warnings.Add($"Model files fetch failed: {ex.Message}");
        }
    }

    private async Task FetchConfigFilesAsync(
        string modelId,
        string? hfToken,
        byte[] modelHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var configFiles = new[] { "config.json", "tokenizer_config.json", "generation_config.json", "model_index.json" };
        int configIndex = 200;

        foreach (var configFile in configFiles)
        {
            try
            {
                var configUrl = $"{HfRawBase}/{modelId}/resolve/main/{configFile}";
                var request = new HttpRequestMessage(HttpMethod.Get, configUrl);
                if (!string.IsNullOrEmpty(hfToken))
                    request.Headers.Add("Authorization", $"Bearer {hfToken}");

                var response = await _httpClient.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                    continue; // Config file might not exist

                var configContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var configBytes = Encoding.UTF8.GetBytes(configContent);
                var configHash = SHA256.HashData(configBytes);

                var configAtom = new AtomData
                {
                    AtomicValue = configBytes.Length <= MaxAtomSize ? configBytes : configBytes.Take(MaxAtomSize).ToArray(),
                    ContentHash = configHash,
                    Modality = "ai-model",
                    Subtype = $"hf-config-{System.IO.Path.GetFileNameWithoutExtension(configFile)}",
                    ContentType = "application/json",
                    CanonicalText = configContent.Length <= 500 ? configContent : configContent[..500] + "...",
                    Metadata = $"{{\"file\":\"{configFile}\",\"size\":{configBytes.Length}}}"
                };

                atoms.Add(configAtom);

                compositions.Add(new AtomComposition
                {
                    ParentAtomHash = modelHash,
                    ComponentAtomHash = configHash,
                    SequenceIndex = configIndex++,
                    Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
                });
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to fetch {configFile}: {ex.Message}");
            }
        }
    }

    private bool IsImportantModelFile(string filePath)
    {
        var importantPatterns = new[] 
        { 
            ".safetensors", 
            ".bin", 
            ".onnx", 
            ".gguf",
            "config.json", 
            "tokenizer",
            ".h5",
            ".pb",
            "model.pt",
            "pytorch_model"
        };

        return importantPatterns.Any(pattern => filePath.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsModelWeightFile(string filePath)
    {
        var weightExtensions = new[] { ".safetensors", ".bin", ".onnx", ".gguf", ".h5", ".pb", ".pt" };
        return weightExtensions.Any(ext => filePath.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }

    private string DetectContentType(string filePath)
    {
        var ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".safetensors" => "application/safetensors",
            ".bin" => "application/octet-stream",
            ".onnx" => "application/x-onnx",
            ".gguf" => "application/x-gguf",
            ".h5" => "application/x-hdf5",
            ".pb" => "application/x-tensorflow",
            ".pt" => "application/x-pytorch",
            ".json" => "application/json",
            _ => "application/octet-stream"
        };
    }
}
