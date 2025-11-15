# Hartonomous Quick Start Guide

**Deploy and run your first atomic ingestion in 30 minutes**

---

## Prerequisites

- **SQL Server 2025 Developer Edition** (or higher)
- **.NET 9.0 SDK**
- **PowerShell 7.4+**
- **16GB RAM minimum** (32GB recommended)
- **Windows 11** or **Linux (Ubuntu 22.04+)**

---

## Installation

### 1. Clone Repository

```powershell
git clone https://github.com/AHartTN/Hartonomous-Sandbox.git
cd Hartonomous-Sandbox
```

### 2. Configure Database Connection

Edit `src/Hartonomous.Database/Hartonomous.Database.sqlproj`:

```xml
<SqlCmdVariable Include="DatabaseName">
  <DefaultValue>Hartonomous</DefaultValue>
</SqlCmdVariable>
<SqlCmdVariable Include="DefaultDataPath">
  <DefaultValue>C:\SQLData\</DefaultValue>
</SqlCmdVariable>
```

### 3. Deploy Database Schema

```powershell
.\scripts\deploy-dacpac.ps1 -Server "localhost" -Database "Hartonomous"
```

**What this does:**
- Creates 99 tables (Atoms, TensorAtoms, AtomRelations, AtomEmbeddings, Models, etc.)
- Deploys 74 stored procedures (OODA loop, governed atomization, inference, search, reconstruction, provenance)
- Registers 60+ CLR functions (atomizers, spatial operations, neural aggregates)
- Sets up Service Broker for autonomous operations
- Configures spatial indexes, temporal tables, columnstore indexes

**Expected output:**
```
✓ Schema deployed successfully
✓ CLR assemblies registered (UNSAFE permission granted)
✓ Service Broker enabled
✓ Spatial indexes created
✓ Temporal tables configured
Database ready for ingestion
```

### 4. Verify Installation

```sql
USE Hartonomous;

-- Check core tables
SELECT COUNT(*) AS TableCount FROM sys.tables WHERE schema_id = SCHEMA_ID('dbo');
-- Expected: 30+

-- Check stored procedures
SELECT COUNT(*) AS ProcedureCount FROM sys.procedures WHERE schema_id = SCHEMA_ID('dbo');
-- Expected: 74

-- Check CLR functions
SELECT COUNT(*) AS CLRFunctionCount FROM sys.assembly_modules;
-- Expected: 60+

-- Verify Service Broker
SELECT is_broker_enabled FROM sys.databases WHERE name = 'Hartonomous';
-- Expected: 1
```

---

## Your First Ingestion

### Ingest Text Document

```sql
-- Create parent atom for document
INSERT INTO dbo.Atoms (Modality, Subtype, ContentHash, AtomicValue)
VALUES ('text', 'document', HASHBYTES('SHA2_256', 'Sample Document'), NULL);

DECLARE @ParentAtomId BIGINT = SCOPE_IDENTITY();

-- Atomize text content
EXEC dbo.sp_AtomizeText
    @ParentAtomId = @ParentAtomId,
    @TextContent = N'The sky is blue. The ocean is also blue. Blue is everywhere.',
    @ChunkingStrategy = 'Sentence',
    @OverlapTokens = 0;
```

**What happened:**
1. Created parent atom (document container, `AtomicValue = NULL`)
2. Decomposed into 3 sentence atoms
3. SHA-256 hash computed for each sentence
4. Deduplicated "blue" tokens (stored once, referenced 3 times)
5. Stored in `dbo.Atoms` with structural relationships in `dbo.AtomRelations`

**Verify deduplication:**
```sql
SELECT 
    a.AtomId,
    a.CanonicalText,
    a.ReferenceCount,
    a.ContentHash
FROM dbo.Atoms a
WHERE a.Modality = 'text' AND a.Subtype = 'sentence'
ORDER BY a.ReferenceCount DESC;
```

---

## Your First Query

### Semantic Search

```sql
-- Find atoms similar to query
EXEC dbo.sp_SemanticSearch
    @QueryText = N'ocean water',
    @TopK = 5,
    @ModelName = NULL; -- Uses default embedding model
```

### Spatial KNN (Nearest Neighbors)

```sql
-- Find atoms near a point in embedding space
DECLARE @QueryPoint GEOMETRY = GEOMETRY::Point(0.5, 0.5, 0.5, 0);

SELECT TOP 5
    ae.AtomId,
    a.CanonicalText,
    ae.SpatialKey.STDistance(@QueryPoint) AS Distance
FROM dbo.AtomEmbeddings ae
JOIN dbo.Atoms a ON ae.AtomId = a.AtomId
WHERE ae.SpatialKey IS NOT NULL
ORDER BY ae.SpatialKey.STDistance(@QueryPoint);
```

### Cross-Modal Query

```sql
-- Find images sharing "blue" with text (after pixel atomization)
SELECT DISTINCT
    img.AtomId AS ImageAtomId,
    img.CanonicalText AS ImagePath,
    txt.CanonicalText AS MatchingText
FROM dbo.Atoms txt
JOIN dbo.AtomRelations ar1 ON txt.AtomId = ar1.SourceAtomId
JOIN dbo.Atoms shared ON ar1.TargetAtomId = shared.AtomId
JOIN dbo.AtomRelations ar2 ON shared.AtomId = ar2.TargetAtomId
JOIN dbo.Atoms img ON ar2.SourceAtomId = img.AtomId
WHERE txt.Modality = 'text'
  AND img.Modality = 'image'
  AND shared.Subtype = 'rgb-pixel'
  AND shared.SpatialKey.STDistance(GEOMETRY::Point(135, 206, 235, 0)) < 10; -- Sky blue
```

---

## Understanding the Output

After atomization, you'll see:

1. **Parent Atom** (`AtomicValue = NULL`) - represents the whole document/image/model
2. **Component Atoms** (`AtomicValue = <64 bytes`) - fundamental units (sentences, pixels, weights)
3. **Structural Relationships** (`dbo.AtomRelations`) - sequential/spatial composition
4. **Semantic Embeddings** (`dbo.AtomEmbeddings`) - 3D spatial projections for similarity search
5. **Deduplication** - Identical content stored once, `ReferenceCount` incremented

**Example: "The sky is blue" document**
```
Parent: AtomId=1, Modality='text', Subtype='document', AtomicValue=NULL, ReferenceCount=1
  ↓ (AtomRelations: SequenceIndex=0)
Component: AtomId=2, Modality='text', Subtype='sentence', AtomicValue='The sky is blue', ReferenceCount=1
```

---

## Next Steps

- **[Architecture Guide](architecture/README.md)** - Understand atomic decomposition, spatial indexing, OODA loop
- **[Database Reference](database/README.md)** - Complete schema, procedures, CLR functions
- **[Query Patterns](queries/README.md)** - Advanced spatial, temporal, cross-modal queries
- **[Development Guide](development/README.md)** - DACPAC workflow, adding atomizers, testing

---

## Troubleshooting

### CLR Functions Not Working

```sql
-- Enable CLR
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;

-- Enable UNSAFE assemblies (required for SIMD, file I/O)
EXEC sp_configure 'clr strict security', 0;
RECONFIGURE;
```

### Service Broker Not Starting

```sql
-- Check broker status
SELECT is_broker_enabled FROM sys.databases WHERE name = 'Hartonomous';

-- Enable if disabled
ALTER DATABASE Hartonomous SET ENABLE_BROKER;
```

### Spatial Index Errors

```sql
-- Rebuild spatial indexes
ALTER INDEX SIX_AtomEmbeddings_SpatialKey ON dbo.AtomEmbeddings REBUILD;
ALTER INDEX SIX_AtomRelations_SpatialExpression ON dbo.AtomRelations REBUILD;
```

---

**You've just deployed a database that treats knowledge like the periodic table - every atom precisely defined, perfectly deduplicated, infinitely queryable.**
