# Master Implementation Plan

**Date**: 2025-11-08
**Status**: Research Complete, Ready for Implementation

## Overview

This folder contains the comprehensive implementation plan derived from:
1. Research findings (67 documented findings in SQL_CLR_RESEARCH_FINDINGS.md)
2. Original TODO_BACKUP.md (10 phases of consolidation work)
3. RECOVERY_STATUS.md (sabotage recovery and lessons learned)
4. Conversation history (all work efforts discussed)

## File Structure

```
docs/audit/
├── 00-MASTER-PLAN.md (this file)
├── 01-CRITICAL-FIXES.md (blocks all other work)
├── 02-SQL-CLR-FIXES.md (SqlClr namespace and functional issues)
├── 03-TEMPORAL-TABLES.md (weight tracking infrastructure)
├── 04-ORPHANED-FILES.md (178+ files integration)
├── 05-CONSOLIDATION.md (generic patterns, DRY refactoring)
├── 06-ARCHITECTURE.md (project restructuring)
└── 07-PERFORMANCE.md (optimizations and Azure integration)
```

## Execution Order

Tasks MUST be completed in this order due to dependencies:

1. **01-CRITICAL-FIXES** (BLOCKS EVERYTHING)
   - Fix SqlClr NuGet restore
   - Fix sp_UpdateModelWeightsFromFeedback (model weights never update!)
   - Fix Sql.Bridge namespace references

2. **02-SQL-CLR-FIXES** (Research-driven fixes)
   - Remove LayerNorm TODOs with explanation
   - Document SQL CLR capabilities/limitations
   - Verify all SqlClr builds

3. **03-TEMPORAL-TABLES** (Core learning mechanism)
   - Convert TensorAtomCoefficients to temporal table
   - Enable automatic weight history tracking
   - Add weight evolution queries

4. **04-ORPHANED-FILES** (Build integration)
   - Add 178+ files to .csproj files
   - Verify DI registration
   - Run full build + test suite

5. **05-CONSOLIDATION** (DRY/SOLID refactoring)
   - Generic repository pattern
   - Generic event handlers
   - Split multi-class files

6. **06-ARCHITECTURE** (Major restructuring)
   - Consolidate console apps into Worker
   - Merge Data into Infrastructure
   - Multi-target Hartonomous.Core

7. **07-PERFORMANCE** (Optional enhancements)
   - SIMD optimizations (where possible)
   - Azure integration
   - Caching improvements

## Success Criteria

Each phase complete when:
- ✅ All tasks in phase file marked complete
- ✅ Full solution builds without errors
- ✅ All tests pass
- ✅ Changes committed to git

## Key Learnings Applied

From sabotage incident (commit cbb980c):
- ❌ NEVER batch-delete files without verification
- ❌ NEVER assume files are duplicates
- ❌ NEVER skip build verification
- ✅ ALWAYS add files to .csproj immediately after creation
- ✅ ALWAYS build after each change
- ✅ ALWAYS commit incrementally
- ✅ ALWAYS test before deleting old code

## Current Blockers

**Phase 1 blockers**:
1. SqlClr NuGet restore requires Visual Studio or full .NET Framework SDK
2. sp_UpdateModelWeightsFromFeedback cursor only PRINTs, never UPDATEs

**All other work blocked until Phase 1 complete.**

## Research Foundation

All decisions backed by official MS docs:
- SQL CLR: Only .NET Framework 4.8.1, pure managed code
- Transformers: Too compute-intensive for SQL CLR
- Temporal tables: Perfect for weight history
- Multi-targeting: Core can target net481+net8.0
- SDK-style: Auto-includes files, no manual entries

See `SQL_CLR_RESEARCH_FINDINGS.md` and `RESEARCH_SUMMARY.md` for details.

## Next Action

Read `01-CRITICAL-FIXES.md` and begin Phase 1.
