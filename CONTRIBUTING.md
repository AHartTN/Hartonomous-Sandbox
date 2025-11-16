# Contributing to Hartonomous

**Note**: This is proprietary software. Contributions are accepted only from authorized contributors with explicit written permission.

---

## Development Philosophy

Hartonomous follows a **database-first** development approach:

1. **SQL Server owns the schema** (DACPAC deployment)
2. **T-SQL stored procedures are the primary API**
3. **CLR functions** (.NET Framework 4.8.1) for SIMD-accelerated computation
4. **Worker services** (.NET 8) for background processing only - NO business logic
5. **Minimal APIs** as thin HTTP wrappers over stored procedures

**Golden Rule**: If you can do it in T-SQL, do it in T-SQL. Only use CLR/C# when absolutely necessary (SIMD, geometric projections, synthesis).

---

## Development Workflow

### 1. Database-First Development

**DO**:
- Define schema in `.sql` files in `src/Hartonomous.Database/`
- Create stored procedures for business logic
- Use CLR only for performance-critical operations (SIMD, geometric operations)
- Deploy via DACPAC

**DON'T**:
- Use Entity Framework migrations (schema must be in DACPAC)
- Put business logic in C# services (belongs in stored procedures)
- Create .NET Standard dependencies in CLR project (incompatible with SQL CLR)

### 2. Adding New Functionality

**Process**:
1. **Define T-SQL stored procedure** in `src/Hartonomous.Database/StoredProcedures/`
2. **Add CLR function if needed** in `src/Hartonomous.Database/` (.NET Framework 4.8.1 project)
3. **Deploy DACPAC** to update database schema
4. **Update worker services** if background processing needed
5. **Add API endpoint** (optional) as thin wrapper over stored procedure

**Example**: Adding new reasoning framework

```sql
-- Step 1: Create stored procedure
-- src/Hartonomous.Database/StoredProcedures/sp_MyNewReasoningFramework.sql
CREATE PROCEDURE dbo.sp_MyNewReasoningFramework
    @Prompt NVARCHAR(MAX),
    @SessionId UNIQUEIDENTIFIER
AS
BEGIN
    -- Implementation here
    -- Stores results in new table: MyReasoningResults
END
GO
```

```sql
-- Step 2: Create table for results
-- src/Hartonomous.Database/Tables/MyReasoningResults.sql
CREATE TABLE dbo.MyReasoningResults (
    ResultId BIGINT IDENTITY PRIMARY KEY,
    SessionId UNIQUEIDENTIFIER NOT NULL,
    ResultData NVARCHAR(MAX),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO
```

```csharp
// Step 3: Add API endpoint (optional)
// src/Hartonomous.Api/Endpoints/ReasoningEndpoints.cs
app.MapPost("/api/reasoning/my-new-framework", async (
    [FromBody] MyReasoningRequest request,
    [FromServices] IDbConnection db) =>
{
    await db.ExecuteAsync(
        "dbo.sp_MyNewReasoningFramework",
        new { request.Prompt, request.SessionId },
        commandType: CommandType.StoredProcedure);

    return Results.Ok();
});
```

### 3. Code Standards

**T-SQL**:
- Use `dbo` schema for all objects
- Prefix stored procedures with `sp_`
- Prefix functions with `fn_`
- Prefix CLR functions with `clr_`
- Always include `@SessionId UNIQUEIDENTIFIER` for provenance tracking
- Use `GETUTCDATE()` for timestamps (not `GETDATE()`)

**C# (.NET 8)**:
- Follow standard C# conventions
- Minimal business logic - defer to stored procedures
- Use `async/await` for database calls
- Dependency injection via constructor

**C# CLR (.NET Framework 4.8.1)**:
- NO .NET Standard dependencies (incompatible)
- Use `SqlFunction`, `SqlProcedure`, `SqlAggregate` attributes
- SIMD operations via `System.Numerics.Vectors`
- Mark assemblies with `PERMISSION_SET = UNSAFE` when needed

---

## Testing Requirements

### Unit Tests

**T-SQL Tests** (using tSQLt framework):
```sql
-- Example: Testing spatial projection
CREATE PROCEDURE test.[test sp_ProjectTo3D produces valid geometry]
AS
BEGIN
    DECLARE @Input VARBINARY(MAX) = 0x...; -- Sample embedding
    DECLARE @Result GEOMETRY;

    EXEC @Result = dbo.fn_ProjectTo3D @Input;

    EXEC tSQLt.AssertNotEquals @Expected = NULL, @Actual = @Result;
    EXEC tSQLt.AssertEquals @Expected = 'Point', @Actual = @Result.STGeometryType();
END
GO
```

**C# Unit Tests** (using xUnit):
```csharp
[Fact]
public void LandmarkProjection_ProjectTo3D_ProducesDeterministicOutput()
{
    var input = new float[1998]; // Sample embedding
    var result1 = LandmarkProjection.ProjectTo3D(input);
    var result2 = LandmarkProjection.ProjectTo3D(input);

    Assert.Equal(result1.X, result2.X);
    Assert.Equal(result1.Y, result2.Y);
    Assert.Equal(result1.Z, result2.Z);
}
```

### Integration Tests

Test complete workflows (atomization → embedding → spatial projection):

```csharp
[Fact]
public async Task CompleteIngestionPipeline_ProducesValidGeometry()
{
    // Atomize text
    var sessionId = Guid.NewGuid();
    await db.ExecuteAsync("dbo.sp_AtomizeText_Governed",
        new { InputText = "test", SessionId = sessionId });

    // Wait for workers to process
    await Task.Delay(5000);

    // Verify spatial geometry created
    var result = await db.QuerySingleAsync<decimal?>(
        "SELECT TOP 1 SpatialGeometry.STX FROM dbo.AtomEmbeddings WHERE CreatedAt >= @Time",
        new { Time = DateTime.UtcNow.AddMinutes(-1) });

    Assert.NotNull(result);
}
```

---

## Pull Request Process

1. **Create feature branch** from `main`
2. **Make changes** following standards above
3. **Add tests** (T-SQL and C# unit tests, integration tests if applicable)
4. **Update documentation** if adding new features
5. **Build and test locally**:
   ```bash
   dotnet build
   dotnet test
   sqlpackage /Action:Publish /SourceFile:... /TargetConnectionString:...
   ```
6. **Create pull request** with description of changes
7. **Code review** (required approval from maintainer)
8. **Merge** after approval

### Commit Message Format

```
<type>: <short summary>

<detailed description if needed>

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
```

**Types**: `feat`, `fix`, `refactor`, `docs`, `test`, `perf`

**Examples**:
```
feat: Add spatial projection for audio embeddings

Implements clr_ProjectAudioTo3D for audio spectrogram embeddings
using same landmark projection as text/images.

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
```

---

## Architecture Constraints

**MUST PRESERVE**:
- Two-stage O(log N) + O(K) query pattern
- Deterministic 3D projection (Gram-Schmidt with fixed landmarks)
- Spatial R-Tree indexes (not VECTOR indexes until read-write compatible)
- Model weights as queryable GEOMETRY
- Cross-modal geometric space (all modalities in same 3D space)
- Reasoning frameworks (CoT, ToT, Reflexion)
- Agent tools framework (AgentTools table registry)
- Behavioral analysis (SessionPaths as GEOMETRY)
- OODA loop self-improvement (weight updates, model pruning, UX fixing)
- Full provenance tracking (Neo4j Merkle DAG)

**MUST AVOID**:
- .NET Standard dependencies in CLR project
- Business logic in worker services (belongs in stored procedures)
- FILESTREAM (incompatible with atomic storage)
- Non-deterministic operations (breaks reproducibility)
- Matrix multiplication as PRIMARY path (spatial navigation is core)

---

## Documentation Standards

**When adding new features**:

1. **Update rewrite guide** (`docs/rewrite-guide/`) if architectural changes
2. **Add API documentation** (`docs/api/`) for new stored procedures/endpoints
3. **Add user guide** (`docs/guides/`) for user-facing features
4. **Update README.md** if major capability added

**Documentation must include**:
- Purpose and use cases
- T-SQL examples
- Expected output
- Performance characteristics
- Integration with existing features

---

## Security Considerations

**SQL CLR**:
- Always use asymmetric key signing for assemblies
- Minimize UNSAFE permission set usage
- Validate all inputs in stored procedures
- No dynamic SQL without parameterization

**API**:
- Validate all inputs before calling stored procedures
- Use parameterized queries (never string concatenation)
- Implement rate limiting
- Log all operations with SessionId for audit trail

---

## Performance Guidelines

**T-SQL**:
- Always use index hints for spatial queries: `WITH (INDEX(IX_AtomEmbeddings_SpatialGeometry))`
- Avoid cursors (use set-based operations)
- Use `NOLOCK` hint only when stale reads acceptable
- Monitor query plans: `SET STATISTICS IO ON; SET STATISTICS TIME ON;`

**C# CLR**:
- Use SIMD operations for vector math (`System.Numerics.Vectors`)
- Avoid allocations in hot paths
- Profile with BenchmarkDotNet before optimizing

**Worker Services**:
- Process in batches (not one-at-a-time)
- Use connection pooling
- Implement backoff/retry with exponential delay

---

## Getting Help

**Documentation**:
- [Architecture](./ARCHITECTURE.md) - System overview
- [Rewrite Guide](./docs/rewrite-guide/) - Complete technical specification
- [API Reference](./docs/api/) - API documentation
- [Operations](./docs/operations/) - Troubleshooting

**Questions**:
- GitHub Discussions (for authorized contributors)
- Internal documentation wiki
- Direct communication with maintainer

---

## License

This is **proprietary software**. All rights reserved.

Contributions are only accepted from authorized contributors with explicit written permission. By contributing, you agree that your contributions become the property of the author and are subject to the same proprietary license.
