# Development Setup Guide

This guide will help you set up your local development environment for Hartonomous.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Database Setup](#database-setup)
3. [Neo4j Setup](#neo4j-setup)
4. [Application Configuration](#application-configuration)
5. [Building the Solution](#building-the-solution)
6. [Running Tests](#running-tests)
7. [Debugging](#debugging)
8. [Common Issues](#common-issues)

---

## Prerequisites

### Required Software

#### 1. .NET 10 SDK

Download and install from [https://dotnet.microsoft.com/download/dotnet/10.0](https://dotnet.microsoft.com/download/dotnet/10.0)

**Verify Installation**:

```powershell
dotnet --version
# Expected: 10.0.x or higher
```

#### 2. SQL Server 2025 Developer Edition

Download from [https://www.microsoft.com/sql-server/sql-server-downloads](https://www.microsoft.com/sql-server/sql-server-downloads)

**Installation Notes**:

- Use **default instance** (MSSQLSERVER) or **named instance** (e.g., `localhost\SQL2025`)
- Enable **CLR Integration** (required for CLR assemblies)
- Enable **Mixed Mode Authentication** (SQL Server + Windows)

**Verify Installation**:

```powershell
sqlcmd -S localhost -E -Q "SELECT @@VERSION"
# Should show SQL Server 2025 version
```

#### 3. Neo4j 5.x

Download from [https://neo4j.com/download/](https://neo4j.com/download/)

**Installation Options**:

**Option A: Neo4j Desktop (Recommended for Development)**

- Install Neo4j Desktop
- Create new database with name `hartonomous-dev`
- Default credentials: `neo4j` / `password` (change on first login)

**Option B: Docker**

```powershell
docker run -d `
  --name neo4j-hartonomous `
  -p 7474:7474 -p 7687:7687 `
  -e NEO4J_AUTH=neo4j/your-password `
  neo4j:5.15
```

**Verify Installation**:

Open browser to [http://localhost:7474](http://localhost:7474) and log in.

#### 4. PowerShell 7+

Download from [https://github.com/PowerShell/PowerShell/releases](https://github.com/PowerShell/PowerShell/releases)

**Verify Installation**:

```powershell
$PSVersionTable.PSVersion
# Expected: 7.x or higher
```

#### 5. Git

Download from [https://git-scm.com/downloads](https://git-scm.com/downloads)

**Verify Installation**:

```powershell
git --version
# Expected: 2.x or higher
```

### Optional Software

#### Visual Studio 2022

Download from [https://visualstudio.microsoft.com/vs/](https://visualstudio.microsoft.com/vs/)

**Required Workloads**:

- ASP.NET and web development
- Data storage and processing
- .NET desktop development

#### Visual Studio Code

Download from [https://code.visualstudio.com/](https://code.visualstudio.com/)

**Recommended Extensions**:

- C# Dev Kit
- SQL Server (mssql)
- PowerShell
- Neo4j Tools

#### Azure CLI (For Deployment)

Download from [https://docs.microsoft.com/cli/azure/install-azure-cli](https://docs.microsoft.com/cli/azure/install-azure-cli)

#### Docker Desktop (For Containerized Dev)

Download from [https://www.docker.com/products/docker-desktop](https://www.docker.com/products/docker-desktop)

---

## Database Setup

### 1. Clone Repository

```powershell
git clone https://github.com/hartonomous/Hartonomous.git
cd Hartonomous
```

### 2. Configure Database Connection

**Update Configuration**:

Create or edit `src/Hartonomous.Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=Hartonomous;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True",
    "Neo4j": "bolt://localhost:7687"
  },
  "Neo4j": {
    "Username": "neo4j",
    "Password": "your-password"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Hartonomous": "Debug"
    }
  }
}
```

**Connection String Variations**:

**Windows Authentication**:

```
Server=localhost;Database=Hartonomous;Trusted_Connection=True;TrustServerCertificate=True
```

**SQL Server Authentication**:

```
Server=localhost;Database=Hartonomous;User Id=sa;Password=YourPassword;TrustServerCertificate=True
```

**Named Instance**:

```
Server=localhost\SQL2025;Database=Hartonomous;Trusted_Connection=True;TrustServerCertificate=True
```

### 3. Build DACPAC

```powershell
# Run from repository root
.\scripts\build-dacpac.ps1
```

**Output**: `src/Hartonomous.Database/bin/Debug/Hartonomous.Database.dacpac`

### 4. Deploy Database

```powershell
# Deploy to local SQL Server
.\scripts\deploy-dacpac.ps1 -ServerName "localhost" -DatabaseName "Hartonomous"
```

**PowerShell Parameters**:

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `ServerName` | No | `localhost` | SQL Server instance |
| `DatabaseName` | No | `Hartonomous` | Database name |
| `UseWindowsAuth` | No | `$true` | Use Windows Authentication |
| `SqlUsername` | No* | - | SQL username (if not Windows Auth) |
| `SqlPassword` | No* | - | SQL password (if not Windows Auth) |

*Required if `UseWindowsAuth = $false`

**Verify Deployment**:

```powershell
# Check database exists
sqlcmd -S localhost -E -Q "SELECT name FROM sys.databases WHERE name = 'Hartonomous'"

# Check CLR assemblies
sqlcmd -S localhost -d Hartonomous -E -Q "SELECT name FROM sys.assemblies WHERE is_user_defined = 1"
```

**Expected Output**:

```
name
----
Hartonomous.Clr
Microsoft.SqlServer.Types
```

### 5. Seed Initial Data (Optional)

```powershell
# Seed kernel atoms (fundamental concepts)
.\scripts\kernel-seeding.ps1
```

---

## Neo4j Setup

### 1. Create Database

**Neo4j Desktop**:

1. Open Neo4j Desktop
2. Create new project: "Hartonomous Development"
3. Create database: "hartonomous-dev"
4. Start database

**Cypher CLI**:

```cypher
CREATE DATABASE hartonomous
```

### 2. Create Constraints and Indexes

Run from repository root:

```powershell
# Apply Neo4j schema
.\scripts\neo4j\create-schema.ps1
```

**Manual Cypher** (if script unavailable):

```cypher
// Atom node constraints
CREATE CONSTRAINT atom_id_unique IF NOT EXISTS
FOR (a:Atom) REQUIRE a.atomId IS UNIQUE;

CREATE CONSTRAINT atom_hash_unique IF NOT EXISTS
FOR (a:Atom) REQUIRE a.canonicalHash IS UNIQUE;

// Indexes
CREATE INDEX atom_generation_idx IF NOT EXISTS
FOR (a:Atom) ON (a.generation);

CREATE INDEX atom_modality_idx IF NOT EXISTS
FOR (a:Atom) ON (a.modality);

CREATE INDEX atom_tenant_idx IF NOT EXISTS
FOR (a:Atom) ON (a.tenantId);
```

### 3. Verify Connection

**C# Test**:

```csharp
// In Hartonomous.IntegrationTests or manual console app
using Neo4j.Driver;

var driver = GraphDatabase.Driver(
    "bolt://localhost:7687",
    AuthTokens.Basic("neo4j", "your-password")
);

await using var session = driver.AsyncSession();
var result = await session.RunAsync("RETURN 1 AS num");
var record = await result.SingleAsync();

Console.WriteLine($"Neo4j connection successful: {record["num"]}");
```

---

## Application Configuration

### 1. User Secrets (Recommended)

Store sensitive configuration in user secrets:

```powershell
cd src/Hartonomous.Api

# Initialize user secrets
dotnet user-secrets init

# Set Neo4j password
dotnet user-secrets set "Neo4j:Password" "your-password"

# Set SQL Server password (if using SQL Auth)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=Hartonomous;User Id=sa;Password=YourPassword;TrustServerCertificate=True"
```

**View All Secrets**:

```powershell
dotnet user-secrets list
```

### 2. Environment Variables (Alternative)

Set environment variables for configuration:

**PowerShell**:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=localhost;Database=Hartonomous;Trusted_Connection=True;TrustServerCertificate=True"
$env:Neo4j__Password = "your-password"
```

**Windows (Persistent)**:

```powershell
[System.Environment]::SetEnvironmentVariable("Neo4j__Password", "your-password", "User")
```

### 3. appsettings.Development.json

Complete development configuration:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=Hartonomous;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True",
    "Neo4j": "bolt://localhost:7687"
  },
  "Neo4j": {
    "Username": "neo4j",
    "Password": "your-password",
    "MaxConnectionPoolSize": 100,
    "ConnectionTimeout": 30
  },
  "OpenAI": {
    "ApiKey": "your-openai-key",
    "EmbeddingModel": "text-embedding-3-large",
    "EmbeddingDimensions": 1536
  },
  "Atomization": {
    "DefaultChunkSize": 64,
    "MaxConcurrency": 4,
    "EnableDeduplication": true
  },
  "Spatial": {
    "LandmarkCount": 100,
    "ProjectionDimensions": 3,
    "RTreeMaxLevel": 4
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning",
      "Hartonomous": "Debug",
      "Neo4j.Driver": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

---

## Building the Solution

### 1. Restore Dependencies

```powershell
# From repository root
dotnet restore
```

### 2. Build Solution

```powershell
# Build all projects
dotnet build

# Build specific project
dotnet build src/Hartonomous.Api

# Build in Release mode
dotnet build --configuration Release
```

### 3. Build DACPAC (Database)

```powershell
.\scripts\build-dacpac.ps1
```

### 4. Verify Build

```powershell
# Check build output
ls src/Hartonomous.Api/bin/Debug/net10.0/
```

**Expected Files**:

- `Hartonomous.Api.dll`
- `Hartonomous.Core.dll`
- `Hartonomous.Infrastructure.dll`
- `Hartonomous.Data.Entities.dll`

---

## Running Tests

### 1. Run All Tests

```powershell
# All test projects
dotnet test

# With verbose output
dotnet test --verbosity normal

# With code coverage
dotnet test --collect:"XPlat Code Coverage"
```

### 2. Run Specific Test Projects

```powershell
# Unit tests only
dotnet test tests/Hartonomous.UnitTests

# Integration tests
dotnet test tests/Hartonomous.IntegrationTests

# Database tests (requires local SQL Server)
dotnet test tests/Hartonomous.DatabaseTests

# End-to-end tests
dotnet test tests/Hartonomous.EndToEndTests
```

### 3. Run Tests by Filter

```powershell
# Run tests by name pattern
dotnet test --filter "FullyQualifiedName~SemanticSearch"

# Run tests by category
dotnet test --filter "Category=Integration"

# Run tests by trait
dotnet test --filter "Priority=High"
```

### 4. View Code Coverage

```powershell
# Generate coverage report
dotnet test --collect:"XPlat Code Coverage"

# Install ReportGenerator (one-time)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator `
  -reports:**/coverage.cobertura.xml `
  -targetdir:coverage-report `
  -reporttypes:Html

# Open report
start coverage-report/index.html
```

---

## Debugging

### Debugging in Visual Studio

1. **Open Solution**: `Hartonomous.sln`
2. **Set Startup Project**: Right-click `Hartonomous.Api` → Set as Startup Project
3. **Launch Profile**: Use `https` or `http` profile
4. **Press F5** to start debugging

**Breakpoints**:

- Set breakpoints in controllers or services
- Step through code with F10 (Step Over) or F11 (Step Into)

### Debugging in VS Code

1. **Open Folder**: Open `Hartonomous` folder in VS Code
2. **Install Extensions**: C# Dev Kit
3. **Create launch.json**:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core Launch (web)",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/Hartonomous.Api/bin/Debug/net10.0/Hartonomous.Api.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/Hartonomous.Api",
      "stopAtEntry": false,
      "serverReadyAction": {
        "action": "openExternally",
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
      },
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      },
      "sourceFileMap": {
        "/Views": "${workspaceFolder}/Views"
      }
    }
  ]
}
```

4. **Press F5** to start debugging

### Debugging CLR Assemblies

**Enable CLR Debugging**:

```sql
-- Enable CLR debugging on SQL Server
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;
```

**Attach to SQL Server Process**:

1. In Visual Studio: Debug → Attach to Process
2. Find `sqlservr.exe` process
3. Set breakpoints in `Hartonomous.Clr` project
4. Execute CLR stored procedure

**Example**:

```sql
-- Trigger breakpoint
EXEC dbo.sp_SpatialAStar @StartAtomId = 12345, @GoalX = 4.5, @GoalY = 6.2, @GoalZ = 3.1;
```

### Debugging Neo4j Queries

**Enable Query Logging**:

```json
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Neo4j.Driver": "Debug"
    }
  }
}
```

**Neo4j Browser**:

- Open [http://localhost:7474](http://localhost:7474)
- Run queries manually: `MATCH (a:Atom) RETURN a LIMIT 10`
- Use `EXPLAIN` or `PROFILE` for query plans

---

## Common Issues

### Issue: "Login failed for user"

**Cause**: SQL Server authentication issue

**Solutions**:

1. **Check SQL Server is running**:

```powershell
Get-Service MSSQLSERVER
```

2. **Verify connection string** in `appsettings.Development.json`
3. **Enable Mixed Mode** if using SQL Auth:

   - SQL Server Configuration Manager → SQL Server Services → Properties → Security → SQL Server and Windows Authentication mode

### Issue: "Assembly not found: Hartonomous.Clr"

**Cause**: CLR assembly not deployed

**Solution**:

```powershell
# Redeploy database
.\scripts\deploy-dacpac.ps1 -ServerName "localhost" -DatabaseName "Hartonomous"
```

**Verify CLR Assemblies**:

```sql
SELECT name, permission_set_desc 
FROM sys.assemblies 
WHERE is_user_defined = 1;
```

### Issue: "Cannot connect to Neo4j"

**Cause**: Neo4j not running or incorrect credentials

**Solutions**:

1. **Verify Neo4j is running**:

   - Neo4j Desktop: Check database status
   - Docker: `docker ps | grep neo4j`

2. **Check connection string** in `appsettings.Development.json`
3. **Reset password**:

```cypher
// In Neo4j Browser
ALTER CURRENT USER SET PASSWORD FROM 'old-password' TO 'new-password';
```

### Issue: "Port already in use"

**Cause**: Another application using port 5000/5001

**Solutions**:

1. **Change port** in `launchSettings.json`:

```json
{
  "profiles": {
    "https": {
      "applicationUrl": "https://localhost:7000;http://localhost:7001"
    }
  }
}
```

2. **Kill process** using port:

```powershell
# Find process using port 5000
netstat -ano | findstr :5000

# Kill process (replace PID)
taskkill /PID <PID> /F
```

### Issue: "Test failures on DatabaseTests"

**Cause**: Local database not in expected state

**Solution**:

```powershell
# Reset database
.\scripts\deploy-dacpac.ps1 -ServerName "localhost" -DatabaseName "Hartonomous_Test"

# Run tests
dotnet test tests/Hartonomous.DatabaseTests
```

---

## Next Steps

- Read [Contributing Guide](contributing.md) for development workflow
- Review [Code Standards](code-standards.md) for coding conventions
- Check [Architecture Documentation](../architecture/) for system design
- Explore [API Reference](../api/) for endpoint details

---

## Questions?

If you encounter issues not covered here:

- Check [Troubleshooting Guide](../operations/troubleshooting.md)
- Open a [GitHub Issue](https://github.com/hartonomous/Hartonomous/issues)
- Ask in [Discord](https://discord.gg/hartonomous)
