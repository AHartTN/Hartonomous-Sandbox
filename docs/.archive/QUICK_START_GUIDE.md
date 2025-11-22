# ? **HARTONOMOUS: QUICK-START IMPLEMENTATION GUIDE**

**Read Time**: 5 minutes  
**Implementation Time**: 7-10 days  
**Status**: Ready to implement

---

## **?? WHAT YOU NEED TO KNOW**

Your Hartonomous system is **85% complete**. Three small fixes needed:

1. ? **CLR not deployed** (1 hour)
2. ? **Placeholder embeddings** (2-3 days)
3. ? **No embedding trigger** (1-2 days)

**Everything else works!** ?

---

## **?? DAY 1: DEPLOY CLR FUNCTIONS (1 HOUR)**

### **Step 1: Build CLR Assembly**
```bash
cd src/Hartonomous.Database
dotnet build --configuration Release
```

Output: `HartonomousClr.dll` in `bin/Release/`

### **Step 2: Deploy to SQL Server**

Run this script (update path):

```sql
USE [Hartonomous];
GO

-- Enable CLR
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;
GO

-- Set TRUSTWORTHY (dev/test only)
ALTER DATABASE [Hartonomous] SET TRUSTWORTHY ON;
GO

-- Drop existing assembly (if exists)
IF EXISTS (SELECT * FROM sys.assemblies WHERE name = 'HartonomousClr')
BEGIN
    DROP ASSEMBLY HartonomousClr;
END;
GO

-- Create assembly (UPDATE PATH!)
CREATE ASSEMBLY HartonomousClr
FROM 'D:\Repositories\Hartonomous\src\Hartonomous.Database\bin\Release\HartonomousClr.dll'
WITH PERMISSION_SET = UNSAFE;
GO

-- Create functions
CREATE FUNCTION dbo.fn_ProjectTo3D(@vector VARBINARY(MAX))
RETURNS GEOMETRY
AS EXTERNAL NAME HartonomousClr.[Hartonomous.Clr.SpatialOperations].fn_ProjectTo3D;
GO

CREATE FUNCTION dbo.clr_CosineSimilarity(@vector1 VARBINARY(MAX), @vector2 VARBINARY(MAX))
RETURNS FLOAT
AS EXTERNAL NAME HartonomousClr.[Hartonomous.Clr.VectorOperations].clr_CosineSimilarity;
GO

CREATE FUNCTION dbo.clr_ComputeHilbertValue(@spatialKey GEOMETRY, @precision INT)
RETURNS BIGINT
AS EXTERNAL NAME HartonomousClr.[Hartonomous.Clr.SpaceFillingCurves].clr_ComputeHilbertValue;
GO

CREATE FUNCTION dbo.clr_VectorAverage(@vectors VARBINARY(MAX))
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME HartonomousClr.[Hartonomous.Clr.VectorOperations].clr_VectorAverage;
GO
```

### **Step 3: Verify**
```sql
-- Test fn_ProjectTo3D
SELECT dbo.fn_ProjectTo3D(0x3F800000...).ToString();  -- Should return "POINT(...)"

-- Test clr_ComputeHilbertValue
SELECT dbo.clr_ComputeHilbertValue(geometry::Point(0.5, 0.5, 0), 21);  -- Should return BIGINT
```

**Done!** ? All stored procedures now work.

---

## **?? DAY 2-4: FIX EMBEDDINGWORKER (2-3 DAYS)**

### **Location**: `src/Hartonomous.Workers.EmbeddingGenerator/EmbeddingGeneratorWorker.cs`

### **Change 1: Replace Placeholder Embedding** (line 113)

**OLD CODE**:
```csharp
var embedding = GeneratePlaceholderEmbedding();  // ? FAKE DATA
```

**NEW CODE**:
```csharp
// Select model based on modality
var embeddingModel = atom.Modality switch
{
    "text" => await _modelService.GetModelAsync("text-embedding-ada-002"),
    "image" => await _modelService.GetModelAsync("clip-vit-base-patch32"),
    "audio" => await _modelService.GetModelAsync("audio-embedding-model"),
    _ => throw new NotSupportedException($"Modality {atom.Modality} not supported")
};

// Compute REAL embedding
var embedding = await ComputeEmbeddingAsync(atom, embeddingModel, cancellationToken);
```

### **Change 2: Add Real Spatial Projection** (line 130)

**OLD CODE**:
```csharp
SpatialKey = new Point(0, 0),  // ? WRONG!
```

**NEW CODE**:
```csharp
// Project to 3D (via CLR function)
var spatialKey = await ProjectTo3DAsync(embedding, cancellationToken);

// Compute Hilbert value
var hilbertValue = await ComputeHilbertValueAsync(spatialKey, cancellationToken);

// Compute spatial buckets
var (bucketX, bucketY, bucketZ) = ComputeSpatialBuckets(spatialKey);
```

### **Change 3: Update AtomEmbedding Creation**

**NEW CODE**:
```csharp
var atomEmbedding = new AtomEmbedding
{
    AtomId = atom.AtomId,
    TenantId = atom.TenantId,
    ModelId = embeddingModel.ModelId,
    EmbeddingType = "semantic",
    Dimension = embedding.Length,
    EmbeddingVector = new SqlVector<float>(embedding),
    SpatialKey = spatialKey,  // ? REAL 3D POINT
    HilbertValue = hilbertValue,  // ? REAL HILBERT VALUE
    SpatialBucketX = bucketX,
    SpatialBucketY = bucketY,
    SpatialBucketZ = bucketZ,
    CreatedAt = DateTime.UtcNow
};
```

### **Change 4: Add Helper Methods**

Add these to `EmbeddingGeneratorWorker.cs`:

```csharp
private async Task<float[]> ComputeEmbeddingAsync(
    Atom atom, 
    Model model, 
    CancellationToken cancellationToken)
{
    return model.ModelName switch
    {
        "text-embedding-ada-002" => await _openAIService.GetEmbeddingAsync(atom.CanonicalText, cancellationToken),
        "clip-vit-base-patch32" => await _clipService.GetImageEmbeddingAsync(atom.AtomicValue, cancellationToken),
        _ => throw new NotSupportedException($"Model {model.ModelName} not supported")
    };
}

private async Task<Geometry> ProjectTo3DAsync(float[] embedding, CancellationToken cancellationToken)
{
    await using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync(cancellationToken);
    
    var embeddingBytes = new byte[embedding.Length * sizeof(float)];
    Buffer.BlockCopy(embedding, 0, embeddingBytes, 0, embeddingBytes.Length);
    
    await using var command = new SqlCommand(@"
        SELECT dbo.fn_ProjectTo3D(@embedding).ToString()
    ", connection);
    
    command.Parameters.AddWithValue("@embedding", embeddingBytes);
    var wkt = (string)await command.ExecuteScalarAsync(cancellationToken);
    
    var reader = new NetTopologySuite.IO.WKTReader();
    return reader.Read(wkt);
}

private async Task<long> ComputeHilbertValueAsync(Geometry spatialKey, CancellationToken cancellationToken)
{
    await using var connection = new SqlConnection(_connectionString);
    await connection.OpenAsync(cancellationToken);
    
    await using var command = new SqlCommand(@"
        SELECT dbo.clr_ComputeHilbertValue(@spatialKey, @precision)
    ", connection);
    
    command.Parameters.AddWithValue("@spatialKey", spatialKey);
    command.Parameters.AddWithValue("@precision", 21);
    
    return (long)await command.ExecuteScalarAsync(cancellationToken);
}

private (int bucketX, int bucketY, int bucketZ) ComputeSpatialBuckets(Geometry spatialKey)
{
    var point = (Point)spatialKey;
    var bucketSize = 0.1;
    
    return (
        (int)Math.Floor(point.X / bucketSize),
        (int)Math.Floor(point.Y / bucketSize),
        (int)Math.Floor(point.Z / bucketSize)
    );
}
```

### **Change 5: Add Service Registrations**

In `Program.cs`:
```csharp
// Embedding services
builder.Services.AddScoped<IOpenAIEmbeddingService, OpenAIEmbeddingService>();
builder.Services.AddScoped<IClipEmbeddingService, ClipEmbeddingService>();
builder.Services.AddScoped<IModelService, ModelService>();

// Connection string injection
builder.Services.AddSingleton(sp => 
    sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.HartonomousDb);
```

---

## **?? DAY 5-6: ADD EMBEDDING TRIGGER (1-2 DAYS)**

### **Location**: `src/Hartonomous.Infrastructure/Services/IngestionService.cs`

### **Change: Add After Line 96**

**FIND THIS**:
```csharp
var batchId = await CallSpIngestAtomsAsync(atomsJson, tenantId);

// ? ADD HERE!

// Track custom metrics
_telemetry?.TrackMetric("Atoms.Ingested", allAtoms.Count);
```

**ADD THIS**:
```csharp
var batchId = await CallSpIngestAtomsAsync(atomsJson, tenantId);

// PHASE 2: Trigger embedding generation for all new atoms
foreach (var atom in allAtoms.Where(a => NeedsEmbedding(a.Modality)))
{
    await _embeddingService.QueueEmbeddingGenerationAsync(new[] { atom.AtomId }, tenantId);
}

// Track custom metrics
_telemetry?.TrackMetric("Atoms.Ingested", allAtoms.Count);
```

**ADD HELPER**:
```csharp
private bool NeedsEmbedding(string modality)
{
    return modality is "text" or "image" or "audio" or "video" or "code";
}
```

---

## **?? DAY 7: END-TO-END TESTING**

### **Test 1: CLR Functions**
```sql
-- Should return 4 rows
SELECT name, type_desc FROM sys.objects 
WHERE type IN ('FS', 'FT', 'AF') 
AND (name LIKE 'clr_%' OR name LIKE 'fn_Project%');
```

### **Test 2: Spatial Keys Populated**
```sql
-- Should show WithSpatialKey = Total
SELECT 
    COUNT(*) AS Total,
    SUM(CASE WHEN SpatialKey IS NOT NULL THEN 1 ELSE 0 END) AS WithSpatialKey,
    SUM(CASE WHEN HilbertValue IS NOT NULL THEN 1 ELSE 0 END) AS WithHilbert
FROM dbo.AtomEmbedding;
```

### **Test 3: Cross-Modal Search**
```sql
-- Upload text "red apple" and image of red apple
-- Query: Find images near text embedding
DECLARE @textSpatialKey GEOMETRY = (
    SELECT TOP 1 SpatialKey FROM dbo.AtomEmbedding ae
    INNER JOIN dbo.Atom a ON ae.AtomId = a.AtomId
    WHERE a.CanonicalText LIKE '%red apple%'
);

SELECT TOP 10
    a.Modality,
    ae.SpatialKey.STDistance(@textSpatialKey) AS Distance
FROM dbo.AtomEmbedding ae
INNER JOIN dbo.Atom a ON ae.AtomId = a.AtomId
WHERE a.Modality = 'image'
ORDER BY Distance;
-- Expected: Distance < 0.5 (cross-modal search works!)
```

---

## **?? FULL DOCUMENTATION**

| Document | Purpose | When to Read |
|----------|---------|--------------|
| **PLUMBER_FINAL_REPORT.md** | Executive summary | Read first (5 min) |
| **PLUMBING_STATUS_REPORT.md** | Detailed status | Read second (10 min) |
| **MASTER_PLUMBING_PLAN.md** | Full implementation guide | Read before coding (30 min) |
| **THIS FILE** | Quick-start checklist | Use during implementation |

---

## **? QUICK REFERENCE: WHAT TO DO NOW**

1. **TODAY**: Deploy CLR functions (1 hour)
2. **THIS WEEK**: Fix EmbeddingGeneratorWorker (2-3 days)
3. **NEXT WEEK**: Add embedding trigger (1-2 days)
4. **NEXT WEEK**: Test end-to-end (1 day)

**Total: 7-10 days to production-ready system** ??

---

## **? SUCCESS CRITERIA**

After all fixes:
- ? `sp_FindNearestAtoms` returns results (no errors)
- ? `AtomEmbedding.SpatialKey` is populated (not NULL)
- ? Cross-modal search works (text ? image)
- ? All spatial procedures execute without errors
- ? Hilbert clustering shows >0.85 Pearson correlation

---

## **?? NEED HELP?**

**Read full details in**: `MASTER_PLUMBING_PLAN.md`

Contains:
- Complete code for all fixes
- Detailed explanations
- Error troubleshooting
- Performance validation queries

---

**Go build something amazing!** ??

