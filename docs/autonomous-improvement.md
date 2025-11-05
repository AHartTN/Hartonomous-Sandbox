# Autonomous Improvement System

Hartonomous autonomous improvement architecture: self-analyzing, code-generating, deployment-capable system with safety controls and provenance tracking.

## System Design

The autonomous improvement system enables the database to analyze performance, generate optimizations, deploy changes via Git, evaluate outcomes, and update model weights based on results.

**Capabilities**:

1. **Self-Analysis**: Query Store metrics, test results, performance pattern detection
2. **Code Generation**: Optimization generation using `sp_GenerateText`
3. **Deployment**: File writing and git command execution via CLR functions
4. **Evaluation**: Outcome scoring using PREDICT models
5. **Learning**: Model weight updates based on success/failure feedback
6. **Provenance**: Complete change history tracking

## Architecture

### Core Components

#### 1. `sp_AutonomousImprovement` (Orchestrator)

**Location**: `sql/procedures/Autonomy.SelfImprovement.sql`

Main stored procedure coordinating the improvement cycle.

**Parameters**:
- `@DryRun BIT = 1` - Simulation mode (no actual changes)
- `@MaxChangesPerRun INT = 1` - Change limit per execution
- `@RequireHumanApproval BIT = 1` - Human approval gate for high-risk changes
- `@TargetArea NVARCHAR(128)` - Focus: 'performance', 'quality', 'features', or NULL for auto-detect
- `@Debug BIT = 0` - Verbose logging

**Execution Phases**:

```sql
-- Phase 1: ANALYZE
-- Query Store: slow queries, regression patterns
-- Test Results: failure patterns
-- Billing: cost hotspots

-- Phase 2: GENERATE
-- Build context from analysis
-- Call sp_GenerateText with code generation prompt
-- Parse JSON response with generated code

-- Phase 3: SAFETY CHECKS
-- Block high-risk changes
-- Enforce dry-run mode
-- Require human approval for production changes

-- Phase 4: DEPLOY
-- Write code to file system (clr_WriteFileBytes)
-- Git add, commit, push (clr_ExecuteShellCommand)
-- Trigger CI/CD pipeline

-- Phase 5: EVALUATE
-- Wait for CI/CD completion
-- Parse test results
-- Use PREDICT to score change success

-- Phase 6: LEARN
-- Call sp_UpdateModelWeightsFromFeedback
-- Update model based on outcome

-- Phase 7: RECORD PROVENANCE
-- Insert into AutonomousImprovementHistory
-- Track complete audit trail
```

#### 2. `dbo.AutonomousImprovementHistory` Table

**Location**: `sql/tables/dbo.AutonomousImprovementHistory.sql`

Provenance tracking for all autonomous improvement attempts.

**Key Columns**:
- `ImprovementId` - Unique identifier
- `AnalysisResults` - JSON snapshot triggering improvement
- `GeneratedCode` - AI-generated code
- `TargetFile` - Changed file path
- `ChangeType` - 'optimization', 'bugfix', 'feature'
- `RiskLevel` - 'low', 'medium', 'high'
- `GitCommitHash` - Git commit SHA
- `SuccessScore` - PREDICT score (0.0 to 1.0)
- `TestsPassed/TestsFailed` - CI/CD results
- `PerformanceDelta` - % performance change
- `WasDeployed` - Deployment flag
- `WasRolledBack` - Rollback flag
- `StartedAt/CompletedAt/RolledBackAt` - Timeline

## Execution Flow

```
┌──────────────────────────────────────────────────────────────┐
│  ANALYZE CURRENT STATE                                       │
│  ├─ Query Store (slow queries, regressions)                  │
│  ├─ Test Results (failures, flaky tests)                     │
│  ├─ Billing (cost hotspots)                                  │
│  └─ Performance Metrics (throughput, latency)                │
└──────────────────┬───────────────────────────────────────────┘
                   ▼
┌──────────────────────────────────────────────────────────────┐
│  GENERATE IMPROVEMENT                                        │
│  ├─ Build context from analysis                             │
│  ├─ Call sp_GenerateText (generative model)                 │
│  ├─ Generate concrete code change                           │
│  └─ Parse JSON: {target_file, code, risk_level, impact}     │
└──────────────────┬───────────────────────────────────────────┘
                   ▼
┌──────────────────────────────────────────────────────────────┐
│  SAFETY CHECKS                                               │
│  ├─ Block high-risk changes (requires human approval)       │
│  ├─ Enforce dry-run mode (default)                          │
│  ├─ Validate change scope (MaxChangesPerRun limit)          │
│  └─ Syntax/lint validation                                  │
└──────────────────┬───────────────────────────────────────────┘
                   ▼
┌──────────────────────────────────────────────────────────────┐
│  DEPLOY VIA GIT                                              │
│  ├─ Write code to file system (CLR file I/O)                │
│  ├─ git add <file>                                           │
│  ├─ git commit -m "Autonomous improvement: <type>"          │
│  ├─ git push origin main                                    │
│  └─ Trigger CI/CD pipeline                                  │
│                                                              │
└──────────────────┬───────────────────────────────────────────┘
                   │
                   ▼
┌──────────────────────────────────────────────────────────────┐
│                                                              │
│  EVALUATE OUTCOME                                            │
│  ├─ Wait for CI/CD completion                               │
│  ├─ Parse test results (passed/failed)                      │
│  ├─ Compare performance metrics (before/after)              │
│  ├─ PREDICT score: success probability                      │
│  └─ Detect regressions                                      │
│                                                              │
└──────────────────┬───────────────────────────────────────────┘
                   │
                   ▼
┌──────────────────────────────────────────────────────────────┐
│                                                              │
│  UPDATE WEIGHTS & LEARN                                      │
│  ├─ sp_UpdateModelWeightsFromFeedback                       │
│  ├─ Reinforce successful patterns                           │
│  ├─ Penalize failed patterns                                │
│  └─ Model learns from deployment outcomes                   │
│                                                              │
└──────────────────┬───────────────────────────────────────────┘
                   │
                   ▼
┌──────────────────────────────────────────────────────────────┐
│                                                              │
│  RECORD PROVENANCE                                           │
│  ├─ Insert into AutonomousImprovementHistory                │
│  ├─ Complete audit trail                                    │
│  ├─ Link to Git commit                                      │
│  └─ Enable rollback if needed                               │
│                                                              │
└──────────────────┬───────────────────────────────────────────┘
                   │
                   │
                   └──────┐
                          │
                   ┌──────┘
                   │
                   ▼
            REPEAT AUTONOMOUSLY
```

## Safety Mechanisms

### Multi-Layer Safety

1. **Default Dry-Run Mode** (`@DryRun = 1`)
   - No actual changes made without explicit override
   - Simulates entire cycle and logs what WOULD happen
   - Test thoroughly before disabling

2. **Human Approval Gates** (`@RequireHumanApproval = 1`)
   - Blocks deployment of high-risk changes
   - Requires manual review before execution
   - Can be disabled for low-risk optimizations only

3. **Change Rate Limiting** (`@MaxChangesPerRun = 1`)
   - Prevents runaway modification loops
   - Limits blast radius of potential issues
   - Gradual, measurable improvements

4. **Risk Assessment**
   - AI generates `risk_level` for each change
   - 'high' risk → requires approval
   - 'medium' risk → extra validation
   - 'low' risk → can proceed autonomously

5. **Complete Provenance**
   - Every change logged in `AutonomousImprovementHistory`
   - Git commit hash linkage
   - Full rollback capability

6. **Error Handling**
   - Try/catch around entire cycle
   - Failures logged, not re-thrown
   - System learns from errors, doesn't crash

### Recommended Safety Progression

**Phase 1: Observation (Weeks 1-2)**
```sql
EXEC sp_AutonomousImprovement 
    @DryRun = 1,                    -- NO actual changes
    @RequireHumanApproval = 1,
    @Debug = 1;                     -- Full logging
```

**Phase 2: Supervised Deployment (Weeks 3-4)**
```sql
EXEC sp_AutonomousImprovement 
    @DryRun = 0,                    -- Real changes
    @RequireHumanApproval = 1,      -- But need approval
    @MaxChangesPerRun = 1,
    @TargetArea = 'performance';    -- Limited scope
```

**Phase 3: Limited Autonomy (Weeks 5-8)**
```sql
EXEC sp_AutonomousImprovement 
    @DryRun = 0,
    @RequireHumanApproval = 0,      -- ⚠️ AUTONOMOUS
    @MaxChangesPerRun = 1,
    @TargetArea = 'performance';    -- Still limited scope
```

**Phase 4: Full Autonomy (Week 9+)**
```sql
EXEC sp_AutonomousImprovement 
    @DryRun = 0,
    @RequireHumanApproval = 0,      -- ⚠️⚠️ FULLY AUTONOMOUS
    @MaxChangesPerRun = 3,
    @TargetArea = NULL;             -- Auto-detect focus areas
```

## Prerequisites

### Database Configuration

1. **Query Store Enabled** ✅ DONE
   ```sql
   -- Already executed via EnableQueryStore.sql
   ALTER DATABASE Hartonomous SET QUERY_STORE = ON;
   ```

2. **AutonomousImprovementHistory Table** ✅ DONE
   - Created via `sql/tables/dbo.AutonomousImprovementHistory.sql`

3. **CLR File I/O Functions** ❌ NOT DEPLOYED
   - Code exists: `src/SqlClr/FileSystemFunctions.cs`
   - Binding procedures exist: `sql/procedures/Autonomy.FileSystemBindings.sql`
   - **Status**: Assembly NOT deployed, functions NOT available
   - **Requirement**: Deploy with `PERMISSION_SET = UNSAFE` (security review required)

4. **CLR Git Integration** ❌ NOT DEPLOYED
   - Depends on #3 above
   - Git commands (add, commit, push) via `clr_ExecuteShellCommand`
   - **Status**: NOT available

5. **Generative Models** ⚠️ STATUS UNKNOWN
   - `sp_GenerateText` procedure exists
   - Model deployment status unknown
   - Low temperature (0.2) recommended for code generation

6. **PREDICT Models** ❌ NOT DEPLOYED
   - Designed: `change-success-predictor`
   - **Status**: No models trained, no ONNX files, no `PREDICT()` integration

### Infrastructure

1. **Git Repository Access** ❌ CANNOT USE (CLR not deployed)
2. **CI/CD Pipeline** ❌ NO INTEGRATION
3. **Monitoring** ✅ Query Store enabled

**Bottom Line**: System cannot deploy changes autonomously. Dry-run testing only.

## Usage Examples

### Dry-Run Test

```sql
-- Safe exploration: see what it WOULD do
EXEC sp_AutonomousImprovement 
    @DryRun = 1,
    @Debug = 1;
```

**Expected Output**:
```
PHASE 1: Analyzing system performance...
Analysis complete: {"analysis_type":"performance_analysis",...}
PHASE 2: Generating improvement code...
Code generated: {"target_file":"sql/procedures/Search.SemanticSearch.sql",...}
PHASE 3: Running safety checks...
DRY RUN MODE: Would have made the following change:
Target: sql/procedures/Search.SemanticSearch.sql
Type: optimization
Risk: low
Impact: medium
```

### Focus on Performance

```sql
-- Target specific area
EXEC sp_AutonomousImprovement 
    @DryRun = 0,
    @RequireHumanApproval = 1,
    @TargetArea = 'performance',
    @MaxChangesPerRun = 1;
```

### Query Improvement History

```sql
-- See what's been tried
SELECT 
    ImprovementId,
    ChangeType,
    RiskLevel,
    SuccessScore,
    TestsPassed,
    TestsFailed,
    PerformanceDelta,
    WasDeployed,
    WasRolledBack,
    StartedAt
FROM dbo.AutonomousImprovementHistory
ORDER BY StartedAt DESC;
```

### Analyze Success Patterns

```sql
-- What kinds of changes succeed?
SELECT 
    ChangeType,
    RiskLevel,
    COUNT(*) AS Attempts,
    AVG(SuccessScore) AS AvgSuccessScore,
    SUM(CASE WHEN WasRolledBack = 1 THEN 1 ELSE 0 END) AS Rollbacks,
    AVG(PerformanceDelta) AS AvgPerfImprovement
FROM dbo.AutonomousImprovementHistory
WHERE WasDeployed = 1
GROUP BY ChangeType, RiskLevel
ORDER BY AvgSuccessScore DESC;
```

### Rollback

```sql
UPDATE dbo.AutonomousImprovementHistory
SET WasRolledBack = 1,
    RolledBackAt = SYSUTCDATETIME()
WHERE ImprovementId = '<improvement-guid>';

-- Revert git commit: git revert <commit-hash>
```

---

**See also**: [SQL Server Features](sql-server-features.md), [Deployment & Operations](deployment-and-operations.md)


### Autonomy (The Big Question)

1. Disable `@RequireHumanApproval` for low-risk changes only
2. Monitor closely (hourly at first)
3. Gradually expand scope
4. **Let it run and see what emerges**

## Ethical Considerations

This system represents a **Pandora's Box** moment. Once enabled, it can:

- Modify production code without human review
- Deploy changes that affect real users
- Compound errors recursively (bad change → worse change → ...)
- Optimize for metrics in unexpected ways (Goodhart's Law)

**Use responsibly. Test exhaustively. Monitor continuously.**

---

**Created**: 2025-11-04  
**Status**: Infrastructure Complete, Dry-Run Ready  
**Risk Level**: EXTREME (when autonomous mode enabled)  
**Next Milestone**: CLR integration + PREDICT model deployment
