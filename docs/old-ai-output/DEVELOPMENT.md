# Development Guide

## Development Environment Setup

### Required Tools

- **Visual Studio 2022** (Community or higher) with workloads:
  - ASP.NET and web development
  - .NET desktop development
  - Data storage and processing
- **SQL Server 2025** Developer Edition
- **SQL Server Management Studio (SSMS)** 19+
- **.NET 10 SDK**
- **PowerShell 7+**
- **Git** for version control

### Optional Tools

- **Neo4j Desktop** for graph database development
- **Postman** or **REST Client** for API testing
- **Docker Desktop** for containerized development
- **Azure Data Studio** as SSMS alternative

## Initial Setup

### 1. Clone Repository

```powershell
git clone https://github.com/AHartTN/Hartonomous-Sandbox.git
cd Hartonomous
```

### 2. Configure SQL Server

```sql
-- Enable required features
EXEC sp_configure 'clr enabled', 1;
EXEC sp_configure 'clr strict security', 0;
EXEC sp_configure 'filestream access level', 2;
RECONFIGURE;
```

Restart SQL Server service.

### 3. Deploy Database

```powershell
.\scripts\deploy\deploy-database.ps1 `
    -ServerInstance "localhost" `
    -Database "Hartonomous"
```

### 4. Configure User Secrets

```powershell
cd src\Hartonomous.Api

dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=Hartonomous;Integrated Security=true;TrustServerCertificate=true;"
dotnet user-secrets set "Neo4j:Password" "your-neo4j-password"
```

## Solution Structure

```
Hartonomous.sln                   # Main solution
├── src/
│   ├── Hartonomous.Api/          # Web API project
│   ├── Hartonomous.Core/         # Domain models, interfaces
│   ├── Hartonomous.Infrastructure/ # External integrations
│   ├── SqlClr/                   # SQL CLR functions (.NET Framework 4.8.1)
│   ├── ModelIngestion/           # GGUF/ONNX model loader service
│   ├── CesConsumer/              # CDC event consumer service
│   └── Neo4jSync/                # Graph sync service
└── tests/
    ├── Hartonomous.UnitTests/
    ├── Hartonomous.IntegrationTests/
    ├── Hartonomous.DatabaseTests/
    └── Hartonomous.EndToEndTests/
```

## Building the Solution

### Full Build

```powershell
# Restore dependencies
dotnet restore Hartonomous.sln

# Build all projects
dotnet build Hartonomous.sln -c Debug

# Build for Release
dotnet build Hartonomous.sln -c Release
```

### Build Specific Projects

```powershell
# SQL CLR library
dotnet build src\SqlClr\SqlClrFunctions.csproj

# SQL CLR functions
dotnet build src\SqlClr\SqlClrFunctions.csproj

# Web API
dotnet build src\Hartonomous.Api\Hartonomous.Api.csproj
```

## Running the Application

### Start API

```powershell
cd src\Hartonomous.Api
dotnet run
```

API runs on http://localhost:5000 and https://localhost:5001

### Start Background Services

```powershell
# Model Ingestion (terminal 1)
cd src\ModelIngestion
dotnet run

# CDC Consumer (terminal 2)
cd src\CesConsumer
dotnet run

# Neo4j Sync (terminal 3)
cd src\Neo4jSync
dotnet run
```

### Run All Services with Watch

```powershell
# API with hot reload
cd src\Hartonomous.Api
dotnet watch run

# Services with hot reload
cd src\ModelIngestion
dotnet watch run
```

## Testing

### Run All Tests

```powershell
dotnet test Hartonomous.Tests.sln
```

### Run Specific Test Project

```powershell
dotnet test tests\Hartonomous.UnitTests
dotnet test tests\Hartonomous.IntegrationTests
dotnet test tests\Hartonomous.DatabaseTests
```

### Run Tests with Coverage

```powershell
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Generate HTML report
reportgenerator -reports:coverage.opencover.xml -targetdir:coverage-report
```

### Run Specific Test

```powershell
dotnet test --filter "FullyQualifiedName~Hartonomous.UnitTests.VectorTests.DotProduct_ReturnsCorrectValue"
```

## Debugging

### Debug API in Visual Studio

1. Open `Hartonomous.sln` in Visual Studio
2. Set `Hartonomous.Api` as startup project
3. Press F5 to start debugging
4. Set breakpoints as needed

### Debug SQL CLR Functions

1. Attach Visual Studio debugger to SQL Server process:
   - Debug → Attach to Process
   - Find `sqlservr.exe`
   - Select "Managed (v4.x)" code type
2. Set breakpoints in CLR code
3. Execute SQL query that calls CLR function

```sql
-- Trigger CLR function
SELECT dbo.VectorDotProduct(@vector1, @vector2);
```

### Debug Background Services

```powershell
# Run with debugger attached
cd src\ModelIngestion
dotnet run --debug
```

In Visual Studio:
- Debug → Attach to Process
- Find `ModelIngestion.exe`
- Attach debugger

### View Logs

```powershell
# API logs
Get-Content src\Hartonomous.Api\logs\log-$(Get-Date -Format 'yyyyMMdd').txt -Tail 50 -Wait

# Service logs
Get-Content src\ModelIngestion\logs\log-$(Get-Date -Format 'yyyyMMdd').txt -Tail 50 -Wait
```

## Working with SQL CLR

### Update CLR Assembly

After modifying CLR code:

```powershell
# Rebuild CLR project
dotnet build src\SqlClr\SqlClrFunctions.csproj -c Release

# Redeploy to SQL Server
.\scripts\update-clr-assembly.sql
```

Or use the automated script:

```powershell
.\scripts\deploy-clr-unsafe.ps1 -ServerInstance "localhost" -Database "Hartonomous"
```

### Test CLR Functions

```sql
-- Test vector dot product
DECLARE @v1 VARBINARY(MAX) = 0x3F8000003F0000003E800000; -- [1.0, 0.5, 0.25]
DECLARE @v2 VARBINARY(MAX) = 0x3F8000003F0000003F000000; -- [1.0, 0.5, 0.5]

SELECT dbo.VectorDotProduct(@v1, @v2) AS DotProduct;

-- Test embedding generation
EXEC dbo.sp_GenerateEmbedding 
    @Text = 'test embedding',
    @ModelIdentifier = 'all-MiniLM-L6-v2';
```

## Database Development

### Create New Migration

```powershell
cd src\Hartonomous.Data

dotnet ef migrations add MigrationName --startup-project ..\Hartonomous.Api
```

### Apply Migrations

```powershell
dotnet ef database update --startup-project ..\Hartonomous.Api
```

### Rollback Migration

```powershell
dotnet ef database update PreviousMigrationName --startup-project ..\Hartonomous.Api
```

### Add Stored Procedure

1. Create SQL file in `sql\procedures\`
2. Follow naming convention: `Schema.ProcedureName.sql`
3. Install manually:

```powershell
sqlcmd -S localhost -d Hartonomous -i sql\procedures\YourProcedure.sql
```

## Code Style Guidelines

### C# Conventions

- Use C# 12 features (file-scoped namespaces, primary constructors, etc.)
- Follow Microsoft naming conventions
- Use `var` for local variables when type is obvious
- Prefer expression-bodied members for simple methods
- Use nullable reference types

Example:

```csharp
namespace Hartonomous.Core.Services;

public class EmbeddingService(IDbConnection connection, ILogger<EmbeddingService> logger)
{
    public async Task<float[]> GenerateEmbeddingAsync(string text, string modelId)
    {
        ArgumentNullException.ThrowIfNull(text);
        
        logger.LogInformation("Generating embedding for text length {Length}", text.Length);
        
        var result = await connection.QuerySingleAsync<float[]>(
            "dbo.sp_GenerateEmbedding",
            new { Text = text, ModelIdentifier = modelId },
            commandType: CommandType.StoredProcedure
        );
        
        return result;
    }
}
```

### SQL Conventions

- Use UPPER CASE for keywords
- Use schema-qualified names
- Include SET NOCOUNT ON
- Use transactions for multi-step operations
- Add error handling

Example:

```sql
CREATE PROCEDURE dbo.sp_InsertEmbedding
    @Text NVARCHAR(MAX),
    @Vector GEOMETRY,
    @ModelId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        INSERT INTO dbo.Embeddings (Text, EmbeddingGeometry, ModelId, CreatedDate)
        VALUES (@Text, @Vector, @ModelId, GETUTCDATE());
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        THROW;
    END CATCH
END
```

## Performance Profiling

### API Performance

```csharp
// Use MiniProfiler
using StackExchange.Profiling;

var profiler = MiniProfiler.Current;
using (profiler.Step("Generate Embedding"))
{
    var embedding = await _embeddingService.GenerateEmbeddingAsync(text, modelId);
}
```

### SQL Performance

```sql
-- Enable actual execution plan in SSMS
SET STATISTICS TIME ON;
SET STATISTICS IO ON;

-- Run your query
EXEC dbo.sp_VectorSearch @QueryVector, @TopK = 10;

-- Check Query Store
SELECT TOP 10
    qt.query_sql_text,
    rs.avg_duration / 1000.0 AS avg_ms,
    rs.count_executions
FROM sys.query_store_query q
JOIN sys.query_store_query_text qt ON q.query_text_id = qt.query_text_id
JOIN sys.query_store_plan p ON q.query_id = p.query_id
JOIN sys.query_store_runtime_stats rs ON p.plan_id = rs.plan_id
ORDER BY rs.avg_duration DESC;
```

### CLR Performance

```csharp
using System.Diagnostics;

var sw = Stopwatch.StartNew();
var result = ComputeExpensiveOperation();
sw.Stop();

SqlContext.Pipe.Send($"Operation completed in {sw.ElapsedMilliseconds}ms");
```

## Common Development Tasks

### Add New API Endpoint

1. Create controller:

```csharp
[ApiController]
[Route("api/[controller]")]
public class EmbeddingsController : ControllerBase
{
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] EmbeddingRequest request)
    {
        var embedding = await _service.GenerateAsync(request.Text);
        return Ok(embedding);
    }
}
```

2. Add tests:

```csharp
[Fact]
public async Task Generate_ValidText_ReturnsEmbedding()
{
    var request = new EmbeddingRequest { Text = "test" };
    var result = await _controller.Generate(request);
    Assert.IsType<OkObjectResult>(result);
}
```

### Add New CLR Function

1. Create function in SqlClr project:

```csharp
[SqlFunction(IsDeterministic = true)]
public static SqlDouble ComputeSimilarity(SqlBytes vector1, SqlBytes vector2)
{
    var v1 = DeserializeVector(vector1);
    var v2 = DeserializeVector(vector2);
    return CosineSimilarity(v1, v2);
}
```

2. Deploy assembly
3. Test in SQL:

```sql
SELECT dbo.ComputeSimilarity(@v1, @v2);
```

### Add New Background Service

1. Create service class:

```csharp
public class MyBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await DoWorkAsync();
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

2. Register in `Program.cs`:

```csharp
builder.Services.AddHostedService<MyBackgroundService>();
```

## Troubleshooting Development Issues

### Build Errors

**Error:** `Could not load file or assembly 'System.Text.Json'`

**Solution:** SQL CLR project uses newer packages. Ensure .NET Framework 4.8.1 compatibility.

**Error:** `CLR integration is not enabled`

**Solution:**

```sql
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;
```

### Runtime Errors

**Error:** `Timeout expired waiting for connection from pool`

**Solution:** Increase connection pool size or check for connection leaks.

```
Server=localhost;Database=Hartonomous;Max Pool Size=200;
```

**Error:** `Service Broker queue not processing messages`

**Solution:**

```sql
ALTER DATABASE Hartonomous SET ENABLE_BROKER;
ALTER QUEUE OODAQueue WITH STATUS = ON;
```

## Git Workflow

### Branching Strategy

- `main` - Production-ready code
- `develop` - Integration branch
- `feature/*` - Feature branches
- `bugfix/*` - Bug fix branches
- `hotfix/*` - Production hotfixes

### Commit Convention

```
type(scope): subject

body

footer
```

Types: feat, fix, docs, style, refactor, perf, test, chore

Example:

```
feat(api): add vector search endpoint

Implement spatial vector search with configurable top-k results.
Uses GEOMETRY spatial indexes for O(log n) performance.

Closes #123
```

### Pull Request Process

1. Create feature branch from `develop`
2. Make changes and commit
3. Write tests
4. Update documentation
5. Push and create PR
6. Address review feedback
7. Merge to `develop`

## Resources

- [SQL Server CLR Integration](https://learn.microsoft.com/en-us/sql/relational-databases/clr-integration)
- [ASP.NET Core Documentation](https://learn.microsoft.com/en-us/aspnet/core/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [Neo4j .NET Driver](https://neo4j.com/docs/dotnet-manual/current/)

## Support

- GitHub Issues: https://github.com/AHartTN/Hartonomous-Sandbox/issues
- Discussions: https://github.com/AHartTN/Hartonomous-Sandbox/discussions
