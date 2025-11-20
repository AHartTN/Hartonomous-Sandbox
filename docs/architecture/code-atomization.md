# Code Atomization: AST as Atoms

**Status**: Architecture Correction (Nov 2025)  
**Framework**: .NET Framework 4.8.1 + Roslyn  
**Pattern**: AST Nodes → Atom Rows (NOT CodeAtom table)

---

## Executive Summary

**CRITICAL DESIGN CORRECTION**: The `dbo.CodeAtom` table violates atomic decomposition principles and is **DEPRECATED**.

**Correct Pattern**: Code is atomized using the SAME pattern as AI models and documents:
- Each Roslyn **SyntaxNode** = ONE **Atom** row
- AST hierarchy = **AtomRelation** edges with `RelationType='AST_CONTAINS'`
- Code embeddings = **AtomEmbedding** with spatial R-Tree indexing
- Cross-modal queries = "Find code similar to this text" via shared spatial index

---

## The Problem: CodeAtom Table Violations

### Architectural Violations

| Violation | CodeAtom (WRONG) | Atom (CORRECT) |
|-----------|------------------|----------------|
| **Atomic Decomposition** | Stores entire code snippets | Each AST node is an Atom (64-byte limit) |
| **Modality Support** | Code-only silo | Universal: text, code, image, audio, tensor |
| **Cross-Modal Queries** | Impossible | "Find code similar to this text description" |
| **Temporal Versioning** | Manual `CreatedAt`/`UpdatedAt` | `SYSTEM_VERSIONING` (automatic history) |
| **Multi-Tenancy** | Missing `TenantId` | Full tenant isolation via RLS |
| **Deduplication** | `CodeHash` (code-specific) | `ContentHash` (universal SHA-256) |
| **Normalization** | `Embedding` column on table | `AtomEmbedding` join table |
| **AST Hierarchy** | No structure tracking | `AtomRelation` graph with `AST_CONTAINS` |
| **Data Type** | `TEXT` (deprecated SQL type) | `NVARCHAR(MAX)` (modern Unicode) |
| **Referential Integrity** | No CASCADE | Automatic CASCADE cleanup |

### Schema Comparison

```sql
-- WRONG: CodeAtom table (DEPRECATED)
CREATE TABLE dbo.CodeAtom (
    CodeAtomId BIGINT IDENTITY PRIMARY KEY,
    Language NVARCHAR(50) NOT NULL,
    Code TEXT NOT NULL,  -- ❌ DEPRECATED TYPE
    Framework NVARCHAR(200) NULL,
    Embedding GEOMETRY NULL,  -- ❌ BREAKS NORMALIZATION
    CodeHash VARBINARY(32) NULL,  -- ❌ CODE-SPECIFIC
    Tags JSON NULL,
    QualityScore REAL NULL,
    UsageCount INT NOT NULL DEFAULT 0,
    -- ❌ NO TenantId, NO SYSTEM_VERSIONING
);

-- CORRECT: Atom table with Modality='code'
CREATE TABLE dbo.Atom (
    AtomId BIGINT IDENTITY PRIMARY KEY,
    TenantId INT NOT NULL,  -- ✅ MULTI-TENANCY
    Modality VARCHAR(50) NOT NULL,  -- ✅ 'code' (also text, image, audio, tensor)
    Subtype VARCHAR(50) NULL,  -- ✅ 'MethodDeclaration', 'ClassDeclaration', etc.
    ContentHash BINARY(32) NOT NULL,  -- ✅ UNIVERSAL DEDUPLICATION
    AtomicValue VARBINARY(64) NULL,  -- ✅ 64-BYTE ATOMIC LIMIT
    CanonicalText NVARCHAR(MAX) NULL,  -- ✅ MODERN UNICODE
    Metadata JSON NULL,  -- ✅ EXTENSIBLE (Language, Framework, Tags, QualityScore, etc.)
    SYSTEM_VERSIONING = ON  -- ✅ TEMPORAL HISTORY
);

-- ✅ Normalized embeddings
CREATE TABLE dbo.AtomEmbedding (
    AtomEmbeddingId BIGINT IDENTITY PRIMARY KEY,
    AtomId BIGINT NOT NULL FOREIGN KEY REFERENCES dbo.Atom(AtomId) ON DELETE CASCADE,
    SpatialKey GEOMETRY NOT NULL,  -- 3D projection for R-Tree
    EmbeddingVector VECTOR(1998) NOT NULL,  -- Full-dimensional vector
    UNIQUE (AtomId, ModelId)
);

-- ✅ AST hierarchy
CREATE TABLE dbo.AtomRelation (
    RelationId BIGINT IDENTITY PRIMARY KEY,
    FromAtomId BIGINT NOT NULL,  -- Parent AST node
    ToAtomId BIGINT NOT NULL,  -- Child AST node
    RelationType NVARCHAR(50) NOT NULL,  -- 'AST_CONTAINS'
    SequenceIndex INT NULL,  -- Order in parent's child list
    FOREIGN KEY (FromAtomId) REFERENCES dbo.Atom(AtomId),
    FOREIGN KEY (ToAtomId) REFERENCES dbo.Atom(AtomId)
);
```

---

## Roslyn Integration: C# AST Atomization

### Overview

**Roslyn** (Microsoft.CodeAnalysis) is the .NET Compiler Platform providing:
- **SyntaxTree**: Parsed representation of C# source code
- **SyntaxNode**: Base class for all AST nodes (classes, methods, statements, etc.)
- **SyntaxFactory**: Programmatic creation of syntax nodes for code generation
- **SyntaxRewriter**: AST transformation (refactoring, optimization)
- **SyntaxWalker**: AST traversal for analysis

### CLR Function: sp_AtomizeCode

**Purpose**: Walk Roslyn SyntaxTree depth-first, create Atom for each SyntaxNode

**Implementation**:

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Data.SqlTypes;
using System.Security.Cryptography;

namespace Hartonomous.Clr.CodeAtomizers
{
    public class RoslynAtomizer
    {
        [SqlProcedure]
        public static void AtomizeCode(
            SqlString sourceCode,
            SqlString language,
            SqlString framework,
            SqlInt32 tenantId,
            out SqlInt64 rootAtomId)
        {
            // 1. Parse C# source to Roslyn SyntaxTree
            SyntaxTree tree = CSharpSyntaxTree.ParseText(sourceCode.Value);
            CompilationUnit root = tree.GetCompilationUnitRoot();
            
            // 2. Walk AST depth-first, create Atoms
            rootAtomId = WalkAndAtomize(
                node: root,
                parentAtomId: null,
                language: language.Value,
                framework: framework.Value,
                tenantId: tenantId.Value
            );
        }
        
        private static long WalkAndAtomize(
            SyntaxNode node,
            long? parentAtomId,
            string language,
            string framework,
            int tenantId)
        {
            // Extract node properties
            string syntaxKind = node.Kind().ToString();
            string canonicalText = node.ToFullString();  // With trivia
            byte[] serialized = SerializeSyntaxNode(node);
            byte[] contentHash = SHA256.HashData(serialized);
            
            // Build Metadata JSON
            var metadata = new
            {
                Language = language,
                Framework = framework,
                SyntaxKind = syntaxKind,
                RoslynType = node.GetType().FullName,
                Span = new { Start = node.Span.Start, Length = node.Span.Length },
                LeadingTrivia = node.GetLeadingTrivia().ToFullString(),
                TrailingTrivia = node.GetTrailingTrivia().ToFullString(),
                CyclomaticComplexity = CalculateCyclomaticComplexity(node),
                Depth = GetDepth(node)
            };
            
            string metadataJson = JsonConvert.SerializeObject(metadata);
            
            // Insert Atom
            long atomId = ExecuteSql(
                @"INSERT INTO dbo.Atom (
                    TenantId, Modality, Subtype, ContentHash, 
                    AtomicValue, CanonicalText, Metadata
                  )
                  OUTPUT INSERTED.AtomId
                  VALUES (
                    @TenantId, 'code', @Subtype, @ContentHash,
                    @AtomicValue, @CanonicalText, @Metadata
                  )",
                new SqlParameter("@TenantId", tenantId),
                new SqlParameter("@Subtype", syntaxKind),
                new SqlParameter("@ContentHash", contentHash),
                new SqlParameter("@AtomicValue", serialized.Length <= 64 ? serialized : DBNull.Value),
                new SqlParameter("@CanonicalText", canonicalText),
                new SqlParameter("@Metadata", metadataJson)
            );
            
            // Generate embeddings
            byte[] astVector = GenerateAstVector(node, metadata);
            SqlGeometry spatialKey = ProjectToGeometry(astVector);
            
            ExecuteSql(
                @"INSERT INTO dbo.AtomEmbedding (AtomId, ModelId, SpatialKey, EmbeddingVector)
                  VALUES (@AtomId, @ModelId, @SpatialKey, @EmbeddingVector)",
                new SqlParameter("@AtomId", atomId),
                new SqlParameter("@ModelId", GetCodeEmbeddingModelId()),
                new SqlParameter("@SpatialKey", spatialKey),
                new SqlParameter("@EmbeddingVector", astVector)
            );
            
            // Create parent-child relationship
            if (parentAtomId.HasValue)
            {
                ExecuteSql(
                    @"INSERT INTO dbo.AtomRelation (FromAtomId, ToAtomId, RelationType, SequenceIndex)
                      VALUES (@ParentAtomId, @AtomId, 'AST_CONTAINS', @SequenceIndex)",
                    new SqlParameter("@ParentAtomId", parentAtomId.Value),
                    new SqlParameter("@AtomId", atomId),
                    new SqlParameter("@SequenceIndex", GetChildIndex(node))
                );
            }
            
            // Recursively atomize children
            foreach (SyntaxNode child in node.ChildNodes())
            {
                WalkAndAtomize(child, atomId, language, framework, tenantId);
            }
            
            return atomId;
        }
        
        private static byte[] GenerateAstVector(SyntaxNode node, dynamic metadata)
        {
            float[] features = new float[1998];  // Match embedding dimension
            
            // 1. Encode SyntaxKind (one-hot)
            int kindIndex = (int)node.Kind();
            if (kindIndex < 300) features[kindIndex] = 1.0f;
            
            // 2. Structural features
            features[300] = metadata.Depth / 100.0f;
            features[301] = node.ChildNodes().Count() / 50.0f;
            features[302] = metadata.CyclomaticComplexity / 20.0f;
            features[303] = node.Span.Length / 10000.0f;
            
            // 3. Token type distribution
            var tokens = node.DescendantTokens().GroupBy(t => t.Kind()).ToDictionary(g => g.Key, g => g.Count());
            foreach (var kvp in tokens.Take(100))
            {
                features[304 + (int)kvp.Key % 100] = kvp.Value / 100.0f;
            }
            
            // 4. Identifier entropy
            var identifiers = node.DescendantTokens().Where(t => t.IsKind(SyntaxKind.IdentifierToken)).Select(t => t.Text);
            features[404] = CalculateEntropy(identifiers);
            
            // 5. Gram-Schmidt orthogonalization
            return GramSchmidtOrthogonalize(features);
        }
    }
}
```

### AST Structure Examples

#### Example 1: Simple Method

**C# Source**:
```csharp
public void ProcessData(int count)
{
    if (count > 0)
    {
        Console.WriteLine("Processing...");
    }
}
```

**Atomized Structure** (7 Atoms):

```
Atom 1: MethodDeclaration (root)
  Modality: 'code'
  Subtype: 'MethodDeclaration'
  CanonicalText: 'public void ProcessData(int count) { ... }'
  Metadata: { "SyntaxKind": "MethodDeclaration", "Modifiers": ["public"], ... }
  
  ├─ Atom 2: ParameterList
  │    Subtype: 'ParameterList'
  │    CanonicalText: '(int count)'
  │    Relation: FromAtomId=1, ToAtomId=2, RelationType='AST_CONTAINS', SequenceIndex=0
  │
  │    └─ Atom 3: Parameter
  │         Subtype: 'Parameter'
  │         CanonicalText: 'int count'
  │         Relation: FromAtomId=2, ToAtomId=3, RelationType='AST_CONTAINS'
  │
  └─ Atom 4: Block
       Subtype: 'Block'
       CanonicalText: '{ if (count > 0) { ... } }'
       Relation: FromAtomId=1, ToAtomId=4, RelationType='AST_CONTAINS', SequenceIndex=1
       
       └─ Atom 5: IfStatement
            Subtype: 'IfStatement'
            CanonicalText: 'if (count > 0) { ... }'
            Relation: FromAtomId=4, ToAtomId=5, RelationType='AST_CONTAINS'
            
            ├─ Atom 6: BinaryExpression (condition)
            │    Subtype: 'GreaterThanExpression'
            │    CanonicalText: 'count > 0'
            │    Relation: FromAtomId=5, ToAtomId=6
            │
            └─ Atom 7: Block (then clause)
                 Subtype: 'Block'
                 CanonicalText: '{ Console.WriteLine("Processing..."); }'
                 Relation: FromAtomId=5, ToAtomId=7
```

**Queries**:

```sql
-- Reconstruct method from atoms
WITH RECURSIVE AstTree AS (
    SELECT a.AtomId, a.CanonicalText, ar.SequenceIndex, 0 AS Depth
    FROM dbo.Atom a
    WHERE a.Modality = 'code' AND a.Subtype = 'MethodDeclaration' AND a.AtomId = @rootAtomId
    
    UNION ALL
    
    SELECT a.AtomId, a.CanonicalText, ar.SequenceIndex, at.Depth + 1
    FROM dbo.Atom a
    INNER JOIN dbo.AtomRelation ar ON a.AtomId = ar.ToAtomId
    INNER JOIN AstTree at ON ar.FromAtomId = at.AtomId
    WHERE ar.RelationType = 'AST_CONTAINS'
)
SELECT REPLICATE('  ', Depth) + CanonicalText AS IndentedCode
FROM AstTree
ORDER BY SequenceIndex;

-- Find all methods with similar structure (spatial query)
DECLARE @queryPoint GEOMETRY = (
    SELECT SpatialKey 
    FROM dbo.AtomEmbedding 
    WHERE AtomId = @targetMethodAtomId
);

SELECT TOP 10
    a.CanonicalText AS SimilarMethod,
    ae.SpatialKey.STDistance(@queryPoint) AS StructuralDistance
FROM dbo.AtomEmbedding ae
INNER JOIN dbo.Atom a ON ae.AtomId = a.AtomId
WHERE a.Modality = 'code'
  AND a.Subtype = 'MethodDeclaration'
  AND ae.SpatialKey.STIntersects(@queryPoint.STBuffer(5.0)) = 1
ORDER BY StructuralDistance ASC;
```

---

## Migration Path: CodeAtom → Atom

### Step 1: Backup Existing Data

```sql
-- Backup CodeAtom table
SELECT * INTO dbo.CodeAtom_Backup_20251120
FROM dbo.CodeAtom;

-- Backup existing embeddings
SELECT ca.CodeAtomId, ca.Embedding
INTO dbo.CodeAtomEmbedding_Backup_20251120
FROM dbo.CodeAtom ca
WHERE ca.Embedding IS NOT NULL;
```

### Step 2: Migrate to Atom Table

```sql
-- Migrate CodeAtom rows to Atom
INSERT INTO dbo.Atom (
    Modality,
    Subtype,
    ContentHash,
    CanonicalText,
    Metadata,
    TenantId
)
OUTPUT INSERTED.AtomId, INSERTED.ContentHash INTO @MigrationMapping
SELECT 
    'code' AS Modality,
    ca.CodeType AS Subtype,
    ca.CodeHash AS ContentHash,
    CAST(ca.Code AS NVARCHAR(MAX)) AS CanonicalText,  -- TEXT → NVARCHAR(MAX)
    JSON_OBJECT(
        'Language': ca.Language,
        'Framework': ca.Framework,
        'QualityScore': ca.QualityScore,
        'UsageCount': ca.UsageCount,
        'Tags': JSON_QUERY(ca.Tags),
        'Description': ca.Description,
        'CreatedBy': ca.CreatedBy,
        'LegacyCodeAtomId': ca.CodeAtomId
    ) AS Metadata,
    0 AS TenantId  -- Default tenant (update as needed)
FROM dbo.CodeAtom ca;
```

### Step 3: Migrate Embeddings

```sql
-- Migrate embeddings to AtomEmbedding
INSERT INTO dbo.AtomEmbedding (AtomId, ModelId, SpatialKey)
SELECT 
    mm.AtomId,
    @CodeEmbeddingModelId AS ModelId,
    ca.Embedding AS SpatialKey
FROM dbo.CodeAtom ca
INNER JOIN @MigrationMapping mm ON ca.CodeHash = mm.ContentHash
WHERE ca.Embedding IS NOT NULL;
```

### Step 4: Verify Migration

```sql
-- Verify row count matches
DECLARE @CodeAtomCount INT = (SELECT COUNT(*) FROM dbo.CodeAtom);
DECLARE @MigratedCount INT = (
    SELECT COUNT(*) 
    FROM dbo.Atom 
    WHERE Modality = 'code' 
      AND JSON_VALUE(Metadata, '$.LegacyCodeAtomId') IS NOT NULL
);

IF @CodeAtomCount = @MigratedCount
    PRINT 'Migration successful: ' + CAST(@MigratedCount AS VARCHAR) + ' rows migrated';
ELSE
    RAISERROR('Migration incomplete: %d CodeAtom rows, %d migrated Atom rows', 16, 1, @CodeAtomCount, @MigratedCount);
```

### Step 5: Drop Legacy Table

```sql
-- After thorough verification (wait 30 days)
DROP TABLE dbo.CodeAtom;

-- Drop backup after 90 days
-- DROP TABLE dbo.CodeAtom_Backup_20251120;
-- DROP TABLE dbo.CodeAtomEmbedding_Backup_20251120;
```

---

## Cross-Language Support

### Tree-sitter Integration

For non-.NET languages, use **Tree-sitter** (incremental parsing library):

| Language | Tree-sitter Parser | AST Node Mapping |
|----------|-------------------|------------------|
| Python | tree-sitter-python | function_definition → Subtype='FunctionDefinition' |
| JavaScript | tree-sitter-javascript | function_declaration → Subtype='FunctionDeclaration' |
| TypeScript | tree-sitter-typescript | interface_declaration → Subtype='InterfaceDeclaration' |
| Go | tree-sitter-go | function_declaration → Subtype='FunctionDeclaration' |
| Rust | tree-sitter-rust | function_item → Subtype='FunctionItem' |
| Java | tree-sitter-java | method_declaration → Subtype='MethodDeclaration' |

**CLR Integration**:

```csharp
// Tree-sitter C# wrapper
[SqlProcedure]
public static void AtomizeCodeTreeSitter(
    SqlString sourceCode,
    SqlString language,
    SqlInt32 tenantId,
    out SqlInt64 rootAtomId)
{
    // 1. Get Tree-sitter parser for language
    Parser parser = GetTreeSitterParser(language.Value);
    
    // 2. Parse source to Tree-sitter Tree
    Tree tree = parser.Parse(sourceCode.Value);
    Node rootNode = tree.RootNode;
    
    // 3. Walk tree, create Atoms
    rootAtomId = WalkTreeSitterNode(rootNode, null, language.Value, tenantId.Value);
}
```

---

## Benefits Summary

### What You Gain

1. **✅ Universal Atomic Decomposition**
   - ALL content types (text, code, images, audio, tensors) use SAME storage pattern
   - No special-case code tables

2. **✅ Cross-Modal Queries**
   - "Find code that implements this text description"
   - "Find documentation similar to this code"
   - Shared spatial R-Tree enables semantic bridging

3. **✅ Temporal Versioning**
   - `SYSTEM_VERSIONING` tracks every code change automatically
   - Time-travel queries: "Show this method as it existed on Nov 15"

4. **✅ Multi-Tenancy**
   - Row-level security isolates code by tenant
   - Shared code libraries deduplicate across tenants

5. **✅ AST-Level Refactoring**
   - Store SyntaxRewriter transformations as new Atoms
   - Version AST changes, not just text diffs

6. **✅ Round-Trip Fidelity**
   - Reconstruct exact Roslyn SyntaxTree from Atoms
   - No information loss (trivia, spans, metadata preserved)

7. **✅ Deduplication Across Projects**
   - Same utility function used in 10 projects: 1 Atom, 10 references
   - 30-40% storage reduction for shared code

8. **✅ Spatial Code Search**
   - O(log N) queries for "find similar code structure"
   - R-Tree enables structural similarity at scale

### What You Don't Lose

- ❌ **Code storage functionality**: All preserved in `Atom.CanonicalText` + `Atom.Metadata`
- ❌ **Embeddings**: Migrated to `AtomEmbedding` (normalized, not duplicated)
- ❌ **Quality scores**: Stored in `Metadata` JSON
- ❌ **Usage tracking**: Stored in `Metadata` JSON or `Atom.ReferenceCount`

---

## Next Steps

1. **Update `sp_AtomizeCode`**: Modify to insert into `Atom` instead of `CodeAtom`
2. **Migrate existing CodeAtom data**: Follow migration path above
3. **Update documentation**: Mark `CodeAtom` as deprecated in all docs
4. **Add AST query examples**: Document common queries (find methods, refactor classes)
5. **Implement Tree-sitter support**: Add cross-language AST parsing
6. **Performance benchmarks**: Validate O(log N) code search performance

---

## Related Documentation

- [Semantic-First Architecture](semantic-first.md) - How spatial R-Tree enables O(log N) queries
- [Model Atomization](model-atomization.md) - AI model decomposition pattern (same as code)
- [Database Schema](../implementation/database-schema.md) - Complete Atom table schema
- [SQL Audit Part 5](../../SQL_AUDIT_PART5.md) - CodeAtom architectural analysis

---

**Document Version**: 1.0  
**Date**: November 20, 2025  
**Status**: Architecture Correction (CodeAtom → Atom migration)
