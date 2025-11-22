# Atomization Pattern Update Guide

**Audience**: Developers updating existing atomizers  
**Goal**: Migrate to enhanced BaseAtomizer pattern for proper Atom/AtomRelation compliance

---

## The Old Pattern (DO NOT USE)

```csharp
// ? OLD: Manual atom creation with truncation risk
public class OldAtomizer : BaseAtomizer<byte[]>
{
    protected override async Task AtomizeCoreAsync(...)
    {
        // Create file atom (manual)
        var fileMetadata = Encoding.UTF8.GetBytes($"{source.FileName}:{source.ContentType}");
        var fileHash = CreateContentHash(fileMetadata);
        
        var fileAtom = new AtomData
        {
            AtomicValue = fileMetadata.Take(64).ToArray(),  // ? TRUNCATES!
            ContentHash = fileHash,
            Modality = "document",
            Subtype = "file-metadata",
            CanonicalText = source.FileName
        };
        
        atoms.Add(fileAtom);
        
        // Create content atoms (manual)
        var chunks = SplitIntoChunks(input);
        foreach (var chunk in chunks)
        {
            var chunkHash = CreateContentHash(chunk);
            var chunkAtom = new AtomData
            {
                AtomicValue = chunk.Take(64).ToArray(),  // ? TRUNCATES!
                ContentHash = chunkHash,
                Modality = "text",
                Subtype = "chunk"
            };
            
            atoms.Add(chunkAtom);
            
            // ? NO AtomComposition created!
        }
    }
}
```

**Problems**:
1. Truncates content > 64 bytes (data loss)
2. No AtomComposition/AtomRelation created
3. Duplicated logic across atomizers (DRY violation)
4. No spatial metadata support

---

## The New Pattern (USE THIS)

```csharp
// ? NEW: Use BaseAtomizer helpers for compliance
public class NewAtomizer : BaseAtomizer<byte[]>
{
    protected override async Task AtomizeCoreAsync(
        byte[] input,
        SourceMetadata source,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        // Step 1: Create file metadata atom using helper
        var fileHash = CreateFileMetadataAtom(input, source, atoms);
        
        // Step 2: Create content atoms using helper
        var chunks = SplitIntoChunks(input);
        
        foreach (var (chunk, index) in chunks.Select((c, i) => (c, i)))
        {
            var chunkText = Encoding.UTF8.GetString(chunk);
            
            // Create content atom (handles overflow automatically)
            var chunkHash = CreateContentAtom(
                content: chunk,
                modality: "text",
                subtype: "chunk",
                canonicalText: chunkText,
                metadata: System.Text.Json.JsonSerializer.Serialize(new
                {
                    chunkIndex = index,
                    chunkSize = chunk.Length,
                    chunkType = "paragraph"
                }),
                atoms: atoms
            );
            
            // Step 3: Create AtomRelation linking file to chunk
            CreateAtomRelation(
                parentHash: fileHash,
                childHash: chunkHash,
                relationType: "parent-child",
                compositions: compositions,
                sequenceIndex: index,
                weight: 1.0f,
                importance: ComputeImportance(chunk),  // Optional
                spatialMetadata: new Dictionary<string, object>
                {
                    ["chunkPosition"] = index,
                    ["chunkLength"] = chunk.Length
                }
            );
        }
    }
    
    // ...implement abstract methods...
}
```

**Benefits**:
1. ? No data loss (overflow preserved in CanonicalText)
2. ? AtomRelation automatically created
3. ? Consistent pattern (DRY compliance)
4. ? Spatial metadata support built-in

---

## Method Reference

### CreateFileMetadataAtom()

Creates a file-level metadata atom with automatic overflow handling.

```csharp
protected byte[] CreateFileMetadataAtom(
    TInput input,
    SourceMetadata source,
    List<AtomData> atoms)
```

**Returns**: ContentHash of created atom (use for AtomRelation parent)

**Behavior**:
- Calls `GetFileMetadataBytes()` to get file metadata
- If ? 64 bytes: Stores in AtomicValue
- If > 64 bytes: Stores fingerprint in AtomicValue, full content in CanonicalText
- Adds overflow metadata automatically

**Example**:
```csharp
var fileHash = CreateFileMetadataAtom(input, source, atoms);
// Use fileHash as parent in AtomRelations
```

---

### CreateContentAtom()

Creates a content atom with automatic overflow handling.

```csharp
protected byte[] CreateContentAtom(
    byte[] content,
    string modality,
    string? subtype,
    string? canonicalText,
    string? metadata,
    List<AtomData> atoms)
```

**Parameters**:
- `content`: Raw bytes of the atom content
- `modality`: "text", "code", "image", "weight", "embedding", etc.
- `subtype`: "chunk", "token", "dimension", "function", "class", etc.
- `canonicalText`: Human-readable text representation (or null to auto-generate Base64)
- `metadata`: JSON metadata string (or null)
- `atoms`: List to add the atom to

**Returns**: ContentHash of created atom (use for AtomRelation child)

**Behavior**:
- If content ? 64 bytes: Stores in AtomicValue
- If content > 64 bytes: 
  - Stores fingerprint in AtomicValue (SHA256 + first 32 bytes)
  - Stores full content in CanonicalText
  - If canonicalText is null, stores content as Base64
  - Adds overflow metadata with size and encoding info

**Example**:
```csharp
var chunkHash = CreateContentAtom(
    content: chunkBytes,
    modality: "text",
    subtype: "chunk",
    canonicalText: chunkText,
    metadata: "{\"index\":0}",
    atoms: atoms
);
```

---

### CreateAtomRelation()

Creates an AtomComposition linking parent and child atoms.

```csharp
protected void CreateAtomRelation(
    byte[] parentHash,
    byte[] childHash,
    string relationType,
    List<AtomComposition> compositions,
    int? sequenceIndex = null,
    float? weight = null,
    float? importance = null,
    Dictionary<string, object>? spatialMetadata = null)
```

**Parameters**:
- `parentHash`: ContentHash of parent atom
- `childHash`: ContentHash of child atom
- `relationType`: "parent-child", "sequence", "embedding", "reference", etc.
- `compositions`: List to add the composition to
- `sequenceIndex`: Position in sequence (for ordered relationships)
- `weight`: Relationship strength (0.0 to 1.0)
- `importance`: Importance score (for weighting in retrieval)
- `spatialMetadata`: Dictionary with spatial fields (converted to JSON)

**Spatial Metadata Keys**:
```csharp
spatialMetadata: new Dictionary<string, object>
{
    // Hilbert curve bucket (computed from 3D coordinates)
    ["hilbertBucket"] = 123456789L,
    
    // 3D bucket coordinates
    ["bucketX"] = 42,
    ["bucketY"] = 17,
    ["bucketZ"] = 89,
    
    // 3D spatial coordinates (from landmark projection)
    ["coordX"] = 0.123,
    ["coordY"] = -0.456,
    ["coordZ"] = 0.789,
    
    // Optional 4D/5D coordinates
    ["coordT"] = 1.0,  // Temporal dimension
    ["coordW"] = 0.0,  // 5th dimension
    
    // Custom metadata
    ["chunkPosition"] = 5,
    ["chunkLength"] = 1024
}
```

**Example**:
```csharp
CreateAtomRelation(
    parentHash: fileHash,
    childHash: chunkHash,
    relationType: "parent-child",
    compositions: compositions,
    sequenceIndex: 0,
    weight: 1.0f,
    importance: 0.8f,
    spatialMetadata: new Dictionary<string, object>
    {
        ["chunkIndex"] = 0,
        ["chunkSize"] = 512
    }
);
```

---

## Migration Checklist

When updating an existing atomizer:

### 1. Replace Manual Atom Creation

**Before**:
```csharp
var atom = new AtomData
{
    AtomicValue = content.Take(64).ToArray(),
    ContentHash = CreateContentHash(content),
    Modality = "text",
    Subtype = "chunk"
};
atoms.Add(atom);
```

**After**:
```csharp
var hash = CreateContentAtom(
    content: content,
    modality: "text",
    subtype: "chunk",
    canonicalText: textContent,
    metadata: null,
    atoms: atoms
);
```

### 2. Add AtomRelation Creation

**Add after creating child atoms**:
```csharp
CreateAtomRelation(
    parentHash: fileHash,
    childHash: contentHash,
    relationType: "parent-child",
    compositions: compositions,
    sequenceIndex: index
);
```

### 3. Implement Required Abstract Methods

```csharp
protected override string GetModality() 
    => "text";  // or "code", "image", etc.

protected override byte[] GetFileMetadataBytes(byte[] input, SourceMetadata source)
    => Encoding.UTF8.GetBytes($"{source.FileName}:{source.ContentType}:{input.Length}");

protected override string GetCanonicalFileText(byte[] input, SourceMetadata source)
    => $"File: {source.FileName} ({input.Length} bytes)";

protected override string GetFileMetadataJson(byte[] input, SourceMetadata source)
    => System.Text.Json.JsonSerializer.Serialize(new
    {
        fileName = source.FileName,
        contentType = source.ContentType,
        size = input.Length
    });

protected override string GetDetectedFormat()
    => "UTF-8 Text";  // Describe detected format
```

### 4. Test the Changes

```csharp
[Fact]
public async Task AtomizeAsync_LargeContent_PreservesOverflow()
{
    // Arrange
    var input = new byte[1024];  // > 64 bytes
    var source = new SourceMetadata { FileName = "test.txt" };
    var atomizer = new MyAtomizer(logger);
    
    // Act
    var result = await atomizer.AtomizeAsync(input, source, CancellationToken.None);
    
    // Assert
    var atom = result.Atoms.First();
    Assert.Equal(64, atom.AtomicValue.Length);  // Fingerprint
    Assert.NotNull(atom.CanonicalText);         // Full content preserved
    
    var metadata = System.Text.Json.JsonDocument.Parse(atom.Metadata!);
    Assert.True(metadata.RootElement.GetProperty("overflow").GetBoolean());
    Assert.Equal(1024, metadata.RootElement.GetProperty("originalSize").GetInt32());
}

[Fact]
public async Task AtomizeAsync_CreatesRelations()
{
    // Arrange
    var input = Encoding.UTF8.GetBytes("Hello World");
    var source = new SourceMetadata { FileName = "test.txt" };
    var atomizer = new MyAtomizer(logger);
    
    // Act
    var result = await atomizer.AtomizeAsync(input, source, CancellationToken.None);
    
    // Assert
    Assert.True(result.Compositions.Count > 0);
    Assert.Equal("parent-child", result.Compositions[0].RelationType);
    Assert.NotNull(result.Compositions[0].SequenceIndex);
}
```

---

## Example: Complete Atomizer Migration

### Before (Old Pattern)

```csharp
public class DocumentAtomizer : BaseAtomizer<byte[]>
{
    protected override async Task AtomizeCoreAsync(
        byte[] input,
        SourceMetadata source,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var text = Encoding.UTF8.GetString(input);
        
        // Manual file atom
        var fileAtom = new AtomData
        {
            AtomicValue = input.Take(64).ToArray(),  // ? Truncates
            ContentHash = CreateContentHash(input),
            Modality = "document",
            Subtype = "file-metadata",
            CanonicalText = source.FileName
        };
        atoms.Add(fileAtom);
        
        // Manual chunk atoms (no relations)
        var chunks = text.Split('\n\n');
        foreach (var chunk in chunks)
        {
            var chunkBytes = Encoding.UTF8.GetBytes(chunk);
            var chunkAtom = new AtomData
            {
                AtomicValue = chunkBytes.Take(64).ToArray(),  // ? Truncates
                ContentHash = CreateContentHash(chunkBytes),
                Modality = "text",
                Subtype = "chunk",
                CanonicalText = chunk
            };
            atoms.Add(chunkAtom);
        }
    }
}
```

### After (New Pattern)

```csharp
public class DocumentAtomizer : BaseAtomizer<byte[]>
{
    protected override async Task AtomizeCoreAsync(
        byte[] input,
        SourceMetadata source,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        // Step 1: Create file atom using helper (handles overflow)
        var fileHash = CreateFileMetadataAtom(input, source, atoms);
        
        var text = Encoding.UTF8.GetString(input);
        var chunks = text.Split('\n\n');
        
        // Step 2: Create chunk atoms using helper
        foreach (var (chunk, index) in chunks.Select((c, i) => (c, i)))
        {
            var chunkBytes = Encoding.UTF8.GetBytes(chunk);
            
            // Create content atom (handles overflow)
            var chunkHash = CreateContentAtom(
                content: chunkBytes,
                modality: "text",
                subtype: "chunk",
                canonicalText: chunk,
                metadata: System.Text.Json.JsonSerializer.Serialize(new
                {
                    paragraphIndex = index,
                    wordCount = chunk.Split(' ').Length
                }),
                atoms: atoms
            );
            
            // Step 3: Create AtomRelation
            CreateAtomRelation(
                parentHash: fileHash,
                childHash: chunkHash,
                relationType: "parent-child",
                compositions: compositions,
                sequenceIndex: index,
                weight: 1.0f,
                importance: ComputeChunkImportance(chunk)
            );
        }
    }
    
    private float ComputeChunkImportance(string chunk)
    {
        // Example: Longer chunks = higher importance
        return Math.Min(1.0f, chunk.Length / 1000.0f);
    }
    
    // Required abstract method implementations
    protected override string GetModality() => "document";
    
    protected override byte[] GetFileMetadataBytes(byte[] input, SourceMetadata source)
        => Encoding.UTF8.GetBytes($"{source.FileName}:{source.ContentType}:{input.Length}");
    
    protected override string GetCanonicalFileText(byte[] input, SourceMetadata source)
        => $"Document: {source.FileName} ({input.Length} bytes)";
    
    protected override string GetFileMetadataJson(byte[] input, SourceMetadata source)
        => System.Text.Json.JsonSerializer.Serialize(new
        {
            fileName = source.FileName,
            contentType = source.ContentType,
            size = input.Length,
            encoding = "UTF-8"
        });
    
    protected override string GetDetectedFormat() => "Plain Text Document";
    
    public override int Priority => 10;
    
    public override bool CanHandle(string contentType, string? fileExtension)
        => contentType.StartsWith("text/") || fileExtension?.ToLower() == "txt";
}
```

**Improvements**:
1. ? No data loss (overflow preserved)
2. ? AtomRelations created (parent-child links)
3. ? Metadata includes chunk statistics
4. ? Importance scores computed
5. ? Consistent with other atomizers (DRY)

---

## Common Patterns

### Pattern 1: Sequential Content (Text Chunks, Code Blocks)

```csharp
// Create file atom
var fileHash = CreateFileMetadataAtom(input, source, atoms);

// Create sequential content atoms
foreach (var (content, index) in contentItems.Select((c, i) => (c, i)))
{
    var contentHash = CreateContentAtom(...);
    
    CreateAtomRelation(
        parentHash: fileHash,
        childHash: contentHash,
        relationType: "parent-child",
        compositions: compositions,
        sequenceIndex: index  // ? Sequential order
    );
}
```

### Pattern 2: Hierarchical Content (AST, DOM Tree)

```csharp
// Create file atom
var fileHash = CreateFileMetadataAtom(input, source, atoms);

// Create root node
var rootHash = CreateContentAtom(...);
CreateAtomRelation(fileHash, rootHash, "parent-child", compositions);

// Recursively create child nodes
void ProcessNode(Node node, byte[] parentHash)
{
    var nodeHash = CreateContentAtom(...);
    CreateAtomRelation(parentHash, nodeHash, "parent-child", compositions);
    
    foreach (var child in node.Children)
    {
        ProcessNode(child, nodeHash);  // ? Recursive hierarchy
    }
}

ProcessNode(rootNode, rootHash);
```

### Pattern 3: Multi-Dimensional Content (Embeddings)

```csharp
// Create embedding atom
var embeddingHash = CreateFileMetadataAtom(input, source, atoms);

// Create dimension atoms (each float = one atom)
foreach (var (value, dimIndex) in embedding.Select((v, i) => (v, i)))
{
    var valueBytes = BitConverter.GetBytes(value);
    
    var dimHash = CreateContentAtom(
        content: valueBytes,
        modality: "embedding",
        subtype: "dimension",
        canonicalText: value.ToString("F6"),
        metadata: System.Text.Json.JsonSerializer.Serialize(new
        {
            dimensionIndex = dimIndex,
            modelId = modelId
        }),
        atoms: atoms
    );
    
    CreateAtomRelation(
        parentHash: embeddingHash,
        childHash: dimHash,
        relationType: "embedding",
        compositions: compositions,
        sequenceIndex: dimIndex,  // ? Dimension index
        weight: Math.Abs(value)   // ? Magnitude as weight
    );
}
```

### Pattern 4: Spatial Content (Image Patches, Video Frames)

```csharp
// Create image atom
var imageHash = CreateFileMetadataAtom(input, source, atoms);

// Create patch atoms with spatial coordinates
foreach (var patch in ExtractPatches(image))
{
    var patchHash = CreateContentAtom(...);
    
    CreateAtomRelation(
        parentHash: imageHash,
        childHash: patchHash,
        relationType: "parent-child",
        compositions: compositions,
        sequenceIndex: patch.Index,
        spatialMetadata: new Dictionary<string, object>
        {
            ["patchX"] = patch.X,      // ? Spatial position
            ["patchY"] = patch.Y,
            ["patchWidth"] = patch.Width,
            ["patchHeight"] = patch.Height
        }
    );
}
```

---

## Troubleshooting

### Issue: "AtomicValue too large"

**Symptom**: Exception thrown when atom content > 64 bytes

**Solution**: Use `CreateContentAtom()` helper instead of manual creation
```csharp
// ? Wrong
atoms.Add(new AtomData { AtomicValue = largeContent });

// ? Correct
CreateContentAtom(largeContent, modality, subtype, text, metadata, atoms);
```

### Issue: "AtomRelations not created"

**Symptom**: Atoms created but no rows in AtomRelation table

**Solution**: Call `CreateAtomRelation()` for each parent-child link
```csharp
var fileHash = CreateFileMetadataAtom(...);
var contentHash = CreateContentAtom(...);

// Add this line:
CreateAtomRelation(fileHash, contentHash, "parent-child", compositions);
```

### Issue: "Spatial metadata missing"

**Symptom**: AtomRelation.SpatialMetadata is NULL

**Solution**: Pass `spatialMetadata` parameter to `CreateAtomRelation()`
```csharp
CreateAtomRelation(
    parentHash, 
    childHash, 
    "parent-child", 
    compositions,
    spatialMetadata: new Dictionary<string, object>
    {
        ["coordX"] = 0.5,
        ["coordY"] = -0.3
    }
);
```

---

## Summary

**Migration Steps**:
1. Replace manual `new AtomData` with `CreateContentAtom()`
2. Replace manual file atom creation with `CreateFileMetadataAtom()`
3. Add `CreateAtomRelation()` calls for all parent-child links
4. Implement required abstract methods
5. Test overflow handling and relation creation

**Benefits**:
- ? Zero data loss (overflow preserved)
- ? Consistent atomization across all atomizers
- ? Automatic spatial metadata support
- ? DRY compliance (no duplicated logic)

**References**:
- Audit Report: `docs/ALGORITHM_ATOMIZATION_AUDIT.md`
- Summary: `docs/ATOMIZATION_IMPROVEMENTS_SUMMARY.md`
- Implementation: `src/Hartonomous.Infrastructure/Atomizers/BaseAtomizer.cs`

---

*Last Updated: January 2025*
