# Technical Debt Catalog

**Document Type**: Technical Analysis  
**Last Updated**: November 11, 2024  
**Status**: Active - 47+ identified items

## Executive Summary

This catalog documents all identified incomplete implementations, temporary solutions, and technical debt across the Hartonomous codebase. Items are classified by priority and impact to facilitate systematic remediation planning.

**Total Items**: 47+ instances  
**Categories**: Placeholder implementations (2), Future work (12), Simplified implementations (18), Temporary solutions (15)

## Classification System

- **P0 - Critical**: Blocks production readiness, user-facing functionality broken
- **P1 - High**: Significant impact on quality, performance, or maintainability
- **P2 - Medium**: Moderate impact, technical debt accumulation
- **P3 - Low**: Minor issues, optimization opportunities

---

## P0: Critical Production Blockers

### 1. Text-to-Speech Generation Placeholder

**Location**: `src/Hartonomous.Infrastructure/Services/Generation/ContentGenerationSuite.cs` (Lines 218-242)

**Issue**: Production TTS endpoint returns 440Hz sine wave tone instead of synthesized speech.

**Current Implementation**:
```csharp
// TEMPORARY PLACEHOLDER: Generates WAV file with sine wave tone
// Requires: ONNX TTS pipeline (Tacotron2/FastSpeech2 + HiFi-GAN vocoder)
```

**Impact**:
- User-facing API endpoint unusable for speech synthesis
- Feature advertised in API documentation but non-functional

**Required Work**:
1. Implement ONNX TTS model loading from database
2. Integrate phoneme tokenizer
3. Implement mel-spectrogram generation pipeline
4. Integrate vocoder (HiFi-GAN or equivalent)
5. Add audio encoding support (WAV/MP3)

**Estimated Effort**: 40-60 hours  
**Dependencies**: ONNX model ingestion pipeline, model repository integration

---

### 2. Image Generation Placeholder

**Location**: `src/Hartonomous.Infrastructure/Services/Generation/ContentGenerationSuite.cs` (Lines 244-290)

**Issue**: Production image generation endpoint returns random noise instead of diffusion-based image synthesis.

**Current Implementation**:
```csharp
// TEMPORARY PLACEHOLDER: Generates PNG with random pixel noise
// Requires: Stable Diffusion or equivalent diffusion model
```

**Impact**:
- User-facing API endpoint unusable for image generation
- Feature documented but non-functional

**Required Work**:
1. Implement Stable Diffusion pipeline or equivalent
2. Integrate CLIP text encoder
3. Implement diffusion scheduler (DDPM/DDIM)
4. Add image postprocessing and encoding
5. Integrate with model repository

**Estimated Effort**: 60-80 hours  
**Dependencies**: ONNX Stable Diffusion model, CLIP integration

---

## P1: High-Priority Technical Debt

### 3. GPU Acceleration Disabled

**Location**: `src/Hartonomous.Core.Performance/GpuVectorAccelerator.cs`

**Issue**: ILGPU kernels disabled due to dependency conflicts. All vector operations fall back to CPU SIMD.

**Current State**:
```csharp
return false; // TODO: Enable when ILGPU kernels are implemented
```

**Impact**:
- 10-20x performance degradation on large tensor operations
- Limits scalability for production workloads

**Required Work**:
1. Resolve ILGPU dependency version conflicts
2. Implement and test GPU kernels
3. Add fallback detection and graceful degradation
4. Add telemetry for GPU utilization monitoring

**Estimated Effort**: 30-40 hours  
**Dependencies**: Dependency resolution (System.Memory vs System.Text.Json conflict)

---

### 4. Simplified Token Counting

**Location**: `src/Hartonomous.Infrastructure/Services/Generation/ContentGenerationSuite.cs` (Line 165)

**Issue**: Token counting uses character-length approximation instead of actual tokenizer.

**Current Implementation**:
```csharp
// Simplified: Assume ~1 token per 4 characters (English average)
int estimatedTokens = request.Prompt.Length / 4;
```

**Impact**:
- Inaccurate billing calculations
- Potential over/under-estimation of model context limits
- Inconsistent with industry standards (GPT tokenization)

**Required Work**:
1. Integrate SentencePiece or BPE tokenizer
2. Add language-specific tokenization support
3. Implement token caching for performance
4. Update billing calculations

**Estimated Effort**: 20-30 hours

---

### 5. EF Core Configuration Incompleteness

**Location**: `src/Hartonomous.Data/Configurations/` (40+ configuration files)

**Issue**: Many entity configurations lack proper index definitions, query filters, and relationship configurations.

**Impact**:
- Suboptimal query performance
- Missing database constraints
- Potential data integrity issues

**Required Work**:
1. Audit all 40 configuration files
2. Add missing indexes (especially on foreign keys)
3. Configure query filters for soft deletes
4. Add value conversions for enums and JSON columns
5. Verify relationship mappings

**Estimated Effort**: 50-70 hours  
**Reference**: See [CODE_REFACTORING_AUDIT.md](../../CODE_REFACTORING_AUDIT.md) Section 3.2

---

## P2: Medium-Priority Debt

### 6. Inline SQL Queries (76 instances)

**Location**: Multiple files across `src/Hartonomous.Data/Repositories/`

**Issue**: Raw SQL strings embedded in C# code instead of stored procedure calls.

**Example**:
```csharp
await context.Database.ExecuteSqlRawAsync(
    "UPDATE Atoms SET ProcessingStatus = {0} WHERE AtomId = {1}", 
    status, atomId);
```

**Impact**:
- Bypasses EF Core query translation
- Reduces testability
- Harder to optimize and maintain
- Potential SQL injection risk if not parameterized

**Required Work**:
1. Identify all 76 inline SQL instances
2. Migrate to stored procedures or EF Core queries
3. Add integration tests for migrated queries

**Estimated Effort**: 40-60 hours

---

### 7. TODO Comments (100+ instances)

**Location**: Across entire codebase

**Issue**: Production code contains TODO/FIXME/HACK comments indicating deferred work.

**Impact**:
- Unclear code ownership
- Potential forgotten bugs or incomplete features
- Unprofessional appearance in code reviews

**Required Work**:
1. Catalog all TODO comments
2. Create GitHub issues for actionable items
3. Remove or convert to proper documentation
4. Implement or explicitly mark as won't-fix

**Estimated Effort**: 30-40 hours  
**Reference**: Grep pattern: `TODO|FIXME|HACK|PLACEHOLDER|STUB|WIP|FUTURE|LATER`

---

## P3: Low-Priority Optimization Opportunities

### 8. Hardcoded Configuration Values

**Location**: Multiple service implementations

**Issue**: Configuration values (timeouts, batch sizes, thresholds) hardcoded instead of externalized.

**Impact**:
- Requires recompilation for configuration changes
- Difficult to tune for different environments
- Reduces operational flexibility

**Required Work**:
1. Identify hardcoded values
2. Move to appsettings.json or Azure App Configuration
3. Add validation for configuration values
4. Document configuration options

**Estimated Effort**: 20-30 hours

---

### 9. Missing XML Documentation (200+ methods)

**Location**: All public APIs

**Issue**: Many public methods and classes lack XML documentation comments.

**Impact**:
- Poor IntelliSense experience
- Harder for new developers to understand APIs
- API documentation gaps

**Required Work**:
1. Add XML comments to all public APIs
2. Enable XML documentation generation
3. Generate API documentation site
4. Add code analysis rule to enforce documentation

**Estimated Effort**: 40-60 hours

---

## Remediation Strategy

### Phase 1: Critical Production Blockers (P0)
**Timeline**: 2-3 weeks  
**Focus**: Items 1-2 (TTS and Image generation placeholders)

### Phase 2: High-Priority Debt (P1)
**Timeline**: 4-6 weeks  
**Focus**: Items 3-5 (GPU acceleration, token counting, EF Core configs)

### Phase 3: Medium-Priority Debt (P2)
**Timeline**: 6-8 weeks  
**Focus**: Items 6-7 (Inline SQL, TODO comments)

### Phase 4: Low-Priority Optimizations (P3)
**Timeline**: Ongoing  
**Focus**: Items 8-9 (Configuration, documentation)

## Tracking and Accountability

- **GitHub Issues**: All P0 and P1 items tracked as issues
- **Weekly Reviews**: Technical debt review in sprint planning
- **Metrics**: Track debt reduction (items resolved per sprint)
- **Prevention**: Code review checklist includes "No new TODO comments"

## References

- **Source Analysis**: [CODE_REFACTORING_AUDIT.md](../../CODE_REFACTORING_AUDIT.md)
- **Testing Plan**: [TESTING_AUDIT_AND_COVERAGE_PLAN.md](../../TESTING_AUDIT_AND_COVERAGE_PLAN.md)
- **Implementation Checklist**: [IMPLEMENTATION_CHECKLIST.md](../../IMPLEMENTATION_CHECKLIST.md)

---

**Document Maintenance**: This catalog should be updated quarterly or after each major release to reflect debt reduction progress and newly identified items.
