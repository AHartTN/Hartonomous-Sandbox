# Catalog Management: Multi-File Model Coordination

**Status**: Production Implementation  
**Date**: January 2025  
**Purpose**: Coordinate multi-file AI models (HuggingFace, Ollama, Stable Diffusion)

---

## Overview

Modern AI models are rarely single files. HuggingFace models span 20+ shards, Stable Diffusion pipelines contain 5 components, and Ollama manifests reference external blobs. **Catalog Management** ensures all files arrive, validates integrity, and orchestrates atomization.

### Multi-File Model Reality

**Single-file models** (simple):
```
llama-2-7b.gguf  (13.5 GB)
```

**Multi-file models** (complex):
```
llama-2-70b-hf/
├── model-00001-of-00029.safetensors  (4.98 GB)
├── model-00002-of-00029.safetensors  (4.98 GB)
├── model-00003-of-00029.safetensors  (4.98 GB)
├── ...
├── model-00029-of-00029.safetensors  (1.52 GB)
├── model.safetensors.index.json      (285 KB)
├── config.json                        (761 bytes)
├── tokenizer.json                     (1.84 MB)
├── tokenizer_config.json              (1.32 KB)
└── special_tokens_map.json            (414 bytes)
```

**Problem**: What if only 28 of 29 shards arrive? **Solution**: Catalog validation.

---

## Core Concepts

### 1. Model Catalog

A catalog is the **single source of truth** for multi-file models.

```sql
CREATE TABLE dbo.ModelCatalogs (
    CatalogID INT IDENTITY PRIMARY KEY,
    CatalogName NVARCHAR(255) UNIQUE NOT NULL,  -- e.g., "llama-2-70b-hf"
    ModelFormat NVARCHAR(50),                    -- "HuggingFace", "Ollama", "StableDiffusion"
    ExpectedFileCount INT,                       -- 33 files expected
    ReceivedFileCount INT DEFAULT 0,             -- 33 received ✅
    IsComplete BIT DEFAULT 0,                    -- TRUE when all files present
    CreatedDate DATETIME DEFAULT GETDATE(),
    CompletedDate DATETIME NULL,
    CONSTRAINT CHK_FileCount CHECK (ReceivedFileCount <= ExpectedFileCount)
);
```

### 2. Catalog Files

Each file in the catalog tracks upload status.

```sql
CREATE TABLE dbo.CatalogFiles (
    CatalogFileID INT IDENTITY PRIMARY KEY,
    CatalogID INT FOREIGN KEY REFERENCES dbo.ModelCatalogs(CatalogID),
    FileName NVARCHAR(500) NOT NULL,
    FileRole NVARCHAR(50),                -- "weights", "config", "tokenizer", "index"
    ExpectedSizeBytes BIGINT,             -- From manifest
    ActualSizeBytes BIGINT NULL,          -- After upload
    SHA256Hash BINARY(32) NULL,           -- Integrity check
    IsReceived BIT DEFAULT 0,
    UploadDate DATETIME NULL,
    FileData VARBINARY(MAX) NULL,         -- Actual file content
    INDEX IX_CatalogID_FileName (CatalogID, FileName)
);
```

### 3. Catalog Validation

```sql
CREATE PROCEDURE dbo.sp_ValidateCatalog
    @catalogName NVARCHAR(255)
AS
BEGIN
    -- Check if all expected files have been received
    UPDATE dbo.ModelCatalogs
    SET IsComplete = CASE
        WHEN ReceivedFileCount = ExpectedFileCount THEN 1
        ELSE 0
    END,
    CompletedDate = CASE
        WHEN ReceivedFileCount = ExpectedFileCount THEN GETDATE()
        ELSE NULL
    END
    WHERE CatalogName = @catalogName;
    
    -- Return missing files
    SELECT cf.FileName, cf.FileRole
    FROM dbo.CatalogFiles cf
    INNER JOIN dbo.ModelCatalogs mc ON cf.CatalogID = mc.CatalogID
    WHERE mc.CatalogName = @catalogName
      AND cf.IsReceived = 0;
END;
GO
```

---

## Format-Specific Catalog Types

### Format 1: HuggingFace Sharded Models

**Manifest File**: `model.safetensors.index.json`

```json
{
  "metadata": {
    "total_size": 141032816640
  },
  "weight_map": {
    "model.layers.0.self_attn.q_proj.weight": "model-00001-of-00029.safetensors",
    "model.layers.0.self_attn.k_proj.weight": "model-00001-of-00029.safetensors",
    "model.layers.0.self_attn.v_proj.weight": "model-00001-of-00029.safetensors",
    "model.layers.1.self_attn.q_proj.weight": "model-00002-of-00029.safetensors",
    ...
  }
}
```

**Catalog Creation**:

```csharp
public void CreateHuggingFaceCatalog(string catalogName, byte[] indexJsonData)
{
    var index = JsonConvert.DeserializeObject<SafeTensorsIndex>(
        Encoding.UTF8.GetString(indexJsonData));
    
    // Extract unique shard file names
    var shardFiles = index.weight_map.Values.Distinct().ToList();
    
    using (var conn = new SqlConnection(connectionString))
    {
        conn.Open();
        
        // 1. Create catalog
        using (var cmd = new SqlCommand(@"
            INSERT INTO dbo.ModelCatalogs (CatalogName, ModelFormat, ExpectedFileCount)
            VALUES (@name, 'HuggingFace', @count);
            SELECT SCOPE_IDENTITY();", conn))
        {
            cmd.Parameters.AddWithValue("@name", catalogName);
            cmd.Parameters.AddWithValue("@count", shardFiles.Count + 3);  // shards + index + config + tokenizer
            int catalogId = Convert.ToInt32(cmd.ExecuteScalar());
            
            // 2. Register shard files
            foreach (var shard in shardFiles)
            {
                using (var insertCmd = new SqlCommand(@"
                    INSERT INTO dbo.CatalogFiles (CatalogID, FileName, FileRole)
                    VALUES (@catalogId, @fileName, 'weights');", conn))
                {
                    insertCmd.Parameters.AddWithValue("@catalogId", catalogId);
                    insertCmd.Parameters.AddWithValue("@fileName", shard);
                    insertCmd.ExecuteNonQuery();
                }
            }
            
            // 3. Register metadata files
            RegisterFile(conn, catalogId, "model.safetensors.index.json", "index");
            RegisterFile(conn, catalogId, "config.json", "config");
            RegisterFile(conn, catalogId, "tokenizer.json", "tokenizer");
        }
    }
}

public class SafeTensorsIndex
{
    public Dictionary<string, string> weight_map { get; set; }
    public IndexMetadata metadata { get; set; }
}

public class IndexMetadata
{
    public long total_size { get; set; }
}
```

**File Upload Handler**:

```csharp
public void UploadCatalogFile(string catalogName, string fileName, byte[] fileData)
{
    byte[] hash = SHA256.HashData(fileData);
    
    using (var conn = new SqlConnection(connectionString))
    {
        conn.Open();
        
        // Find the catalog file entry
        using (var cmd = new SqlCommand(@"
            UPDATE dbo.CatalogFiles
            SET FileData = @data,
                ActualSizeBytes = @size,
                SHA256Hash = @hash,
                IsReceived = 1,
                UploadDate = GETDATE()
            WHERE CatalogID = (SELECT CatalogID FROM dbo.ModelCatalogs WHERE CatalogName = @catalogName)
              AND FileName = @fileName;
            
            -- Increment received count
            UPDATE dbo.ModelCatalogs
            SET ReceivedFileCount = ReceivedFileCount + 1
            WHERE CatalogName = @catalogName;", conn))
        {
            cmd.Parameters.AddWithValue("@catalogName", catalogName);
            cmd.Parameters.AddWithValue("@fileName", fileName);
            cmd.Parameters.AddWithValue("@data", fileData);
            cmd.Parameters.AddWithValue("@size", fileData.Length);
            cmd.Parameters.AddWithValue("@hash", hash);
            cmd.ExecuteNonQuery();
        }
    }
}
```

---

### Format 2: Ollama Manifests

**Manifest File**: `manifest.json` (references external blobs)

```json
{
  "schemaVersion": 2,
  "mediaType": "application/vnd.ollama.image.manifest.v2+json",
  "config": {
    "mediaType": "application/vnd.ollama.image.config.v1+json",
    "digest": "sha256:8ab4849b038cf0abc5b1c9b8ee1443dca6b93a045c2272180d985126eb40bf6f",
    "size": 483
  },
  "layers": [
    {
      "mediaType": "application/vnd.ollama.image.model",
      "digest": "sha256:8934d96d3f08982e95922b2b7a2c626a1fe873d7c3b06e8e56d7bc0a1fef9246",
      "size": 3825819519
    },
    {
      "mediaType": "application/vnd.ollama.image.license",
      "digest": "sha256:097a36493f718248845233af1d3fefe7a303f864fae13bc31a3a9704229378ca",
      "size": 8433
    },
    {
      "mediaType": "application/vnd.ollama.image.template",
      "digest": "sha256:109037bec39c0becc8221222ae23557559bc594290945a2c4221ab4f303b8871",
      "size": 136
    },
    {
      "mediaType": "application/vnd.ollama.image.params",
      "digest": "sha256:22a838ceb7fb22755a3b0ae9b4eadde629d19be1f651f73efb8c6b4e2cd0eea0",
      "size": 84
    }
  ]
}
```

**Catalog Creation**:

```csharp
public void CreateOllamaCatalog(string catalogName, byte[] manifestData)
{
    var manifest = JsonConvert.DeserializeObject<OllamaManifest>(
        Encoding.UTF8.GetString(manifestData));
    
    int expectedFiles = 1 + manifest.layers.Count;  // manifest + all layers
    
    using (var conn = new SqlConnection(connectionString))
    {
        conn.Open();
        
        using (var cmd = new SqlCommand(@"
            INSERT INTO dbo.ModelCatalogs (CatalogName, ModelFormat, ExpectedFileCount)
            VALUES (@name, 'Ollama', @count);
            SELECT SCOPE_IDENTITY();", conn))
        {
            cmd.Parameters.AddWithValue("@name", catalogName);
            cmd.Parameters.AddWithValue("@count", expectedFiles);
            int catalogId = Convert.ToInt32(cmd.ExecuteScalar());
            
            // Register config
            RegisterFile(conn, catalogId, $"blobs/{manifest.config.digest}", "config",
                manifest.config.size);
            
            // Register layers
            foreach (var layer in manifest.layers)
            {
                string role = layer.mediaType.Contains("model") ? "weights" :
                              layer.mediaType.Contains("license") ? "license" :
                              layer.mediaType.Contains("template") ? "template" : "params";
                
                RegisterFile(conn, catalogId, $"blobs/{layer.digest}", role, layer.size);
            }
        }
    }
}

public class OllamaManifest
{
    public int schemaVersion { get; set; }
    public OllamaBlob config { get; set; }
    public List<OllamaLayer> layers { get; set; }
}

public class OllamaBlob
{
    public string mediaType { get; set; }
    public string digest { get; set; }
    public long size { get; set; }
}

public class OllamaLayer : OllamaBlob { }
```

---

### Format 3: Stable Diffusion Pipelines

**Manifest File**: `model_index.json`

```json
{
  "_class_name": "StableDiffusionPipeline",
  "_diffusers_version": "0.21.0",
  "feature_extractor": ["transformers", "CLIPImageProcessor"],
  "safety_checker": ["stable_diffusion", "StableDiffusionSafetyChecker"],
  "scheduler": ["diffusers", "PNDMScheduler"],
  "text_encoder": ["transformers", "CLIPTextModel"],
  "tokenizer": ["transformers", "CLIPTokenizer"],
  "unet": ["diffusers", "UNet2DConditionModel"],
  "vae": ["diffusers", "AutoencoderKL"]
}
```

**Expected Files**:
```text
stable-diffusion-v1-5/
├── model_index.json
├── text_encoder/
│   ├── config.json
│   └── pytorch_model.bin
├── unet/
│   ├── config.json
│   └── diffusion_pytorch_model.safetensors
├── vae/
│   ├── config.json
│   └── diffusion_pytorch_model.safetensors
├── safety_checker/
│   ├── config.json
│   └── pytorch_model.bin
└── scheduler/
    └── scheduler_config.json
```

**Catalog Creation**:

```csharp
public void CreateStableDiffusionCatalog(string catalogName, byte[] indexData)
{
    var index = JsonConvert.DeserializeObject<ModelIndex>(
        Encoding.UTF8.GetString(indexData));
    
    var requiredComponents = new[] {
        "text_encoder", "unet", "vae", "safety_checker", "scheduler"
    };
    
    // Each component has 1-2 files (config + optional weights)
    int expectedFiles = 1 + (requiredComponents.Length * 2);  // index + components
    
    using (var conn = new SqlConnection(connectionString))
    {
        conn.Open();
        
        using (var cmd = new SqlCommand(@"
            INSERT INTO dbo.ModelCatalogs (CatalogName, ModelFormat, ExpectedFileCount)
            VALUES (@name, 'StableDiffusion', @count);
            SELECT SCOPE_IDENTITY();", conn))
        {
            cmd.Parameters.AddWithValue("@name", catalogName);
            cmd.Parameters.AddWithValue("@count", expectedFiles);
            int catalogId = Convert.ToInt32(cmd.ExecuteScalar());
            
            // Register index
            RegisterFile(conn, catalogId, "model_index.json", "index");
            
            // Register component files
            foreach (var component in requiredComponents)
            {
                RegisterFile(conn, catalogId, $"{component}/config.json", "config");
                
                if (component != "scheduler")
                {
                    string weightsFile = component == "unet" || component == "vae"
                        ? "diffusion_pytorch_model.safetensors"
                        : "pytorch_model.bin";
                    
                    RegisterFile(conn, catalogId, $"{component}/{weightsFile}", "weights");
                }
            }
        }
    }
}
```

---

## Missing File Detection

### Automated Detection Procedure

```sql
CREATE PROCEDURE dbo.sp_DetectMissingFiles
    @catalogName NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @catalogID INT;
    DECLARE @isComplete BIT;
    
    SELECT @catalogID = CatalogID, @isComplete = IsComplete
    FROM dbo.ModelCatalogs
    WHERE CatalogName = @catalogName;
    
    IF @isComplete = 1
    BEGIN
        PRINT 'Catalog is complete. No missing files.';
        RETURN;
    END
    
    -- List missing files
    SELECT
        cf.FileName,
        cf.FileRole,
        cf.ExpectedSizeBytes,
        DATEDIFF(HOUR, mc.CreatedDate, GETDATE()) AS HoursSinceCreation
    FROM dbo.CatalogFiles cf
    INNER JOIN dbo.ModelCatalogs mc ON cf.CatalogID = mc.CatalogID
    WHERE cf.CatalogID = @catalogID
      AND cf.IsReceived = 0
    ORDER BY cf.FileRole, cf.FileName;
    
    -- Summary statistics
    SELECT
        mc.CatalogName,
        mc.ReceivedFileCount,
        mc.ExpectedFileCount,
        (mc.ReceivedFileCount * 100.0 / mc.ExpectedFileCount) AS PercentComplete,
        DATEDIFF(HOUR, mc.CreatedDate, GETDATE()) AS HoursSinceCreation
    FROM dbo.ModelCatalogs mc
    WHERE mc.CatalogID = @catalogID;
END;
GO
```

**Example Output**:

```text
FileName                                FileRole    ExpectedSizeBytes    HoursSinceCreation
--------------------------------------- ----------- -------------------- ------------------
model-00027-of-00029.safetensors        weights     4981678080           12
model-00029-of-00029.safetensors        weights     1523456789           12

CatalogName           ReceivedFileCount    ExpectedFileCount    PercentComplete    HoursSinceCreation
--------------------- -------------------- -------------------- ------------------ ------------------
llama-2-70b-hf        31                   33                   93.94%             12
```

---

## Integrity Validation

### SHA-256 Hash Verification

```sql
CREATE PROCEDURE dbo.sp_VerifyFileIntegrity
    @catalogName NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Check if actual file sizes match expected sizes
    SELECT
        cf.FileName,
        cf.ExpectedSizeBytes,
        cf.ActualSizeBytes,
        CASE
            WHEN cf.ExpectedSizeBytes IS NULL THEN 'No expected size'
            WHEN cf.ActualSizeBytes = cf.ExpectedSizeBytes THEN '✅ Match'
            ELSE '❌ Mismatch'
        END AS SizeCheck,
        cf.SHA256Hash
    FROM dbo.CatalogFiles cf
    INNER JOIN dbo.ModelCatalogs mc ON cf.CatalogID = mc.CatalogID
    WHERE mc.CatalogName = @catalogName
      AND cf.IsReceived = 1
    ORDER BY cf.FileName;
END;
GO
```

**Usage**:

```sql
EXEC dbo.sp_VerifyFileIntegrity @catalogName = 'llama-2-70b-hf';
```

---

## Atomization Trigger

Once a catalog is complete, trigger atomization:

```sql
CREATE TRIGGER trg_CatalogComplete
ON dbo.ModelCatalogs
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Check if IsComplete changed from 0 to 1
    IF UPDATE(IsComplete)
    BEGIN
        DECLARE @catalogID INT;
        DECLARE @catalogName NVARCHAR(255);
        
        SELECT @catalogID = i.CatalogID, @catalogName = i.CatalogName
        FROM inserted i
        INNER JOIN deleted d ON i.CatalogID = d.CatalogID
        WHERE i.IsComplete = 1 AND d.IsComplete = 0;
        
        IF @catalogID IS NOT NULL
        BEGIN
            -- Queue atomization message
            DECLARE @messageBody NVARCHAR(MAX) = JSON_OBJECT(
                'action': 'atomize_catalog',
                'catalogName': @catalogName,
                'catalogID': @catalogID,
                'timestamp': GETDATE()
            );
            
            EXEC dbo.sp_SendServiceBrokerMessage
                @queueName = 'AnalyzeQueue',
                @messageBody = @messageBody;
            
            PRINT 'Atomization queued for catalog: ' + @catalogName;
        END
    END
END;
GO
```

---

## API Endpoints

### RESTful Catalog API

```csharp
[ApiController]
[Route("api/catalogs")]
public class CatalogsController : ControllerBase
{
    [HttpPost("{catalogName}/files/{fileName}")]
    public async Task<IActionResult> UploadFile(
        string catalogName,
        string fileName,
        IFormFile file)
    {
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        byte[] fileData = stream.ToArray();
        
        catalogManager.UploadCatalogFile(catalogName, fileName, fileData);
        
        // Check if catalog is now complete
        var catalog = catalogManager.GetCatalog(catalogName);
        
        if (catalog.IsComplete)
        {
            return Ok(new {
                message = $"File uploaded. Catalog '{catalogName}' is now complete!",
                completedDate = catalog.CompletedDate
            });
        }
        else
        {
            return Ok(new {
                message = $"File uploaded. {catalog.ReceivedFileCount}/{catalog.ExpectedFileCount} files received.",
                percentComplete = (catalog.ReceivedFileCount * 100.0 / catalog.ExpectedFileCount)
            });
        }
    }
    
    [HttpGet("{catalogName}/status")]
    public IActionResult GetCatalogStatus(string catalogName)
    {
        var catalog = catalogManager.GetCatalog(catalogName);
        var missingFiles = catalogManager.GetMissingFiles(catalogName);
        
        return Ok(new {
            catalogName = catalog.CatalogName,
            format = catalog.ModelFormat,
            isComplete = catalog.IsComplete,
            receivedFiles = catalog.ReceivedFileCount,
            expectedFiles = catalog.ExpectedFileCount,
            percentComplete = (catalog.ReceivedFileCount * 100.0 / catalog.ExpectedFileCount),
            missingFiles = missingFiles.Select(f => f.FileName).ToArray()
        });
    }
}
```

---

## Cross-References

- **Related**: [Model Parsers](model-parsers.md) - Parsing individual catalog files
- **Related**: [Model Atomization](model-atomization.md) - Atomizing complete catalogs
- **Related**: [Archive Handler](archive-handler.md) - Extracting TAR/ZIP catalogs

---

## Performance Metrics

- **HuggingFace 70B**: 29 shards + 4 metadata files = 33 files tracked
- **Ollama llama2**: 1 manifest + 4 blobs = 5 files tracked
- **Stable Diffusion**: 1 index + 9 component files = 10 files tracked

**Result**: Robust multi-file model coordination with automatic validation and atomization triggering.
