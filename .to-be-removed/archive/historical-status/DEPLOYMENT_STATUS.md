# Hartonomous Deployment Status

## Completed ✅

### 1. Database Schema Singularization
- **Status:** Complete
- **All 87 tables** renamed from plural to singular following old-school T-SQL conventions
  - Database: `dbo.Atom` (singular)
  - Code: `DbSet<Atom> Atoms` (entity singular, property plural)
- Tables like Atom, Model, AtomEmbedding, AtomRelation, TensorAtom now singular

### 2. Idempotent DACPAC Deployment
- **Script:** `scripts/deploy-dacpac.ps1`
- **Features:**
  - Automatically handles schema evolution
  - Drops objects not in source (except assemblies)
  - Supports both alternate DACPAC paths
  - No data loss on compatible schema changes
- **Usage:** `.\scripts\deploy-dacpac.ps1 -Server localhost -Database Hartonomous -TrustServerCertificate`

### 3. Idempotent DACPAC Build
- **Script:** `scripts/build-dacpac.ps1`
- **Features:**
  - Checks timestamps, skips rebuild if up-to-date
  - Supports alternate output paths
  - MSBuild integration for SQL projects

### 4. Entity Scaffolding
- **Script:** `scripts/scaffold-entities.ps1`
- **Status:** Generates singular entity classes matching table names
- **Features:**
  - Backs up existing entities before regenerating
  - Uses `--no-pluralize` flag
  - Cleans up old files automatically

### 5. Final Cleanup Script
- **Script:** `scripts/finalize-singular-schema.ps1`
- **Purpose:** One-time fix for constraint names (PK_, FK_, IX_)
- **Status:** Executed successfully, can be kept for future reference

## Outstanding Issues ⚠️

### Missing Entity Types in Core Project
The following repository interfaces reference entities that no longer exist as database tables:
- `AtomicAudioSample`
- `AtomicPixel`
- `AtomicTextToken`
- `LayerTensorSegment`

**Root Cause:** These tables were removed from the database schema during prior refactoring but repository interfaces were not updated.

**Resolution Required:**
1. Remove obsolete repository interfaces from `src/Hartonomous.Core/Interfaces/`
2. OR add these tables back to the database schema if still needed
3. Check for any service/controller code referencing these repositories

**Files to Review:**
- `src/Hartonomous.Core/Interfaces/IAtomicAudioSampleRepository.cs`
- `src/Hartonomous.Core/Interfaces/IAtomicPixelRepository.cs`
- `src/Hartonomous.Core/Interfaces/IAtomicTextTokenRepository.cs`
- `src/Hartonomous.Core/Interfaces/ILayerTensorSegmentRepository.cs`

### Name Collision
- `SemanticFeatures` exists as both an entity (`Hartonomous.Data.Entities.SemanticFeatures`) and a value object (`Hartonomous.Core.ValueObjects.SemanticFeatures`)
- **Resolution:** Fully qualify the type or rename one of them

## Deployment Workflow

### Local Development
```powershell
# Full deployment
.\Deploy.ps1

# Or step-by-step
.\scripts\build-dacpac.ps1
.\scripts\deploy-dacpac.ps1 -Server localhost -Database Hartonomous -TrustServerCertificate
.\scripts\scaffold-entities.ps1 -Server localhost -Database Hartonomous -TrustServerCertificate
```

### Azure Arc Deployment (HART-SERVER, HART-DESKTOP)
**Target Platforms:**
- Azure SQL Managed Instance (supports CLR)
- SQL Server on Azure VMs
- Azure Arc-enabled SQL Server (on-prem with Azure management)

**NOT Compatible:**
- Azure SQL Database (does NOT support CLR assemblies)

### Connection Strings
```
# HART-DESKTOP (current)
Server=localhost;Database=Hartonomous;Integrated Security=True;TrustServerCertificate=True;

# HART-SERVER (Ubuntu)
Server=HART-SERVER;Database=Hartonomous;Integrated Security=True;TrustServerCertificate=True;
```

## Architecture Summary

### Core Philosophy
- **Database-first:** SQL DACPAC is source of truth
- **C# Scaffolded:** Entity classes auto-generated from schema
- **CLR-Heavy:** ~16,780 lines of SIMD-optimized C# in database for compute
- **Spatial R-Tree:** O(log N) similarity search instead of vector DB

### Key Technologies
- SQL Server 2019+ with CLR integration
- Spatial indexes for 1998D embeddings projected to 3D
- System-versioned temporal tables
- Service Broker OODA loop for autonomous improvement

## Next Steps

1. **Fix Missing Repository References** (Priority: HIGH)
   - Remove obsolete repository interfaces OR
   - Add back missing tables to schema

2. **Resolve SemanticFeatures Ambiguity** (Priority: MEDIUM)
   - Rename value object or fully qualify usages

3. **Test Deployment to HART-SERVER** (Priority: MEDIUM)
   - Validate Ubuntu/Linux deployment
   - Test Neo4j sync (neo4j://HART-SERVER:7687)

4. **Azure DevOps Pipeline Integration** (Priority: LOW)
   - Update pipeline to use new deployment scripts
   - Add automated testing post-deployment

## Files Modified

### Scripts (Production-Ready)
- ✅ `scripts/build-dacpac.ps1` - Idempotent DACPAC build
- ✅ `scripts/deploy-dacpac.ps1` - Idempotent deployment with schema evolution
- ✅ `scripts/scaffold-entities.ps1` - Entity generation
- ✅ `scripts/finalize-singular-schema.ps1` - One-time constraint fix (can archive)
- ✅ `Deploy.ps1` - Master orchestrator

### Database Schema
- ✅ All 87 table files in `src/Hartonomous.Database/Tables/` renamed to singular
- ✅ All constraints (PK, FK, IX, UQ) use singular naming
- ✅ DACPAC builds cleanly (309.22 KB)

### Entities
- ✅ `src/Hartonomous.Data.Entities/Entities/` - Singular entity classes
- ✅ `src/Hartonomous.Data.Entities/HartonomousDbContext.cs` - Updated

## Validation Commands

```powershell
# Verify database schema
sqlcmd -S localhost -d Hartonomous -E -C -Q "SELECT TOP 10 TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA='dbo' ORDER BY TABLE_NAME"

# Verify entity generation
ls src/Hartonomous.Data.Entities/Entities/ | Select-String "^Atom"

# Build check
dotnet build src/Hartonomous.Data.Entities/Hartonomous.Data.Entities.csproj

# Full build (will fail until repository interfaces fixed)
dotnet build Hartonomous.sln
```

---
**Last Updated:** 2025-11-16
**Status:** 95% Complete - Awaiting repository interface cleanup
