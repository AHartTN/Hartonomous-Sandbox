# Phase 6: Architecture Restructuring

**Priority**: LOW-MEDIUM
**Estimated Time**: 6-10 hours
**Dependencies**: Phase 5 complete (consolidation done)

## Overview

Major project structure changes. From TODO_BACKUP.md and RECOVERY_STATUS.md architecture recommendations.

---

## Task 6.1: Consolidate Console Apps into Worker

**Status**: ❌ NOT STARTED
**Source**: RECOVERY_STATUS.md, ARCHITECTURAL_AUDIT.md

### Problem
3 separate console app projects:
- CesConsumer (Event Sourcing consumer)
- Neo4jSync (Graph synchronization)
- Hartonomous.Admin (Admin tasks)

Each has separate hosting, configuration, deployment.

### Solution

Create unified Worker project with multiple BackgroundService implementations:

**Step 1**: Create new project
```powershell
cd src
dotnet new worker -n Hartonomous.Worker
```

**Step 2**: Migrate CesConsumer
```csharp
public class CesConsumerWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Move logic from CesConsumer/Program.cs
    }
}
```

**Step 3**: Migrate Neo4jSync
```csharp
public class Neo4jSyncWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Move logic from Neo4jSync/Program.cs
    }
}
```

**Step 4**: Migrate Admin tasks
```csharp
public class AdminWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Move logic from Hartonomous.Admin
    }
}
```

**Step 5**: Register all workers
```csharp
// Program.cs
builder.Services.AddHostedService<CesConsumerWorker>();
builder.Services.AddHostedService<Neo4jSyncWorker>();
builder.Services.AddHostedService<AdminWorker>();
```

**Step 6**: Delete old projects
- Delete `src/CesConsumer/`
- Delete `src/Neo4jSync/`
- Delete `src/Hartonomous.Admin/`
- Update `Hartonomous.sln`

### Benefits
- Single deployment
- Shared configuration
- Unified logging/monitoring
- Easier debugging

---

## Task 6.2: Merge Hartonomous.Data into Infrastructure

**Status**: ❌ NOT STARTED
**Source**: ARCHITECTURAL_AUDIT.md

### Problem
Hartonomous.Data project contains only EF Core DbContext.
Creates unnecessary project boundary.

### Solution

**Step 1**: Move DbContext
```
From: src/Hartonomous.Data/HartonomousDbContext.cs
To:   src/Hartonomous.Infrastructure/Data/HartonomousDbContext.cs
```

**Step 2**: Move entity configurations
```
From: src/Hartonomous.Data/Configurations/*.cs
To:   src/Hartonomous.Infrastructure/Data/Configurations/*.cs
```

**Step 3**: Update namespaces
```csharp
// Before:
namespace Hartonomous.Data;

// After:
namespace Hartonomous.Infrastructure.Data;
```

**Step 4**: Update project references
Remove `Hartonomous.Data` references, add `Hartonomous.Infrastructure` if needed.

**Step 5**: Delete Hartonomous.Data project
- Delete `src/Hartonomous.Data/`
- Update `Hartonomous.sln`

### Reorganize Infrastructure Repositories

```
src/Hartonomous.Infrastructure/
  Repositories/
    EfCore/
      AtomRepository.cs
      ModelRepository.cs
      InferenceRepository.cs
      Configurations/ (entity configs)
    Dapper/
      BulkInsertRepository.cs
      ReportingRepository.cs
```

---

## Task 6.3: Multi-Target Hartonomous.Core

**Status**: ❌ NOT STARTED
**Research**: FINDING 50-67 in SQL_CLR_RESEARCH_FINDINGS.md

### Problem
Hartonomous.Core only targets net8.0.
SqlClr (.NET Framework 4.8.1) cannot reference it.

### Solution

Convert Core to multi-target `net481;net8.0`:

**File**: `src/Hartonomous.Core/Hartonomous.Core.csproj`

Change:
```xml
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
</PropertyGroup>
```

To:
```xml
<PropertyGroup>
  <TargetFrameworks>net481;net8.0</TargetFrameworks>
</PropertyGroup>
```

### Add Framework-Specific Dependencies

```xml
<ItemGroup Condition="'$(TargetFramework)' == 'net481'">
  <!-- Backport packages for .NET Framework -->
  <PackageReference Include="System.Text.Json" Version="8.0.5" />
  <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
</ItemGroup>

<ItemGroup Condition="'$(TargetFramework)' == 'net8.0'">
  <!-- .NET 8 native packages (if any) -->
</ItemGroup>
```

### Benefits
- SqlClr can reference Core net481 build
- API can reference Core net8.0 build (better performance)
- Single codebase, two optimized outputs
- Shared business logic across frameworks

### Verification
```powershell
cd src/Hartonomous.Core
dotnet build

# Verify two builds created:
ls bin/Debug/net481/Hartonomous.Core.dll
ls bin/Debug/net8.0/Hartonomous.Core.dll
```

---

## Task 6.4: Multi-Target Hartonomous.Infrastructure

**Status**: ❌ NOT STARTED
**Dependencies**: Task 6.3 complete

### Problem
Same as Core - Infrastructure also needs to support both frameworks.

### Solution

Multi-target Infrastructure `net481;net8.0`:

**File**: `src/Hartonomous.Infrastructure/Hartonomous.Infrastructure.csproj`

Change:
```xml
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
</PropertyGroup>
```

To:
```xml
<PropertyGroup>
  <TargetFrameworks>net481;net8.0</TargetFrameworks>
</PropertyGroup>
```

### Add Framework-Specific Dependencies

```xml
<ItemGroup Condition="'$(TargetFramework)' == 'net481'">
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
</ItemGroup>

<ItemGroup Condition="'$(TargetFramework)' == 'net8.0'">
  <!-- Same packages or use .NET 8 optimized versions -->
</ItemGroup>
```

---

## Task 6.5: Create Legacy Solution File

**Status**: ❌ NOT STARTED
**Source**: FINDING 55 in SQL_CLR_RESEARCH_FINDINGS.md

### Problem
SqlClr old-style project may not open properly in all VS versions.

### Solution

Create `Hartonomous.Legacy.sln` for old-style projects:

```powershell
cd D:\Repositories\Hartonomous
dotnet new sln -n Hartonomous.Legacy

# Add only old-style projects:
dotnet sln Hartonomous.Legacy.sln add src/SqlClr/SqlClrFunctions.csproj
# Add any other old-style projects
```

### Benefits
- Separate solution for SqlClr development
- Works in older Visual Studio versions
- Main Hartonomous.sln remains modern

---

## Task 6.6: Update Deployment Scripts

**Status**: ❌ NOT STARTED
**Dependencies**: Tasks 6.1, 6.2 complete

### Problem
Deployment scripts reference old project structure.

### Files to Update

**deploy/deploy-local-dev.ps1**:
- Remove CesConsumer references
- Remove Neo4jSync references
- Add Hartonomous.Worker

**deploy/deploy-to-hart-server.ps1**:
- Update project paths
- Update systemd service files

**deploy/*.service files**:
- Consolidate into hartonomous-worker.service
- Update ExecStart paths

### Example Updated Service File

`deploy/hartonomous-worker.service`:
```ini
[Unit]
Description=Hartonomous Background Worker
After=network.target

[Service]
Type=notify
ExecStart=/usr/bin/dotnet /opt/hartonomous/Hartonomous.Worker.dll
WorkingDirectory=/opt/hartonomous
Restart=always
RestartSec=10
User=hartonomous

[Install]
WantedBy=multi-user.target
```

---

## Success Criteria

Phase 6 complete when:
- ✅ Console apps consolidated into Worker project
- ✅ Hartonomous.Data merged into Infrastructure
- ✅ Hartonomous.Core multi-targets net481+net8.0
- ✅ Hartonomous.Infrastructure multi-targets net481+net8.0
- ✅ Legacy solution file created
- ✅ Deployment scripts updated
- ✅ Full solution builds with 0 errors
- ✅ All tests pass
- ✅ Changes committed to git

## Project Count Reduction

Before: 9+ projects  
After: 6 projects

Removed:
- CesConsumer
- Neo4jSync
- Hartonomous.Admin
- Hartonomous.Data

Added:
- Hartonomous.Worker

Remaining:
- Hartonomous.Api
- Hartonomous.Core (multi-targeted)
- Hartonomous.Infrastructure (multi-targeted)
- SqlClr (old-style, single-target)
- Hartonomous.Tests

## Next Phase

After Phase 6 complete → `07-PERFORMANCE.md`
