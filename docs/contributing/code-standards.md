# Code Standards

This document outlines coding conventions, best practices, and style guidelines for Hartonomous.

---

## Table of Contents

1. [C# Coding Standards](#c-coding-standards)
2. [T-SQL Style Guide](#t-sql-style-guide)
3. [PowerShell Best Practices](#powershell-best-practices)
4. [Markdown Formatting](#markdown-formatting)
5. [Git Commit Messages](#git-commit-messages)

---

## C# Coding Standards

### Naming Conventions

#### PascalCase

**Classes, Interfaces, Methods, Properties, Events**:

```csharp
public class AtomQueryService { }
public interface IAtomQueryService { }
public async Task<Atom> GetAtomByIdAsync(int atomId) { }
public string CanonicalHash { get; set; }
public event EventHandler AtomCreated;
```

#### camelCase

**Private fields, local variables, parameters**:

```csharp
private readonly IAtomQueryService _atomQueryService;
private int _atomCount;

public void ProcessAtom(int atomId, string canonicalText)
{
    var processedAtom = Transform(atomId);
    var result = await _atomQueryService.GetAtomAsync(atomId);
}
```

#### UPPER_CASE

**Constants**:

```csharp
public const int MAX_ATOM_SIZE = 64;
public const string DEFAULT_MODALITY = "text";
```

#### Interface Names

**Prefix with `I`**:

```csharp
public interface IAtomizer<T> { }
public interface IReasoningService { }
```

### Code Organization

#### File Structure

**One class per file** (except nested classes):

```
AtomQueryService.cs         // Contains only AtomQueryService class
IAtomQueryService.cs         // Contains only IAtomQueryService interface
```

#### Namespace Organization

```csharp
// Align with folder structure
namespace Hartonomous.Core.Services;

// Using directives: System first, then alphabetical
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data.Entities;
```

#### Class Member Order

```csharp
public class AtomQueryService : IAtomQueryService
{
    // 1. Constants
    private const int MAX_RESULTS = 1000;
    
    // 2. Static fields
    private static readonly ILogger Logger = LoggerFactory.Create();
    
    // 3. Private fields
    private readonly HartonomousContext _context;
    private readonly ILogger<AtomQueryService> _logger;
    
    // 4. Constructor(s)
    public AtomQueryService(
        HartonomousContext context,
        ILogger<AtomQueryService> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    // 5. Public properties
    public int AtomCount { get; private set; }
    
    // 6. Public methods
    public async Task<Atom> GetAtomByIdAsync(int atomId)
    {
        // Implementation
    }
    
    // 7. Private methods
    private bool ValidateAtomId(int atomId)
    {
        // Implementation
    }
}
```

### Async/Await Patterns

#### Always Use Async Suffix

```csharp
// ✅ Good
public async Task<Atom> GetAtomByIdAsync(int atomId)
{
    return await _context.Atoms.FindAsync(atomId);
}

// ❌ Bad
public async Task<Atom> GetAtomById(int atomId)
{
    return await _context.Atoms.FindAsync(atomId);
}
```

#### Avoid Async Void

```csharp
// ✅ Good
public async Task ProcessAtomAsync(int atomId)
{
    await DoWorkAsync();
}

// ❌ Bad (except event handlers)
public async void ProcessAtom(int atomId)
{
    await DoWorkAsync();
}

// ✅ OK for event handlers
private async void OnAtomCreated(object sender, EventArgs e)
{
    await ProcessEventAsync();
}
```

#### ConfigureAwait

**Libraries**: Use `ConfigureAwait(false)` in library code:

```csharp
// In Hartonomous.Core, Hartonomous.Infrastructure
public async Task<Atom> GetAtomAsync(int atomId)
{
    return await _context.Atoms.FindAsync(atomId).ConfigureAwait(false);
}
```

**ASP.NET Controllers**: Do NOT use `ConfigureAwait`:

```csharp
// In Hartonomous.Api controllers
[HttpGet("{id}")]
public async Task<ActionResult<Atom>> GetAtom(int id)
{
    var atom = await _atomService.GetAtomAsync(id); // No ConfigureAwait
    return Ok(atom);
}
```

### LINQ Best Practices

#### Method Syntax vs Query Syntax

**Prefer Method Syntax**:

```csharp
// ✅ Good (Method syntax)
var results = atoms
    .Where(a => a.Modality == "text")
    .OrderBy(a => a.CreatedAt)
    .Take(10)
    .ToList();

// ❌ Avoid (Query syntax)
var results = (from a in atoms
               where a.Modality == "text"
               orderby a.CreatedAt
               select a).Take(10).ToList();
```

#### Deferred Execution

**Understand when queries execute**:

```csharp
// Query definition (not executed yet)
var query = _context.Atoms
    .Where(a => a.Modality == "text")
    .OrderBy(a => a.CreatedAt);

// Execution happens here
var results = await query.ToListAsync(); // Database query executes
var count = await query.CountAsync();     // Another database query
```

**Avoid multiple enumerations**:

```csharp
// ❌ Bad (2 database queries)
var query = _context.Atoms.Where(a => a.Modality == "text");
var count = await query.CountAsync();  // Query 1
var items = await query.ToListAsync(); // Query 2

// ✅ Good (1 database query)
var items = await _context.Atoms
    .Where(a => a.Modality == "text")
    .ToListAsync();
var count = items.Count; // In-memory count
```

### Exception Handling

#### Service Layer: Throw Exceptions

```csharp
public class AtomIngestionService
{
    public async Task<Atom> IngestFileAsync(byte[] fileData, string fileName)
    {
        if (fileData == null || fileData.Length == 0)
            throw new ArgumentException("File data cannot be empty", nameof(fileData));
        
        if (fileData.Length > MAX_FILE_SIZE)
            throw new InvalidOperationException($"File exceeds maximum size of {MAX_FILE_SIZE} bytes");
        
        // Business logic
        return atom;
    }
}
```

#### Controllers: No Try/Catch (Global Handler)

```csharp
// ✅ Good (let exceptions bubble to global handler)
[HttpPost]
public async Task<ActionResult<Atom>> IngestFile(IFormFile file)
{
    var fileData = await file.GetBytesAsync();
    var result = await _ingestionService.IngestFileAsync(fileData, file.FileName);
    return Ok(result);
}

// ❌ Bad (don't catch in controllers)
[HttpPost]
public async Task<ActionResult<Atom>> IngestFile(IFormFile file)
{
    try
    {
        var fileData = await file.GetBytesAsync();
        var result = await _ingestionService.IngestFileAsync(fileData, file.FileName);
        return Ok(result);
    }
    catch (Exception ex)
    {
        return BadRequest(ex.Message); // Don't do this
    }
}
```

### Dependency Injection

#### Constructor Injection

```csharp
public class AtomQueryService
{
    private readonly HartonomousContext _context;
    private readonly ILogger<AtomQueryService> _logger;
    
    public AtomQueryService(
        HartonomousContext context,
        ILogger<AtomQueryService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}
```

#### Service Lifetimes

```csharp
// Scoped: Per-request lifetime (default for EF DbContext)
builder.Services.AddScoped<IAtomQueryService, AtomQueryService>();
builder.Services.AddScoped<IIngestionService, IngestionService>();

// Singleton: Application lifetime (stateless services)
builder.Services.AddSingleton<IFileTypeDetector, FileTypeDetector>();

// Transient: New instance per injection (rarely used)
builder.Services.AddTransient<IAtomizer<byte[]>, ByteArrayAtomizer>();
```

### XML Documentation Comments

**Public APIs**:

```csharp
/// <summary>
/// Retrieves an atom by its unique identifier.
/// </summary>
/// <param name="atomId">The unique atom ID.</param>
/// <returns>The atom if found, otherwise null.</returns>
/// <exception cref="ArgumentOutOfRangeException">
/// Thrown when <paramref name="atomId"/> is less than or equal to 0.
/// </exception>
public async Task<Atom?> GetAtomByIdAsync(int atomId)
{
    if (atomId <= 0)
        throw new ArgumentOutOfRangeException(nameof(atomId), "Atom ID must be greater than 0");
    
    return await _context.Atoms.FindAsync(atomId);
}
```

---

## T-SQL Style Guide

### Naming Conventions

#### Tables: PascalCase

```sql
CREATE TABLE Atom (...);
CREATE TABLE AtomEmbedding (...);
CREATE TABLE GenerationStream (...);
```

#### Stored Procedures: `sp_PascalCase`

```sql
CREATE PROCEDURE dbo.sp_SpatialAStar (...);
CREATE PROCEDURE dbo.sp_BulkInsertAtoms (...);
```

#### Functions: `fn_PascalCase`

```sql
CREATE FUNCTION dbo.fn_CalculateSimilarity (...);
CREATE FUNCTION dbo.fn_ProjectToSpatialCoordinates (...);
```

#### Columns: PascalCase

```sql
CREATE TABLE Atom
(
    AtomId INT PRIMARY KEY,
    CanonicalHash CHAR(64) NOT NULL,
    CanonicalText NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
```

### SQL Formatting

#### Indentation and Capitalization

**Keywords: UPPERCASE**  
**Identifiers: PascalCase**

```sql
-- ✅ Good
SELECT 
    a.AtomId,
    a.CanonicalHash,
    a.CanonicalText,
    ae.EmbeddingVector
FROM dbo.Atom AS a
INNER JOIN dbo.AtomEmbedding AS ae
    ON a.AtomId = ae.AtomId
WHERE a.Modality = 'text'
    AND a.CreatedAt >= '2025-01-01'
ORDER BY a.CreatedAt DESC;

-- ❌ Bad
select a.atomid, a.canonicalHash, a.canonicalText, ae.embeddingVector
from dbo.atom a inner join dbo.atomembedding ae on a.atomid=ae.atomid
where a.modality='text' and a.createdat>='2025-01-01'
order by a.createdat desc;
```

#### Stored Procedure Structure

```sql
-- =============================================
-- Description: Performs A* pathfinding in 3D semantic space
-- Parameters:
--   @StartAtomId: Starting atom ID
--   @GoalX, @GoalY, @GoalZ: Goal coordinates
--   @MaxPathLength: Maximum path hops (default 10)
-- Returns: Path as table of atoms with costs
-- Performance: O(E log V) where E = edges, V = vertices
-- =============================================
CREATE OR ALTER PROCEDURE dbo.sp_SpatialAStar
    @StartAtomId INT,
    @GoalX FLOAT,
    @GoalY FLOAT,
    @GoalZ FLOAT,
    @MaxPathLength INT = 10
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Declare variables
    DECLARE @CurrentAtomId INT;
    DECLARE @CurrentCost FLOAT;
    
    -- Create temp tables
    CREATE TABLE #OpenSet
    (
        AtomId INT PRIMARY KEY,
        FCost FLOAT,
        GCost FLOAT
    );
    
    -- Algorithm implementation
    INSERT INTO #OpenSet (AtomId, FCost, GCost)
    VALUES (@StartAtomId, 0.0, 0.0);
    
    -- Main loop
    WHILE EXISTS (SELECT 1 FROM #OpenSet)
    BEGIN
        -- Find node with lowest F cost
        SELECT TOP 1 
            @CurrentAtomId = AtomId,
            @CurrentCost = GCost
        FROM #OpenSet
        ORDER BY FCost;
        
        -- Process node
        -- ...
    END;
    
    -- Return results
    SELECT * FROM #Path ORDER BY StepNumber;
    
    -- Cleanup
    DROP TABLE #OpenSet;
    DROP TABLE #ClosedSet;
    DROP TABLE #Path;
END;
```

### Query Performance

#### Always Include SET NOCOUNT ON

```sql
CREATE PROCEDURE dbo.sp_GetAtoms
AS
BEGIN
    SET NOCOUNT ON; -- Prevents "X rows affected" messages
    
    SELECT * FROM dbo.Atom;
END;
```

#### Use Appropriate Indexes

```sql
-- Non-clustered index on frequently queried column
CREATE NONCLUSTERED INDEX IX_Atom_CanonicalHash
ON dbo.Atom(CanonicalHash)
INCLUDE (CanonicalText, Modality);

-- Spatial index for geometry queries
CREATE SPATIAL INDEX IX_AtomEmbedding_Spatial
ON dbo.AtomEmbedding(SpatialKey)
USING GEOMETRY_GRID
WITH (
    GRIDS = (LEVEL_1 = HIGH, LEVEL_2 = HIGH, LEVEL_3 = MEDIUM, LEVEL_4 = LOW),
    CELLS_PER_OBJECT = 64
);
```

---

## PowerShell Best Practices

### Naming Conventions

**Functions: Verb-Noun**:

```powershell
# ✅ Good
function Deploy-Database { }
function Build-Dacpac { }
function Test-Connection { }

# ❌ Bad
function DatabaseDeploy { }
function dacpac_build { }
function CheckConnection { }
```

**Approved Verbs**: Use `Get-Verb` to see approved verbs

### Parameters

**CmdletBinding and Parameter Validation**:

```powershell
function Deploy-Database {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true, HelpMessage="SQL Server instance name")]
        [ValidateNotNullOrEmpty()]
        [string]$ServerName,
        
        [Parameter(Mandatory=$false)]
        [ValidatePattern('^\w+$')]
        [string]$DatabaseName = "Hartonomous",
        
        [Parameter(Mandatory=$false)]
        [ValidateSet('Debug', 'Release')]
        [string]$Configuration = 'Debug',
        
        [switch]$Force
    )
    
    # Implementation
}
```

### Error Handling

```powershell
function Deploy-Database {
    [CmdletBinding()]
    param([string]$ServerName)
    
    try {
        # Deployment logic
        sqlpackage /Action:Publish /SourceFile:database.dacpac /TargetServerName:$ServerName
        
        if ($LASTEXITCODE -ne 0) {
            throw "Deployment failed with exit code $LASTEXITCODE"
        }
        
        Write-Host "Deployment successful" -ForegroundColor Green
    }
    catch {
        Write-Error "Failed to deploy database: $_"
        throw
    }
}
```

---

## Markdown Formatting

### File Structure

```markdown
# Title (H1 - Once per file)

Brief introduction paragraph.

---

## Section 1 (H2)

Content for section 1.

### Subsection 1.1 (H3)

Detailed content.

---

## Section 2 (H2)

Content for section 2.
```

### Code Blocks

**Always specify language**:

````markdown
```csharp
public class Example { }
```

```sql
SELECT * FROM Atom;
```

```powershell
Get-Service MSSQLSERVER
```
````

### Tables

```markdown
| Column 1 | Column 2 | Column 3 |
|----------|----------|----------|
| Value 1  | Value 2  | Value 3  |
| Value 4  | Value 5  | Value 6  |
```

---

## Git Commit Messages

### Format

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Examples

```
feat(api): add semantic search endpoint

Implements /api/query/semantic with spatial pre-filtering.
Supports text queries with Top-K results and deduplication.

Closes #123
```

```
fix(database): resolve deadlock in atom insertion

Added READPAST hint to prevent blocking on concurrent inserts.
Reduces deadlock frequency from 5% to <0.1%.

Fixes #456
```

See [Contributing Guide](contributing.md) for complete commit message guidelines.
