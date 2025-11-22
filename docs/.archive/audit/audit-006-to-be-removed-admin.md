# Documentation Audit Segment 006: .to-be-removed Administrative Documents

**Generated**: 2025-01-XX  
**Scope**: .to-be-removed/ root level administrative/planning documents  
**Total Files**: ~30 root files catalogued (subset shown)  
**Purpose**: Catalog project status reports, audit logs, verification documents, and administrative files

---

## Overview of .to-be-removed/ Directory

This directory contains **historical project management artifacts** from the development and documentation phases of Hartonomous. Files include status reports, audit logs, verification records, and planning documents that were used during active development but are no longer part of the core documentation structure.

**Directory Structure**:
- Root: ~30 administrative markdown files
- architecture/: 18 architecture design documents
- operations/: 6 operational runbooks
- setup/: 3 setup guides (Arc authentication)
- guides/: 1 README file
- rewrite-guide/: (large section, separate audit needed)
- archive/: Historical status documents
- api/: API documentation
- user-suggested/: User-contributed documents

**Overall Status**: These are **HISTORICAL ARTIFACTS** - valuable for understanding project evolution but not current reference documentation.

---

## Root Level Administrative Files

### 1. README.md
- **Type**: Historical project overview
- **Length**: 385 lines
- **Date**: Not specified (references 2019+ SQL Server)
- **Quality**: ⭐⭐⭐ Good - Marketing-focused overview

**Purpose**:
Root README from earlier project phase presenting Hartonomous as "Autonomous Geometric Reasoning System" with marketing pitch.

**Key Claims**:
- "SQL Server spatial R-Tree indexes achieving O(log N) performance"
- "1 billion atoms queried in ~18ms"
- "Replaces traditional vector databases and neural network architectures"
- Model weights as queryable GEOMETRY (T-SQL weight updates)
- Complete capabilities: Chain of Thought, Tree of Thought, Reflexion reasoning

**Content Structure**:
- What is Hartonomous: Spatial indexes ARE the ANN algorithm
- Core Innovation: O(log N) + O(K) pattern
- Model weights as queryable GEOMETRY
- Complete capabilities: Autonomous reasoning frameworks, Agent tools framework
- Documentation references: PROJECT-STATUS.md, docs/rewrite-guide/INDEX.md

**Status Analysis**:
- **OUTDATED**: Points to non-existent docs/rewrite-guide/ paths
- **MARKETING-FOCUSED**: More pitch than technical reference
- **SUPERSEDED**: Current docs/ architecture is more comprehensive

**Recommendation**: ARCHIVE - Historical artifact, superseded by current README in root

---

### 2. PROJECT-STATUS.md
- **Type**: Project milestone report
- **Length**: 324 lines
- **Date**: November 18, 2025
- **Quality**: ⭐⭐⭐⭐ Very Good - Comprehensive status snapshot

**Purpose**:
Executive summary of production-readiness status post-CLR refactor.

**Key Status Points**:
- **Phase**: Production-Ready Documentation Complete
- **Architecture**: O(log N) pattern validated, 3.6M× speedup validated
- **Documentation**: 46 files complete (~500,000 words)
- **CLR Assembly**: 49 files staged (225,000 lines)
- **Validation**: Test suite ready, deployment pending

**Documentation Suite Status** (18 architecture files):
1. ADVERSARIAL-MODELING-ARCHITECTURE.md ✅
2. ARCHIVE-HANDLER.md (design phase)
3. CATALOG-MANAGER.md (design phase)
4. COGNITIVE-KERNEL-SEEDING.md (bootstrap)
5. COMPLETE-MODEL-PARSERS.md (design phase)
6. END-TO-END-FLOWS.md (7 flows)
7. ENTROPY-GEOMETRY-ARCHITECTURE.md ✅
8. INFERENCE-AND-GENERATION.md ✅
9. MODEL-ATOMIZATION-AND-INGESTION.md ✅
10. MODEL-COMPRESSION-AND-OPTIMIZATION.md (159:1 validated)
11. MODEL-PROVIDER-LAYER.md (design phase)
12. NOVEL-CAPABILITIES-ARCHITECTURE.md ✅
13. OODA-AUTONOMOUS-LOOP-ARCHITECTURE.md ✅
14. SEMANTIC-FIRST-ARCHITECTURE.md ✅
15. SQL-SERVER-2025-INTEGRATION.md (99% validation)
16. TEMPORAL-CAUSALITY-ARCHITECTURE.md ✅
17. TRAINING-AND-FINE-TUNING.md ✅
18. UNIVERSAL-FILE-FORMAT-REGISTRY.md (design phase)

**Rewrite Guide**: 28 files (00-Architectural-Principles.md through 23+ implementation guides)

**CLR Implementation**:
- Enums: 7 files, ~12,000 lines (LayerType, ModelFormat, QuantizationType, etc.)
- MachineLearning: 17 files, ~147,000 lines (ComputationalGeometry 24,899 lines, SpaceFillingCurves 15,371 lines)
- ModelParsers: 5 files, ~48,000 lines (ONNX, TensorFlow, StableDiffusion, PyTorch, GGUF)
- Models: 10 files, ~17,000 lines (TensorInfo, ModelMetadata, SpatialCandidate)

**Value**:
- Comprehensive snapshot of November 2025 project state
- Documents all architecture files (many in .to-be-removed/architecture/)
- CLR file inventory with line counts
- References to rewrite-guide documentation (46 files)

**Issues**:
- References documentation in .to-be-removed/ (now archived)
- Snapshot in time, not living document
- Some "design phase" files may not exist or be incomplete

**Recommendation**: VALUABLE ARCHIVE - Historical project status, useful for understanding what was planned vs. implemented

---

### 3. AUDIT-REPORT.md
- **Type**: Build validation report
- **Length**: 251 lines
- **Date**: November 16, 2025 (Week 1 Complete)
- **Quality**: ⭐⭐⭐⭐ Very Good - Detailed build validation

**Purpose**:
Week 1 deliverable completion report with build validation results.

**Key Findings**:
✅ **WEEK 1 COMPLETE**: All deliverables achieved
✅ **BUILD SUCCESSFUL**: DACPAC generates with zero errors
✅ **CLR VALIDATED**: No incompatible .NET Standard dependencies
✅ **CORE INNOVATION INTACT**: All geometric AI components present
⚠️ **Security Warnings**: 16 NuGet vulnerabilities (non-blocking)

**Build Status**:
- C# Projects: BUILDS SUCCESSFULLY (all non-database projects)
- Database Project: BUILDS SUCCESSFULLY with MSBuild
- DACPAC: 325.38 KB
- CLR DLL: 351.50 KB
- Build Warnings: 163 (unresolved system object references - expected)
- Build Errors: 0

**CLR Dependency Audit** - CLEAN:
- System.Collections.Immutable - NOT FOUND ✅
- System.Reflection.Metadata - NOT FOUND ✅
- System.Memory - NOT FOUND ✅
- System.Buffers - NOT FOUND ✅
- **All CLR code uses .NET Framework 4.8.1 compatible libraries only**

**Core Innovation Files Validated**:
1. LandmarkProjection.cs - 1998D → 3D projection ✅
2. HilbertCurve.cs - 3D Hilbert curve, 21-bit precision ✅
3. AttentionGeneration.cs - Multi-head attention ✅
4. Spatial indexes - Fine-grained + coarse R-Tree ✅
5. OODA Loop - All 4 procedures (sp_Analyze, sp_Hypothesize, sp_Decide, sp_Act) ✅

**Value**:
- Documents successful build milestone
- CLR dependency validation (critical for SQL Server deployment)
- Confirms core innovation files present
- No System.Collections.Immutable issue (unlike CLR-DEPLOYMENT.md critical issue)

**Recommendation**: KEEP AS HISTORICAL REFERENCE - Documents successful Week 1 milestone, useful for understanding build process

---

### 4. DOCUMENTATION-AUDIT-2025-11-18.md
- **Type**: Documentation validation report
- **Length**: 1,996 lines (LARGE)
- **Date**: November 18, 2025
- **Quality**: ⭐⭐⭐⭐⭐ Excellent - Systematic validation against Microsoft docs

**Purpose**:
Systematic validation of 93+ markdown files against authoritative Microsoft documentation using Microsoft Docs Search/Fetch tools.

**Scope**:
- Phase 1: Core SQL Server Features + Performance Analysis
- Files Validated: 5 primary architecture files + CLR documentation
- Accuracy Rating: **99%** (optional terminology refinement)

**Key Findings**:

✅ **SQL Server 2025 Features** - VALIDATED:
- Vector type (1998 dimensions) ✅
- VECTOR_DISTANCE, sp_invoke_external_rest_endpoint ✅
- Native JSON ✅

✅ **CLR Integration** - VALIDATED:
- CLR strict security, CAS deprecation, assembly signing ✅
- All match Microsoft best practices ✅

✅ **Performance Claims** - VALIDATED:
- **3,600,000× speedup**: CONFIRMED (spatial pre-filter vs brute-force)
- **93.75% compression**: CONFIRMED (SVD rank-64 on 2048×2048)
- Triple locality preservation via Hilbert curves ✅

⚠️ **Spatial Indexing** - Terminology Clarification (LOW severity):
- Documentation uses "R-Tree" (conceptual) vs "grid tessellation" (SQL Server implementation)
- Architecture sound, O(log N) complexity accurate
- Custom triple-Hilbert projection + spatial indexes is sophisticated

✅ **Temporal Tables** - ACCURATE:
- System-versioned tables, retention policies, SYSTEM_TIME queries ✅

✅ **In-Memory OLTP** - CORRECTLY DOCUMENTED:
- Memory-optimized tables, natively compiled procedures ✅

**Validation Sources**:
- Microsoft Learn: What's new in SQL Server 2025
- Official docs for VECTOR type, VECTOR_DISTANCE, sp_invoke_external_rest_endpoint
- CLR integration best practices
- Spatial data documentation

**Value**:
- **CRITICAL VALIDATION**: Confirms technical accuracy against Microsoft official sources
- 99% accuracy rating gives confidence in documentation quality
- Only issue is R-Tree terminology (conceptual vs implementation detail)
- Systematic methodology using Microsoft's own documentation tools

**Files Validated**:
- SQL-SERVER-2025-INTEGRATION.md - ACCURATE ✅
- (File continues for 1,996 lines with detailed validation of each architecture file)

**Recommendation**: **EXTREMELY VALUABLE** - Keep as authoritative validation record, reference for technical accuracy claims

---

### 5. VERIFICATION-LOG.md
- **Type**: Test execution log
- **Length**: 191 lines
- **Date**: January 17, 2025
- **Quality**: ⭐⭐⭐⭐ Very Good - Empirical test results

**Purpose**:
Track validation of core architectural claims vs actual implementation through automated testing.

**Latest Milestone**: DATABASE BUILD SUCCESS ✅
- Hartonomous.Database.dacpac generated (335KB)
- All CLR dependencies resolved
- 70+ CLR files compiled without errors
- Core geometric engine included
- OODA loop procedures included
- Ready for deployment

**Test Execution Summary**:

**Run 1: January 17, 2025** (Initial CLR Tests)
- Command: `dotnet test tests/Hartonomous.Database.CLR.Tests`
- Results: **18 passed, 5 failed**

**✅ PASSED Tests** (18 - Validated Claims):
1. Projection is deterministic (same input → same output) ✅
2. Static initialization is consistent ✅
3. Determinism holds at scale (100, 1K, 10K vectors) ✅
4. Numerical stability - no NaN/Inf ✅
5. Relative distance ordering preserved ✅
6. Null safety validation ✅
7. Input validation (wrong dimensions rejected) ✅
8. Hilbert curve is deterministic ✅
9. Origin/max coordinate handling ✅
10. **Locality preservation validated** ✅
11. No collisions in test grid ✅
12. Collision rate <0.01% for 10K samples ✅
13. Performance: <100ms for 1000 points ✅

**❌ FAILED Tests** (5):
- Hilbert3D inverse function doesn't round-trip correctly
- Root cause: Bug in InverseHilbert3D implementation
- **Note**: Inverse function used for debugging/visualization only, not production

**Core Innovation Claims - Validation Status**:

1. **Deterministic 3D Projection** - ✅ PROVEN
   - Evidence: 10,000 vector batch, 100% identical results across runs
   - Fixed seed (42) confirmed
   - SIMD acceleration present and working

2. **Hilbert Curve Locality Preservation** - ✅ PROVEN
   - Evidence: Nearby points have nearby Hilbert values
   - Collision rate <0.01%
   - Test passed empirically

3. **Numerical Stability** - ✅ PROVEN
   - No NaN/Infinity in any test
   - Gram-Schmidt orthonormalization stable

4. **Relative Distance Preservation** - ✅ PROVEN
   - Local topology preserved in projection
   - Johnson-Lindenstrauss lemma validated empirically

5. **Performance Claims** - ⏳ PARTIALLY VALIDATED
   - Hilbert: <100ms for 1000 points ✅
   - Projection: Not yet benchmarked ⏳
   - Full two-stage query: Not yet tested ⏳
   - Status: PENDING (Week 3 benchmarks required)

**Value**:
- **EMPIRICAL VALIDATION**: Not just documentation, actual test results
- 18/23 tests passing proves core innovation works
- Identifies specific bug (inverse Hilbert) but notes it's non-critical
- Clear evidence for determinism, locality preservation, numerical stability

**Recommendation**: **HIGHLY VALUABLE** - Empirical proof of core claims, keep as validation record

---

### 6. DEPLOYMENT-READY-REPORT.md
- **Type**: Implementation progress report
- **Length**: 326 lines
- **Date**: January 17, 2025
- **Quality**: ⭐⭐⭐⭐ Very Good - Deployment checklist

**Purpose**:
Report on major milestone: Database ready for deployment with validated core innovation.

**Executive Summary**:
Successfully **BUILT THE DATABASE** with all core innovation components intact.

**What's Actually Done**:
1. **Core CLR Functions Validated** (18/23 tests passing)
   - Deterministic 3D projection working ✅
   - Hilbert locality preservation proven ✅
   - Numerical stability verified ✅
   - Low collision rate confirmed ✅

2. **Database Project Built Successfully**
   - DACPAC generated: 335KB ✅
   - 70+ CLR files compiled ✅
   - All dependencies resolved ✅
   - Ready for SQL Server deployment ✅

3. **Build Infrastructure Fixed**
   - Central package management working ✅
   - All C# projects building ✅
   - Test infrastructure in place ✅

**Technical Achievements**:

**Validated Core Innovation** (with empirical evidence):
| Component | Status | Evidence |
|-----------|--------|----------|
| Deterministic 3D Projection | ✅ PROVEN | 10,000 vectors, 100% reproducible |
| Hilbert Locality Preservation | ✅ PROVEN | Empirical test passed |
| Numerical Stability | ✅ PROVEN | No NaN/Inf |
| Low Collision Rate | ✅ PROVEN | <0.01% for 10K samples |
| SIMD Acceleration | ✅ VERIFIED | Code inspection |
| Gram-Schmidt Orthonormal | ⏳ NEEDS TEST | Add dot product test |

**Database Components Included**:
- CLR Assembly (Hartonomous.Clr.dll):
  - LandmarkProjection.cs, HilbertCurve.cs, AttentionGeneration.cs ✅
  - ReasoningFrameworkAggregates.cs (CoT/ToT/Reflexion) ✅
  - VectorMath.cs (SIMD operations) ✅
  - All aggregate functions (9 files) ✅
  - Model inference, audio/image/video processing ✅

- SQL Components (in DACPAC):
  - All table schemas, views, functions ✅
  - All stored procedures (except 3 with syntax errors - minor) ✅
  - Service Broker configuration ✅
  - Spatial index definitions ✅

**Temporarily Excluded** (minor syntax errors):
- sp_FindNearestAtoms.sql (syntax error line 13) ⚠️
- sp_IngestAtoms.sql (syntax error line 9) ⚠️
- sp_RunInference.sql (parser errors) ⚠️
- **Note**: Not core to geometric engine, fixable post-deployment

**Deployment Readiness**:
✅ Prerequisites Met:
- DACPAC built successfully
- CLR assembly signed
- Dependencies resolved
- Test infrastructure validated
- Deployment script created (scripts/Deploy-Database.ps1)

**Usage**:
```powershell
# Deploy to local SQL Server
.\scripts\Deploy-Database.ps1 -Server localhost -Database Hartonomous -CreateDatabase

# Deploy to remote
.\scripts\Deploy-Database.ps1 -Server myserver.database.windows.net -Database Hartonomous
```

**Value**:
- Documents "ready for deployment" milestone
- Comprehensive component checklist
- Identifies minor issues (3 stored procedures) but notes non-critical
- Practical deployment instructions

**Recommendation**: KEEP AS MILESTONE REFERENCE - Documents deployment-ready state, useful for deployment process

---

## Additional Root Files (Brief Summary)

### Administrative/Planning Documents

**ARCHITECTURAL-SOLUTION.md**: Likely architectural design decisions  
**AZURE-ARC-SERVICE-PRINCIPAL-SETUP.md**: Azure Arc authentication setup  
**AZURE-PRODUCTION-READY.md**: Azure production deployment checklist  
**CLR-ARCHITECTURE-ANALYSIS.md**: CLR architecture analysis  
**CLR-REFACTOR-COMPREHENSIVE.md**: CLR refactoring plan (12,000 words per PROJECT-STATUS.md)  
**CLR-REFACTORING-ANALYSIS.md**: CLR refactoring analysis  
**COMPREHENSIVE-TEST-SUITE.md**: Test suite documentation  
**CONTRIBUTING.md**: Contribution guidelines  
**CRITICAL-GAPS-ANALYSIS.md**: Gap analysis  
**DACPAC-CLR-DEPLOYMENT.md**: DACPAC deployment guide  
**DEPENDENCY-MATRIX.md**: Dependency tracking  
**DOCUMENTATION-GENERATION-COMPLETE.md**: Documentation completion report  
**DOCUMENTATION-GENERATION-SUMMARY.md**: Documentation summary  
**GITHUB-ACTIONS-MIGRATION.md**: CI/CD migration guide  
**OODA-DUAL-TRIGGERING-ARCHITECTURE.md**: OODA loop trigger design  
**QUICKSTART.md**: Quick start guide  
**REFERENTIAL-INTEGRITY-SOLUTION.md**: Database integrity solution  
**RUNNER-ARCHITECTURE.md**: Runner architecture design  
**SETUP-PREREQUISITES.md**: Setup prerequisites  
**UNIVERSAL-FILE-SYSTEM-DESIGN.md**: File system design  

*Note: These files not individually catalogued in this segment due to volume. Will be summarized in subsequent segments if needed.*

---

## Cross-File Analysis

### Temporal Sequence

The files reveal clear project timeline:

1. **November 16, 2025**: AUDIT-REPORT.md - Week 1 build complete
2. **November 18, 2025**: DOCUMENTATION-AUDIT-2025-11-18.md - Documentation validated, PROJECT-STATUS.md - Production ready
3. **January 17, 2025**: VERIFICATION-LOG.md, DEPLOYMENT-READY-REPORT.md - Deployment milestone

**Issues**: January 2025 dates after November 2025 dates suggest timestamp inconsistency or year error.

### Common Themes

**Success Metrics**:
- DACPAC builds successfully (325-335KB across reports)
- 18/23 CLR tests passing
- 3.6M× speedup validated
- 99% documentation accuracy
- O(log N) + O(K) pattern proven

**Critical Components Validated**:
- LandmarkProjection.cs (deterministic 3D projection)
- HilbertCurve.cs (locality preservation)
- AttentionGeneration.cs (O(log N) query)
- OODA Loop (4 procedures)
- Spatial indexes (R-Tree)

**Recurring Issues**:
- 3 stored procedures with syntax errors (sp_FindNearestAtoms, sp_IngestAtoms, sp_RunInference)
- InverseHilbert3D function bug (non-critical)
- 16 NuGet vulnerabilities (non-blocking)
- R-Tree terminology vs implementation (minor)

### Documentation Quality Patterns

**High Quality** (⭐⭐⭐⭐⭐):
- DOCUMENTATION-AUDIT-2025-11-18.md: Systematic validation against Microsoft docs
- VERIFICATION-LOG.md: Empirical test results

**Very Good** (⭐⭐⭐⭐):
- PROJECT-STATUS.md: Comprehensive status snapshot
- AUDIT-REPORT.md: Detailed build validation
- DEPLOYMENT-READY-REPORT.md: Deployment checklist

**Good** (⭐⭐⭐):
- README.md: Marketing-focused overview (outdated paths)

### Value Assessment

**CRITICAL HISTORICAL VALUE**:
- DOCUMENTATION-AUDIT-2025-11-18.md - Authoritative validation
- VERIFICATION-LOG.md - Empirical proof
- PROJECT-STATUS.md - Comprehensive inventory

**VALUABLE REFERENCE**:
- AUDIT-REPORT.md - Build milestone
- DEPLOYMENT-READY-REPORT.md - Deployment process

**SUPERSEDED**:
- README.md - Outdated paths, replaced by current root README

---

## Relationships to Current Documentation

### References to .to-be-removed/architecture/

PROJECT-STATUS.md lists 18 architecture files in .to-be-removed/architecture/:
- SEMANTIC-FIRST-ARCHITECTURE.md
- OODA-AUTONOMOUS-LOOP-ARCHITECTURE.md
- MODEL-ATOMIZATION-AND-INGESTION.md
- ENTROPY-GEOMETRY-ARCHITECTURE.md
- etc.

**Analysis**: These architecture files are separate from current docs/architecture/ (00-principles.md, 01-semantic-first-architecture.md, etc.). Need to audit .to-be-removed/architecture/ to determine overlap/conflicts.

### References to rewrite-guide/

Multiple files reference docs/rewrite-guide/ (46 files, ~500,000 words per PROJECT-STATUS.md). This appears to be a LARGE documentation collection in .to-be-removed/rewrite-guide/ that needs separate audit.

### CLR Implementation References

All reports reference CLR files:
- 49 files, 225,000 lines (per PROJECT-STATUS.md)
- Enums: 7 files, ~12,000 lines
- MachineLearning: 17 files, ~147,000 lines
- ModelParsers: 5 files, ~48,000 lines
- Models: 10 files, ~17,000 lines

**Location**: src/Hartonomous.Database/CLR/ (per AUDIT-REPORT.md)

**Status**: These are IMPLEMENTATION FILES, not documentation. Outside scope of documentation audit but referenced extensively.

---

## Recommendations

### Immediate Actions

**1. ARCHIVE - Keep for Historical Reference**:
- DOCUMENTATION-AUDIT-2025-11-18.md - Authoritative validation record
- VERIFICATION-LOG.md - Empirical test results
- PROJECT-STATUS.md - Project inventory snapshot
- AUDIT-REPORT.md - Build milestone
- DEPLOYMENT-READY-REPORT.md - Deployment milestone

**2. SUPERSEDED - Can Be Removed**:
- README.md (root .to-be-removed/) - Outdated paths, replaced by current root README

**3. PENDING REVIEW**:
- All other root files (~24 files) - Need brief review to determine value
- .to-be-removed/architecture/ (18 files) - Need full audit
- .to-be-removed/rewrite-guide/ (~46 files) - Need full audit (LARGE)
- .to-be-removed/operations/ (6 files) - Need audit
- .to-be-removed/setup/ (3 files) - Need audit

### Documentation Gaps Identified

**Missing from Current Docs**:
1. **Empirical Validation Results**: VERIFICATION-LOG.md has test results not documented elsewhere
2. **Microsoft Docs Validation**: DOCUMENTATION-AUDIT-2025-11-18.md has systematic validation not in current docs
3. **Build Process**: AUDIT-REPORT.md documents successful build process
4. **Deployment Checklist**: DEPLOYMENT-READY-REPORT.md has deployment steps

**Potential Integration**:
- Add "Validation" section to docs/ with key findings from VERIFICATION-LOG.md
- Reference Microsoft validation in architecture docs for credibility
- Add deployment checklist to docs/operations/

---

## Summary Statistics

**Files Catalogued**: 6 detailed + ~24 brief mentions  
**Total Lines Reviewed**: ~3,462 lines (detailed files only)  
**Average Quality**: 4.0 / 5.0 stars (detailed files)  
**Historical Value**: HIGH (5 files critical historical reference)  
**Current Relevance**: LOW (most superseded or timestamped)  

**Key Findings**:
- All detailed files are project milestone/status reports
- High quality documentation with empirical validation
- 99% accuracy against Microsoft official docs
- 18/23 CLR tests passing (empirical proof)
- DACPAC builds successfully (deployment ready)
- References to large .to-be-removed/architecture/ and rewrite-guide/ collections

**Critical Issues Documented**:
- 3 stored procedures with syntax errors (non-critical)
- InverseHilbert3D bug (debugging only, non-critical)
- 16 NuGet vulnerabilities (non-blocking)
- R-Tree terminology clarification needed

**Next Steps**:
1. Continue audit of .to-be-removed/architecture/ (18 files)
2. Continue audit of .to-be-removed/rewrite-guide/ (~46 files - LARGE)
3. Quick review of .to-be-removed/operations/ (6 files)
4. Quick review of .to-be-removed/setup/ (3 files)
5. Final summary with consolidation recommendations
