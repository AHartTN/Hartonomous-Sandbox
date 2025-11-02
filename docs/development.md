# Hartonomous Development Guide

## Table of Contents

- [Development Environment Setup](#development-environment-setup)
- [Project Structure](#project-structure)
- [Building and Running](#building-and-running)
- [Testing](#testing)
- [Debugging](#debugging)
- [Code Style and Standards](#code-style-and-standards)
- [Contributing](#contributing)

## Development Environment Setup

### Required Software

1. **Visual Studio 2022** (17.8+) or **VS Code** with C# extensions
   - Workloads: ASP.NET and web development, .NET desktop development
   - Extensions: C# Dev Kit, C# Extensions, SQL Server (SSMS)

2. **SQL Server 2025** (or compatible Developer Edition)
   - Enable CLR integration
   - Install Full-Text Search feature

3. **.NET 10.0 SDK**
   - Download from <https://dotnet.microsoft.com/download>

4. **Neo4j Desktop** or **Neo4j Server 5.x**
   - Download from <https://neo4j.com/download/>

5. **Azure Storage Emulator** or **Azurite** (for local Event Hub testing)

### Optional Tools

- **Azure Data Studio** (for SQL Server management)
- **Neo4j Browser** (included with Neo4j Desktop)
- **Postman** or **REST Client** extension for VS Code
- **Docker Desktop** (for containerized dependencies)

### Clone and Setup

```bash
# Clone repository
git clone https://github.com/AHartTN/Hartonomous.git
cd Hartonomous

# Restore NuGet packages
dotnet restore Hartonomous.sln

# Build solution
dotnet build Hartonomous.sln
```

### Local Database Setup

```powershell
# Create local database
sqlcmd -S localhost -E -Q "CREATE DATABASE Hartonomous_Dev"

# Run migrations and deploy
.\scripts\deploy-database.ps1 `
  -ServerName "localhost" `
  -DatabaseName "Hartonomous_Dev" `
  -TrustedConnection $true `
  -Verbose
```

### Configuration

Create `appsettings.Development.json` in each project:

**src/Hartonomous.Admin/appsettings.Development.json:**

```json
{
  "ConnectionStrings": {
    "HartonomousDb": "Server=localhost;Database=Hartonomous_Dev;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "DetailedErrors": true,
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

**src/ModelIngestion/appsettings.Development.json:**

```json
{
  "ConnectionStrings": {
    "HartonomousDb": "Server=localhost;Database=Hartonomous_Dev;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "SqlServer": {
    "CommandTimeoutSeconds": 120,
    "EnableRetryOnFailure": true
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

## Project Structure

```text
Hartonomous/
├── src/
│   ├── Hartonomous.Core/          # Domain entities and interfaces
│   │   ├── Entities/               # Entity classes (Atom, Model, etc.)
│   │   ├── Interfaces/             # Repository/service abstractions
│   │   ├── Models/                 # DTOs and value objects
│   │   ├── Utilities/              # Helper classes (VectorUtility, etc.)
│   │   └── ValueObjects/           # Immutable value objects
│   │
│   ├── Hartonomous.Data/          # EF Core data access
│   │   ├── Configurations/         # Fluent entity configurations
│   │   ├── Migrations/             # EF Core migrations
│   │   └── HartonomousDbContext.cs # Main DbContext
│   │
│   ├── Hartonomous.Infrastructure/ # Service implementations
│   │   ├── Repositories/           # Repository implementations
│   │   ├── Services/               # Service implementations
│   │   ├── Data/                   # SQL helpers (SqlCommandExecutor, etc.)
│   │   └── DependencyInjection.cs  # DI registration
│   │
│   ├── ModelIngestion/            # CLI worker for model ingestion
│   │   ├── Content/                # Content extraction services
│   │   ├── ModelFormats/           # Format-specific readers
│   │   └── Program.cs              # Entry point
│   │
│   ├── CesConsumer/               # CDC event listener
│   ├── Neo4jSync/                 # Neo4j sync worker
│   ├── Hartonomous.Admin/         # Blazor admin portal
│   └── SqlClr/                    # SQL CLR functions
│
├── tests/
│   ├── Hartonomous.Core.Tests/
│   ├── Hartonomous.Infrastructure.Tests/
│   ├── Integration.Tests/
│   └── ModelIngestion.Tests/
│
├── docs/                          # Documentation
├── sql/                           # SQL scripts
│   ├── procedures/                # Stored procedures
│   └── verification/              # Verification scripts
│
├── neo4j/                         # Neo4j schemas
└── scripts/                       # Deployment scripts
```

### Layer Responsibilities

**Hartonomous.Core** (Domain Layer)
- Contains NO dependencies on infrastructure
- Defines entities, value objects, interfaces
- Business logic and domain rules

**Hartonomous.Data** (Data Access Layer)
- EF Core context and migrations
- Entity configurations
- Database schema definitions

**Hartonomous.Infrastructure** (Infrastructure Layer)
- Implements interfaces from Core
- Data access (repositories)
- External services (Event Hubs, Neo4j)

**Application Projects**
- Use Core interfaces
- Depend on Infrastructure for DI registration
- Orchestrate workflows

## Building and Running

### Build All Projects

```bash
# Debug build
dotnet build Hartonomous.sln

# Release build
dotnet build Hartonomous.sln -c Release

# Build specific project
dotnet build src/Hartonomous.Core/Hartonomous.Core.csproj
```

### Run Applications

#### Admin Portal

```bash
cd src/Hartonomous.Admin
dotnet run

# Or with hot reload
dotnet watch run
```

Navigate to <https://localhost:5001>

#### Model Ingestion CLI

```bash
cd src/ModelIngestion

# Run with arguments
dotnet run -- ingest-model "C:\Models\bert-base.onnx"

# Or build and run executable
dotnet build -c Release
.\bin\Release\net10.0\ModelIngestion.exe ingest-model "C:\Models\bert-base.onnx"
```

#### CesConsumer (CDC Listener)

```bash
cd src/CesConsumer
dotnet run
```

#### Neo4jSync

```bash
cd src/Neo4jSync
dotnet run
```

### Database Migrations

```bash
cd src/Hartonomous.Data

# Add new migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Rollback to specific migration
dotnet ef database update PreviousMigrationName

# Generate SQL script
dotnet ef migrations script --output migration.sql
```

## Testing

### Run All Tests

```bash
# Run all tests
dotnet test Hartonomous.Tests.sln

# Run with verbosity
dotnet test Hartonomous.Tests.sln --verbosity detailed

# Run specific test project
dotnet test tests/Hartonomous.Core.Tests/Hartonomous.Core.Tests.csproj

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Run Specific Tests

```bash
# By test name filter
dotnet test --filter "FullyQualifiedName~VectorUtility"

# By category
dotnet test --filter "Category=Integration"

# By class
dotnet test --filter "ClassName=AtomIngestionServiceTests"
```

### Integration Tests

Integration tests require SQL Server:

```bash
# Set connection string
$env:ConnectionStrings__HartonomousDb = "Server=localhost;Database=Hartonomous_Test;Trusted_Connection=True;"

# Run integration tests
dotnet test tests/Integration.Tests/Integration.Tests.csproj
```

### Writing Tests

Example unit test:

```csharp
using Xunit;
using Hartonomous.Core.Utilities;

namespace Hartonomous.Core.Tests.Utilities
{
    public class VectorUtilityTests
    {
        [Fact]
        public void CosineSimilarity_ShouldReturnOne_ForIdenticalVectors()
        {
            // Arrange
            var vector1 = new SqlVector<float>(new[] { 1f, 2f, 3f });
            var vector2 = new SqlVector<float>(new[] { 1f, 2f, 3f });
            
            // Act
            var similarity = VectorUtility.CosineSimilarity(vector1, vector2);
            
            // Assert
            Assert.Equal(1.0, similarity, precision: 5);
        }
    }
}
```

Example integration test:

```csharp
using Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace Hartonomous.Infrastructure.Tests
{
    public class AtomIngestionServiceTests : IClassFixture<DatabaseFixture>
    {
        private readonly IServiceProvider _serviceProvider;
        
        public AtomIngestionServiceTests(DatabaseFixture fixture)
        {
            _serviceProvider = fixture.ServiceProvider;
        }
        
        [Fact]
        public async Task IngestAsync_ShouldCreateNewAtom_WhenNoExisting()
        {
            // Arrange
            var service = _serviceProvider.GetRequiredService<IAtomIngestionService>();
            var request = new AtomIngestionRequest(
                Modality: "text",
                ContentHash: RandomHash(),
                CanonicalText: "Test content");
            
            // Act
            var result = await service.IngestAsync(request);
            
            // Assert
            Assert.True(result.IsNewAtom);
            Assert.NotEqual(0, result.AtomId);
        }
    }
}
```

## Debugging

### Debug in Visual Studio

1. Open `Hartonomous.sln` in Visual Studio
2. Set startup project (e.g., `Hartonomous.Admin`)
3. Press F5 or click Debug → Start Debugging
4. Set breakpoints in code

### Debug Multiple Projects

1. Right-click solution → Properties
2. Select "Multiple startup projects"
3. Set Action to "Start" for desired projects:
   - Hartonomous.Admin
   - CesConsumer
   - Neo4jSync

### Debug SQL Queries

Enable detailed logging:

```csharp
services.AddDbContext<HartonomousDbContext>(options =>
{
    options.UseSqlServer(connectionString)
           .EnableSensitiveDataLogging()
           .EnableDetailedErrors()
           .LogTo(Console.WriteLine, LogLevel.Information);
});
```

### Debug SQL CLR Functions

1. Attach debugger to SQL Server process
2. Enable SQL CLR debugging in Visual Studio:
   - Tools → Options → Debugging → General
   - Check "Enable SQL Server debugging"
3. Set breakpoints in CLR code
4. Execute stored procedure that calls CLR function

### Useful SQL Queries for Debugging

```sql
-- Check recent ingestion jobs
SELECT TOP 10 * FROM IngestionJobs ORDER BY StartTime DESC;

-- View atom deduplication
SELECT
    ContentHash,
    COUNT(*) as Instances,
    MAX(ReferenceCount) as MaxRefCount
FROM Atoms
GROUP BY ContentHash
HAVING COUNT(*) > 1;

-- Check embedding coverage
SELECT
    a.Modality,
    COUNT(DISTINCT a.AtomId) as TotalAtoms,
    COUNT(DISTINCT ae.AtomId) as WithEmbeddings
FROM Atoms a
LEFT JOIN AtomEmbeddings ae ON a.AtomId = ae.AtomId
GROUP BY a.Modality;
```

## Code Style and Standards

### Naming Conventions

- **Classes/Interfaces**: PascalCase (`AtomRepository`, `IAtomRepository`)
- **Methods**: PascalCase (`GetByIdAsync`, `IngestAsync`)
- **Parameters/Variables**: camelCase (`atomId`, `cancellationToken`)
- **Private fields**: `_camelCase` (`_context`, `_logger`)
- **Constants**: UPPER_CASE or PascalCase for static readonly

### File Organization

```csharp
// 1. Usings (organized, remove unused)
using System;
using System.Collections.Generic;
using Hartonomous.Core.Entities;

// 2. Namespace
namespace Hartonomous.Infrastructure.Repositories;

// 3. Class with XML documentation
/// <summary>
/// Repository implementation for atom operations.
/// </summary>
public class AtomRepository : IAtomRepository
{
    // 4. Private fields
    private readonly HartonomousDbContext _context;
    private readonly ILogger<AtomRepository> _logger;
    
    // 5. Constructor
    public AtomRepository(
        HartonomousDbContext context,
        ILogger<AtomRepository> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    // 6. Public methods
    public async Task<Atom?> GetByIdAsync(long atomId, CancellationToken cancellationToken)
    {
        // Implementation
    }
    
    // 7. Private helper methods
    private void ValidateAtom(Atom atom)
    {
        // Implementation
    }
}
```

### Async/Await Best Practices

```csharp
// ✅ DO: Use async suffix
public async Task<Atom> GetAtomAsync(long id)

// ✅ DO: Pass CancellationToken
public async Task<Atom> GetAtomAsync(long id, CancellationToken cancellationToken = default)

// ✅ DO: Use ConfigureAwait(false) in library code
var atom = await _repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

// ❌ DON'T: Use async void (except event handlers)
public async void DoWork() // Bad!

// ❌ DON'T: Block on async code
var result = GetAtomAsync(id).Result; // Bad! Use await instead
```

### Error Handling

```csharp
// ✅ DO: Use specific exceptions
if (atom == null)
{
    throw new NotFoundException($"Atom {atomId} not found");
}

// ✅ DO: Log errors with context
catch (SqlException ex)
{
    _logger.LogError(ex, "Failed to retrieve atom {AtomId}", atomId);
    throw;
}

// ❌ DON'T: Swallow exceptions
catch (Exception) { } // Bad!

// ❌ DON'T: Catch Exception without rethrowing
catch (Exception ex)
{
    _logger.LogError(ex, "Error");
    // Missing: throw;
}
```

## Contributing

### Contribution Workflow

1. **Create Issue**: Describe the feature/bug
2. **Fork Repository**: Create your own fork
3. **Create Branch**: `feature/your-feature-name` or `bugfix/issue-number`
4. **Make Changes**: Follow code standards
5. **Write Tests**: Add unit/integration tests
6. **Commit**: Use conventional commit messages
7. **Push**: Push to your fork
8. **Pull Request**: Create PR with detailed description

### Commit Message Format

```text
<type>(<scope>): <subject>

<body>

<footer>
```

**Types**:

- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, no logic change)
- `refactor`: Code refactoring
- `test`: Adding/updating tests
- `chore`: Build/tooling changes

**Examples**:

```text
feat(ingestion): add support for GGUF model format

- Implement GGUFModelReader
- Add GGUF magic number detection
- Update ModelDiscoveryService with GGUF support

Closes #42
```

```text
fix(search): correct cosine similarity calculation

The previous implementation didn't normalize vectors before
computing dot product, leading to incorrect similarity scores.

Fixes #89
```

### Pull Request Checklist

- [ ] Code follows project style guidelines
- [ ] All tests pass locally
- [ ] Added tests for new functionality
- [ ] Updated documentation (if applicable)
- [ ] No merge conflicts with main branch
- [ ] Commit messages follow conventional format
- [ ] PR description explains changes clearly

---

## Next Steps

- Review [Architecture Overview](architecture.md) for system design
- See [API Reference](api-reference.md) for interface documentation
- Consult [Operations Guide](operations.md) for deployment and monitoring

