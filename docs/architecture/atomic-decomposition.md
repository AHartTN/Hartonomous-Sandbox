# Atomic Decomposition: The Periodic Table of Knowledge

**Last Updated**: November 13, 2025  
**Core Principle**: Break ALL content down to the most granular, deduplicatable components  
**Goal**: Eliminate large blob storage entirely through radical atomization + CAS deduplication

---

## Philosophy

Hartonomous implements a **"Periodic Table of Knowledge"** where every piece of information—regardless of modality—is decomposed into its fundamental atomic units. Just as chemical elements combine to form complex molecules, atomic data units combine to reconstruct any content.

### Why Radical Atomization?

1. **Perfect Deduplication**: Identical pixels, audio samples, weights, or tokens are stored exactly once via content-addressable storage (CAS)
2. **No FILESTREAM Needed**: Instead of storing 10GB model weights as a blob, store millions of deduplicated float32 values
3. **Cross-Modal Reuse**: The same RGB value appears in thousands of images—store it once
4. **Temporal Efficiency**: Model weight updates only store changed coefficients, not entire parameter matrices
5. **Spatial Intelligence**: Multi-dimensional data (embeddings, RGB, audio channels, coordinates) maps naturally to GEOMETRY/GEOGRAPHY types

### The Cost-Benefit Trade-off

**Traditional Approach (WRONG)**:
- Store entire image as 5MB FILESTREAM blob
- 1000 similar images = 5GB storage
- No deduplication, no cross-reference

**Atomic Approach (CORRECT)**:
- Decompose 5MB image into 1,920,000 pixels (1920×1080)
- Store each unique RGB triplet once in `dbo.Atoms`
- Store pixel positions as GEOMETRY points
- 1000 similar images share ~80% of pixel atoms
- Storage: ~1.2GB (after deduplication) + geometry indexes
- **Bonus**: Query "find all images containing this exact sky blue shade" becomes trivial

---

## Atomization Strategies by Modality

### Text: Character & Token Level

**WRONG (Current)**:
```csharp
// Splitting by sentences/paragraphs
AtomCandidate { CanonicalText = "The quick brown fox jumps over the lazy dog." }
```

**CORRECT (Atomic)**:
```csharp
// Store individual characters/tokens with positions
AtomCandidate { 
    Modality = "text",
    Subtype = "utf8-char",
    CanonicalText = "T",  // Single character
    Metadata = { 
        {"position", 0}, 
        {"codepoint", 84},
        {"contextHash", "sha256-of-surrounding-10-chars"} 
    },
    SpatialKey = GEOMETRY::Point(0, 0, charIndex, 0)  // Document position as spatial coordinate
}

// OR token-level for efficiency
AtomCandidate { 
    Modality = "text",
    Subtype = "bpe-token",
    BinaryPayload = [token_id_bytes],  // 2-4 bytes per BPE token
    Metadata = { {"tokenId", 1234}, {"vocabulary", "gpt4-tiktoken"} },
    SpatialKey = GEOMETRY::Point(tokenIndex, 0, 0, 0)
}
```

**Storage Formula**:
- 1MB text document = ~1,000,000 chars
- Deduplicated to ~95 unique characters (A-Z, a-z, 0-9, punctuation, spaces, special)
- **Storage**: 95 atom records + 1M position references
- **GEOMETRY index**: O(log n) lookup by position

### Images: Pixel & Channel Level

**WRONG (Current)**:
```csharp
// Entire image or tiles
AtomCandidate { BinaryPayload = entire_jpeg_bytes }  // 5MB blob
```

**CORRECT (Atomic)**:
```csharp
// Individual pixels as spatial points
AtomCandidate {
    Modality = "image",
    Subtype = "rgb-pixel",
    BinaryPayload = [R, G, B],  // 3 bytes
    ContentHash = SHA256([R, G, B]),
    SpatialKey = GEOMETRY::Point(x, y, 0, 0),  // 2D image coordinate
    Metadata = {
        {"sourceImage", imageAtomId},
        {"colorSpace", "sRGB"},
        {"alpha", 255}
    }
}

// OR color palette approach
AtomCandidate {
    Modality = "image",
    Subtype = "rgba-color",
    BinaryPayload = [R, G, B, A],  // 4 bytes - the color itself
    ContentHash = SHA256([R, G, B, A])
}
// Separate table: PixelReferences(ImageId, X, Y, ColorAtomId)
// Use GEOMETRY for pixel grid: GEOMETRY::Point(x, y, colorAtomId, 0)
```

**Storage Formula**:
- 1920×1080 image = 2,073,600 pixels
- Typical photo has ~100,000 unique colors
- **Storage**: 100K atom records + 2M spatial points
- **Query**: "SELECT all atoms WHERE SpatialKey.STX BETWEEN x1 AND x2" = instant region extraction

### Audio: Sample & Channel Level

**WRONG (Current)**:
```csharp
// Entire audio file or time segments
AtomCandidate { BinaryPayload = wav_bytes }  // 30MB for 3min audio
```

**CORRECT (Atomic)**:
```csharp
// Individual audio samples per channel
AtomCandidate {
    Modality = "audio",
    Subtype = "pcm-sample-int16",
    BinaryPayload = BitConverter.GetBytes(amplitude),  // 2 bytes for 16-bit audio
    ContentHash = SHA256(amplitude_bytes),
    SpatialKey = GEOMETRY::Point(sampleIndex, channelIndex, amplitude, 0),  // Time × Channel × Amplitude
    Metadata = {
        {"sampleRate", 44100},
        {"sourceAudio", audioAtomId},
        {"timestamp", sampleIndex / 44100.0}  // Seconds
    }
}

// Stereo audio: channel atoms
AtomCandidate {
    Modality = "audio",
    Subtype = "stereo-frame",
    BinaryPayload = [left_int16, right_int16],  // 4 bytes per frame
    SpatialKey = GEOMETRY::Point(frameIndex, left, right, 0)  // 3D: Time × Left × Right
}
```

**Storage Formula**:
- 3min audio @ 44.1kHz stereo = 15,876,000 samples
- Quantized to 16-bit = 65,536 possible values per channel
- **Storage**: ~131K unique amplitude atoms + 15.8M temporal references
- **Query**: "Find all audio with amplitude spike > 30,000" using spatial filter

### Video: Frame & Pixel Level

**WRONG (Current)**:
```csharp
// Keyframes or scene chunks
AtomCandidate { BinaryPayload = frame_jpeg_bytes }
```

**CORRECT (Atomic)**:
```csharp
// Pixels across time dimension
AtomCandidate {
    Modality = "video",
    Subtype = "temporal-pixel",
    BinaryPayload = [R, G, B],
    SpatialKey = GEOMETRY::Point(x, y, frameIndex, 0),  // 3D: X × Y × Time
    Metadata = {
        {"fps", 30},
        {"sourceVideo", videoAtomId},
        {"codec", "h264"},
        {"timestamp", frameIndex / 30.0}
    }
}
```

**Storage Formula**:
- 1080p video @ 30fps × 60sec = 3,732,480,000 pixels
- Scene changes: ~20% unique pixels per frame after motion compensation
- **Storage**: ~750M pixel atoms (heavily deduplicated across frames)
- **Query**: "Show pixel evolution at (640, 480) across all frames" = single spatial query

### AI Models: Weight & Coefficient Level

**WRONG (Current)**:
```csharp
// Entire weight matrices or layer tensors
TensorAtom { 
    Shape = [4096, 4096],  // 16M parameters
    BinaryPayload = float32_array  // 64MB
}
```

**CORRECT (Atomic)**:
```csharp
// Individual weight coefficients
AtomCandidate {
    Modality = "model",
    Subtype = "float32-weight",
    BinaryPayload = BitConverter.GetBytes(weight_value),  // 4 bytes
    ContentHash = SHA256(float_bytes),
    SpatialKey = GEOMETRY::Point(layerId, rowIndex, colIndex, 0),  // 3D tensor position
    Metadata = {
        {"modelId", modelAtomId},
        {"layerName", "attention.q_proj"},
        {"parameterType", "weight"},
        {"dtype", "float32"}
    }
}

// Store position separately
TensorAtomCoefficient {
    TensorAtomId,  // Points to the actual float32 value atom
    LayerId,
    PositionX,  // Row in matrix
    PositionY,  // Col in matrix
    PositionZ,  // For 3D+ tensors
    SpatialKey = GEOMETRY::Point(PositionX, PositionY, PositionZ, LayerId)
}
```

**Storage Formula**:
- GPT-4 scale: 1.76 trillion parameters
- Quantized to int8: 256 unique values
- Even float32: ~100M unique values after quantization/rounding
- **Storage**: 100M weight atoms + 1.76T position references
- **Temporal versioning**: Only changed weights create new atom versions
- **Query**: "Find all layers where weights > 0.9" using spatial index on value dimension

---

## Spatial Type Exploitation

### GEOMETRY as Multi-Dimensional Index

SQL Server's GEOMETRY type supports 4 dimensions (X, Y, Z, M). We exploit this for:

**Text Documents**:
```sql
-- X = character/token position, Y = line, Z = paragraph, M = document section
GEOMETRY::Point(charPos, lineNum, paraNum, sectionId, 0)

-- Query: Find all text in paragraph 5
SELECT * FROM Atoms WHERE SpatialKey.STZ = 5
```

**RGB Color Space**:
```sql
-- X = Red (0-255), Y = Green (0-255), Z = Blue (0-255), M = Alpha (0-255)
GEOMETRY::Point(R, G, B, A, 0)

-- Query: Find all "sky blue" pixels (R=135, G=206, B=235 ±10)
SELECT * FROM Atoms 
WHERE SpatialKey.STX BETWEEN 125 AND 145
  AND SpatialKey.STY BETWEEN 196 AND 216
  AND SpatialKey.STZ BETWEEN 225 AND 245
```

**Audio Waveforms**:
```sql
-- X = time (sample index), Y = channel, Z = amplitude, M = frequency band (if FFT)
GEOMETRY::Point(sampleIdx, channel, amplitude, 0, 0)

-- Query: Find all high-amplitude moments
SELECT * FROM Atoms WHERE SpatialKey.STZ > 30000
```

**Model Weight Tensors**:
```sql
-- X = layer, Y = row, Z = col, M = value quantile
GEOMETRY::Point(layerId, row, col, NTILE(100) OVER (ORDER BY value), 0)

-- Query: Find all attention weights in top 10% quantile
SELECT * FROM Atoms WHERE SpatialKey.STM >= 90
```

**Embeddings (High-Dimensional)**:
```sql
-- Use PCA to reduce 1998D to 4D for spatial indexing
GEOMETRY::Point(pca_dim1, pca_dim2, pca_dim3, pca_dim4, 0)

-- R-tree index enables nearest-neighbor with spatial join
SELECT TOP 10 * 
FROM Atoms 
WHERE SpatialKey.STDistance(@queryPoint) < @threshold
ORDER BY SpatialKey.STDistance(@queryPoint)
```

### GEOGRAPHY for True Geospatial

Use GEOGRAPHY for actual lat/lon data:
```sql
-- GPS coordinates from sensor data
GEOGRAPHY::Point(latitude, longitude, altitude, timestamp, 4326)  -- WGS84 SRID

-- Query: Find all sensor readings within 1km of location
SELECT * FROM Atoms
WHERE SpatialGeography.STDistance(GEOGRAPHY::Point(lat, lon, 0, 0, 4326)) < 1000
```

---

## Implementation Strategy

### Phase 1: Update Atom Schema

```sql
CREATE TABLE dbo.Atoms (
    AtomId BIGINT IDENTITY(1,1) PRIMARY KEY,
    
    -- Core identity
    Modality VARCHAR(50) NOT NULL,  -- 'text', 'image', 'audio', 'video', 'model', 'sensor'
    Subtype VARCHAR(50) NOT NULL,   -- 'utf8-char', 'rgb-pixel', 'pcm-sample', 'float32-weight'
    ContentHash BINARY(32) NOT NULL UNIQUE,  -- SHA-256 for deduplication
    
    -- Atomic payload (SMALL - typically 1-8 bytes)
    AtomicValue VARBINARY(64) NULL,  -- Raw bytes: single char, RGB triplet, float32, etc.
    CanonicalText NVARCHAR(256) NULL,  -- Optional text representation
    
    -- Multi-dimensional spatial indexing
    SpatialKey GEOMETRY NULL,  -- 4D position: varies by modality
    SpatialGeography GEOGRAPHY NULL,  -- For true geospatial data
    
    -- Metadata
    Metadata NVARCHAR(MAX) NULL,  -- JSON
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME(),
    
    INDEX IX_Atoms_Modality_Subtype (Modality, Subtype),
    INDEX IX_Atoms_ContentHash (ContentHash),
    SPATIAL INDEX SIDX_Atoms_SpatialKey ON SpatialKey,
    SPATIAL INDEX SIDX_Atoms_Geography ON SpatialGeography
);

-- Reconstruction table: Maps source content to atomic components
CREATE TABLE dbo.AtomCompositions (
    CompositionId BIGINT IDENTITY(1,1) PRIMARY KEY,
    SourceAtomId BIGINT NOT NULL,  -- The "parent" (e.g., full image, full document)
    ComponentAtomId BIGINT NOT NULL,  -- The atomic piece
    PositionKey GEOMETRY NOT NULL,  -- Where this atom appears in source
    SequenceIndex BIGINT NULL,  -- Linear ordering if needed
    
    FOREIGN KEY (SourceAtomId) REFERENCES dbo.Atoms(AtomId),
    FOREIGN KEY (ComponentAtomId) REFERENCES dbo.Atoms(AtomId),
    INDEX IX_Composition_Source (SourceAtomId),
    SPATIAL INDEX SIDX_Composition_Position ON PositionKey
);
```

### Phase 2: Update Atomizers

```csharp
// TRUE atomic text atomizer
public class CharacterAtomizer : IAtomizer<string>
{
    public async IAsyncEnumerable<AtomCandidate> AtomizeAsync(
        string text, AtomizationContext context, CancellationToken ct)
    {
        for (int i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            yield return new AtomCandidate
            {
                Modality = "text",
                Subtype = "utf8-char",
                AtomicValue = Encoding.UTF8.GetBytes(new[] { ch }),
                CanonicalText = ch.ToString(),
                SpatialKey = $"POINT({i} 0 0 0)",  // Character position
                Metadata = new() { 
                    ["codepoint"] = (int)ch,
                    ["category"] = char.GetUnicodeCategory(ch).ToString()
                }
            };
        }
    }
}

// TRUE atomic image atomizer
public class PixelAtomizer : IAtomizer<byte[]>
{
    public async IAsyncEnumerable<AtomCandidate> AtomizeAsync(
        byte[] imageBytes, AtomizationContext context, CancellationToken ct)
    {
        using var image = Image.Load<Rgb24>(imageBytes);
        
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                var pixel = image[x, y];
                yield return new AtomCandidate
                {
                    Modality = "image",
                    Subtype = "rgb-pixel",
                    AtomicValue = new byte[] { pixel.R, pixel.G, pixel.B },
                    SpatialKey = $"POINT({x} {y} 0 0)",  // Pixel coordinates
                    Metadata = new() {
                        ["colorSpace"] = "sRGB",
                        ["rgbHex"] = $"#{pixel.R:X2}{pixel.G:X2}{pixel.B:X2}"
                    }
                };
            }
        }
    }
}

// TRUE atomic model weight atomizer
public class WeightAtomizer : IAtomizer<ModelWeights>
{
    public async IAsyncEnumerable<AtomCandidate> AtomizeAsync(
        ModelWeights weights, AtomizationContext context, CancellationToken ct)
    {
        foreach (var layer in weights.Layers)
        {
            for (int i = 0; i < layer.Weights.Length; i++)
            {
                var weight = layer.Weights[i];
                var (row, col) = IndexToRowCol(i, layer.Shape);
                
                yield return new AtomCandidate
                {
                    Modality = "model",
                    Subtype = "float32-weight",
                    AtomicValue = BitConverter.GetBytes(weight),
                    SpatialKey = $"POINT({layer.Id} {row} {col} 0)",  // Layer × Row × Col
                    Metadata = new() {
                        ["layerName"] = layer.Name,
                        ["dtype"] = "float32",
                        ["quantile"] = CalculateQuantile(weight, layer.Weights)
                    }
                };
            }
        }
    }
}
```

### Phase 3: Reconstruction Queries

```sql
-- Reconstruct image from pixels
WITH PixelAtoms AS (
    SELECT 
        ac.ComponentAtomId,
        a.AtomicValue,
        ac.PositionKey.STX AS X,
        ac.PositionKey.STY AS Y,
        ROW_NUMBER() OVER (ORDER BY ac.PositionKey.STY, ac.PositionKey.STX) AS PixelIndex
    FROM dbo.AtomCompositions ac
    JOIN dbo.Atoms a ON ac.ComponentAtomId = a.AtomId
    WHERE ac.SourceAtomId = @imageAtomId
      AND a.Modality = 'image'
      AND a.Subtype = 'rgb-pixel'
)
SELECT 
    X, Y,
    CONVERT(INT, CONVERT(VARBINARY(1), SUBSTRING(AtomicValue, 1, 1))) AS R,
    CONVERT(INT, CONVERT(VARBINARY(1), SUBSTRING(AtomicValue, 2, 1))) AS G,
    CONVERT(INT, CONVERT(VARBINARY(1), SUBSTRING(AtomicValue, 3, 1))) AS B
FROM PixelAtoms
ORDER BY Y, X;

-- Find all documents containing the word "query" (character-level)
WITH TextAtoms AS (
    SELECT 
        ac.SourceAtomId,
        a.CanonicalText,
        ac.PositionKey.STX AS CharPos,
        STRING_AGG(a.CanonicalText, '') WITHIN GROUP (ORDER BY ac.PositionKey.STX) 
            OVER (PARTITION BY ac.SourceAtomId 
                  ORDER BY ac.PositionKey.STX 
                  ROWS BETWEEN 4 PRECEDING AND CURRENT ROW) AS Window5
    FROM dbo.AtomCompositions ac
    JOIN dbo.Atoms a ON ac.ComponentAtomId = a.AtomId
    WHERE a.Modality = 'text' AND a.Subtype = 'utf8-char'
)
SELECT DISTINCT SourceAtomId
FROM TextAtoms
WHERE Window5 = 'query';

-- Export model weights for layer
SELECT 
    ac.PositionKey.STY AS Row,
    ac.PositionKey.STZ AS Col,
    CONVERT(FLOAT, CONVERT(VARBINARY(4), a.AtomicValue)) AS Weight
FROM dbo.AtomCompositions ac
JOIN dbo.Atoms a ON ac.ComponentAtomId = a.AtomId
WHERE ac.SourceAtomId = @layerAtomId
  AND a.Modality = 'model'
  AND a.Subtype = 'float32-weight'
ORDER BY Row, Col;
```

---

## Performance Considerations

### Deduplication Wins

**Example: 1000 similar product images (1920×1080 each)**

Traditional:
- 1000 × 5MB = 5GB storage
- No sharing, no deduplication

Atomic:
- 2,073,600 pixels/image × 1000 images = 2.07B total pixels
- Typical product photos share 60-80% of pixels (backgrounds, lighting)
- Unique pixels: ~800M after deduplication
- Storage: 800M × 3 bytes = 2.4GB raw + indexes
- **40-50% reduction even before compression**

### Index Overhead

- Spatial indexes: ~2-3× data size
- Total: ~7GB for atomic approach vs 5GB traditional
- **BUT**: Enables queries impossible with blobs:
  - "Find all images with this exact shade of blue"
  - "Show pixel evolution across video frames"
  - "Identify reused model weight patterns"

### Reconstruction Cost

- Full reconstruction requires: 
  - Spatial query to get components
  - Assembly in application layer
- **Mitigation**: Cache reconstructed content, lazy load regions
- **Benefit**: Partial reconstruction is trivial (e.g., single video frame, text excerpt, model layer)

---

## Migration Path

1. **Phase 1**: Implement atomic atomizers alongside existing ones
2. **Phase 2**: Test deduplication rates on real datasets
3. **Phase 3**: Migrate high-value use cases (model weights, video frames)
4. **Phase 4**: Deprecate FILESTREAM, remove blob storage
5. **Phase 5**: Optimize reconstruction queries, add caching layer

**Target State**: Zero FILESTREAM, zero varbinary(max) blobs. Everything atomic, everything deduplicated, everything queryable.
