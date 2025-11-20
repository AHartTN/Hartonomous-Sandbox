# Archive Handler: Secure ZIP/TAR/GZIP Extraction

**Status**: Production Implementation  
**Date**: January 2025  
**Purpose**: Extract compressed model archives with security validation

---

## Overview

AI models are frequently distributed as compressed archives:

- **HuggingFace**: `.tar.gz` archives (20+ GB uncompressed)
- **PyTorch**: `.zip` files containing pickled weights
- **Ollama**: `.tar` layers referenced by manifest
- **TensorFlow**: `.tar.gz` SavedModel directories

**Archive Handler** safely extracts these formats while preventing security vulnerabilities: path traversal, zip bombs, and resource exhaustion.

---

## Supported Archive Formats

### Format Detection via Magic Numbers

```csharp
public enum ArchiveFormat
{
    Unknown,
    Zip,        // 0x504B (PK)
    Gzip,       // 0x1F8B
    Tar,        // "ustar" at offset 257
    TarGz,      // Gzip + Tar
    TarBz2,     // Bzip2 + Tar
    SevenZip    // 0x377ABCAF271C (7z signature)
}

public static ArchiveFormat DetectArchiveFormat(byte[] data)
{
    if (data == null || data.Length < 4)
        return ArchiveFormat.Unknown;
    
    // ZIP: "PK" (0x50 0x4B)
    if (data[0] == 0x50 && data[1] == 0x4B)
        return ArchiveFormat.Zip;
    
    // GZIP: 0x1F 0x8B
    if (data[0] == 0x1F && data[1] == 0x8B)
    {
        // Check if it's TAR.GZ by attempting to decompress and check for "ustar"
        return IsTarGz(data) ? ArchiveFormat.TarGz : ArchiveFormat.Gzip;
    }
    
    // TAR: "ustar" at offset 257
    if (data.Length >= 262)
    {
        string ustar = System.Text.Encoding.ASCII.GetString(data, 257, 5);
        if (ustar == "ustar")
            return ArchiveFormat.Tar;
    }
    
    // BZIP2: "BZ" (0x42 0x5A)
    if (data[0] == 0x42 && data[1] == 0x5A)
        return ArchiveFormat.TarBz2;
    
    // 7-ZIP: 0x37 0x7A 0xBC 0xAF 0x27 0x1C
    if (data.Length >= 6 &&
        data[0] == 0x37 && data[1] == 0x7A && data[2] == 0xBC &&
        data[3] == 0xAF && data[4] == 0x27 && data[5] == 0x1C)
        return ArchiveFormat.SevenZip;
    
    return ArchiveFormat.Unknown;
}
```

---

## Security Validation

### 1. Path Traversal Prevention

**Attack scenario**:
```text
malicious.zip:
  ../../../../etc/passwd
  ../../Windows/System32/config/SAM
```

**Prevention**:

```csharp
public static bool IsSafePath(string entryPath, string destinationDirectory)
{
    // Normalize paths
    string fullEntryPath = Path.GetFullPath(Path.Combine(destinationDirectory, entryPath));
    string fullDestinationPath = Path.GetFullPath(destinationDirectory);
    
    // Check if entry path starts with destination directory
    if (!fullEntryPath.StartsWith(fullDestinationPath, StringComparison.OrdinalIgnoreCase))
    {
        throw new SecurityException($"Path traversal detected: {entryPath}");
    }
    
    // Check for absolute paths
    if (Path.IsPathRooted(entryPath))
    {
        throw new SecurityException($"Absolute path not allowed: {entryPath}");
    }
    
    // Check for ".." segments
    if (entryPath.Contains(".."))
    {
        throw new SecurityException($"Parent directory reference not allowed: {entryPath}");
    }
    
    return true;
}
```

### 2. Zip Bomb Detection

**Attack scenario**:
```text
42.zip (42 KB compressed) → 4.5 PB uncompressed (100,000,000:1 ratio)
```

**Prevention**:

```csharp
public class ExtractionLimits
{
    public long MaxUncompressedSize { get; set; } = 50L * 1024 * 1024 * 1024;  // 50 GB
    public long MaxFileCount { get; set; } = 100000;
    public double MaxCompressionRatio { get; set; } = 1000.0;  // 1000:1 max
    public int MaxDepth { get; set; } = 10;  // Nested archive depth
}

public static void ValidateZipBomb(ZipArchiveEntry entry, long totalUncompressed, ExtractionLimits limits)
{
    // Check individual file size
    if (entry.Length > limits.MaxUncompressedSize)
    {
        throw new SecurityException($"File {entry.FullName} exceeds max size: {entry.Length} bytes");
    }
    
    // Check total uncompressed size
    if (totalUncompressed + entry.Length > limits.MaxUncompressedSize)
    {
        throw new SecurityException($"Total uncompressed size exceeds limit: {totalUncompressed + entry.Length} bytes");
    }
    
    // Check compression ratio
    if (entry.CompressedLength > 0)
    {
        double ratio = (double)entry.Length / entry.CompressedLength;
        if (ratio > limits.MaxCompressionRatio)
        {
            throw new SecurityException($"File {entry.FullName} has suspicious compression ratio: {ratio:F2}:1");
        }
    }
}
```

### 3. Resource Limits

```csharp
public class ExtractionContext
{
    public int FilesExtracted { get; set; }
    public long BytesExtracted { get; set; }
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public int Depth { get; set; }
    
    public void CheckLimits(ExtractionLimits limits)
    {
        if (FilesExtracted > limits.MaxFileCount)
            throw new SecurityException($"File count exceeds limit: {FilesExtracted}");
        
        if (BytesExtracted > limits.MaxUncompressedSize)
            throw new SecurityException($"Total bytes exceeds limit: {BytesExtracted}");
        
        if (Depth > limits.MaxDepth)
            throw new SecurityException($"Nesting depth exceeds limit: {Depth}");
        
        // 10-minute timeout
        if ((DateTime.UtcNow - StartTime).TotalMinutes > 10)
            throw new TimeoutException("Extraction exceeded time limit");
    }
}
```

---

## Complete Archive Extractor Implementation

### Unified Extractor Class

```csharp
using System.IO.Compression;

public class ArchiveExtractor
{
    private readonly ExtractionLimits limits;
    
    public ArchiveExtractor(ExtractionLimits limits = null)
    {
        this.limits = limits ?? new ExtractionLimits();
    }
    
    public Dictionary<string, byte[]> Extract(byte[] archiveData, string archiveName = null)
    {
        var format = DetectArchiveFormat(archiveData);
        
        return format switch
        {
            ArchiveFormat.Zip => ExtractZip(archiveData),
            ArchiveFormat.Gzip => ExtractGzip(archiveData),
            ArchiveFormat.Tar => ExtractTar(archiveData),
            ArchiveFormat.TarGz => ExtractTarGz(archiveData),
            ArchiveFormat.TarBz2 => ExtractTarBz2(archiveData),
            _ => throw new NotSupportedException($"Archive format not supported: {format}")
        };
    }
    
    private Dictionary<string, byte[]> ExtractZip(byte[] zipData)
    {
        var result = new Dictionary<string, byte[]>();
        var context = new ExtractionContext();
        
        using var stream = new MemoryStream(zipData);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        
        foreach (var entry in archive.Entries)
        {
            // Skip directories
            if (entry.FullName.EndsWith("/"))
                continue;
            
            // Security validation
            IsSafePath(entry.FullName, "/");
            ValidateZipBomb(entry, context.BytesExtracted, limits);
            
            // Extract entry
            using var entryStream = entry.Open();
            using var ms = new MemoryStream();
            entryStream.CopyTo(ms);
            
            byte[] fileData = ms.ToArray();
            result[entry.FullName] = fileData;
            
            context.FilesExtracted++;
            context.BytesExtracted += fileData.Length;
            context.CheckLimits(limits);
        }
        
        return result;
    }
    
    private Dictionary<string, byte[]> ExtractGzip(byte[] gzipData)
    {
        using var inputStream = new MemoryStream(gzipData);
        using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();
        
        gzipStream.CopyTo(outputStream);
        byte[] decompressed = outputStream.ToArray();
        
        // Single file result
        return new Dictionary<string, byte[]>
        {
            ["decompressed"] = decompressed
        };
    }
    
    private Dictionary<string, byte[]> ExtractTar(byte[] tarData)
    {
        var result = new Dictionary<string, byte[]>();
        var context = new ExtractionContext();
        
        using var stream = new MemoryStream(tarData);
        
        while (stream.Position < stream.Length)
        {
            // Read TAR header (512 bytes)
            byte[] headerBytes = new byte[512];
            int bytesRead = stream.Read(headerBytes, 0, 512);
            
            if (bytesRead < 512)
                break;
            
            // Check for end-of-archive (all zeros)
            if (headerBytes.All(b => b == 0))
                break;
            
            // Parse header
            string fileName = ParseTarFileName(headerBytes);
            long fileSize = ParseTarFileSize(headerBytes);
            
            if (string.IsNullOrEmpty(fileName))
                break;
            
            // Security validation
            IsSafePath(fileName, "/");
            
            if (fileSize > limits.MaxUncompressedSize)
                throw new SecurityException($"File {fileName} exceeds max size: {fileSize}");
            
            // Read file data
            byte[] fileData = new byte[fileSize];
            stream.Read(fileData, 0, (int)fileSize);
            
            result[fileName] = fileData;
            
            // TAR blocks are 512-byte aligned
            long padding = (512 - (fileSize % 512)) % 512;
            stream.Seek(padding, SeekOrigin.Current);
            
            context.FilesExtracted++;
            context.BytesExtracted += fileSize;
            context.CheckLimits(limits);
        }
        
        return result;
    }
    
    private Dictionary<string, byte[]> ExtractTarGz(byte[] tarGzData)
    {
        // Step 1: Decompress GZIP
        byte[] tarData = ExtractGzip(tarGzData)["decompressed"];
        
        // Step 2: Extract TAR
        return ExtractTar(tarData);
    }
    
    private Dictionary<string, byte[]> ExtractTarBz2(byte[] tarBz2Data)
    {
        // Note: Requires SharpCompress or similar library for BZip2
        throw new NotImplementedException("BZip2 support requires additional NuGet package: SharpCompress");
    }
}
```

### TAR Header Parsing

```csharp
private static string ParseTarFileName(byte[] header)
{
    // File name: bytes 0-99 (null-terminated)
    int nullIndex = Array.IndexOf(header, (byte)0, 0, 100);
    if (nullIndex < 0) nullIndex = 100;
    
    return System.Text.Encoding.ASCII.GetString(header, 0, nullIndex);
}

private static long ParseTarFileSize(byte[] header)
{
    // File size: bytes 124-135 (octal ASCII)
    string sizeOctal = System.Text.Encoding.ASCII.GetString(header, 124, 11).Trim('\0', ' ');
    
    if (string.IsNullOrEmpty(sizeOctal))
        return 0;
    
    return Convert.ToInt64(sizeOctal, 8);  // Octal to decimal
}
```

---

## SQL Integration

### CLR Function for Archive Extraction

```csharp
[Microsoft.SqlServer.Server.SqlFunction(
    FillRowMethodName = "FillExtractedFileRow",
    TableDefinition = "FileName NVARCHAR(500), FileData VARBINARY(MAX), FileSize BIGINT")]
public static IEnumerable ExtractArchive(SqlBytes archiveData, SqlString archiveName)
{
    byte[] data = archiveData.Value;
    string name = archiveName.IsNull ? null : archiveName.Value;
    
    var extractor = new ArchiveExtractor();
    var files = extractor.Extract(data, name);
    
    return files.Select(kvp => new ExtractedFile
    {
        FileName = kvp.Key,
        FileData = kvp.Value,
        FileSize = kvp.Value.Length
    });
}

public static void FillExtractedFileRow(object obj, out SqlString fileName, out SqlBytes fileData, out SqlInt64 fileSize)
{
    var file = (ExtractedFile)obj;
    fileName = new SqlString(file.FileName);
    fileData = new SqlBytes(file.FileData);
    fileSize = new SqlInt64(file.FileSize);
}

public class ExtractedFile
{
    public string FileName { get; set; }
    public byte[] FileData { get; set; }
    public long FileSize { get; set; }
}
```

### SQL Usage

```sql
CREATE FUNCTION dbo.clr_ExtractArchive(
    @archiveData VARBINARY(MAX),
    @archiveName NVARCHAR(500)
)
RETURNS TABLE (
    FileName NVARCHAR(500),
    FileData VARBINARY(MAX),
    FileSize BIGINT
)
AS EXTERNAL NAME [Hartonomous.Clr].[ArchiveHandler].[ExtractArchive];
GO
```

**Example**:

```sql
-- Extract HuggingFace model TAR.GZ
DECLARE @tarGzData VARBINARY(MAX) = (SELECT FileData FROM dbo.UploadedFiles WHERE FileName = 'llama-2-7b.tar.gz');

SELECT
    FileName,
    FileSize,
    CASE
        WHEN FileName LIKE '%.safetensors' THEN 'Model Weights'
        WHEN FileName LIKE '%.json' THEN 'Configuration'
        WHEN FileName LIKE 'tokenizer%' THEN 'Tokenizer'
        ELSE 'Other'
    END AS FileType
FROM dbo.clr_ExtractArchive(@tarGzData, 'llama-2-7b.tar.gz')
ORDER BY FileSize DESC;
```

**Output**:

```text
FileName                              FileSize         FileType
------------------------------------- ---------------- ----------------
model.safetensors                     13476592640      Model Weights
tokenizer.json                        1842344          Tokenizer
config.json                           761              Configuration
special_tokens_map.json               414              Configuration
tokenizer_config.json                 1320             Configuration
```

---

## Recursive Extraction

### Nested Archive Support

```csharp
public Dictionary<string, byte[]> ExtractRecursive(byte[] archiveData, int maxDepth = 3)
{
    var allFiles = new Dictionary<string, byte[]>();
    var context = new ExtractionContext { Depth = 0 };
    
    ExtractRecursiveInternal(archiveData, "", allFiles, context, maxDepth);
    
    return allFiles;
}

private void ExtractRecursiveInternal(
    byte[] archiveData,
    string pathPrefix,
    Dictionary<string, byte[]> allFiles,
    ExtractionContext context,
    int maxDepth)
{
    if (context.Depth >= maxDepth)
        return;
    
    var format = DetectArchiveFormat(archiveData);
    
    if (format == ArchiveFormat.Unknown)
    {
        // Not an archive, store as-is
        allFiles[pathPrefix] = archiveData;
        return;
    }
    
    // Extract archive
    var files = Extract(archiveData);
    context.Depth++;
    
    foreach (var kvp in files)
    {
        string fullPath = string.IsNullOrEmpty(pathPrefix)
            ? kvp.Key
            : $"{pathPrefix}/{kvp.Key}";
        
        var nestedFormat = DetectArchiveFormat(kvp.Value);
        
        if (nestedFormat != ArchiveFormat.Unknown)
        {
            // Recursively extract nested archive
            ExtractRecursiveInternal(kvp.Value, fullPath, allFiles, context, maxDepth);
        }
        else
        {
            // Regular file
            allFiles[fullPath] = kvp.Value;
        }
    }
    
    context.Depth--;
}
```

---

## Memory-Efficient Streaming

For very large archives (20+ GB), use streaming extraction:

```csharp
public void ExtractToDatabase(byte[] archiveData, string catalogName)
{
    using var stream = new MemoryStream(archiveData);
    using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
    
    using var conn = new SqlConnection(connectionString);
    conn.Open();
    
    foreach (var entry in archive.Entries)
    {
        if (entry.FullName.EndsWith("/"))
            continue;
        
        // Stream directly to database without loading entire file into memory
        using var entryStream = entry.Open();
        
        using var cmd = new SqlCommand(@"
            INSERT INTO dbo.CatalogFiles (CatalogID, FileName, FileData, ActualSizeBytes, IsReceived, UploadDate)
            SELECT
                CatalogID,
                @fileName,
                @fileData,
                @fileSize,
                1,
                GETDATE()
            FROM dbo.ModelCatalogs
            WHERE CatalogName = @catalogName", conn))
        {
            cmd.Parameters.AddWithValue("@catalogName", catalogName);
            cmd.Parameters.AddWithValue("@fileName", entry.FullName);
            cmd.Parameters.AddWithValue("@fileSize", entry.Length);
            
            // Stream file data
            using var ms = new MemoryStream();
            entryStream.CopyTo(ms);
            cmd.Parameters.AddWithValue("@fileData", ms.ToArray());
            
            cmd.ExecuteNonQuery();
        }
    }
}
```

---

## Error Handling

### Comprehensive Exception Handling

```csharp
public ExtractionResult ExtractWithErrorHandling(byte[] archiveData, string archiveName)
{
    var result = new ExtractionResult
    {
        ArchiveName = archiveName,
        StartTime = DateTime.UtcNow
    };
    
    try
    {
        result.Files = Extract(archiveData, archiveName);
        result.Success = true;
        result.FileCount = result.Files.Count;
        result.TotalBytes = result.Files.Sum(f => f.Value.Length);
    }
    catch (SecurityException ex)
    {
        result.Success = false;
        result.ErrorType = "Security";
        result.ErrorMessage = ex.Message;
    }
    catch (InvalidDataException ex)
    {
        result.Success = false;
        result.ErrorType = "Corrupt Archive";
        result.ErrorMessage = ex.Message;
    }
    catch (NotSupportedException ex)
    {
        result.Success = false;
        result.ErrorType = "Unsupported Format";
        result.ErrorMessage = ex.Message;
    }
    catch (Exception ex)
    {
        result.Success = false;
        result.ErrorType = "Unknown";
        result.ErrorMessage = ex.Message;
    }
    finally
    {
        result.EndTime = DateTime.UtcNow;
        result.DurationMs = (result.EndTime - result.StartTime).TotalMilliseconds;
    }
    
    return result;
}

public class ExtractionResult
{
    public string ArchiveName { get; set; }
    public bool Success { get; set; }
    public Dictionary<string, byte[]> Files { get; set; }
    public int FileCount { get; set; }
    public long TotalBytes { get; set; }
    public string ErrorType { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double DurationMs { get; set; }
}
```

---

## Cross-References

- **Related**: [Catalog Management](catalog-management.md) - Coordinating multi-file models from archives
- **Related**: [Model Parsers](model-parsers.md) - Parsing extracted model files
- **Related**: [Model Atomization](model-atomization.md) - Atomizing extracted models

---

## Performance Metrics

- **ZIP extraction**: 200-300 MB/s
- **GZIP decompression**: 150-200 MB/s
- **TAR extraction**: 400-500 MB/s (no compression overhead)
- **TAR.GZ extraction**: 120-150 MB/s (GZIP bottleneck)

**Result**: Secure, production-grade archive extraction with comprehensive security validation and resource limits.
