# Model Provider Layer - Detailed Specification

**Date:** November 18, 2025  
**Status:** Design - Awaiting Implementation  
**Purpose:** Define retrieval/download layer for models from various sources

---

## Overview

The Model Provider Layer is responsible for **retrieving model files from various sources**. It does NOT parse file formats - that's the responsibility of the Format Parser Layer.

### Key Principles

1. **Single Responsibility**: Providers only handle retrieval/download
2. **Source Agnostic**: Same interface whether from HuggingFace, Ollama, local disk, or cloud
3. **Catalog Support**: Handle both single files and multi-file repositories
4. **Streaming**: Return streams for memory-efficient processing
5. **Metadata First**: Query metadata without full download when possible

---

## Core Interfaces

### IModelProvider

```csharp
namespace Hartonomous.Clr.Providers
{
    /// <summary>
    /// Represents a source for retrieving model files.
    /// Model providers handle retrieval/download only - NOT format parsing.
    /// </summary>
    public interface IModelProvider
    {
        /// <summary>
        /// Provider name (HuggingFace, Ollama, OpenAI, etc.)
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Priority for provider selection (higher = checked first).
        /// Default: 0. Custom providers: 100+.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Check if this provider can handle the given model identifier.
        /// Examples:
        ///   HuggingFace: "mistralai/Mistral-7B-v0.1"
        ///   Ollama: "ollama://llama2:7b"
        ///   Local: "file:///models/my-model.gguf"
        ///   Azure: "azure://my-endpoint/model"
        /// </summary>
        /// <param name="modelIdentifier">Model identifier string</param>
        /// <returns>True if this provider can handle the identifier</returns>
        bool CanHandle(string modelIdentifier);

        /// <summary>
        /// Retrieve model files from the provider.
        /// Returns catalog of files (for multi-file models) or single file stream.
        /// </summary>
        /// <param name="modelIdentifier">Model identifier</param>
        /// <param name="options">Retrieval options (cache, timeout, etc.)</param>
        /// <returns>Retrieval result with streams and metadata</returns>
        ModelRetrievalResult Retrieve(string modelIdentifier, RetrievalOptions options);

        /// <summary>
        /// Get metadata about model without downloading (size, format, etc.)
        /// </summary>
        /// <param name="modelIdentifier">Model identifier</param>
        /// <returns>Metadata including size, file list, detected format</returns>
        ModelProviderMetadata GetMetadata(string modelIdentifier);

        /// <summary>
        /// Validate credentials/configuration for this provider.
        /// </summary>
        /// <returns>True if provider is properly configured</returns>
        bool ValidateConfiguration();
    }
}
```

### ModelRetrievalResult

```csharp
/// <summary>
/// Result of model retrieval operation.
/// Contains either single file stream or catalog of files.
/// </summary>
public class ModelRetrievalResult : IDisposable
{
    /// <summary>
    /// Is this a single file or multi-file catalog?
    /// </summary>
    public bool IsCatalog { get; set; }

    /// <summary>
    /// Single file stream (for simple models like .gguf)
    /// Null if IsCatalog = true
    /// </summary>
    public Stream SingleFileStream { get; set; }

    /// <summary>
    /// File path for single local file (if provider is LocalFileSystem)
    /// </summary>
    public string SingleFilePath { get; set; }

    /// <summary>
    /// Multiple files (for catalogs like HuggingFace repos)
    /// Key: relative file path within catalog
    /// Value: file stream
    /// Null if IsCatalog = false
    /// </summary>
    public Dictionary<string, Stream> CatalogFiles { get; set; }

    /// <summary>
    /// Detected format (if known from provider metadata or file extension)
    /// Null if format cannot be determined at retrieval time
    /// </summary>
    public ModelFormat? DetectedFormat { get; set; }

    /// <summary>
    /// Provider-specific metadata
    /// Examples: download_url, commit_hash, last_modified, author
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; }

    /// <summary>
    /// Should streams be disposed by caller?
    /// True: caller owns streams, must dispose
    /// False: provider manages lifecycle
    /// </summary>
    public bool CallerOwnsStreams { get; set; }

    /// <summary>
    /// Total size in bytes (if known)
    /// </summary>
    public long? TotalSizeBytes { get; set; }

    /// <summary>
    /// Provider that generated this result
    /// </summary>
    public string ProviderName { get; set; }

    public void Dispose()
    {
        if (CallerOwnsStreams)
        {
            SingleFileStream?.Dispose();
            
            if (CatalogFiles != null)
            {
                foreach (var stream in CatalogFiles.Values)
                    stream?.Dispose();
            }
        }
    }
}
```

### RetrievalOptions

```csharp
/// <summary>
/// Options for model retrieval operations.
/// </summary>
public class RetrievalOptions
{
    /// <summary>
    /// Cache directory for downloaded models.
    /// If null, no caching (stream directly).
    /// </summary>
    public string CacheDirectory { get; set; }

    /// <summary>
    /// Force re-download even if cached.
    /// </summary>
    public bool ForceRefresh { get; set; }

    /// <summary>
    /// Download only metadata files (config.json, tokenizer_config.json).
    /// Don't download large weight files.
    /// </summary>
    public bool MetadataOnly { get; set; }

    /// <summary>
    /// Timeout for retrieval operations.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// API token/key for authenticated access.
    /// Should come from Azure Key Vault, not hardcoded.
    /// </summary>
    public string ApiToken { get; set; }

    /// <summary>
    /// Specific revision/commit/tag to retrieve.
    /// HuggingFace: commit hash or branch name
    /// Ollama: tag (e.g., "latest", "13b")
    /// </summary>
    public string Revision { get; set; }

    /// <summary>
    /// File patterns to include (null = all files).
    /// Example: ["*.safetensors", "config.json"]
    /// </summary>
    public string[] IncludePatterns { get; set; }

    /// <summary>
    /// File patterns to exclude.
    /// Example: ["*.md", "*.txt"] to skip documentation
    /// </summary>
    public string[] ExcludePatterns { get; set; }

    /// <summary>
    /// Maximum file size to retrieve (bytes).
    /// Files larger than this will be skipped.
    /// Null = no limit.
    /// </summary>
    public long? MaxFileSizeBytes { get; set; }

    /// <summary>
    /// Progress callback for large downloads.
    /// </summary>
    public Action<long, long> ProgressCallback { get; set; } // (bytesReceived, totalBytes)
}
```

### ModelProviderMetadata

```csharp
/// <summary>
/// Metadata about a model from provider (without full download).
/// </summary>
public class ModelProviderMetadata
{
    /// <summary>
    /// Model identifier as recognized by provider.
    /// </summary>
    public string ModelId { get; set; }

    /// <summary>
    /// Total size in bytes (if known).
    /// </summary>
    public long? SizeBytes { get; set; }

    /// <summary>
    /// List of files in model (for catalogs).
    /// </summary>
    public string[] FileNames { get; set; }

    /// <summary>
    /// Detected format (from file extension or provider metadata).
    /// </summary>
    public ModelFormat? DetectedFormat { get; set; }

    /// <summary>
    /// Model architecture (e.g., "LlamaForCausalLM", "StableDiffusionPipeline").
    /// </summary>
    public string Architecture { get; set; }

    /// <summary>
    /// Provider-specific tags/labels.
    /// Examples: "text-generation", "conversational", "quantized"
    /// </summary>
    public Dictionary<string, string> Tags { get; set; }

    /// <summary>
    /// Last modified timestamp.
    /// </summary>
    public DateTime? LastModified { get; set; }

    /// <summary>
    /// Model author/organization.
    /// </summary>
    public string Author { get; set; }

    /// <summary>
    /// License information.
    /// </summary>
    public string License { get; set; }

    /// <summary>
    /// README or description text.
    /// </summary>
    public string Description { get; set; }
}
```

---

## Provider Implementations

### 1. HuggingFaceProvider

**Purpose:** Retrieve models from HuggingFace Hub (https://huggingface.co)

**Identifier Formats:**
- `owner/repo` (e.g., `mistralai/Mistral-7B-v0.1`)
- `hf://owner/repo` (explicit protocol)
- `https://huggingface.co/owner/repo` (full URL)

**API Integration:**
- Use HuggingFace Hub API: https://huggingface.co/docs/hub/api
- Endpoints:
  - `/api/models/{repo_id}` - Model metadata
  - `/api/models/{repo_id}/tree/{revision}` - File listing
  - `https://huggingface.co/{repo_id}/resolve/{revision}/{filename}` - File download

**Authentication:**
- API token stored in Azure Key Vault
- Environment variable: `HUGGINGFACE_TOKEN`
- Required for private models

**Catalog Structure:**
```
mistralai/Mistral-7B-v0.1/
├── config.json                      (model configuration)
├── model.safetensors                (weights - SafeTensors format)
├── tokenizer.json                   (tokenizer)
├── tokenizer_config.json            (tokenizer configuration)
├── special_tokens_map.json          (special tokens)
├── generation_config.json           (generation parameters)
└── README.md                        (documentation)
```

**Implementation Notes:**
- Check cache first (by commit hash)
- Use Git LFS for large files
- Support revision/branch selection
- Handle rate limiting (429 responses)
- Resume partial downloads

**Code Skeleton:**
```csharp
public class HuggingFaceProvider : IModelProvider
{
    private const string HF_BASE_URL = "https://huggingface.co";
    private const string HF_API_URL = "https://huggingface.co/api";
    private readonly HttpClient _httpClient;
    private readonly string _apiToken;

    public string Name => "HuggingFace";
    public int Priority => 50;

    public bool CanHandle(string modelIdentifier)
    {
        return modelIdentifier.Contains("/") && 
               !modelIdentifier.Contains("://") ||
               modelIdentifier.StartsWith("hf://") ||
               modelIdentifier.StartsWith("https://huggingface.co");
    }

    public ModelRetrievalResult Retrieve(string modelIdentifier, RetrievalOptions options)
    {
        // Parse identifier
        var (owner, repo, revision) = ParseIdentifier(modelIdentifier, options.Revision);
        
        // Get file listing from API
        var files = GetFileList(owner, repo, revision);
        
        // Filter by patterns
        files = ApplyPatternFilters(files, options);
        
        // Download each file (or use cache)
        var catalogFiles = new Dictionary<string, Stream>();
        foreach (var file in files)
        {
            var stream = DownloadFile(owner, repo, revision, file.Path, options);
            catalogFiles[file.Path] = stream;
        }
        
        return new ModelRetrievalResult
        {
            IsCatalog = true,
            CatalogFiles = catalogFiles,
            Metadata = GetMetadataDict(owner, repo, revision),
            CallerOwnsStreams = true,
            ProviderName = Name
        };
    }

    public ModelProviderMetadata GetMetadata(string modelIdentifier)
    {
        var (owner, repo, _) = ParseIdentifier(modelIdentifier);
        
        // Call HuggingFace API: /api/models/{owner}/{repo}
        var response = _httpClient.GetAsync($"{HF_API_URL}/models/{owner}/{repo}").Result;
        var json = JsonDocument.Parse(response.Content.ReadAsStringAsync().Result);
        
        return new ModelProviderMetadata
        {
            ModelId = $"{owner}/{repo}",
            Architecture = json.RootElement.GetProperty("config")
                              .GetProperty("architectures")[0].GetString(),
            Tags = ParseTags(json.RootElement.GetProperty("tags")),
            Author = owner,
            LastModified = DateTime.Parse(json.RootElement.GetProperty("lastModified").GetString()),
            // ... more fields
        };
    }
}
```

---

### 2. OllamaProvider

**Purpose:** Retrieve models from Ollama (https://ollama.com)

**Identifier Formats:**
- `ollama://model:tag` (e.g., `ollama://llama2:7b`)
- `model:tag` (if Ollama is default provider)

**API Integration:**
- Ollama API: http://localhost:11434 (default)
- Endpoints:
  - `GET /api/tags` - List local models
  - `POST /api/pull` - Pull model from registry
  - `GET /api/show` - Show model metadata

**Model Storage:**
- Linux/Mac: `~/.ollama/models/`
- Windows: `%USERPROFILE%\.ollama\models\`

**Model Structure:**
```
~/.ollama/models/manifests/registry.ollama.ai/library/llama2/
├── 7b                               (tag manifest)
└── latest                           (alias to 7b)

~/.ollama/models/blobs/
├── sha256-abc123...                 (GGUF model file)
├── sha256-def456...                 (Modelfile)
└── sha256-789ghi...                 (system prompt)
```

**Modelfile Format:**
```
FROM /path/to/model.gguf
PARAMETER temperature 0.7
PARAMETER top_p 0.9
SYSTEM """You are a helpful assistant."""
TEMPLATE """{{ .System }}{{ .Prompt }}"""
```

**Implementation Notes:**
- Check if model exists locally first
- Use API to pull if not local
- Parse Modelfile for configuration
- GGUF format for model weights
- Support custom models (Modelfile-based)

**Code Skeleton:**
```csharp
public class OllamaProvider : IModelProvider
{
    private const string DEFAULT_OLLAMA_HOST = "http://localhost:11434";
    private readonly HttpClient _httpClient;
    private readonly string _ollamaHost;
    private readonly string _modelsPath;

    public string Name => "Ollama";
    public int Priority => 60;

    public bool CanHandle(string modelIdentifier)
    {
        return modelIdentifier.StartsWith("ollama://") ||
               (modelIdentifier.Contains(":") && 
                !modelIdentifier.Contains("/") && 
                !modelIdentifier.Contains("://"));
    }

    public ModelRetrievalResult Retrieve(string modelIdentifier, RetrievalOptions options)
    {
        var (modelName, tag) = ParseIdentifier(modelIdentifier);
        
        // Check if model exists locally
        if (!IsModelLocal(modelName, tag) && !options.ForceRefresh)
        {
            // Pull model using Ollama API
            PullModel(modelName, tag, options.ProgressCallback);
        }
        
        // Get model file path from Ollama storage
        var modelPath = GetModelPath(modelName, tag);
        var modelfileContent = GetModelfile(modelName, tag);
        
        // Ollama models are typically single GGUF files
        // Return as catalog to include Modelfile
        var catalogFiles = new Dictionary<string, Stream>
        {
            ["model.gguf"] = File.OpenRead(modelPath),
            ["Modelfile"] = new MemoryStream(Encoding.UTF8.GetBytes(modelfileContent))
        };
        
        return new ModelRetrievalResult
        {
            IsCatalog = true,
            CatalogFiles = catalogFiles,
            DetectedFormat = ModelFormat.GGUF,
            Metadata = new Dictionary<string, string>
            {
                ["model_name"] = modelName,
                ["tag"] = tag,
                ["modelfile"] = modelfileContent
            },
            CallerOwnsStreams = true,
            ProviderName = Name
        };
    }

    private void PullModel(string modelName, string tag, Action<long, long> progressCallback)
    {
        var request = new
        {
            name = $"{modelName}:{tag}",
            stream = true
        };
        
        var response = _httpClient.PostAsJsonAsync($"{_ollamaHost}/api/pull", request).Result;
        
        // Stream progress updates
        using (var stream = response.Content.ReadAsStreamAsync().Result)
        using (var reader = new StreamReader(stream))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var json = JsonDocument.Parse(line);
                if (json.RootElement.TryGetProperty("completed", out var completed) &&
                    json.RootElement.TryGetProperty("total", out var total))
                {
                    progressCallback?.Invoke(completed.GetInt64(), total.GetInt64());
                }
            }
        }
    }
}
```

---

### 3. OpenAIProvider

**Purpose:** Provide metadata for OpenAI models (no downloadable weights)

**Identifier Formats:**
- `openai://gpt-4`
- `gpt-4-turbo` (implicit)

**API Integration:**
- OpenAI API: https://api.openai.com/v1
- Models endpoint: `/v1/models`

**Important:** OpenAI doesn't provide downloadable model weights. This provider returns metadata for SQL Server 2025 CREATE EXTERNAL MODEL.

**Implementation Notes:**
- Return metadata only (no actual model files)
- Generate REST endpoint configuration
- API key from Azure Key Vault
- Model capabilities: context window, token limits

**Code Skeleton:**
```csharp
public class OpenAIProvider : IModelProvider
{
    public string Name => "OpenAI";
    public int Priority => 40;

    public ModelRetrievalResult Retrieve(string modelIdentifier, RetrievalOptions options)
    {
        var modelName = ParseModelName(modelIdentifier);
        
        // OpenAI doesn't provide model files - return REST endpoint config
        var metadata = new Dictionary<string, string>
        {
            ["endpoint_type"] = "rest_api",
            ["base_url"] = "https://api.openai.com/v1",
            ["model_name"] = modelName,
            ["endpoint_path"] = "/chat/completions",
            ["auth_type"] = "bearer_token",
            ["api_key_vault_secret"] = "OpenAI-API-Key"
        };
        
        return new ModelRetrievalResult
        {
            IsCatalog = false,
            SingleFileStream = null, // No file to download
            Metadata = metadata,
            ProviderName = Name
        };
    }

    public ModelProviderMetadata GetMetadata(string modelIdentifier)
    {
        var modelName = ParseModelName(modelIdentifier);
        
        // Query OpenAI models API
        var response = _httpClient.GetAsync("https://api.openai.com/v1/models").Result;
        var models = JsonSerializer.Deserialize<OpenAIModelsResponse>(
            response.Content.ReadAsStringAsync().Result);
        
        var model = models.Data.FirstOrDefault(m => m.Id == modelName);
        
        return new ModelProviderMetadata
        {
            ModelId = modelName,
            Tags = new Dictionary<string, string>
            {
                ["context_window"] = GetContextWindow(modelName).ToString(),
                ["owned_by"] = model?.OwnedBy ?? "openai"
            }
        };
    }
}
```

---

### 4. AzureOpenAIProvider

**Purpose:** Provide metadata for Azure OpenAI Service models

**Identifier Formats:**
- `azure://my-resource/gpt-4`
- `azureopenai://my-resource/deployment-name`

**API Integration:**
- Azure OpenAI endpoint: `https://{resource}.openai.azure.com/`
- Use Managed Identity for authentication (no API keys)

**Implementation Notes:**
- Return REST endpoint configuration for CREATE EXTERNAL MODEL
- Use Azure Managed Identity
- Deployment-based (not direct model names)

**Code Skeleton:**
```csharp
public class AzureOpenAIProvider : IModelProvider
{
    public string Name => "AzureOpenAI";
    public int Priority => 45;

    public ModelRetrievalResult Retrieve(string modelIdentifier, RetrievalOptions options)
    {
        var (resourceName, deploymentName) = ParseIdentifier(modelIdentifier);
        
        var metadata = new Dictionary<string, string>
        {
            ["endpoint_type"] = "azure_openai",
            ["resource_name"] = resourceName,
            ["deployment_name"] = deploymentName,
            ["base_url"] = $"https://{resourceName}.openai.azure.com/",
            ["api_version"] = "2024-02-15-preview",
            ["auth_type"] = "managed_identity"
        };
        
        return new ModelRetrievalResult
        {
            IsCatalog = false,
            Metadata = metadata,
            ProviderName = Name
        };
    }
}
```

---

### 5. LocalFileSystemProvider

**Purpose:** Retrieve models from local filesystem

**Identifier Formats:**
- `file:///path/to/model.gguf`
- `/path/to/model` (absolute path)
- `C:\path\to\model` (Windows)

**Implementation Notes:**
- Support both single files and directories
- If directory: enumerate files, return as catalog
- Detect format from file extension + magic numbers
- No caching needed (already local)

**Code Skeleton:**
```csharp
public class LocalFileSystemProvider : IModelProvider
{
    public string Name => "LocalFileSystem";
    public int Priority => 100; // Highest priority

    public bool CanHandle(string modelIdentifier)
    {
        return modelIdentifier.StartsWith("file://") ||
               Path.IsPathRooted(modelIdentifier);
    }

    public ModelRetrievalResult Retrieve(string modelIdentifier, RetrievalOptions options)
    {
        var path = modelIdentifier.Replace("file://", "");
        
        if (File.Exists(path))
        {
            // Single file
            return new ModelRetrievalResult
            {
                IsCatalog = false,
                SingleFileStream = File.OpenRead(path),
                SingleFilePath = path,
                CallerOwnsStreams = true,
                ProviderName = Name
            };
        }
        else if (Directory.Exists(path))
        {
            // Directory catalog
            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            var catalogFiles = new Dictionary<string, Stream>();
            
            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(path, file);
                catalogFiles[relativePath] = File.OpenRead(file);
            }
            
            return new ModelRetrievalResult
            {
                IsCatalog = true,
                CatalogFiles = catalogFiles,
                CallerOwnsStreams = true,
                ProviderName = Name
            };
        }
        else
        {
            throw new FileNotFoundException($"Path not found: {path}");
        }
    }
}
```

---

## ModelProviderRegistry

**Purpose:** Central registry for routing model identifiers to providers

```csharp
namespace Hartonomous.Clr.Providers
{
    /// <summary>
    /// Central registry for all model providers.
    /// Routes model identifiers to appropriate provider based on priority and CanHandle().
    /// </summary>
    public class ModelProviderRegistry
    {
        private readonly List<IModelProvider> _providers;

        public ModelProviderRegistry()
        {
            // Register providers in priority order (highest first)
            _providers = new List<IModelProvider>
            {
                new LocalFileSystemProvider(),     // Priority: 100
                new OllamaProvider(),              // Priority: 60
                new HuggingFaceProvider(),         // Priority: 50
                new AzureOpenAIProvider(),         // Priority: 45
                new OpenAIProvider()               // Priority: 40
            };
            
            // Sort by priority (highest first)
            _providers = _providers.OrderByDescending(p => p.Priority).ToList();
        }

        /// <summary>
        /// Get provider for given model identifier.
        /// Tries providers in priority order.
        /// </summary>
        public IModelProvider GetProvider(string modelIdentifier)
        {
            foreach (var provider in _providers)
            {
                if (provider.CanHandle(modelIdentifier))
                {
                    return provider;
                }
            }
            
            throw new NotSupportedException(
                $"No provider found for model identifier: {modelIdentifier}\n" +
                $"Registered providers: {string.Join(", ", _providers.Select(p => p.Name))}");
        }

        /// <summary>
        /// Register custom provider.
        /// Custom providers are inserted at the beginning (highest priority).
        /// </summary>
        public void RegisterProvider(IModelProvider provider)
        {
            _providers.Insert(0, provider);
            _providers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        /// <summary>
        /// Get all registered providers.
        /// </summary>
        public IReadOnlyList<IModelProvider> GetAllProviders()
        {
            return _providers.AsReadOnly();
        }

        /// <summary>
        /// Remove provider by name.
        /// </summary>
        public bool RemoveProvider(string providerName)
        {
            var provider = _providers.FirstOrDefault(p => p.Name == providerName);
            if (provider != null)
            {
                _providers.Remove(provider);
                return true;
            }
            return false;
        }
    }
}
```

---

## Usage Examples

### Example 1: Retrieve from HuggingFace

```csharp
var registry = new ModelProviderRegistry();
var provider = registry.GetProvider("mistralai/Mistral-7B-v0.1");

var options = new RetrievalOptions
{
    CacheDirectory = "C:\\models\\cache",
    MetadataOnly = false,
    IncludePatterns = new[] { "*.safetensors", "config.json", "tokenizer*.json" },
    ExcludePatterns = new[] { "*.md", "*.txt" }
};

using (var result = provider.Retrieve("mistralai/Mistral-7B-v0.1", options))
{
    Console.WriteLine($"Retrieved from: {result.ProviderName}");
    Console.WriteLine($"Is catalog: {result.IsCatalog}");
    Console.WriteLine($"Files: {result.CatalogFiles.Count}");
    
    foreach (var file in result.CatalogFiles)
    {
        Console.WriteLine($"  - {file.Key}");
        // Pass stream to format parser...
    }
}
```

### Example 2: Retrieve from Ollama

```csharp
var provider = registry.GetProvider("ollama://llama2:7b");

var options = new RetrievalOptions
{
    ProgressCallback = (received, total) =>
    {
        Console.WriteLine($"Progress: {received}/{total} bytes ({received * 100 / total}%)");
    }
};

using (var result = provider.Retrieve("ollama://llama2:7b", options))
{
    // result.CatalogFiles contains:
    //   - "model.gguf" (GGUF format)
    //   - "Modelfile" (configuration)
    
    var ggufStream = result.CatalogFiles["model.gguf"];
    var parser = new GGUFParser();
    var metadata = parser.ReadMetadata(ggufStream);
}
```

### Example 3: Local File

```csharp
var provider = registry.GetProvider("file:///C:/models/my-model.gguf");

using (var result = provider.Retrieve("file:///C:/models/my-model.gguf", new RetrievalOptions()))
{
    // result.SingleFileStream contains the file stream
    var parser = new GGUFParser();
    var tensors = parser.ReadWeights(result.SingleFileStream);
}
```

### Example 4: Azure OpenAI (Metadata Only)

```csharp
var provider = registry.GetProvider("azure://my-resource/gpt-4");

using (var result = provider.Retrieve("azure://my-resource/gpt-4", new RetrievalOptions()))
{
    // No file streams - only metadata for CREATE EXTERNAL MODEL
    var endpointUrl = result.Metadata["base_url"];
    var deploymentName = result.Metadata["deployment_name"];
    
    // Use in SQL Server 2025:
    // CREATE EXTERNAL MODEL MyGPT4
    // WITH (ENDPOINT = '<endpointUrl>', DEPLOYMENT = '<deploymentName>', ...)
}
```

---

## Error Handling

### Common Errors

1. **Provider Not Found**
   - Exception: `NotSupportedException`
   - Cause: No provider can handle the identifier
   - Solution: Check identifier format, register custom provider

2. **Authentication Failed**
   - Exception: `UnauthorizedAccessException`
   - Cause: Invalid API token, missing credentials
   - Solution: Check Key Vault, environment variables

3. **Model Not Found**
   - Exception: `FileNotFoundException` or `HttpRequestException` (404)
   - Cause: Model doesn't exist in provider
   - Solution: Verify model ID, check spelling

4. **Network Timeout**
   - Exception: `TimeoutException`
   - Cause: Download taking too long
   - Solution: Increase `RetrievalOptions.Timeout`, check network

5. **Rate Limit Exceeded**
   - Exception: `HttpRequestException` (429)
   - Cause: Too many requests to provider API
   - Solution: Implement retry with backoff, cache results

### Error Handling Pattern

```csharp
try
{
    var result = provider.Retrieve(modelId, options);
    // Use result...
}
catch (NotSupportedException ex)
{
    Console.WriteLine($"No provider for: {modelId}");
}
catch (UnauthorizedAccessException ex)
{
    Console.WriteLine($"Auth failed: {ex.Message}");
    // Check Key Vault, API tokens
}
catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
{
    Console.WriteLine("Rate limit exceeded - waiting...");
    Thread.Sleep(TimeSpan.FromSeconds(60));
    // Retry...
}
catch (TimeoutException ex)
{
    Console.WriteLine($"Timeout: {ex.Message}");
    // Retry with longer timeout
}
```

---

## Configuration

### appsettings.json

```json
{
  "ModelProviders": {
    "HuggingFace": {
      "ApiToken": "KeyVault:HuggingFace-API-Token",
      "CacheDirectory": "C:\\models\\cache\\huggingface",
      "DefaultRevision": "main"
    },
    "Ollama": {
      "Host": "http://localhost:11434",
      "ModelsPath": "%USERPROFILE%\\.ollama\\models"
    },
    "OpenAI": {
      "ApiKey": "KeyVault:OpenAI-API-Key"
    },
    "AzureOpenAI": {
      "UseManagedIdentity": true,
      "DefaultApiVersion": "2024-02-15-preview"
    },
    "LocalFileSystem": {
      "DefaultModelsPath": "C:\\models"
    }
  }
}
```

---

## Testing Strategy

### Unit Tests

1. **Identifier Parsing**
   - Test each provider's `CanHandle()` with various formats
   - Verify correct provider selected

2. **Metadata Retrieval**
   - Mock API responses
   - Verify metadata parsing

3. **Error Handling**
   - Test all error scenarios
   - Verify appropriate exceptions thrown

### Integration Tests

1. **HuggingFace Public Models**
   - Download small public model
   - Verify catalog structure

2. **Ollama Local Models**
   - Test with Ollama running locally
   - Verify GGUF + Modelfile retrieval

3. **Local Filesystem**
   - Test single file and directory modes
   - Verify stream correctness

### Performance Tests

1. **Large Model Download**
   - Test with multi-GB model
   - Measure download speed, memory usage

2. **Caching Effectiveness**
   - Verify cache hits on repeated retrieval
   - Measure speedup

3. **Concurrent Retrievals**
   - Test multiple simultaneous downloads
   - Verify thread safety

---

## Future Enhancements

1. **More Providers**
   - GitHub LFS repositories
   - AWS S3 / Azure Blob Storage
   - Custom HTTP endpoints
   - IPFS / decentralized storage

2. **Advanced Caching**
   - Content-addressable storage (hash-based)
   - Shared cache across multiple systems
   - Cache eviction policies (LRU, size-based)

3. **Download Resumption**
   - Resume partial downloads after network failure
   - Range request support

4. **Parallel Downloads**
   - Download multiple files simultaneously
   - Chunk-based parallel download for large files

5. **Compression**
   - Download compressed versions where available
   - Decompress on-the-fly

---

**End of Model Provider Layer Specification**
