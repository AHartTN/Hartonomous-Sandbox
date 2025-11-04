# Code Generation via Atom Infrastructure

## Overview
T-SQL code generation uses the **existing Atom/AtomEmbedding architecture** - no separate CodeAtom table needed.

## Field Usage for Code Atoms

| Field | Purpose | Example Values |
|-------|---------|----------------|
| `Modality` | PRIMARY categorization - what type of atom | `"code"` |
| `Subtype` | SECONDARY categorization - language/dialect | `"tsql"`, `"csharp"`, `"fsharp"`, `"clr"` |
| `SourceType` | Where the code came from | `"filesystem"`, `"git"`, `"generated"`, `"optimized"` |
| `Metadata.CodeType` | Specific artifact type (stored in JSON) | `"procedure"`, `"function"`, `"view"`, `"trigger"` |
| `Metadata.Language` | Full language name | `"TSql"`, `"CSharp"`, `"FSharp"` |
| `Metadata.Framework` | Runtime/platform | `"SQL Server 2025"`, `".NET 10 CLR"` |

### Query Examples

```sql
-- Find all T-SQL code
SELECT * FROM Atoms WHERE Modality = 'code' AND Subtype = 'tsql'

-- Find AI-generated code (any language)
SELECT * FROM Atoms WHERE Modality = 'code' AND SourceType = 'generated'

-- Find T-SQL stored procedures specifically
SELECT * FROM Atoms 
WHERE Modality = 'code' 
  AND Subtype = 'tsql'
  AND JSON_VALUE(Metadata, '$.CodeType') = 'procedure'

-- Find CLR functions
SELECT * FROM Atoms WHERE Modality = 'code' AND Subtype = 'clr'
```

## Ingesting T-SQL Code

### Example: Store a T-SQL Procedure
```csharp
var tsqlCode = @"
CREATE OR ALTER PROCEDURE dbo.sp_OptimizedVectorSearch
    @queryVector VECTOR(1998),
    @topK INT = 10
AS
BEGIN
    SELECT TOP (@topK)
        AtomId,
        CanonicalText,
        VECTOR_DISTANCE('cosine', Embedding, @queryVector) AS Distance
    FROM dbo.AtomEmbeddings
    ORDER BY Distance;
END";

var ingestionRequest = new AtomIngestionRequest
{
    HashInput = tsqlCode,
    Modality = "code",              // Code modality (PRIMARY: what type of atom)
    Subtype = "tsql",               // Language/dialect (SECONDARY: language category)
    CanonicalText = tsqlCode,       // The actual code
    Metadata = JsonSerializer.Serialize(new
    {
        Language = "TSql",
        Framework = "SQL Server 2025",
        CodeType = "StoredProcedure",  // Metadata.CodeType: procedure/function/view/trigger/clr
        Description = "Optimized vector similarity search using VECTOR_DISTANCE",
        Tags = new[] { "vector-search", "optimization", "spatial" },
        QualityScore = 0.95,
        UsageCount = 0,
        TestResults = new
        {
            status = "passed",
            duration_ms = 85,
            rows_affected = 10,
            test_date = "2025-11-04T08:00:00Z"
        }
    }),
    Embedding = await _embeddingService.EmbedCodeAsync(tsqlCode),
    EmbeddingType = "code-embedding-tsql",
    SourceUri = "sql/procedures/Search.OptimizedVectorSearch.sql",
    SourceType = "filesystem"
};

var result = await _atomIngestionService.IngestAsync(ingestionRequest);
```

## Metadata Schema for Code Atoms

```json
{
  "Language": "TSql|CSharp|FSharp",
  "Framework": "SQL Server 2025|.NET 10 CLR|EF Core 10",
  "CodeType": "StoredProcedure|Function|View|CLR|Optimization|Migration",
  "Description": "Human-readable description for semantic search",
  "Tags": ["optimization", "vector-search", "ensemble", "spatial"],
  "QualityScore": 0.95,
  "UsageCount": 42,
  "TestResults": {
    "status": "passed|failed",
    "duration_ms": 125,
    "rows_affected": 1000,
    "errors": [],
    "test_date": "2025-11-04T08:00:00Z",
    "performance_metrics": {
      "cpu_ms": 80,
      "logical_reads": 250,
      "physical_reads": 0
    }
  }
}
```

## Querying Code via Semantic Search

### Find Similar T-SQL Patterns
```csharp
// Embed the prompt
var promptEmbedding = await _embeddingService.EmbedCodeAsync(
    "Create a stored procedure that does weighted ensemble inference with spatial projection"
);

// Search existing code atoms
var similarCode = await _atomEmbeddingRepository.SearchByVectorAsync(
    promptEmbedding,
    modality: "code",
    topK: 5
);

// Filter by language
var tsqlExamples = similarCode
    .Where(atom => 
    {
        var metadata = JsonSerializer.Deserialize<CodeMetadata>(atom.Atom.Metadata);
        return metadata.Language == "TSql";
    })
    .OrderByDescending(atom =>
    {
        var metadata = JsonSerializer.Deserialize<CodeMetadata>(atom.Atom.Metadata);
        return metadata.QualityScore ?? 0;
    });
```

## T-SQL Code Generation Procedure

```sql
CREATE OR ALTER PROCEDURE dbo.sp_GenerateCodeFromPrompt
    @prompt NVARCHAR(MAX),
    @language NVARCHAR(50) = 'TSql',
    @topK INT = 10,
    @promptEmbedding VECTOR(1998) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Embed the prompt if not provided
    IF @promptEmbedding IS NULL
    BEGIN
        -- Call embedding generation (placeholder - needs actual implementation)
        THROW 50100, 'Prompt embedding must be provided', 1;
    END;

    -- Search for similar code atoms
    ;WITH SimilarCode AS (
        SELECT
            a.AtomId,
            a.CanonicalText AS Code,
            a.Metadata,
            VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @promptEmbedding) AS Distance,
            JSON_VALUE(a.Metadata, '$.QualityScore') AS QualityScore,
            JSON_VALUE(a.Metadata, '$.Language') AS Language
        FROM dbo.Atoms a
        INNER JOIN dbo.AtomEmbeddings ae ON a.AtomId = ae.AtomId
        WHERE a.Modality = 'code'
          AND a.Subtype = LOWER(@language)
          AND a.IsActive = 1
          AND JSON_VALUE(a.Metadata, '$.Language') = @language
    )
    SELECT TOP (@topK)
        AtomId,
        Code,
        Metadata,
        Distance,
        CAST(QualityScore AS FLOAT) AS QualityScore
    FROM SimilarCode
    WHERE Distance < 0.3  -- Similarity threshold
    ORDER BY 
        Distance ASC,
        CAST(QualityScore AS FLOAT) DESC;
END;
GO
```

## Benefits of Using Existing Atom Architecture

✅ **Deduplication**: ContentHash prevents duplicate code storage
✅ **Provenance**: AtomRelations track code lineage (which code generated which)
✅ **Spatial Indexes**: Already configured for fast vector search
✅ **Multi-Modal**: Can relate T-SQL code to text descriptions, diagrams, test data
✅ **Reference Counting**: Track code usage across the system
✅ **Embedding Infrastructure**: No need to rebuild vector search from scratch

## Code Types Supported

| CodeType | Description | Example |
|----------|-------------|---------|
| `StoredProcedure` | T-SQL procedures | `sp_GenerateCodeFromPrompt` |
| `Function` | Scalar/table-valued functions | `fn_EnsembleAtomScores` |
| `View` | Database views | `vw_ActiveModels` |
| `CLR` | .NET CLR functions | `clr_ComputeVector` |
| `Optimization` | Query hints, indexes | `CREATE INDEX IX_...` |
| `Migration` | Schema changes | `ALTER TABLE ...` |
| `Trigger` | Database triggers | `CREATE TRIGGER ...` |

## Next Steps

1. **Seed Existing T-SQL Procedures**: Ingest all procedures from `sql/procedures/` directory
2. **Implement Embedding Service**: `EmbedCodeAsync(string code)` for T-SQL
3. **Create Generation Logic**: Assemble new code from top-K similar patterns
4. **Docker Sandbox**: Test generated code in isolated SQL Server container
5. **Feedback Loop**: Update QualityScore and UsageCount based on test results
