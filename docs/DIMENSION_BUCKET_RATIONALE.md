# Dimension Bucket Architecture - Why It Exists

## The Problem

SQL Server 2025 `VECTOR(n)` type **requires fixed dimension at table creation**:

```sql
-- This works:
CREATE TABLE Weights (vector VECTOR(768))

-- This does NOT work:
CREATE TABLE Weights (vector VECTOR(?))  -- Variable dimension? NO!
```

**You CANNOT have a single column that stores both VECTOR(768) and VECTOR(1536).**

## The Three Options

### Option 1: Pad to Maximum (REJECTED)
```sql
CREATE TABLE Weights (
    vector VECTOR(1998)  -- Pad all to max
)
```
**Storage waste**: 768-dim model padded to 1998 = **160% overhead** = 4.8 TB wasted at billion-weight scale

### Option 2: Store as VARBINARY (REJECTED)
```sql
CREATE TABLE Weights (
    vector_bytes VARBINARY(MAX)
)
```
**Loses everything**:
- ❌ No VECTOR_DISTANCE()
- ❌ No DiskANN indexing
- ❌ No native vector operations
- ❌ Defeats entire purpose of SQL Server 2025

### Option 3: Dimension-Specific Tables (CHOSEN)
```sql
CREATE TABLE Weights_768 (vector VECTOR(768))
CREATE TABLE Weights_1536 (vector VECTOR(1536))
CREATE TABLE Weights_1998 (vector VECTOR(1998))
```
**Benefits**:
- ✅ No storage waste
- ✅ Native VECTOR indexing
- ✅ DiskANN works optimally
- ✅ Index-only scans possible

## Why These Specific Dimensions?

**768**: Most common (BERT, GPT-2, sentence-transformers)
**1536**: OpenAI embeddings standard
**1998**: Max float32 dimension (SQL Server limit)
**3996**: Max float16 dimension (SQL Server limit)

## Adding New Dimensions

If you need dimension 512 or 1024 or 2048:

### 1. Create table
```sql
CREATE TABLE dbo.Weights_512 (
    weight_id BIGINT IDENTITY(1,1) PRIMARY KEY,
    model_id INT,
    layer_idx INT,
    component_type NVARCHAR(50),
    weight_vector VECTOR(512),  -- NEW DIMENSION
    importance_score FLOAT,
    ...
)
```

### 2. Create EF Core entity
```csharp
public class Weight512 : WeightBase
{
    public override string WeightVectorJson { get; set; } = null!;
    public override int Dimension => 512;
}
```

### 3. Add configuration
```csharp
public class Weight512Configuration : WeightConfigurationBase<Weight512>
{
    protected override string TableName => "Weights_512";
    protected override string VectorColumnType => "VECTOR(512)";
}
```

### 4. Register in DI
```csharp
services.AddScoped<IWeightRepository<Weight512>, WeightRepository<Weight512>>();
```

### 5. Update ModelArchitecture.GetWeightsTableName()
```csharp
return dimension switch
{
    512 => "Weights_512",  // ADD THIS
    768 => "Weights_768",
    ...
};
```

**That's it!** 5 minutes of work to support any new dimension.

## Why This is NOT a Hack

**Mathematical truth**: A 768-dim vector and 1536-dim vector exist in **different spaces**. You cannot:
- Compare them directly (different dimensions)
- Store them in same column (SQL Server constraint)
- Deduplicate across them (mathematically invalid)

The dimension buckets reflect **mathematical reality** + **SQL Server constraints**.

## Alternative if You Hate This

If you REALLY want variable dimensions in one table, you'd need to:

1. **Store as JSON/VARBINARY** → lose all vector features
2. **Implement vector operations in C# CLR** → reinvent SQL Server 2025
3. **Use external vector database** → defeats "database is the model" vision

The dimension bucket architecture is the **only solution** that preserves:
- Native VECTOR indexing
- DiskANN navigation
- Index-only scans
- Zero storage waste
- Your revolutionary vision intact

## The Real Question

**How often do you query ACROSS models with different dimensions?**

Answer: Almost never! Because:
- Student model extraction = same parent model = same dimension
- Deduplication = within dimension class (mathematically required)
- Inference = single model = single dimension

Cross-dimension operations are **mathematically invalid** anyway.

---

**TL;DR**: This isn't a hack - it's the only correct solution given SQL Server 2025's architecture. And it takes 5 minutes to add support for any new dimension you need.
