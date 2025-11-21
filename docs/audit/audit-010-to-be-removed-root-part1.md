# Documentation Audit Segment 010 - Root Files Part 1

**Segment**: 010-part1  
**Location**: `.archive/.to-be-removed/` (root level)  
**Files Catalogued**: 10 files  
**Total Files in Segment**: 10/20 root files  
**Date**: 2025-01-28

---

## Files Analyzed

### 1. QUICKSTART.md
- **Path**: `.archive/.to-be-removed/QUICKSTART.md`
- **Type**: Quickstart Guide
- **Lines**: 457
- **Quality**: ⭐⭐⭐⭐⭐

**Purpose**: Get Hartonomous running locally in 5 minutes with minimal setup.

**Key Content**:
- **Prerequisites**: SQL Server 2019+, .NET 8 SDK, Visual Studio 2022, optional Neo4j 5.x
- **Step 1 - Clone Repository**: Standard git clone
- **Step 2 - Build SQL CLR Project**:
  - Target: .NET Framework 4.8.1
  - Build: `MSBuild src/Hartonomous.Database/Hartonomous.Database.sqlproj /p:Configuration=Release`
  - Output: `bin/Release/Hartonomous.Database.dll`
- **Step 3 - Deploy Database**:
  - Option A: SqlPackage DACPAC deployment
  - Option B: Visual Studio publish profile
- **Verification Queries**:
  ```sql
  -- Check tables
  SELECT COUNT(*) FROM sys.tables WHERE SCHEMA_NAME(schema_id) = 'dbo';
  
  -- Check CLR functions
  SELECT name FROM sys.assembly_modules;
  
  -- Check spatial indexes
  SELECT name FROM sys.indexes WHERE type = 4; -- SPATIAL
  ```

**Key Concepts**:
- Local development setup (no cloud dependencies)
- DACPAC-based deployment (modern approach)
- CLR assembly compilation (.NET Framework 4.8.1 required)
- Verification steps for successful deployment

**Relationships**:
- Complements: `SETUP-PREREQUISITES.md` (detailed requirements)
- Superseded by: `docs/getting-started/00-quickstart.md` (new structure)
- Referenced by: `DACPAC-CLR-DEPLOYMENT.md` (deployment deep-dive)

**Conflicts**: None - practical quickstart guide

**Recommendations**:
- ✅ **PROMOTE**: Excellent onboarding content
- Migrate to `docs/getting-started/quickstart.md`
- Add troubleshooting section for common CLR deployment errors
- Include Neo4j optional setup instructions

---

### 2. CLR-ARCHITECTURE-ANALYSIS.md
- **Path**: `.archive/.to-be-removed/CLR-ARCHITECTURE-ANALYSIS.md`
- **Type**: Technical Analysis
- **Lines**: 1,095
- **Quality**: ⭐⭐⭐⭐⭐
- **Date**: 2025-11-18

**Purpose**: Comprehensive analysis of 72 CLR files (~15,000+ lines) identifying technical debt and refactoring opportunities.

**Key Findings**:
- ✅ **Strong Foundation**: SIMD-optimized helpers (VectorMath, VectorUtilities, PooledList)
- ⚠️ **Code Duplication**: 3+ files have private `ParseVectorJson` implementations
- ⚠️ **Boilerplate**: 30+ aggregates with similar IBinarySerialize patterns
- 🎯 **High Impact**: Consolidate 200-300 lines of duplicate code
- 🔧 **Test Coverage**: Only 2 test files for 72 CLR implementations

**Duplicate Code Locations**:
- `DimensionalityReductionAggregates.cs` (3 instances in same file - lines 235-246, 373-385, 529-531)
- `AttentionGeneration.cs` (lines 338-368)
- 47+ files correctly use `VectorUtilities.ParseVectorJson` ✅

**CLR Registration Process**:
- Primary script: `Register_CLR_Assemblies.sql`
- Security setup in Master DB (asymmetric key, login, UNSAFE ASSEMBLY permission)
- Dependency registration: System.Runtime.CompilerServices.Unsafe, System.Buffers, System.Memory, System.Collections.Immutable, System.Reflection.Metadata, MathNet.Numerics, Microsoft.SqlServer.Types, Newtonsoft.Json
- Main assembly via DACPAC (embedded as hex binary)
- Modern approach: DACPAC with strong-name signing

**Recommendations**:
- Migrate 3 files to shared `VectorUtilities.ParseVectorJson`
- Increase test coverage (target: 70%+ for CLR functions)
- Extract common IBinarySerialize patterns to base classes
- Document CLR registration process in operations guide

**Relationships**:
- Superseded by: `CLR-REFACTORING-ANALYSIS.md` (deeper analysis)
- Complements: `DEPENDENCY-MATRIX.md` (CLR dependencies)
- References: 72 CLR files in `src/Hartonomous.Clr/`

**Recommendations**:
- ✅ **ARCHIVE**: Valuable historical analysis
- Extract refactoring tasks to backlog
- Keep as reference for future CLR optimization work
- Technical debt identified should be tracked

---

### 3. CLR-REFACTOR-COMPREHENSIVE.md
- **Path**: `.archive/.to-be-removed/CLR-REFACTOR-COMPREHENSIVE.md`
- **Type**: Code Expansion Documentation
- **Lines**: 656
- **Quality**: ⭐⭐⭐⭐⭐
- **Status**: 🚀 **STAGED FOR COMMIT**
- **Date**: 2025-11-18

**Purpose**: Document 49 new CLR files (~225,000 lines) staged for deployment.

**Critical Discovery**: Files showing `+++ b/` in git diffs are **NEW ADDITIONS** (not missing implementations).

**File Categories**:
1. **Enums** (7 files, ~12,000 lines):
   - `LayerType.cs` (1,328 lines): 32 neural network layer types (Dense, Embedding, LayerNorm, Attention, MultiHeadAttention, GatedMLPLayer, etc.)
   - `ModelFormat.cs` (670 lines): 7 formats (GGUF, SafeTensors, ONNX, PyTorch, TensorFlow, StableDiffusion)
   - `PruningStrategy.cs` (691 lines): 7 OODA compression strategies (MagnitudeBased, GradientBased, ImportanceBased, StructuredPruning, etc.)
   - `QuantizationType.cs` (1,148 lines): 26 GGML/GGUF quantization schemes (F32, F16, Q8_0, Q4_0, Q2_K through Q8_K, IQ1_S through IQ4_XS)
   - `SpatialIndexStrategy.cs` (686 lines): 7 strategies (RTree, Hilbert3D, Morton2D, Morton3D, KDTree, BallTree)
   - `TensorDtype.cs` (1,404 lines): 36 tensor data types (F32, F16, BF16, I8-I64, U8-U64, Bool, quantized types)
   - `DistanceMetricType.cs` (~400 lines): Universal distance metric selection

2. **MachineLearning/** (17 files, ~147,000 lines):
   - CUSUMDetector, IsolationForest, LocalOutlierFactor, DBSCAN
   - Ensemble methods, time series forecasting
   - All algorithms parameterized via `IDistanceMetric` interface

3. **ModelParsers/** (5 files, ~48,000 lines):
   - Complete parsers for model formats

4. **Models/** (10 files, ~17,000 lines):
   - Domain models for ML operations

5. **Database Tables** (2 files, ~600 lines):
   - Schema definitions for new features

**Integration Points**:
- SQL Server CLR assemblies (EXTERNAL_ACCESS/UNSAFE permission)
- T-SQL interface (CREATE FUNCTION/PROCEDURE statements)
- **Universal Distance Support**: All ML algorithms accept `IDistanceMetric` parameter (Euclidean, Cosine, Manhattan, Minkowski)
- Spatial Indexing: Integration with R-Tree indices via GEOMETRY types
- OODA Loop: Algorithms feed autonomous learning pipeline

**Key Enums Detail**:
- **QuantizationType**: Complete GGUF quantization support (F32, F16, Q8_0, Q4_0, Q2_K, Q3_K, Q4_K, Q5_K, Q6_K, Q8_K, IQ1_S, IQ2_XXS, IQ2_XS, IQ3_XXS, IQ3_S, IQ4_NL, IQ4_XS)
- **TensorDtype**: Comprehensive tensor type system (F64, F32, F16, BF16, I64, I32, I16, I8, U64, U32, U16, U8, Bool, Complex64, Complex128, plus quantized variants)
- **LayerType**: Full transformer architecture support (32 layer types from Dense to custom variants)

**Relationships**:
- Builds on: `CLR-ARCHITECTURE-ANALYSIS.md` (refactoring analysis)
- Implements: Universal distance metrics for all ML algorithms
- Integrates with: OODA loop, spatial indexing, T-SQL interface

**Recommendations**:
- ✅ **ARCHIVE**: Deployment documentation
- **DEPLOY**: 49 files represent major production expansion
- Post-deployment: Update architecture documentation with new capabilities
- Create migration guide for new enum types and ML algorithms

---

### 4. AZURE-PRODUCTION-READY.md
- **Path**: `.archive/.to-be-removed/AZURE-PRODUCTION-READY.md`
- **Type**: Deployment Package Documentation
- **Lines**: 369
- **Quality**: ⭐⭐⭐⭐⭐
- **Status**: ✅ **READY TO DEPLOY**
- **Date**: 2025-01-16

**Purpose**: Azure Arc hybrid deployment package (4-6 hours to production).

**Target Architecture**: Azure Cloud + On-Prem via Azure Arc
- **Azure Cloud Layer**:
  - Key Vault (`kv-hartonomous-production`): Connection strings, secrets
  - App Configuration (`appconfig-hartonomous-production`): OODA settings, inference defaults, feature flags
  - Entra ID: App registrations (API + Blazor UI), roles (Admin/Analyst/User)
  - Azure DevOps: Pipelines (database, app deployment)

- **On-Prem via Azure Arc**:
  - **HART-DESKTOP**: SQL Server 2025 host (Hartonomous DB, CLR assemblies, spatial indexes, OODA procedures, Arc agent)
  - **HART-SERVER**: Application host (Hartonomous.Api Windows Service with Entra ID auth + Key Vault integration, Workers: CES Consumer, Neo4j Sync, OODA Analyzers)

**Phase 1 Deliverables** ✅:
1. **Azure Infrastructure Script** (`01-create-infrastructure.ps1`):
   - Creates Resource Group, Key Vault, App Configuration
   - Creates Entra ID app registrations for API + Blazor UI
   - Stores connection strings as secrets
   - Configures app roles (Admin/Analyst/User)
   - Sets up feature flags

2. **Azure DevOps Pipelines**:
   - `database-pipeline.yml`: DACPAC build & deploy
   - `app-pipeline.yml`: .NET apps build & deploy to Windows Services

3. **API Security Configuration** (`Program.EntraId.cs`):
   - Azure Key Vault integration
   - App Configuration integration
   - Entra ID JWT authentication
   - Role-based authorization
   - Application Insights telemetry
   - Swagger with OAuth2

4. **Production Configuration**: `appsettings.Production.json` template

5. **Complete Deployment Guide**: `AZURE-DEPLOYMENT-GUIDE.md` (50-page comprehensive guide)

6. **Master Orchestration Script**: `MASTER-DEPLOY.ps1` (one-command deployment)

**Timeline**: 4-6 hours to production

**Key Features**:
- Entra ID authentication (no SQL logins)
- Key Vault secrets management
- App Configuration feature flags
- Application Insights monitoring
- Azure Arc hybrid deployment
- Windows Services deployment model

**Relationships**:
- Requires: `AZURE-ARC-SERVICE-PRINCIPAL-SETUP.md` (Arc configuration)
- Implements: `GITHUB-ACTIONS-MIGRATION.md` (CI/CD patterns)
- Complements: `SETUP-PREREQUISITES.md` (system requirements)

**Recommendations**:
- ✅ **PROMOTE**: Critical production deployment documentation
- Move to `docs/operations/azure-deployment.md`
- Add post-deployment validation checklist
- Document rollback procedures
- Include monitoring dashboard setup

---

### 5. DACPAC-CLR-DEPLOYMENT.md
- **Path**: `.archive/.to-be-removed/DACPAC-CLR-DEPLOYMENT.md`
- **Type**: Technical Documentation
- **Lines**: 302
- **Quality**: ⭐⭐⭐⭐⭐

**Purpose**: Explain how SQL Server Database Projects (`.sqlproj`) with CLR assemblies work with DACPACs and correct deployment order.

**Key Finding**: DACPAC **DOES** contain `Hartonomous.Clr.dll` embedded as hex binary, but does **NOT** contain the 16 external dependency DLLs.

**How CLR Assemblies Work in DACPACs**:

1. **C# Source Code Compilation**:
   - Build process compiles all `<Compile Include="CLR\*.cs">` files
   - Produces `Hartonomous.Clr.dll` assembly

2. **DACPAC Contents**:
   - Compiled `Hartonomous.Clr.dll` embedded as hex binary (`CREATE ASSEMBLY FROM 0x4D5A90...`)
   - All T-SQL DDL for tables, views, procedures, functions
   - Metadata about CLR functions/procedures/aggregates
   - Pre/post-deployment scripts

3. **What's NOT in DACPAC**:
   - External assembly DLLs with `<Private>False</Private>` in `.sqlproj`
   - The 16 dependency DLLs (MathNet.Numerics, System.Numerics.Vectors, etc.)

**Why External Assemblies Aren't Embedded**:
```xml
<Reference Include="MathNet.Numerics">
  <HintPath>..\..\dependencies\MathNet.Numerics.dll</HintPath>
  <Private>False</Private>  <!-- THIS IS THE KEY -->
</Reference>
```
- `<Private>False</Private>` means "don't copy to output directory"
- Compile-time references only (needed to build `Hartonomous.Clr.dll`)
- DACPAC contains `CREATE ASSEMBLY` statements expecting these DLLs to exist on SQL Server

**Correct Deployment Order** (Current Pipeline):

**Stage 1 - BuildDatabase**:
- Compiles C# code → `Hartonomous.Clr.dll`
- Compiles T-SQL scripts → schema model
- Produces `Hartonomous.Database.dacpac` (contains `Hartonomous.Clr.dll` as hex binary)
- Artifacts: DACPAC file + 16 external DLL files from `dependencies/` folder

**Stage 2 - DeployDatabase**:
1. Enable CLR Integration (`sp_configure 'clr enabled', 1`)
2. Deploy 16 External CLR Assemblies (`deploy-clr-assemblies.ps1`) - **MUST HAPPEN FIRST**
3. Deploy DACPAC with SqlPackage (creates tables, views, procedures, `Hartonomous.Clr` assembly, CLR functions)
4. Set `TRUSTWORTHY ON`

**Critical**: External assemblies must be deployed **BEFORE** DACPAC because:
- `Hartonomous.Clr.dll` references these assemblies
- SQL Server validates dependencies when creating assemblies
- DACPAC deployment will fail if dependencies don't exist

**Stage 3 - ScaffoldEntities**:
- EF Core scaffolds from COMPLETE database schema
- Sees all tables, views, AND CLR functions

**Stage 4 - BuildDotNet**: Builds .NET solution using scaffolded entities

**Stage 5 - DeployApplications**: Deploys .NET applications to servers

**Relationships**:
- Complements: `DEPENDENCY-MATRIX.md` (assembly dependencies)
- Explains: `QUICKSTART.md` deployment steps
- References: `scripts/deploy-clr-assemblies.ps1`

**Recommendations**:
- ✅ **PROMOTE**: Critical deployment documentation
- Move to `docs/operations/dacpac-clr-deployment.md`
- Add troubleshooting section (common CLR deployment errors)
- Include dependency tier visualization

---

### 6. COMPREHENSIVE-TEST-SUITE.md
- **Path**: `.archive/.to-be-removed/COMPREHENSIVE-TEST-SUITE.md`
- **Type**: Testing Documentation
- **Lines**: 314
- **Quality**: ⭐⭐⭐⭐

**Purpose**: Describe comprehensive test suite validating integrity, correctness, and performance across all layers.

**Test Coverage Strategy** (Testing Pyramid):
```
        E2E Tests (5%)
       /          \
     Integration Tests (10%)
    /                  \
  Database Tests (10%)  Unit Tests (60%)
 /                              \
CLR Function Tests (15%)    T-SQL Tests
```

**Test Projects Created**:

1. **Hartonomous.Clr.Tests** (.NET Framework 4.8.1):
   - Status: ✅ PASSING (4/4 tests)
   - Tests: `VectorMathTests.cs` (DotProduct, SIMD usage), `LandmarkProjectionTests.cs` (3D projection determinism)
   - No database dependency, fast execution (<100ms)

2. **Hartonomous.Core.Tests** (.NET 8.0):
   - Status: ✅ CREATED
   - Tests: `AtomDataTests.cs`, `ContentHashingTests.cs`
   - Dependencies: xUnit 2.9.2, FluentAssertions 7.0.0, Moq 4.20.72

3. **Hartonomous.Atomizers.Tests** (.NET 8.0):
   - Status: ✅ CREATED
   - Tests: `TextAtomizerTests.cs` (sentence splitting, Unicode support, hash uniqueness)

4. **Hartonomous.Integration.Tests** (.NET 8.0):
   - Status: ✅ CREATED
   - Tests: `DatabaseIntegrationTests.cs` (connectivity, table existence, spatial indexes, CLR invocation, Service Broker status)

**Relationships**:
- Implements: `docs/rewrite-guide/15-Testing-Strategy.md` pyramid
- Tests: 72 CLR files from `CLR-ARCHITECTURE-ANALYSIS.md`
- Validates: SIMD optimizations, spatial operations, hash consistency

**Recommendations**:
- ✅ **PROMOTE**: Essential testing documentation
- Move to `docs/operations/testing-guide.md`
- Add performance benchmarking tests
- Expand CLR test coverage (target: 70%+)
- Document test execution in CI/CD pipeline

---

### 7. DEPENDENCY-MATRIX.md
- **Path**: `.archive/.to-be-removed/DEPENDENCY-MATRIX.md`
- **Type**: Technical Reference
- **Lines**: 242
- **Quality**: ⭐⭐⭐⭐⭐
- **Date**: 2024-11-17

**Purpose**: Complete CLR assembly dependency analysis for SQL Server 2025 deployment.

**Summary**:
- Total Dependencies: 14 assemblies
- Available in GAC: 6 assemblies (should NOT deploy)
- Require Deployment: 8 assemblies
- DACPAC-Generated: 2 assemblies (Hartonomous.Database.dll, SqlClrFunctions.dll)

**Assemblies to DEPLOY** (Not in GAC):
1. `System.Runtime.CompilerServices.Unsafe.dll` (4.6.28619.01) - **DEPLOY FIRST** (no dependencies)
2. `System.Buffers.dll` (4.6.28619.01) - **DEPLOY SECOND** (no dependencies)
3. `System.Numerics.Vectors.dll` (4.6.26515.06) - **DEPLOY THIRD** (depends on GAC System.Numerics)
4. `System.Memory.dll` (4.6.28619.01) - **DEPLOY FOURTH** (depends on above 3)
5. `System.Collections.Immutable.dll` (4.700.20.21406) - Requires System.Memory
6. `System.Reflection.Metadata.dll` (4.700.20.21406) - Requires System.Collections.Immutable
7. `MathNet.Numerics.dll` (5.0.0.0) - Mathematical functions library
8. `Microsoft.SqlServer.Types.dll` (16.0.1000.6) - SQL Server spatial types
9. `Newtonsoft.Json.dll` (13.0.1.25517) - JSON serialization

**Assemblies in GAC** (DO NOT DEPLOY):
- System.Drawing.dll, System.Numerics, System.Runtime.Serialization.dll, System.ValueTuple.dll, SMDiagnostics.dll, System.ServiceModel.Internals.dll

**Deployment Order** (Based on Dependency Graph):
```
1. System.Runtime.CompilerServices.Unsafe.dll (no dependencies)
2. System.Buffers.dll (no dependencies)
3. System.Numerics.Vectors.dll (depends on GAC: System.Numerics)
4. System.Memory.dll (depends on: Unsafe, Buffers, Numerics.Vectors)
5. System.Collections.Immutable.dll (depends on: System.Memory)
6. System.Reflection.Metadata.dll (depends on: System.Collections.Immutable)
7. Microsoft.SqlServer.Types.dll (independent)
8. Newtonsoft.Json.dll (independent, uses GAC assemblies)
9. MathNet.Numerics.dll (independent, uses GAC assemblies)
```

**Relationships**:
- Referenced by: `DACPAC-CLR-DEPLOYMENT.md` (deployment order)
- Referenced by: `CLR-ARCHITECTURE-ANALYSIS.md` (CLR registration)
- Complements: `SETUP-PREREQUISITES.md` (system requirements)

**Recommendations**:
- ✅ **PROMOTE**: Critical reference documentation
- Move to `docs/operations/clr-dependencies.md`
- Add assembly version compatibility matrix
- Document GAC query commands for verification
- Include troubleshooting for assembly conflicts

---

### 8. OODA-DUAL-TRIGGERING-ARCHITECTURE.md
- **Path**: `.archive/.to-be-removed/OODA-DUAL-TRIGGERING-ARCHITECTURE.md`
- **Type**: Architecture Clarification
- **Lines**: 484
- **Quality**: ⭐⭐⭐⭐⭐
- **Status**: ✅ User-Validated
- **Date**: 2025-01-18

**Purpose**: Correct misconceptions about Service Broker vs scheduled execution - both are intentional.

**Clarification**: Hartonomous OODA loop uses **DUAL-TRIGGERING** mechanism:

1. ✅ **Scheduled OODA Loop** (Every 15 minutes) - System maintenance, entropy reduction
2. ✅ **Event-Driven Service Broker** (On-demand) - User requests, autonomous computation

**Both mechanisms are complementary** - NOT a gap, NOT a choice between SQL Agent OR Service Broker.

**Architecture Correction**:

**❌ Incorrect Understanding**: OODA loop uses **EITHER** SQL Agent scheduling **OR** Service Broker internal activation.

**✅ Correct Understanding**: OODA loop uses **BOTH** triggering mechanisms for different purposes.

**Dual-Triggering Architecture**:
```
├─ Scheduled OODA Loop (SQL Agent: Every 15 minutes)
│  Purpose: System maintenance, entropy reduction, weight pruning
│  Trigger: SQL Agent job → sp_Analyze → Service Broker cascade
│  Use Cases:
│    - Detect slow queries (sp_Analyze)
│    - Identify index opportunities (sp_Hypothesize)
│    - Prune unused model weights (sp_Act)
│    - Update model weights from feedback (sp_Learn)
│
└─ Event-Driven Service Broker (On-demand)
   Purpose: User requests, autonomous computation, Gödel Engine
   Trigger: API request → BEGIN DIALOG → Service Broker queue → sp_Hypothesize
   Use Cases:
     - User initiates model inference
     - User creates AutonomousComputeJob
     - System detects anomaly requiring immediate action
     - External event triggers hypothesis generation
```

**1. Scheduled OODA Loop** (15-Minute Cycle):
- **Observe**: Collect system metrics (query performance, cache hit ratios, index usage)
- **Orient**: Detect patterns (slow queries, cache misses, unused indexes)
- **Decide**: Generate hypotheses (create index, prune weights, warm cache)
- **Act**: Execute safe improvements automatically
- **Learn**: Measure impact, update model weights

**Trigger Mechanism**:
```sql
-- SQL Server Agent job: Every 15 minutes
EXEC msdb.dbo.sp_add_job @job_name = 'OodaCycle_15min';

EXEC msdb.dbo.sp_add_schedule 
    @schedule_name = 'Every15Minutes',
    @freq_type = 4,          -- Daily
    @freq_interval = 1,      -- Every day
    @freq_subday_type = 4,   -- Minutes
    @freq_subday_interval = 15;
```

**2. Event-Driven Service Broker** (On-demand):
- User API requests trigger immediate hypothesis generation
- Autonomous compute jobs via Service Broker queues
- Internal activation for background processing

**Relationships**:
- Clarifies: `docs/architecture/02-ooda-autonomous-loop.md` (corrects misconceptions)
- Implements: Dual-triggering for different use cases
- Complements: Service Broker configuration documentation

**Recommendations**:
- ✅ **PROMOTE**: Critical architecture clarification
- Integrate into `docs/architecture/ooda-loop.md`
- Add sequence diagrams for both triggering paths
- Document configuration for both mechanisms
- Explain use case decision matrix (when to use which trigger)

---

### 9. UNIVERSAL-FILE-SYSTEM-DESIGN.md
- **Path**: `.archive/.to-be-removed/UNIVERSAL-FILE-SYSTEM-DESIGN.md`
- **Type**: Design Document
- **Lines**: 2,114
- **Quality**: ⭐⭐⭐⭐⭐
- **Status**: Design Phase - Awaiting Approval
- **Date**: 2025-11-18

**Purpose**: Replace incomplete model parser implementations with enterprise-grade universal file handling system.

**Key Architectural Principles**:
- **Separation of Concerns**: Model providers ≠ Format parsers
- **No Cop-Outs**: Complete implementations, no "recommend conversion"
- **Streaming First**: Memory-efficient processing for large files
- **SQL Server 2025 Native**: Use `vector`/`json` types, not CLR wrappers
- **Universal Coverage**: Documents, images, video, audio, telemetry, archives, AI models

**System Layers**:

1. **Model Provider Layer** (Retrieval/download - DOES NOT PARSE):
   - `IModelProvider` abstraction
   - Implementations: HuggingFaceProvider, OllamaProvider, LocalFileProvider, AzureBlobProvider, OpenAIProvider
   - Returns: Single file stream or multi-file catalog

2. **Format Parser Layer** (Format-specific parsing after retrieval):
   - GGUF, ONNX, PyTorch, SafeTensors, PDF, images, etc.
   - Streaming parsers for memory efficiency

3. **Archive Handler Layer**:
   - ZIP, TAR, 7Z compressed file extraction

4. **Catalog Manager Layer**:
   - Multi-file coordination (HuggingFace repos, model families)

5. **SQL Server 2025 Integration**:
   - Native `vector`/`json` types
   - REST endpoints for external access

**Provider Implementations Detailed**:

**HuggingFaceProvider**:
- Format: `owner/repo` or `hf://owner/repo`
- Returns catalog with: model.safetensors, config.json, tokenizer files, README.md

**OllamaProvider**:
- Format: `ollama://model:tag`
- Connects to Ollama API (http://localhost:11434)
- Returns GGUF file stream

**LocalFileProvider**:
- Format: `file:///path/to/model`
- Direct file system access

**AzureBlobProvider**:
- Format: `azure://container/blob`
- Azure Blob Storage integration

**Relationships**:
- Supersedes: Incomplete model parser implementations
- Integrates with: `MODEL-PROVIDER-LAYER.md` concepts
- Complements: `CLR-REFACTOR-COMPREHENSIVE.md` (ModelParsers/)

**Recommendations**:
- ✅ **PROMOTE**: Comprehensive design document
- Move to `docs/architecture/universal-file-system.md`
- Prioritize implementation (critical for production)
- Create implementation roadmap with phases
- Document performance requirements for streaming

---

### 10. RUNNER-ARCHITECTURE.md
- **Path**: `.archive/.to-be-removed/RUNNER-ARCHITECTURE.md`
- **Type**: CI/CD Documentation
- **Lines**: 144
- **Quality**: ⭐⭐⭐⭐⭐

**Purpose**: Document GitHub Actions self-hosted runner architecture with specific job assignments.

**Runner Configuration**:

**HART-DESKTOP (Windows)**:
- Location: `D:\GitHub\actions-runner`
- Service: `actions.runner.AHartTN-Hartonomous-Sandbox.HART-DESKTOP`
- Labels: `self-hosted`, `windows`, `sql-server`
- Capabilities: Windows Server 2025, .NET Framework 4.8.1, MSBuild (VS 2022), SQL Server 2022, SqlPackage, Azure CLI, PowerShell 7.5

**Assigned Jobs**:
1. `build-dacpac` - Builds .sqlproj with CLR assemblies (requires .NET Framework + MSBuild)
2. `deploy-database` - Deploys DACPAC, CLR assemblies, sets up SQL Server
3. `scaffold-entities` - Generates EF Core entities from database

**hart-server (Linux)**:
- Location: `/var/workload/GitHub/actions-runner`
- Service: `actions.runner.AHartTN-Hartonomous-Sandbox.hart-server`
- Service User: `github-runner` (system account)
- Labels: `self-hosted`, `linux`, `hart-server`
- Capabilities: Ubuntu Linux, .NET 8 SDK / .NET 10 SDK, Docker, faster builds

**Assigned Jobs**:
1. `build-and-test` - Builds and tests .NET 10 solution (cross-platform)
2. `build-applications` - Publishes API and worker applications

**Job Dependencies & Flow**:
```
build-dacpac (Windows)
    ↓
deploy-database (Windows)
    ↓
scaffold-entities (Windows)
    ↓
build-and-test (Linux)
    ↓
build-applications (Linux)
```

**Technical Constraints**:

**Why Windows for Database Jobs?**
1. CLR assemblies target .NET Framework 4.8.1
2. MSBuild requirement (not cross-platform)
3. SQL Server access (localhost connection)
4. Azure Arc integration

**Why Linux for .NET 10 Jobs?**
1. .NET 10 is cross-platform
2. Better performance
3. Cost efficiency (dedicated workload disk)
4. Resource isolation

**Service Management**:
```powershell
# Windows
Get-Service -Name "actions.runner.AHartTN-Hartonomous-Sandbox.HART-DESKTOP"
Restart-Service -Name "actions.runner.AHartTN-Hartonomous-Sandbox.HART-DESKTOP"

# Linux
sudo systemctl status actions.runner.AHartTN-Hartonomous-Sandbox.hart-server.service
sudo systemctl restart actions.runner.AHartTN-Hartonomous-Sandbox.hart-server.service
```

**Relationships**:
- Implements: `GITHUB-ACTIONS-MIGRATION.md` (CI/CD strategy)
- Complements: `AZURE-PRODUCTION-READY.md` (deployment architecture)
- References: `.github/workflows/*.yml` pipeline definitions

**Recommendations**:
- ✅ **PROMOTE**: Critical CI/CD documentation
- Move to `docs/operations/runner-architecture.md`
- Add runner troubleshooting guide
- Document runner maintenance procedures
- Include capacity planning guidelines

---

## Cross-File Analysis

### Common Themes
1. **Production Readiness**: Multiple files focus on deployment, testing, and production operations
2. **CLR Architecture**: Deep analysis of CLR code, dependencies, and deployment
3. **Azure Integration**: Strong focus on Azure Arc, Entra ID, and cloud-hybrid deployment
4. **Dual-Triggering**: Clarification of architectural patterns (scheduled + event-driven)

### Technical Debt Identified
1. **CLR Code Duplication**: 200-300 lines of duplicate code (ParseVectorJson, GGUFTensorInfo)
2. **Test Coverage**: Only 2 test files for 72 CLR implementations (target: 70%+)
3. **Documentation Gaps**: Some advanced features lack SQL integration documentation

### Critical Dependencies
1. DACPAC deployment → External assemblies → CLR functions → EF scaffolding
2. Azure Arc → Entra ID → Key Vault → Production deployment
3. Self-hosted runners → Windows (database) + Linux (apps) → CI/CD pipeline

### Consolidation Opportunities
1. Merge CLR analysis files into single comprehensive refactoring guide
2. Combine deployment documentation into unified operations guide
3. Integrate testing documentation with CI/CD pipeline docs

---

## Recommendations Summary

### Promote to docs/ (9 files):
1. `QUICKSTART.md` → `docs/getting-started/quickstart.md`
2. `AZURE-PRODUCTION-READY.md` → `docs/operations/azure-deployment.md`
3. `DACPAC-CLR-DEPLOYMENT.md` → `docs/operations/dacpac-clr-deployment.md`
4. `COMPREHENSIVE-TEST-SUITE.md` → `docs/operations/testing-guide.md`
5. `DEPENDENCY-MATRIX.md` → `docs/operations/clr-dependencies.md`
6. `OODA-DUAL-TRIGGERING-ARCHITECTURE.md` → integrate into `docs/architecture/ooda-loop.md`
7. `UNIVERSAL-FILE-SYSTEM-DESIGN.md` → `docs/architecture/universal-file-system.md`
8. `RUNNER-ARCHITECTURE.md` → `docs/operations/runner-architecture.md`

### Archive for Reference (2 files):
1. `CLR-ARCHITECTURE-ANALYSIS.md` - Historical technical debt analysis
2. `CLR-REFACTOR-COMPREHENSIVE.md` - Code expansion documentation (post-deployment)

### Next Actions
1. Extract refactoring tasks from CLR analysis to backlog
2. Deploy 49 new CLR files from CLR-REFACTOR-COMPREHENSIVE
3. Implement universal file system design
4. Increase CLR test coverage to 70%+
5. Consolidate duplicate code (ParseVectorJson, GGUFTensorInfo)

---

**Status**: ✅ Part 1 Complete (10/20 root files catalogued)
**Next**: Part 2 will cover remaining 10 root files
