# Universal File Format System - Comprehensive Design Document

**Date:** November 18, 2025  
**Status:** Design Phase - Awaiting Approval  
**Purpose:** Replace incomplete model parser implementations with enterprise-grade universal file handling system

---

## Executive Summary

This document defines a comprehensive architecture for handling **all file formats** in the Hartonomous system, not just AI models. The design separates concerns between:

1. **Model Providers** (HuggingFace, Ollama, OpenAI, etc.) - Retrieval/download layer
2. **Format Parsers** (GGUF, ONNX, PyTorch, PDF, images, etc.) - Format-specific parsing after retrieval
3. **Archive Handlers** (ZIP, TAR, 7Z) - Compressed file extraction
4. **Catalog Managers** - Multi-file coordination (HuggingFace repos, model families)
5. **SQL Server 2025 Integration** - Native vector/json types, REST endpoints

### Key Architectural Principles

- **Separation of Concerns**: Model providers ≠ Format parsers
- **No Cop-Outs**: Complete implementations, no "recommend conversion"
- **Streaming First**: Memory-efficient processing for large files
- **SQL Server 2025 Native**: Use `vector`/`json` types, not CLR wrappers
- **Universal Coverage**: Documents, images, video, audio, telemetry, archives, AI models

---

## 1. Model Provider Layer (NEW)

**Purpose:** Retrieve/download models from various sources - **DOES NOT PARSE FORMATS**

### 1.1 Core Abstraction

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
        /// Check if this provider can handle the given model identifier.
        /// Examples:
        ///   HuggingFace: "mistralai/Mistral-7B-v0.1"
        ///   Ollama: "ollama://llama2:7b"
        ///   Local: "file:///models/my-model.gguf"
        ///   Azure: "azure://my-endpoint/model"
        /// </summary>
        bool CanHandle(string modelIdentifier);

        /// <summary>
        /// Retrieve model files from the provider.
        /// Returns catalog of files (for multi-file models) or single file stream.
        /// </summary>
        ModelRetrievalResult Retrieve(string modelIdentifier, RetrievalOptions options);

        /// <summary>
        /// Get metadata about model without downloading (size, format, etc.)
        /// </summary>
        ModelProviderMetadata GetMetadata(string modelIdentifier);
    }

    public class ModelRetrievalResult
    {
        /// <summary>
        /// Is this a single file or multi-file catalog?
        /// </summary>
        public bool IsCatalog { get; set; }

        /// <summary>
        /// Single file stream (for simple models)
        /// </summary>
        public Stream SingleFileStream { get; set; }

        /// <summary>
        /// File path for single local file
        /// </summary>
        public string SingleFilePath { get; set; }

        /// <summary>
        /// Multiple files (for catalogs like HuggingFace repos)
        /// </summary>
        public Dictionary<string, Stream> CatalogFiles { get; set; }

        /// <summary>
        /// Detected format (if known from provider metadata)
        /// </summary>
        public ModelFormat? DetectedFormat { get; set; }

        /// <summary>
        /// Provider-specific metadata
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; }

        /// <summary>
        /// Should streams be disposed by caller?
        /// </summary>
        public bool CallerOwnsStreams { get; set; }
    }

    public class RetrievalOptions
    {
        /// <summary>
        /// Cache directory for downloaded models
        /// </summary>
        public string CacheDirectory { get; set; }

        /// <summary>
        /// Force re-download even if cached
        /// </summary>
        public bool ForceRefresh { get; set; }

        /// <summary>
        /// Download only metadata files (config.json, tokenizer_config.json)
        /// </summary>
        public bool MetadataOnly { get; set; }

        /// <summary>
        /// Timeout for retrieval operations
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(30);
    }

    public class ModelProviderMetadata
    {
        public string ModelId { get; set; }
        public long? SizeBytes { get; set; }
        public string[] FileNames { get; set; }
        public ModelFormat? DetectedFormat { get; set; }
        public string Architecture { get; set; }
        public Dictionary<string, string> Tags { get; set; }
    }
}
```

### 1.2 Provider Implementations

#### 1.2.1 HuggingFaceProvider

```csharp
/// <summary>
/// Retrieves models from HuggingFace Hub (https://huggingface.co)
/// Handles multi-file repositories with config.json, tokenizer, weights, README, etc.
/// </summary>
public class HuggingFaceProvider : IModelProvider
{
    public string Name => "HuggingFace";

    public bool CanHandle(string modelIdentifier)
    {
        // Format: "owner/repo" or "hf://owner/repo"
        return modelIdentifier.Contains("/") && !modelIdentifier.Contains("://")
               || modelIdentifier.StartsWith("hf://");
    }

    public ModelRetrievalResult Retrieve(string modelIdentifier, RetrievalOptions options)
    {
        // Use HuggingFace API or Git LFS to download
        // Returns catalog with:
        //   - model.safetensors or pytorch_model.bin
        //   - config.json
        //   - tokenizer_config.json
        //   - tokenizer.json
        //   - vocab.txt / special_tokens_map.json
        //   - README.md
        //   - model_index.json (for Stable Diffusion)
    }
}
```

#### 1.2.2 OllamaProvider

```csharp
/// <summary>
/// Retrieves models from Ollama (https://ollama.com)
/// Ollama serves models in GGUF format via local API.
/// </summary>
public class OllamaProvider : IModelProvider
{
    public string Name => "Ollama";

    public bool CanHandle(string modelIdentifier)
    {
        // Format: "ollama://model:tag" or just "model:tag" if Ollama is default
        return modelIdentifier.StartsWith("ollama://") 
               || (modelIdentifier.Contains(":") && !modelIdentifier.Contains("/"));
    }

    public ModelRetrievalResult Retrieve(string modelIdentifier, RetrievalOptions options)
    {
        // Connect to Ollama API (default: http://localhost:11434)
        // Use /api/pull endpoint to download model
        // Models are stored in Ollama's cache (~/.ollama/models)
        // Return stream to GGUF file
        //
        // Ollama model structure:
        //   - Single GGUF file (quantized)
        //   - Modelfile (configuration)
        //   - Optional system prompt
    }
}
```

#### 1.2.3 OpenAIProvider

```csharp
/// <summary>
/// Retrieves models from OpenAI API (GPT-4, GPT-3.5, etc.)
/// Note: OpenAI doesn't provide downloadable weights, only API access.
/// This provider returns metadata for CREATE EXTERNAL MODEL in SQL Server 2025.
/// </summary>
public class OpenAIProvider : IModelProvider
{
    public string Name => "OpenAI";

    public bool CanHandle(string modelIdentifier)
    {
        // Format: "openai://gpt-4" or "gpt-4-turbo"
        return modelIdentifier.StartsWith("openai://") 
               || modelIdentifier.StartsWith("gpt-");
    }

    public ModelRetrievalResult Retrieve(string modelIdentifier, RetrievalOptions options)
    {
        // OpenAI doesn't provide model files
        // Return metadata for SQL Server 2025 CREATE EXTERNAL MODEL:
        //   - Endpoint URL: https://api.openai.com/v1/chat/completions
        //   - Model name: gpt-4-turbo, gpt-3.5-turbo, etc.
        //   - API key from Azure Key Vault
        //   - Context window, token limits
    }
}
```

#### 1.2.4 AzureOpenAIProvider

```csharp
/// <summary>
/// Retrieves models from Azure OpenAI Service
/// Similar to OpenAI but uses Azure endpoints and Managed Identity.
/// </summary>
public class AzureOpenAIProvider : IModelProvider
{
    public string Name => "AzureOpenAI";

    public bool CanHandle(string modelIdentifier)
    {
        // Format: "azure://my-resource/gpt-4"
        return modelIdentifier.StartsWith("azure://");
    }

    public ModelRetrievalResult Retrieve(string modelIdentifier, RetrievalOptions options)
    {
        // Use Azure OpenAI endpoint with Managed Identity
        // Return metadata for CREATE EXTERNAL MODEL:
        //   - Endpoint: https://{resource}.openai.azure.com/
        //   - Deployment name
        //   - API version
        //   - Managed Identity for auth (no API keys!)
    }
}
```

#### 1.2.5 LocalFileSystemProvider

```csharp
/// <summary>
/// Retrieves models from local filesystem.
/// Handles single files or directory-based catalogs.
/// </summary>
public class LocalFileSystemProvider : IModelProvider
{
    public string Name => "LocalFileSystem";

    public bool CanHandle(string modelIdentifier)
    {
        // Format: "file:///path/to/model" or "/path/to/model" or "C:\path\to\model"
        return modelIdentifier.StartsWith("file://") 
               || Path.IsPathRooted(modelIdentifier);
    }

    public ModelRetrievalResult Retrieve(string modelIdentifier, RetrievalOptions options)
    {
        // Check if path is file or directory
        // If file: return single stream
        // If directory: enumerate files, return catalog
        // Detect format from file extension/magic numbers
    }
}
```

### 1.3 Provider Registry

```csharp
/// <summary>
/// Central registry for all model providers.
/// Routes model identifiers to appropriate provider.
/// </summary>
public class ModelProviderRegistry
{
    private readonly List<IModelProvider> _providers;

    public ModelProviderRegistry()
    {
        _providers = new List<IModelProvider>
        {
            new HuggingFaceProvider(),
            new OllamaProvider(),
            new OpenAIProvider(),
            new AzureOpenAIProvider(),
            new LocalFileSystemProvider()
        };
    }

    public IModelProvider GetProvider(string modelIdentifier)
    {
        foreach (var provider in _providers)
        {
            if (provider.CanHandle(modelIdentifier))
                return provider;
        }
        throw new NotSupportedException($"No provider found for: {modelIdentifier}");
    }

    public void RegisterProvider(IModelProvider provider)
    {
        _providers.Insert(0, provider); // Custom providers take precedence
    }
}
```

---

## 2. Format Parser Layer (REWRITE)

**Purpose:** Parse file formats **after** retrieval from provider

### 2.1 Separation from Providers

**CRITICAL:** Format parsers do NOT care where the file came from. They receive a `Stream` or `byte[]` and parse the format.

```
ModelProvider.Retrieve() → Stream → FormatParser.Parse() → TensorInfo[]
                           ↓
                     (any source)
                           ↓
              HuggingFace, Ollama, Local, etc.
```

### 2.2 Existing Interfaces (Keep, But Enhance)

```csharp
namespace Hartonomous.Clr.Contracts
{
    /// <summary>
    /// Parses a specific model file format.
    /// DOES NOT handle retrieval - only parsing of format after retrieval.
    /// </summary>
    public interface IModelFormatReader
    {
        ModelFormat Format { get; }
        
        /// <summary>
        /// Check if this reader can parse the given stream.
        /// Uses magic numbers, not file extensions.
        /// </summary>
        bool CanParse(Stream stream);
        
        ModelMetadata ReadMetadata(Stream stream);
        Dictionary<string, TensorInfo> ReadWeights(Stream stream);
    }
}
```

### 2.3 Complete Parser Implementations (REWRITE ALL)

#### 2.3.1 PyTorchParser (COMPLETE REWRITE)

**Current Issues:**
- Claims ZipArchive not available (FALSE)
- Throws NotSupportedException (LAZY)
- No actual ZIP parsing

**New Implementation:**

```csharp
/// <summary>
/// PyTorch format parser with FULL ZIP support.
/// Handles both legacy pickle format (with security warnings) and modern ZIP format.
/// </summary>
public class PyTorchParser : IModelFormatReader
{
    public ModelFormat Format => ModelFormat.PyTorch;

    public bool CanParse(Stream stream)
    {
        byte[] header = new byte[4];
        stream.Read(header, 0, 4);
        stream.Position = 0;

        // ZIP format: 0x50 0x4B 0x03 0x04 (PK..)
        bool isZip = header[0] == 0x50 && header[1] == 0x4B && 
                     header[2] == 0x03 && header[3] == 0x04;

        // Pickle format: 0x80 (PROTO opcode)
        bool isPickle = header[0] == 0x80;

        return isZip || isPickle;
    }

    public Dictionary<string, TensorInfo> ReadWeights(Stream stream)
    {
        // Detect format type
        byte[] header = new byte[4];
        stream.Read(header, 0, 4);
        stream.Position = 0;

        bool isZip = header[0] == 0x50 && header[1] == 0x4B;

        if (isZip)
        {
            return ReadZipFormat(stream);
        }
        else
        {
            // Pickle format - SECURITY WARNING
            // Don't throw NotSupportedException - log warning and attempt parse
            return ReadPickleFormat(stream);
        }
    }

    private Dictionary<string, TensorInfo> ReadZipFormat(Stream stream)
    {
        // FULL ZIP IMPLEMENTATION using System.IO.Compression.ZipArchive
        // Requires EXTERNAL_ACCESS assembly permission
        
        var tensors = new Dictionary<string, TensorInfo>();

        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true))
        {
            // PyTorch ZIP structure:
            //   data.pkl - metadata (pickle format)
            //   data/0.storage - tensor data (file 0)
            //   data/1.storage - tensor data (file 1)
            //   ... more storage files
            //   version - version number

            // Read version
            var versionEntry = archive.GetEntry("version");
            if (versionEntry != null)
            {
                using (var versionStream = versionEntry.Open())
                using (var reader = new StreamReader(versionStream))
                {
                    string version = reader.ReadToEnd().Trim();
                    // Parse version for compatibility checks
                }
            }

            // Read metadata from data.pkl
            var metadataEntry = archive.GetEntry("data.pkl");
            if (metadataEntry == null)
                throw new InvalidDataException("PyTorch ZIP missing data.pkl");

            Dictionary<string, TensorMetadata> metadata;
            using (var metadataStream = metadataEntry.Open())
            {
                // Parse pickle format for tensor metadata
                // Extract tensor names, shapes, dtypes, storage offsets
                metadata = ParsePickleMetadata(metadataStream);
            }

            // Read tensor data from data/*.storage files
            foreach (var entry in archive.Entries)
            {
                if (entry.FullName.StartsWith("data/") && entry.FullName.EndsWith(".storage"))
                {
                    using (var storageStream = entry.Open())
                    {
                        // Read tensor data, match with metadata
                        // Create TensorInfo objects
                        var storageId = Path.GetFileNameWithoutExtension(entry.Name);
                        // Map storage to tensors using metadata
                    }
                }
            }
        }

        return tensors;
    }

    private Dictionary<string, TensorInfo> ReadPickleFormat(Stream stream)
    {
        // SECURITY WARNING: Pickle format can execute arbitrary code
        // Log warning to SQL Server error log
        // Attempt to parse pickle protocol safely (read-only operations)
        
        // Parse pickle opcodes:
        //   PROTO, GLOBAL, REDUCE, BUILD, SETITEMS, etc.
        // Extract tensor metadata without executing code
        
        // Return dictionary with security warning in metadata
    }
}
```

#### 2.3.2 ONNXParser (COMPLETE PROTOBUF)

**Current Issues:**
- Simplified protobuf parsing
- Doesn't handle full ONNX schema

**New Implementation:**

```csharp
/// <summary>
/// ONNX format parser with COMPLETE protobuf support.
/// Uses protobuf-net for full ModelProto/GraphProto/TensorProto parsing.
/// </summary>
public class ONNXParser : IModelFormatReader
{
    // Option 1: Use protobuf-net library (recommended)
    // Add NuGet: protobuf-net (works with .NET Framework 4.8.1)
    // Requires EXTERNAL_ACCESS for protobuf-net.dll
    
    // Option 2: Hand-code complete protobuf parser
    // More work but avoids external dependency
    
    public Dictionary<string, TensorInfo> ReadWeights(Stream stream)
    {
        // Use protobuf-net to deserialize ModelProto
        var model = Serializer.Deserialize<Onnx.ModelProto>(stream);
        
        // model.Graph contains GraphProto
        // model.Graph.Node[] contains NodeProto (operations)
        // model.Graph.Initializer[] contains TensorProto (weights)
        // model.Graph.Input[] / Output[] contains ValueInfoProto
        
        var tensors = new Dictionary<string, TensorInfo>();
        
        foreach (var tensorProto in model.Graph.Initializer)
        {
            var tensorInfo = new TensorInfo
            {
                Name = tensorProto.Name,
                Dtype = MapOnnxDataType(tensorProto.DataType),
                Shape = tensorProto.Dims.ToArray(),
                DataSize = CalculateDataSize(tensorProto),
                // Extract actual tensor data from raw_data or typed fields
            };
            
            tensors[tensorInfo.Name] = tensorInfo;
        }
        
        return tensors;
    }
    
    // Full ONNX DataType enum mapping (not simplified)
    private TensorDtype MapOnnxDataType(Onnx.TensorProto.Types.DataType onnxType)
    {
        // Complete mapping for ALL ONNX types:
        // FLOAT, UINT8, INT8, UINT16, INT16, INT32, INT64, STRING,
        // BOOL, FLOAT16, DOUBLE, UINT32, UINT64, COMPLEX64, COMPLEX128,
        // BFLOAT16, FLOAT8E4M3FN, FLOAT8E4M3FNUZ, FLOAT8E5M2, etc.
    }
}
```

#### 2.3.3 TensorFlowParser (COMPLETE SavedModel)

**Current Issues:**
- Simplified protobuf
- Skips many node types

**New Implementation:**

```csharp
/// <summary>
/// TensorFlow SavedModel parser with complete GraphDef support.
/// Handles all node types, functions, control flow.
/// </summary>
public class TensorFlowParser : IModelFormatReader
{
    public Dictionary<string, TensorInfo> ReadWeights(Stream stream)
    {
        // TensorFlow SavedModel structure:
        //   saved_model.pb - SavedModel protobuf
        //   variables/ - Variable data files
        //     variables.index
        //     variables.data-00000-of-00001
        //   assets/ - Additional assets
        
        // Parse complete SavedModel protobuf:
        //   meta_graphs[] - Multiple computation graphs
        //   graph_def - GraphDef with nodes
        //   saver_def - Checkpoint saver config
        
        // Support ALL node types:
        //   Variable, VariableV2, VarHandleOp (variables)
        //   Const (constants)
        //   Placeholder (inputs)
        //   MatMul, Conv2D, etc. (operations)
        //   If, While, Case (control flow)
        //   PartitionedCall, StatefulPartitionedCall (functions)
    }
}
```

#### 2.3.4 StableDiffusionParser (CATALOG COORDINATOR)

**Current Issues:**
- Tries to use SafeTensorsParser as static class
- Falls back to PyTorchParser which throws NotSupportedException

**New Implementation:**

```csharp
/// <summary>
/// Stable Diffusion parser that coordinates multi-file catalog parsing.
/// SD models consist of multiple components (UNet, VAE, TextEncoder).
/// </summary>
public class StableDiffusionParser : IModelFormatReader
{
    private readonly SafeTensorsParser _safeTensorsParser = new SafeTensorsParser();
    private readonly PyTorchParser _pytorchParser = new PyTorchParser();

    public Dictionary<string, TensorInfo> ReadWeights(Stream stream)
    {
        // This is a CATALOG parser - expects directory structure, not single stream
        // Should be called from CatalogManager with file map
        
        throw new InvalidOperationException(
            "StableDiffusionParser requires catalog mode. " +
            "Use CatalogManager.ParseStableDiffusion() instead.");
    }
    
    public Dictionary<string, TensorInfo> ParseCatalog(Dictionary<string, Stream> files)
    {
        // Stable Diffusion structure:
        //   unet/diffusion_pytorch_model.safetensors (or .bin)
        //   vae/diffusion_pytorch_model.safetensors
        //   text_encoder/pytorch_model.safetensors
        //   tokenizer/vocab.json, merges.txt
        //   scheduler/scheduler_config.json
        //   model_index.json (component mapping)
        
        var allTensors = new Dictionary<string, TensorInfo>();
        
        // Parse each component
        foreach (var kvp in files)
        {
            string filename = kvp.Key;
            Stream stream = kvp.Value;
            
            if (filename.EndsWith(".safetensors"))
            {
                var tensors = _safeTensorsParser.ReadWeights(stream);
                // Prefix tensor names with component (unet., vae., text_encoder.)
                foreach (var t in tensors)
                    allTensors[filename + "/" + t.Key] = t.Value;
            }
            else if (filename.EndsWith(".bin"))
            {
                var tensors = _pytorchParser.ReadWeights(stream);
                foreach (var t in tensors)
                    allTensors[filename + "/" + t.Key] = t.Value;
            }
        }
        
        return allTensors;
    }
}
```

---

## 3. Archive Handler Infrastructure

**Purpose:** Handle compressed files (ZIP, TAR, 7Z) with recursive extraction

### 3.1 EXTERNAL_ACCESS Assembly Requirements

```csharp
// Hartonomous.Clr.csproj - Add signing
<PropertyGroup>
  <SignAssembly>true</SignAssembly>
  <AssemblyOriginatorKeyFile>..\..\deploy\SqlClrKey.snk</AssemblyOriginatorKeyFile>
</PropertyGroup>

// Add System.IO.Compression reference
<ItemGroup>
  <Reference Include="System.IO.Compression" />
  <Reference Include="System.IO.Compression.FileSystem" />
</ItemGroup>
```

```sql
-- SQL Server deployment script
-- Enable CLR
sp_configure 'clr enabled', 1;
RECONFIGURE;

-- Drop existing assembly (if upgrading)
DROP ASSEMBLY IF EXISTS [Hartonomous.Clr];

-- Create EXTERNAL_ACCESS assembly (not SAFE)
CREATE ASSEMBLY [Hartonomous.Clr]
FROM 'D:\Repositories\Hartonomous\src\Hartonomous.Clr\bin\Release\net481\Hartonomous.Clr.dll'
WITH PERMISSION_SET = EXTERNAL_ACCESS;
GO

-- Grant EXTERNAL ACCESS ASSEMBLY permission to database owner
-- (or create certificate-based trust)
USE master;
GO

-- Option 1: Trustworthy database (simpler for development)
ALTER DATABASE [Hartonomous] SET TRUSTWORTHY ON;
GO

-- Option 2: Certificate-based trust (production recommended)
CREATE CERTIFICATE SqlClrCertificate
FROM FILE = 'D:\Repositories\Hartonomous\deploy\SqlClrKey.cer';
GO

CREATE LOGIN SqlClrLogin
FROM CERTIFICATE SqlClrCertificate;
GO

GRANT EXTERNAL ACCESS ASSEMBLY TO SqlClrLogin;
GO
```

### 3.2 Archive Handler Implementation

```csharp
namespace Hartonomous.Clr.Core
{
    /// <summary>
    /// Handles compressed archive extraction with streaming and recursive support.
    /// Requires EXTERNAL_ACCESS assembly permission for System.IO.Compression.
    /// </summary>
    public class ArchiveHandler
    {
        /// <summary>
        /// Extract files from archive with memory-efficient streaming.
        /// Supports recursive extraction (archives within archives).
        /// </summary>
        public Dictionary<string, Stream> ExtractArchive(
            Stream archiveStream, 
            ArchiveFormat format,
            ArchiveExtractionOptions options)
        {
            switch (format)
            {
                case ArchiveFormat.Zip:
                    return ExtractZip(archiveStream, options);
                case ArchiveFormat.Tar:
                    return ExtractTar(archiveStream, options);
                case ArchiveFormat.Gzip:
                    return ExtractGzip(archiveStream, options);
                default:
                    throw new NotSupportedException($"Archive format {format} not supported");
            }
        }

        private Dictionary<string, Stream> ExtractZip(Stream stream, ArchiveExtractionOptions options)
        {
            var files = new Dictionary<string, Stream>();

            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    // Skip directories
                    if (entry.FullName.EndsWith("/"))
                        continue;

                    // Security: Check for path traversal attacks
                    string safeName = SanitizePath(entry.FullName);
                    
                    // Open entry stream
                    Stream entryStream = entry.Open();
                    
                    // Check if this entry is itself an archive
                    if (options.RecursiveExtraction && IsArchive(safeName))
                    {
                        // Recursively extract nested archive
                        var nestedFormat = DetectArchiveFormat(entryStream);
                        var nestedFiles = ExtractArchive(entryStream, nestedFormat, options);
                        
                        // Add nested files with prefixed paths
                        foreach (var nested in nestedFiles)
                        {
                            files[safeName + "/" + nested.Key] = nested.Value;
                        }
                    }
                    else
                    {
                        // Copy to memory stream for SQL Server compatibility
                        // (ZipArchive streams aren't seekable, SQL needs seekable)
                        var memoryStream = new MemoryStream();
                        entryStream.CopyTo(memoryStream);
                        memoryStream.Position = 0;
                        
                        files[safeName] = memoryStream;
                    }
                }
            }

            return files;
        }

        private string SanitizePath(string path)
        {
            // Prevent path traversal: "../../../etc/passwd"
            // Normalize separators, remove dangerous patterns
            path = path.Replace("\\", "/");
            path = path.Replace("../", "");
            path = path.Replace("..\\", "");
            return path;
        }

        private bool IsArchive(string filename)
        {
            string ext = Path.GetExtension(filename).ToLowerInvariant();
            return ext == ".zip" || ext == ".tar" || ext == ".gz" || 
                   ext == ".7z" || ext == ".rar";
        }
    }

    public class ArchiveExtractionOptions
    {
        /// <summary>
        /// Extract archives within archives recursively
        /// </summary>
        public bool RecursiveExtraction { get; set; } = true;

        /// <summary>
        /// Maximum recursion depth (prevent zip bombs)
        /// </summary>
        public int MaxRecursionDepth { get; set; } = 10;

        /// <summary>
        /// File patterns to extract (null = all files)
        /// </summary>
        public string[] FilePatterns { get; set; }

        /// <summary>
        /// Maximum extracted size (prevent zip bombs)
        /// </summary>
        public long MaxExtractedSize { get; set; } = 10L * 1024 * 1024 * 1024; // 10 GB
    }

    public enum ArchiveFormat
    {
        Zip = 1,
        Tar = 2,
        Gzip = 3,
        BZip2 = 4,
        SevenZip = 5,
        Rar = 6
    }
}
```

---

## 4. Catalog Manager (Multi-File Coordination)

**Purpose:** Coordinate parsing of multi-file model repositories (HuggingFace structure)

```csharp
namespace Hartonomous.Clr.Core
{
    /// <summary>
    /// Manages multi-file model catalogs (HuggingFace repos, Stable Diffusion models).
    /// Coordinates parsing of config files, tokenizers, weights, etc.
    /// </summary>
    public class CatalogManager
    {
        private readonly ArchiveHandler _archiveHandler = new ArchiveHandler();
        private readonly ModelFormatDetector _formatDetector = new ModelFormatDetector();

        /// <summary>
        /// Parse HuggingFace model repository structure.
        /// Expected files:
        ///   - model.safetensors OR pytorch_model.bin
        ///   - config.json
        ///   - tokenizer_config.json, tokenizer.json
        ///   - vocab.txt, special_tokens_map.json
        ///   - README.md
        /// </summary>
        public ModelCatalog ParseHuggingFaceRepo(Dictionary<string, Stream> files)
        {
            var catalog = new ModelCatalog();

            // Parse config.json
            if (files.ContainsKey("config.json"))
            {
                catalog.Config = ParseConfigJson(files["config.json"]);
            }

            // Parse tokenizer files
            if (files.ContainsKey("tokenizer_config.json"))
            {
                catalog.TokenizerConfig = ParseTokenizerConfigJson(files["tokenizer_config.json"]);
            }

            // Parse weights
            string weightsFile = files.Keys.FirstOrDefault(k => 
                k.EndsWith(".safetensors") || k.EndsWith(".bin") || k.EndsWith(".gguf"));

            if (weightsFile != null)
            {
                var format = _formatDetector.DetectFormat(files[weightsFile]);
                var parser = GetParserForFormat(format);
                catalog.Tensors = parser.ReadWeights(files[weightsFile]);
            }

            return catalog;
        }

        /// <summary>
        /// Parse Stable Diffusion model (multi-component structure).
        /// </summary>
        public ModelCatalog ParseStableDiffusionModel(Dictionary<string, Stream> files)
        {
            // Stable Diffusion has separate UNet, VAE, TextEncoder components
            // Each in its own directory with weights + config
            
            var catalog = new ModelCatalog();
            catalog.IsMultiComponent = true;
            catalog.Components = new Dictionary<string, Dictionary<string, TensorInfo>>();

            // Parse model_index.json to get component mapping
            if (files.ContainsKey("model_index.json"))
            {
                catalog.ComponentMapping = ParseModelIndexJson(files["model_index.json"]);
            }

            // Parse each component
            string[] components = { "unet", "vae", "text_encoder", "safety_checker" };
            foreach (var component in components)
            {
                var componentFiles = files.Where(kvp => kvp.Key.StartsWith(component + "/"))
                                          .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                
                if (componentFiles.Any())
                {
                    // Parse component weights
                    var weightsFile = componentFiles.Keys.FirstOrDefault(k => 
                        k.EndsWith(".safetensors") || k.EndsWith(".bin"));
                    
                    if (weightsFile != null)
                    {
                        var format = _formatDetector.DetectFormat(componentFiles[weightsFile]);
                        var parser = GetParserForFormat(format);
                        catalog.Components[component] = parser.ReadWeights(componentFiles[weightsFile]);
                    }
                }
            }

            return catalog;
        }

        /// <summary>
        /// Parse Ollama model structure (GGUF + Modelfile).
        /// </summary>
        public ModelCatalog ParseOllamaModel(Dictionary<string, Stream> files)
        {
            var catalog = new ModelCatalog();

            // Ollama structure:
            //   - Single GGUF file (quantized model)
            //   - Modelfile (configuration: FROM, PARAMETER, TEMPLATE, SYSTEM, etc.)
            
            var ggufFile = files.Keys.FirstOrDefault(k => k.EndsWith(".gguf"));
            if (ggufFile != null)
            {
                var parser = new GGUFParser();
                catalog.Tensors = parser.ReadWeights(files[ggufFile]);
            }

            if (files.ContainsKey("Modelfile"))
            {
                catalog.ModelfileConfig = ParseModelfile(files["Modelfile"]);
            }

            return catalog;
        }
    }

    public class ModelCatalog
    {
        public Dictionary<string, string> Config { get; set; }
        public Dictionary<string, string> TokenizerConfig { get; set; }
        public Dictionary<string, TensorInfo> Tensors { get; set; }
        public bool IsMultiComponent { get; set; }
        public Dictionary<string, Dictionary<string, TensorInfo>> Components { get; set; }
        public Dictionary<string, string> ComponentMapping { get; set; }
        public string ModelfileConfig { get; set; }
    }
}
```

---

## 5. Universal File Format Registry

**Purpose:** Detect and route ALL file types (not just AI models)

```csharp
namespace Hartonomous.Clr.Core
{
    /// <summary>
    /// Universal file format detection and parser routing.
    /// Supports: AI models, documents, images, video, audio, archives, data formats.
    /// </summary>
    public class UniversalFileFormatRegistry
    {
        private readonly Dictionary<FileCategory, List<IFileFormatHandler>> _handlers;

        public UniversalFileFormatRegistry()
        {
            _handlers = new Dictionary<FileCategory, List<IFileFormatHandler>>
            {
                [FileCategory.AIModel] = new List<IFileFormatHandler>
                {
                    new GGUFParser(),
                    new SafeTensorsParser(),
                    new ONNXParser(),
                    new PyTorchParser(),
                    new TensorFlowParser()
                },
                [FileCategory.Document] = new List<IFileFormatHandler>
                {
                    new PDFParser(),
                    new DOCXParser(),
                    new XLSXParser(),
                    new CSVParser(),
                    new TextParser()
                },
                [FileCategory.Image] = new List<IFileFormatHandler>
                {
                    new PNGParser(),
                    new JPEGParser(),
                    new TIFFParser(),
                    new BMPParser()
                },
                [FileCategory.Video] = new List<IFileFormatHandler>
                {
                    new MP4Parser(),
                    new AVIParser(),
                    new MKVParser()
                },
                [FileCategory.Audio] = new List<IFileFormatHandler>
                {
                    new WAVParser(),
                    new MP3Parser(),
                    new FLACParser()
                },
                [FileCategory.Archive] = new List<IFileFormatHandler>
                {
                    new ZipHandler(),
                    new TarHandler(),
                    new GzipHandler(),
                    new SevenZipHandler()
                },
                [FileCategory.Data] = new List<IFileFormatHandler>
                {
                    new JSONParser(),
                    new XMLParser(),
                    new ParquetParser(),
                    new AvroParser(),
                    new ProtobufParser()
                }
            };
        }

        public IFileFormatHandler DetectAndGetHandler(Stream stream)
        {
            // Read magic numbers (first 16 bytes usually sufficient)
            byte[] magic = new byte[16];
            int bytesRead = stream.Read(magic, 0, magic.Length);
            stream.Position = 0;

            // Try each category in priority order
            var categories = new[]
            {
                FileCategory.Archive,  // Check archives first (might contain models)
                FileCategory.AIModel,
                FileCategory.Document,
                FileCategory.Image,
                FileCategory.Video,
                FileCategory.Audio,
                FileCategory.Data
            };

            foreach (var category in categories)
            {
                foreach (var handler in _handlers[category])
                {
                    if (handler.CanHandle(magic))
                        return handler;
                }
            }

            throw new NotSupportedException("Unknown file format");
        }
    }

    public enum FileCategory
    {
        AIModel = 1,
        Document = 2,
        Image = 3,
        Video = 4,
        Audio = 5,
        Archive = 6,
        Data = 7,
        Unknown = 99
    }

    /// <summary>
    /// Base interface for all file format handlers (parsers, extractors, readers).
    /// </summary>
    public interface IFileFormatHandler
    {
        string FormatName { get; }
        FileCategory Category { get; }
        bool CanHandle(byte[] magicBytes);
    }
}
```

### 5.1 Magic Number Database

```csharp
/// <summary>
/// Magic number constants for file format detection.
/// https://en.wikipedia.org/wiki/List_of_file_signatures
/// </summary>
public static class MagicNumbers
{
    // Archives
    public static readonly byte[] ZIP = { 0x50, 0x4B, 0x03, 0x04 }; // "PK.."
    public static readonly byte[] GZIP = { 0x1F, 0x8B };
    public static readonly byte[] TAR = { 0x75, 0x73, 0x74, 0x61, 0x72 }; // "ustar" at offset 257
    public static readonly byte[] SEVEN_ZIP = { 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C };
    public static readonly byte[] RAR = { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07 }; // "Rar!.."

    // AI Models
    public static readonly byte[] GGUF = { 0x47, 0x47, 0x55, 0x46 }; // "GGUF"
    public static readonly byte[] SAFETENSORS = { }; // 8-byte little-endian header size
    public static readonly byte[] ONNX = { 0x08, 0x01 }; // Protobuf field 1, wire type 0
    public static readonly byte[] PYTORCH_PICKLE = { 0x80 }; // Pickle PROTO opcode

    // Documents
    public static readonly byte[] PDF = { 0x25, 0x50, 0x44, 0x46 }; // "%PDF"
    public static readonly byte[] DOCX = ZIP; // DOCX is ZIP archive
    public static readonly byte[] XLSX = ZIP; // XLSX is ZIP archive

    // Images
    public static readonly byte[] PNG = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    public static readonly byte[] JPEG = { 0xFF, 0xD8, 0xFF };
    public static readonly byte[] TIFF_LE = { 0x49, 0x49, 0x2A, 0x00 }; // "II*." little-endian
    public static readonly byte[] TIFF_BE = { 0x4D, 0x4D, 0x00, 0x2A }; // "MM.*" big-endian
    public static readonly byte[] BMP = { 0x42, 0x4D }; // "BM"
    public static readonly byte[] GIF = { 0x47, 0x49, 0x46, 0x38 }; // "GIF8"

    // Video
    public static readonly byte[] MP4 = { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70 }; // "....ftyp"
    public static readonly byte[] AVI = { 0x52, 0x49, 0x46, 0x46 }; // "RIFF"
    public static readonly byte[] MKV = { 0x1A, 0x45, 0xDF, 0xA3 }; // Matroska

    // Audio
    public static readonly byte[] WAV = { 0x52, 0x49, 0x46, 0x46 }; // "RIFF"
    public static readonly byte[] MP3 = { 0xFF, 0xFB }; // MP3 frame header
    public static readonly byte[] FLAC = { 0x66, 0x4C, 0x61, 0x43 }; // "fLaC"

    // Data
    public static readonly byte[] PARQUET = { 0x50, 0x41, 0x52, 0x31 }; // "PAR1"
    public static readonly byte[] AVRO = { 0x4F, 0x62, 0x6A, 0x01 }; // "Obj."
}
```

---

## 6. SQL Server 2025 Integration

**Purpose:** Use native `vector`/`json` types instead of CLR types

### 6.1 Schema Changes

```sql
-- Current schema (uses CLR types - WRONG)
CREATE TABLE ModelTensors
(
    TensorId INT PRIMARY KEY,
    TensorName NVARCHAR(255),
    Shape NVARCHAR(MAX), -- JSON array stored as string - WRONG
    Embedding VARBINARY(MAX) -- Binary data - WRONG
);

-- New schema (uses SQL Server 2025 native types - CORRECT)
CREATE TABLE ModelTensors
(
    TensorId INT PRIMARY KEY,
    TensorName NVARCHAR(255),
    Shape json NOT NULL, -- Native JSON type (binary format)
    Embedding vector(1536) NOT NULL, -- Native vector type (float32)
    EmbeddingHalfPrecision vector(768) NULL, -- Float16 for compression
    Metadata json NULL,
    CONSTRAINT PK_ModelTensors PRIMARY KEY (TensorId)
);

-- Vector similarity search (native function)
CREATE INDEX IX_ModelTensors_Embedding 
ON ModelTensors (Embedding)
USING VECTOR_INDEX; -- Approximate nearest neighbor index

-- Query with vector distance
SELECT TOP 10 
    TensorId,
    TensorName,
    VECTOR_DISTANCE('cosine', Embedding, @queryVector) AS Similarity
FROM ModelTensors
ORDER BY Similarity;
```

### 6.2 CLR Integration with Native Types

```csharp
using Microsoft.Data.SqlClient.Server;
using Microsoft.Data.SqlClient; // Version 6.1.0+ for SqlVector<T>

namespace Hartonomous.Clr.Integration
{
    public class SqlServer2025Integration
    {
        /// <summary>
        /// Store tensor using native vector type (not varbinary).
        /// </summary>
        [SqlFunction]
        public static void StoreTensorWithNativeVector(
            string tensorName,
            SqlJson shapeJson, // Native JSON type
            SqlVector<float> embedding) // Native vector type
        {
            // Use SqlVector<T> instead of float[] or byte[]
            // Binary TDS 7.4 protocol transport (efficient)
            
            using (SqlConnection conn = new SqlConnection("context connection=true"))
            {
                conn.Open();
                
                using (SqlCommand cmd = new SqlCommand(
                    "INSERT INTO ModelTensors (TensorName, Shape, Embedding) VALUES (@name, @shape, @embedding)",
                    conn))
                {
                    cmd.Parameters.Add("@name", SqlDbType.NVarChar).Value = tensorName;
                    cmd.Parameters.Add("@shape", SqlDbType.Json).Value = shapeJson;
                    cmd.Parameters.Add("@embedding", SqlDbType.Vector).Value = embedding;
                    
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Call external REST endpoint using sp_invoke_external_rest_endpoint.
        /// Example: Call Azure OpenAI for embeddings.
        /// </summary>
        [SqlFunction]
        public static SqlVector<float> GetEmbeddingFromAzureOpenAI(string text)
        {
            using (SqlConnection conn = new SqlConnection("context connection=true"))
            {
                conn.Open();
                
                using (SqlCommand cmd = new SqlCommand(@"
                    DECLARE @response NVARCHAR(MAX);
                    DECLARE @url NVARCHAR(500) = 'https://my-openai.openai.azure.com/openai/deployments/text-embedding-ada-002/embeddings?api-version=2023-05-15';
                    
                    EXEC sp_invoke_external_rest_endpoint
                        @url = @url,
                        @method = 'POST',
                        @credential = [AzureOpenAICredential], -- Managed Identity
                        @payload = @payload,
                        @response = @response OUTPUT;
                    
                    SELECT @response;
                ", conn))
                {
                    string payload = JsonSerializer.Serialize(new { input = text });
                    cmd.Parameters.Add("@payload", SqlDbType.NVarChar).Value = payload;
                    
                    string response = (string)cmd.ExecuteScalar();
                    
                    // Parse response and extract vector
                    var json = JsonDocument.Parse(response);
                    var embeddingArray = json.RootElement.GetProperty("data")[0]
                                             .GetProperty("embedding").EnumerateArray()
                                             .Select(e => (float)e.GetDouble())
                                             .ToArray();
                    
                    return new SqlVector<float>(embeddingArray);
                }
            }
        }
    }
}
```

### 6.3 CREATE EXTERNAL MODEL Integration

```sql
-- Register external AI model endpoints in SQL Server 2025
CREATE EXTERNAL MODEL AzureOpenAI_GPT4
WITH
(
    ENDPOINT = 'https://my-openai.openai.azure.com/openai/deployments/gpt-4/chat/completions',
    API_VERSION = '2024-02-15-preview',
    CREDENTIAL = [AzureOpenAICredential], -- Managed Identity
    MODEL_TYPE = 'CHAT_COMPLETION'
);

-- Use external model in queries
SELECT 
    TensorName,
    dbo.InvokeExternalModel('AzureOpenAI_GPT4', 'Explain this tensor: ' + TensorName) AS Explanation
FROM ModelTensors
WHERE TensorId = 123;
```

---

## 7. End-to-End Flow Examples

### 7.1 HuggingFace Model → Parse → Store

```csharp
// Step 1: Retrieve model from HuggingFace
var providerRegistry = new ModelProviderRegistry();
var provider = providerRegistry.GetProvider("mistralai/Mistral-7B-v0.1");

var retrievalResult = provider.Retrieve(
    "mistralai/Mistral-7B-v0.1",
    new RetrievalOptions { CacheDirectory = "C:\\models\\cache" });

// Step 2: Parse catalog
var catalogManager = new CatalogManager();
var catalog = catalogManager.ParseHuggingFaceRepo(retrievalResult.CatalogFiles);

// Step 3: Store tensors using SQL Server 2025 native types
using (SqlConnection conn = new SqlConnection(connectionString))
{
    conn.Open();
    
    foreach (var tensor in catalog.Tensors)
    {
        using (SqlCommand cmd = new SqlCommand(@"
            INSERT INTO ModelTensors (TensorName, Shape, Embedding, Metadata)
            VALUES (@name, @shape, @embedding, @metadata)",
            conn))
        {
            // Use native types
            cmd.Parameters.Add("@name", SqlDbType.NVarChar).Value = tensor.Key;
            cmd.Parameters.Add("@shape", SqlDbType.Json).Value = 
                new SqlJson(JsonSerializer.Serialize(tensor.Value.Shape));
            cmd.Parameters.Add("@embedding", SqlDbType.Vector).Value = 
                new SqlVector<float>(tensor.Value.Data);
            cmd.Parameters.Add("@metadata", SqlDbType.Json).Value = 
                new SqlJson(JsonSerializer.Serialize(tensor.Value.Metadata));
            
            cmd.ExecuteNonQuery();
        }
    }
}
```

### 7.2 Ollama Model → Parse → Store

```csharp
// Step 1: Retrieve model from Ollama
var provider = providerRegistry.GetProvider("ollama://llama2:7b");
var retrievalResult = provider.Retrieve("ollama://llama2:7b", options);

// Step 2: Parse GGUF format
var formatDetector = new ModelFormatDetector();
var format = formatDetector.DetectFormat(retrievalResult.SingleFileStream);

var parser = new GGUFParser();
var tensors = parser.ReadWeights(retrievalResult.SingleFileStream);

// Step 3: Store with native types (same as HuggingFace)
```

### 7.3 Local ZIP Archive → Recursive Extract → Parse

```csharp
// Step 1: Retrieve from local filesystem
var provider = providerRegistry.GetProvider("file:///C:/models/model.zip");
var retrievalResult = provider.Retrieve("file:///C:/models/model.zip", options);

// Step 2: Extract archive
var archiveHandler = new ArchiveHandler();
var files = archiveHandler.ExtractArchive(
    retrievalResult.SingleFileStream,
    ArchiveFormat.Zip,
    new ArchiveExtractionOptions { RecursiveExtraction = true });

// Step 3: Detect formats and parse each file
var registry = new UniversalFileFormatRegistry();

foreach (var file in files)
{
    var handler = registry.DetectAndGetHandler(file.Value);
    
    if (handler is IModelFormatReader modelParser)
    {
        var tensors = modelParser.ReadWeights(file.Value);
        // Store tensors
    }
    else if (handler.Category == FileCategory.Document)
    {
        // Parse document, extract text/embeddings
    }
}
```

---

## 8. Implementation Phases

### Phase 1: Foundation (Week 1)
- [ ] Create Model Provider interfaces and registry
- [ ] Implement HuggingFaceProvider (stub with local file fallback)
- [ ] Implement OllamaProvider (stub with local file fallback)
- [ ] Implement LocalFileSystemProvider (complete)
- [ ] Set up EXTERNAL_ACCESS assembly signing

### Phase 2: Archive Handling (Week 1-2)
- [ ] Implement complete ZipArchive support
- [ ] Implement recursive extraction
- [ ] Add security: path traversal prevention, zip bomb protection
- [ ] Test with nested archives

### Phase 3: Model Parsers Rewrite (Week 2-3)
- [ ] Rewrite PyTorchParser with full ZIP support
- [ ] Rewrite ONNXParser with complete protobuf
- [ ] Rewrite TensorFlowParser with full SavedModel
- [ ] Fix StableDiffusionParser catalog coordination

### Phase 4: Catalog Manager (Week 3)
- [ ] Implement HuggingFace repo parsing
- [ ] Implement Ollama model parsing
- [ ] Implement Stable Diffusion multi-component parsing
- [ ] Add config.json, tokenizer parsing

### Phase 5: SQL Server 2025 Integration (Week 4)
- [ ] Update schema to use native vector/json types
- [ ] Implement SqlVector<T> integration in CLR
- [ ] Add sp_invoke_external_rest_endpoint wrappers
- [ ] Test CREATE EXTERNAL MODEL integration

### Phase 6: Universal File Format (Week 4-5)
- [ ] Implement document parsers (PDF, DOCX, XLSX)
- [ ] Implement image parsers (PNG, JPEG, TIFF)
- [ ] Implement video/audio parsers (metadata extraction)
- [ ] Implement data parsers (JSON, XML, Parquet)

### Phase 7: Testing & Documentation (Week 5-6)
- [ ] Comprehensive unit tests for all parsers
- [ ] Integration tests with real models from HuggingFace/Ollama
- [ ] Performance benchmarks (streaming, memory usage)
- [ ] Update documentation and examples

---

## 9. Success Criteria

- ✅ **No Cop-Outs**: Zero "recommend conversion" or "not supported" exceptions
- ✅ **Complete Implementations**: Full protobuf, full ZIP, full SavedModel parsing
- ✅ **Universal Coverage**: Documents, images, video, audio, archives, AI models
- ✅ **Provider Separation**: Model providers (HF, Ollama) separate from format parsers
- ✅ **SQL Server 2025**: Native vector/json types, no CLR wrappers
- ✅ **Archive Support**: Recursive extraction, path traversal protection
- ✅ **Catalog Management**: Multi-file models (HF repos, SD components, Ollama)
- ✅ **Memory Efficient**: Streaming for large files, no full-memory loads
- ✅ **0 C# Errors**: Clean compilation
- ✅ **≤34 Warnings**: Maintain current ceiling
- ✅ **Test Coverage**: All parsers tested with real-world files

---

## 10. Design Decisions (RESOLVED)

### 1. Ollama API Integration ✅ **DECISION: Use HTTP API**
- **Rationale**: API provides versioning, consistency, and pull capabilities
- **Config**: Support both API endpoint and local path (`D:\Models`) for seeding
- **Authentication**: Token-based via config or secure storage

### 2. HuggingFace Authentication ✅ **DECISION: Token-based with secure storage**
- **Business Model**: Pay-to-upload for global model inclusion, pay-to-hide for private/sharded content
- **Token Storage**: Configuration key with secure token fetch mechanism
- **Multi-tenancy**: Row-level security for content isolation (see Section 11)

### 3. Protobuf Library Choice ✅ **DECISION: Use protobuf-net library**
- **Rationale**: Don't reinvent the wheel - leverage existing battle-tested implementation
- **Philosophy**: Focus on our capabilities (geometric AI, spatial reasoning), not reimplementing competitors
- **Requirements**: .NET Framework 4.8.1 compatible, EXTERNAL_ACCESS assembly permission

### 4. External REST Endpoints ✅ **DECISION: Use sp_invoke_external_rest_endpoint**
- **Rationale**: In-process, in-memory execution - shortest flight path
- **Alternative**: SQL Service Broker for async message-based patterns
- **Benefits**: Native SQL Server 2025 integration, managed execution context

### 5. Vector Type Usage ✅ **DECISION: Selective use - don't replace geometric AI**
- **Current**: 1998-dimensional embeddings with Gram-Schmidt orthogonalization
- **Vector Type For**: External model embeddings from OpenAI/Azure that arrive as vectors
- **Geometric AI Preserved**: R-Tree spatial indexing, landmark projection, Hilbert curves remain unchanged
- **Enhancement**: Use vector type for embedding storage/similarity when it doesn't conflict with spatial algorithms

### 6. Streaming Definition ✅ **DECISION: MemoryStream for archives, NEW streaming for live data**
- **Archive Extraction**: Use MemoryStream for ZIP/TAR contents (manageable sizes)
- **Live Streaming**: Video/audio/telemetry/SCADA streams - analog-to-digital conversion, real-time ingestion
- **New Requirement**: Add streaming data ingestion layer for live feeds (see Section 12)

### 7. Security Model ✅ **DECISION: Row-level security + certificate-based EXTERNAL_ACCESS**
- **Assembly Trust**: Use wildcard certificate from OpenWrt router (Let's Encrypt) for signing
- **Multi-tenancy**: Row-level security (RLS) for tenant isolation, content sharding
- **Privacy Model**: Tenants can mark content private/public, pay-based access control
- **Development**: Can use TRUSTWORTHY for local dev, certificates for production

---

## 11. Multi-Tenancy & Security Architecture

### 11.1 Business Model

**Pay-to-Contribute**: Users/organizations pay to upload content that becomes part of the global model
**Pay-for-Privacy**: Users pay to keep content private, sharded, or tenant-isolated
**Free Tier**: Basic access with public content only

### 11.2 Row-Level Security (RLS)

SQL Server 2025 RLS implementation for tenant isolation:

```sql
-- Tenant identification table
CREATE TABLE Tenants (
    TenantId INT PRIMARY KEY IDENTITY(1,1),
    TenantName NVARCHAR(255) NOT NULL,
    SubscriptionTier NVARCHAR(50) NOT NULL, -- Free, Basic, Premium, Enterprise
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Content ownership and visibility
CREATE TABLE ContentMetadata (
    ContentId BIGINT PRIMARY KEY IDENTITY(1,1),
    TenantId INT NOT NULL REFERENCES Tenants(TenantId),
    IsPublic BIT NOT NULL DEFAULT 0, -- False = private to tenant
    IsGlobalContributor BIT NOT NULL DEFAULT 0, -- True = part of global model (paid)
    ShardId INT NULL, -- For horizontal partitioning
    AccessLevel NVARCHAR(50) NOT NULL DEFAULT 'Private', -- Private, Shared, Public, Global
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    INDEX IX_ContentMetadata_Tenant_Access (TenantId, AccessLevel, IsPublic)
);

-- Row-Level Security predicate function
CREATE FUNCTION Security.fn_TenantAccessPredicate(@TenantId INT)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN
    SELECT 1 AS AccessResult
    WHERE
        -- User can see their own content
        @TenantId = CAST(SESSION_CONTEXT(N'TenantId') AS INT)
        -- OR content is public
        OR EXISTS (
            SELECT 1 FROM dbo.ContentMetadata cm
            WHERE cm.TenantId = @TenantId AND cm.IsPublic = 1
        )
        -- OR user has premium/enterprise tier (see shared content)
        OR EXISTS (
            SELECT 1 FROM dbo.Tenants t
            WHERE t.TenantId = CAST(SESSION_CONTEXT(N'TenantId') AS INT)
              AND t.SubscriptionTier IN ('Premium', 'Enterprise')
        );

-- Apply RLS policy
CREATE SECURITY POLICY Security.TenantAccessPolicy
ADD FILTER PREDICATE Security.fn_TenantAccessPredicate(TenantId)
ON dbo.ModelTensors,
ADD FILTER PREDICATE Security.fn_TenantAccessPredicate(TenantId)
ON dbo.ContentMetadata,
ADD BLOCK PREDICATE Security.fn_TenantAccessPredicate(TenantId)
ON dbo.ModelTensors AFTER INSERT;

-- Set tenant context in connection
-- Application must call this after authentication
EXEC sp_set_session_context @key = N'TenantId', @value = @authenticatedTenantId;
```

### 11.3 Horizontal Sharding

For scaling beyond single-server capacity:

```sql
-- Shard mapping
CREATE TABLE ShardMap (
    ShardId INT PRIMARY KEY,
    ServerName NVARCHAR(255) NOT NULL,
    DatabaseName NVARCHAR(255) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    TenantIdRangeStart INT NOT NULL,
    TenantIdRangeEnd INT NOT NULL,
    INDEX IX_ShardMap_TenantRange (TenantIdRangeStart, TenantIdRangeEnd)
);

-- Routing function
CREATE FUNCTION dbo.fn_GetShardForTenant(@TenantId INT)
RETURNS INT
AS
BEGIN
    RETURN (
        SELECT TOP 1 ShardId
        FROM ShardMap
        WHERE @TenantId BETWEEN TenantIdRangeStart AND TenantIdRangeEnd
          AND IsActive = 1
    );
END;
```

### 11.4 Assembly Trust (EXTERNAL_ACCESS)

Use Let's Encrypt wildcard certificate from OpenWrt router:

```powershell
# Generate strong name key pair
sn -k D:\Repositories\Hartonomous\deploy\SqlClrKey.snk

# Sign assembly with strong name
# (Already configured in .csproj with <AssemblyOriginatorKeyFile>)

# Export certificate from OpenWrt router
# Wildcard cert: *.yourdomain.com
# Copy to: D:\Repositories\Hartonomous\deploy\LetsEncrypt.cer

# Create certificate in SQL Server
USE master;
GO

CREATE CERTIFICATE SqlClrCertificate
FROM FILE = 'D:\Repositories\Hartonomous\deploy\LetsEncrypt.cer';
GO

CREATE LOGIN SqlClrLogin
FROM CERTIFICATE SqlClrCertificate;
GO

GRANT EXTERNAL ACCESS ASSEMBLY TO SqlClrLogin;
GO

-- Create assembly with EXTERNAL_ACCESS
USE Hartonomous;
GO

CREATE ASSEMBLY [Hartonomous.Clr]
FROM 'D:\Repositories\Hartonomous\src\Hartonomous.Clr\bin\Release\net481\Hartonomous.Clr.dll'
WITH PERMISSION_SET = EXTERNAL_ACCESS;
GO
```

### 11.5 Content Access Control

```csharp
namespace Hartonomous.Clr.Security
{
    /// <summary>
    /// Enforces content access control based on tenant and subscription tier.
    /// </summary>
    public class ContentAccessControl
    {
        /// <summary>
        /// Check if current tenant can access content.
        /// </summary>
        [SqlFunction(DataAccess = DataAccessKind.Read)]
        public static bool CanAccessContent(int contentId)
        {
            using (SqlConnection conn = new SqlConnection("context connection=true"))
            {
                conn.Open();
                
                // Get current tenant from session context
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT CAST(SESSION_CONTEXT(N'TenantId') AS INT)", conn))
                {
                    int currentTenantId = (int)cmd.ExecuteScalar();
                    
                    // Check access via RLS-enabled query
                    using (SqlCommand checkCmd = new SqlCommand(@"
                        SELECT COUNT(*)
                        FROM ContentMetadata
                        WHERE ContentId = @contentId
                          AND (
                              TenantId = @tenantId
                              OR IsPublic = 1
                              OR IsGlobalContributor = 1
                          )", conn))
                    {
                        checkCmd.Parameters.AddWithValue("@contentId", contentId);
                        checkCmd.Parameters.AddWithValue("@tenantId", currentTenantId);
                        
                        return (int)checkCmd.ExecuteScalar() > 0;
                    }
                }
            }
        }
    }
}
```

---

## 12. Streaming Data Ingestion Layer (NEW)

### 12.1 Live Stream Types

**Video Streams**: RTSP, HLS, WebRTC, RTMP
**Audio Streams**: RTP, SRT, Icecast
**Telemetry**: MQTT, OPC UA, Modbus
**SCADA**: DNP3, IEC 60870-5-104
**IoT**: CoAP, AMQP, Kafka

### 12.2 Analog-to-Digital Conversion

For real-time data ingestion from analog sources:

```csharp
namespace Hartonomous.Clr.Streaming
{
    /// <summary>
    /// Handles analog-to-digital conversion and real-time stream ingestion.
    /// </summary>
    public class StreamIngestionHandler
    {
        /// <summary>
        /// Ingest video stream frame-by-frame.
        /// </summary>
        public static void IngestVideoStream(
            string streamUrl,
            int frameRateLimit,
            Action<byte[], VideoFrameMetadata> frameCallback)
        {
            // Use FFmpeg.AutoGen or similar for video decoding
            // Extract frames at specified rate
            // Call frameCallback for each frame
            //
            // Frame processing:
            //   1. Decode frame to raw pixels
            //   2. Resize/normalize if needed
            //   3. Extract embeddings (via external model)
            //   4. Store frame + embedding in SQL
        }

        /// <summary>
        /// Ingest audio stream with windowing.
        /// </summary>
        public static void IngestAudioStream(
            string streamUrl,
            TimeSpan windowSize,
            TimeSpan overlap,
            Action<float[], AudioWindowMetadata> windowCallback)
        {
            // Use NAudio or similar for audio decoding
            // Apply sliding window
            // Extract features (MFCC, spectrogram)
            // Call windowCallback for each window
        }

        /// <summary>
        /// Ingest telemetry stream (MQTT).
        /// </summary>
        public static void IngestMqttStream(
            string brokerUrl,
            string[] topics,
            Action<string, byte[], DateTime> messageCallback)
        {
            // Connect to MQTT broker
            // Subscribe to topics
            // Parse messages (JSON, protobuf, etc.)
            // Call messageCallback for each message
            //
            // Storage strategy:
            //   - Time-series table with timestamp indexing
            //   - Aggregate metrics in real-time
            //   - Trigger alerts on anomalies
        }

        /// <summary>
        /// Ingest SCADA data (OPC UA).
        /// </summary>
        public static void IngestOpcUaStream(
            string endpointUrl,
            string[] nodeIds,
            TimeSpan samplingInterval,
            Action<string, object, DateTime> dataChangeCallback)
        {
            // Connect to OPC UA server
            // Subscribe to node data changes
            // Monitor values at sampling interval
            // Call dataChangeCallback for changes
        }
    }

    public class VideoFrameMetadata
    {
        public DateTime Timestamp { get; set; }
        public int FrameNumber { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Codec { get; set; }
        public long BytePosition { get; set; }
    }

    public class AudioWindowMetadata
    {
        public DateTime StartTime { get; set; }
        public TimeSpan Duration { get; set; }
        public int SampleRate { get; set; }
        public int Channels { get; set; }
        public double RmsAmplitude { get; set; }
    }
}
```

### 12.3 SQL Service Broker Integration

For async, message-based stream processing:

```sql
-- Enable Service Broker
ALTER DATABASE Hartonomous SET ENABLE_BROKER;

-- Create message types
CREATE MESSAGE TYPE [StreamDataMessage]
VALIDATION = WELL_FORMED_XML;

CREATE MESSAGE TYPE [StreamProcessedMessage]
VALIDATION = WELL_FORMED_XML;

-- Create contract
CREATE CONTRACT [StreamProcessingContract]
(
    [StreamDataMessage] SENT BY INITIATOR,
    [StreamProcessedMessage] SENT BY TARGET
);

-- Create queues
CREATE QUEUE StreamIngestionQueue;
CREATE QUEUE StreamProcessingQueue;

-- Create services
CREATE SERVICE [StreamIngestionService]
ON QUEUE StreamIngestionQueue
([StreamProcessingContract]);

CREATE SERVICE [StreamProcessingService]
ON QUEUE StreamProcessingQueue
([StreamProcessingContract]);

-- Send message to queue (from CLR or T-SQL)
DECLARE @conversationHandle UNIQUEIDENTIFIER;
DECLARE @messageBody XML = '<StreamData>...</StreamData>';

BEGIN DIALOG CONVERSATION @conversationHandle
FROM SERVICE [StreamIngestionService]
TO SERVICE 'StreamProcessingService'
ON CONTRACT [StreamProcessingContract]
WITH ENCRYPTION = OFF;

SEND ON CONVERSATION @conversationHandle
MESSAGE TYPE [StreamDataMessage](@messageBody);

-- Process messages (activation procedure)
CREATE PROCEDURE ProcessStreamData
AS
BEGIN
    DECLARE @conversationHandle UNIQUEIDENTIFIER;
    DECLARE @messageBody XML;
    DECLARE @messageType NVARCHAR(256);

    RECEIVE TOP(1)
        @conversationHandle = conversation_handle,
        @messageBody = message_body,
        @messageType = message_type_name
    FROM StreamProcessingQueue;

    -- Process stream data
    -- Extract embeddings, store in tables
    -- Send response if needed

    END CONVERSATION @conversationHandle;
END;

-- Activate automatic processing
ALTER QUEUE StreamProcessingQueue
WITH ACTIVATION (
    STATUS = ON,
    PROCEDURE_NAME = ProcessStreamData,
    MAX_QUEUE_READERS = 10,
    EXECUTE AS SELF
);
```

### 12.4 Stream Storage Schema

```sql
-- Video frames
CREATE TABLE VideoFrames (
    FrameId BIGINT PRIMARY KEY IDENTITY(1,1),
    StreamId INT NOT NULL,
    TenantId INT NOT NULL,
    Timestamp DATETIME2 NOT NULL,
    FrameNumber INT NOT NULL,
    FrameData VARBINARY(MAX) NOT NULL, -- JPEG/PNG encoded
    Embedding vector(512) NULL, -- Image embedding (SQL Server 2025)
    Metadata json NULL,
    INDEX IX_VideoFrames_Stream_Time (StreamId, Timestamp),
    INDEX IX_VideoFrames_Tenant (TenantId)
);

-- Audio windows
CREATE TABLE AudioWindows (
    WindowId BIGINT PRIMARY KEY IDENTITY(1,1),
    StreamId INT NOT NULL,
    TenantId INT NOT NULL,
    StartTime DATETIME2 NOT NULL,
    Duration INT NOT NULL, -- milliseconds
    SampleRate INT NOT NULL,
    AudioData VARBINARY(MAX) NOT NULL, -- WAV PCM
    SpectrogramData VARBINARY(MAX) NULL,
    Embedding vector(128) NULL, -- Audio embedding
    Metadata json NULL,
    INDEX IX_AudioWindows_Stream_Time (StreamId, StartTime)
);

-- Telemetry time-series
CREATE TABLE TelemetryData (
    DataId BIGINT PRIMARY KEY IDENTITY(1,1),
    StreamId INT NOT NULL,
    TenantId INT NOT NULL,
    Timestamp DATETIME2 NOT NULL,
    Topic NVARCHAR(255) NOT NULL,
    Value FLOAT NULL,
    ValueJson json NULL, -- For complex values
    Tags json NULL,
    INDEX IX_TelemetryData_Stream_Time (StreamId, Timestamp),
    INDEX IX_TelemetryData_Topic_Time (Topic, Timestamp)
);

-- SCADA readings
CREATE TABLE ScadaReadings (
    ReadingId BIGINT PRIMARY KEY IDENTITY(1,1),
    StreamId INT NOT NULL,
    TenantId INT NOT NULL,
    Timestamp DATETIME2 NOT NULL,
    NodeId NVARCHAR(500) NOT NULL, -- OPC UA node ID
    Value FLOAT NOT NULL,
    Quality INT NOT NULL, -- OPC UA quality code
    Metadata json NULL,
    INDEX IX_ScadaReadings_Stream_Time (StreamId, Timestamp),
    INDEX IX_ScadaReadings_Node_Time (NodeId, Timestamp)
);

-- Stream registry
CREATE TABLE StreamRegistry (
    StreamId INT PRIMARY KEY IDENTITY(1,1),
    TenantId INT NOT NULL,
    StreamName NVARCHAR(255) NOT NULL,
    StreamType NVARCHAR(50) NOT NULL, -- Video, Audio, Telemetry, SCADA
    SourceUrl NVARCHAR(1000) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastDataReceived DATETIME2 NULL,
    INDEX IX_StreamRegistry_Tenant (TenantId)
);
```

### 12.5 Real-Time Processing Pipeline

```
Live Stream → CLR Ingestion Handler → Parse/Decode → Extract Features/Embeddings
                                                              ↓
                                                   Call sp_invoke_external_rest_endpoint
                                                   (Azure OpenAI, Custom Model)
                                                              ↓
                                                   Store: Frame/Window + Embedding
                                                              ↓
                                                   Service Broker Queue (async)
                                                              ↓
                                                   Post-processing, Aggregation, Alerts
```

---

## 13. Implementation Phases (UPDATED)

### Phase 1: Foundation (Week 1)
- [x] Design document approved
- [ ] Create Model Provider interfaces and registry
- [ ] Implement LocalFileSystemProvider (D:\Models support)
- [ ] Implement OllamaProvider with API integration
- [ ] Set up EXTERNAL_ACCESS assembly signing with Let's Encrypt cert
- [ ] Configure row-level security schema

### Phase 2: Archive Handling (Week 1-2)
- [ ] Implement complete ZipArchive support (System.IO.Compression)
- [ ] Add protobuf-net library integration
- [ ] Implement recursive extraction with security
- [ ] Test with nested archives and large files

### Phase 3: Model Parsers Rewrite (Week 2-3)
- [ ] Rewrite PyTorchParser with full ZIP support (no cop-outs)
- [ ] Rewrite ONNXParser using protobuf-net (complete schema)
- [ ] Rewrite TensorFlowParser with full SavedModel
- [ ] Fix StableDiffusionParser catalog coordination

### Phase 4: Catalog Manager (Week 3)
- [ ] Implement HuggingFace repo parsing with token auth
- [ ] Implement Ollama model catalog parsing
- [ ] Add config.json, tokenizer file parsing
- [ ] Implement pay-to-upload/pay-to-hide business logic

### Phase 5: SQL Server 2025 Integration (Week 4)
- [ ] Selective vector type usage (external embeddings only, preserve geometric AI)
- [ ] Implement sp_invoke_external_rest_endpoint wrappers
- [ ] Test SQL Service Broker for async processing
- [ ] Deploy row-level security policies

### Phase 6: Streaming Data Ingestion (Week 4-5) **NEW**
- [ ] Implement video stream ingestion (RTSP, HLS)
- [ ] Implement audio stream ingestion (RTP, SRT)
- [ ] Implement telemetry ingestion (MQTT, OPC UA)
- [ ] Add Service Broker activation for async processing
- [ ] Create time-series storage schema

### Phase 7: Universal File Format (Week 5-6)
- [ ] Implement document parsers (PDF, DOCX, XLSX)
- [ ] Implement image parsers (PNG, JPEG, TIFF)
- [ ] Implement video/audio metadata parsers
- [ ] Implement data parsers (JSON, XML, Parquet)

### Phase 8: Testing & Documentation (Week 6-7)
- [ ] Comprehensive unit tests (.NET Framework 4.8.1 compliant)
- [ ] Integration tests with real models from HuggingFace/Ollama/D:\Models
- [ ] Performance benchmarks (streaming, memory usage)
- [ ] Multi-tenancy and RLS testing
- [ ] Real-time stream ingestion testing

---

| Format | Magic Bytes | Offset | Notes |
|--------|-------------|--------|-------|
| GGUF | `47 47 55 46` | 0 | "GGUF" ASCII |
| SafeTensors | (8-byte size) | 0 | Little-endian header size |
| ONNX | `08 01` | 0 | Protobuf ModelProto |
| PyTorch (ZIP) | `50 4B 03 04` | 0 | ZIP archive |
| PyTorch (Pickle) | `80` | 0 | Pickle PROTO opcode |
| TensorFlow | `08 01` | 0 | Protobuf SavedModel |
| ZIP | `50 4B 03 04` | 0 | "PK.." |
| GZIP | `1F 8B` | 0 | |
| TAR | `75 73 74 61 72` | 257 | "ustar" |
| 7-Zip | `37 7A BC AF 27 1C` | 0 | |
| PDF | `25 50 44 46` | 0 | "%PDF" |
| PNG | `89 50 4E 47 0D 0A 1A 0A` | 0 | |
| JPEG | `FF D8 FF` | 0 | |
| MP4 | `66 74 79 70` | 4 | "ftyp" |
| WAV | `52 49 46 46` | 0 | "RIFF" |

---

## Appendix B: SQL Server 2025 Feature Matrix

| Feature | SQL Server 2022 | SQL Server 2025 | Hartonomous Usage |
|---------|-----------------|-----------------|-------------------|
| Vector data type | ❌ | ✅ (float32/float16) | Store embeddings natively |
| VECTOR_DISTANCE() | ❌ | ✅ (cosine, euclidean, dot) | Similarity search |
| Vector indexes | ❌ | ✅ (approximate NN) | Fast retrieval |
| JSON data type | ❌ (varchar) | ✅ (binary) | Config, metadata storage |
| sp_invoke_external_rest_endpoint | ❌ | ✅ | Call Azure OpenAI, Functions |
| CREATE EXTERNAL MODEL | ❌ | ✅ | Register AI endpoints |
| SqlVector<T> | ❌ | ✅ (.NET 6.1.0+) | CLR integration |
| TDS 7.4 protocol | ❌ | ✅ (binary transport) | Efficient vector/JSON transfer |

---

**End of Design Document**

**Next Action:** Review and approval before implementation begins.
