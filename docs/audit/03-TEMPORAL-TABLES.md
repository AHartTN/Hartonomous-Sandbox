# Phase 3: Temporal Tables for Weight Tracking

**Priority**: HIGH
**Estimated Time**: 2-3 hours
**Dependencies**: Phase 1 complete (sp_UpdateModelWeightsFromFeedback fixed)

## Overview

Convert TensorAtomCoefficients to temporal table for automatic weight history tracking.

---

## Task 3.1: Convert TensorAtomCoefficients to Temporal

**Status**: ❌ NOT STARTED
**Research**: FINDING 27-33 in SQL_CLR_RESEARCH_FINDINGS.md

### Problem
No history of model weight changes. Cannot analyze:
- Learning trajectory over time
- Weight stability
- Overfitting detection
- Rollback to previous weights

### Solution: System-Versioned Temporal Table

**FINDING 27**: SQL Server 2016+ automatic history tracking
**FINDING 28**: No trigger/application code needed
**FINDING 29**: Query point-in-time with `FOR SYSTEM_TIME AS OF`

### Implementation Script

Create: `sql/migrations/001_TemporalTableConversion.sql`

```sql
-- Convert TensorAtomCoefficients to temporal table
USE HartonomousDB;
GO

-- Step 1: Add period columns (hidden from SELECT *)
ALTER TABLE dbo.TensorAtomCoefficients
ADD 
    ValidFrom DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL
        CONSTRAINT DF_TensorAtomCoefficients_ValidFrom DEFAULT SYSUTCDATETIME(),
    ValidTo DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL
        CONSTRAINT DF_TensorAtomCoefficients_ValidTo DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
    PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo);
GO

-- Step 2: Create history table
CREATE TABLE dbo.TensorAtomCoefficients_History
(
    TensorAtomCoefficientID BIGINT NOT NULL,
    TensorAtomID BIGINT NOT NULL,
    LayerID INT NOT NULL,
    EntityID BIGINT NULL,
    CoefficientValue FLOAT NOT NULL,
    CreatedDate DATETIME2 NOT NULL,
    LastModified DATETIME2 NOT NULL,
    ValidFrom DATETIME2 NOT NULL,
    ValidTo DATETIME2 NOT NULL,
    INDEX IX_TensorAtomCoefficients_History_Period NONCLUSTERED (ValidTo, ValidFrom)
);
GO

-- Step 3: Enable system versioning
ALTER TABLE dbo.TensorAtomCoefficients
SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.TensorAtomCoefficients_History));
GO

PRINT 'TensorAtomCoefficients converted to temporal table';
PRINT 'History will be automatically maintained on every UPDATE/DELETE';
```

### Verification Query

```sql
-- Verify temporal table enabled
SELECT 
    t.name AS TableName,
    t.temporal_type_desc,
    ht.name AS HistoryTable
FROM sys.tables t
LEFT JOIN sys.tables ht ON t.history_table_id = ht.object_id
WHERE t.name = 'TensorAtomCoefficients';

-- Should return:
-- TableName: TensorAtomCoefficients
-- temporal_type_desc: SYSTEM_VERSIONED_TEMPORAL_TABLE
-- HistoryTable: TensorAtomCoefficients_History
```

---

## Task 3.2: Create Weight Analysis Queries

**Status**: ❌ NOT STARTED
**Research**: FINDING 35-37 in SQL_CLR_RESEARCH_FINDINGS.md

### Problem
Need queries to analyze weight evolution.

### Solution: Historical Query Views

Create: `sql/views/WeightHistory.sql`

```sql
-- View 1: Current weights
CREATE OR ALTER VIEW dbo.vw_CurrentWeights
AS
SELECT 
    TensorAtomCoefficientID,
    TensorAtomID,
    LayerID,
    CoefficientValue,
    LastModified
FROM dbo.TensorAtomCoefficients
FOR SYSTEM_TIME AS OF SYSUTCDATETIME();
GO

-- View 2: Weight changes in last 24 hours
CREATE OR ALTER VIEW dbo.vw_RecentWeightChanges
AS
SELECT 
    curr.TensorAtomCoefficientID,
    curr.TensorAtomID,
    curr.LayerID,
    prev.CoefficientValue AS PreviousValue,
    curr.CoefficientValue AS CurrentValue,
    curr.CoefficientValue - prev.CoefficientValue AS Delta,
    curr.LastModified
FROM dbo.TensorAtomCoefficients curr
FOR SYSTEM_TIME AS OF DATEADD(HOUR, -24, SYSUTCDATETIME()) prev
WHERE curr.TensorAtomCoefficientID = prev.TensorAtomCoefficientID
  AND ABS(curr.CoefficientValue - prev.CoefficientValue) > 0.0001;
GO

-- View 3: Weight stability (standard deviation over time)
CREATE OR ALTER VIEW dbo.vw_WeightStability
AS
SELECT 
    TensorAtomID,
    LayerID,
    COUNT(*) AS ChangeCount,
    AVG(CoefficientValue) AS AvgValue,
    STDEV(CoefficientValue) AS StdDevValue,
    MIN(CoefficientValue) AS MinValue,
    MAX(CoefficientValue) AS MaxValue
FROM dbo.TensorAtomCoefficients
FOR SYSTEM_TIME ALL
GROUP BY TensorAtomID, LayerID
HAVING COUNT(*) > 1;
GO
```

### Usage Examples

```sql
-- Query: Show current weights
SELECT * FROM dbo.vw_CurrentWeights WHERE LayerID = 1;

-- Query: Show recent changes
SELECT * FROM dbo.vw_RecentWeightChanges ORDER BY ABS(Delta) DESC;

-- Query: Identify unstable weights (high variance)
SELECT * FROM dbo.vw_WeightStability 
WHERE StdDevValue > 0.5 
ORDER BY StdDevValue DESC;

-- Query: Point-in-time weights (e.g., yesterday at 3pm)
SELECT TensorAtomCoefficientID, CoefficientValue
FROM dbo.TensorAtomCoefficients
FOR SYSTEM_TIME AS OF '2025-11-07 15:00:00'
WHERE LayerID = 1;
```

---

## Task 3.3: Add Weight Rollback Procedure

**Status**: ❌ NOT STARTED
**Research**: FINDING 38 in SQL_CLR_RESEARCH_FINDINGS.md

### Problem
If learning goes wrong (overfitting, divergence), need to rollback weights.

### Solution: Rollback Stored Procedure

Create: `sql/procedures/System.WeightRollback.sql`

```sql
CREATE OR ALTER PROCEDURE System.sp_RollbackWeightsToTimestamp
    @RollbackTimestamp DATETIME2,
    @DryRun BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @RowsAffected INT;
    
    -- Validate timestamp
    IF @RollbackTimestamp > SYSUTCDATETIME()
    BEGIN
        RAISERROR('Cannot rollback to future timestamp', 16, 1);
        RETURN;
    END;
    
    IF @RollbackTimestamp < DATEADD(DAY, -30, SYSUTCDATETIME())
    BEGIN
        RAISERROR('Cannot rollback more than 30 days', 16, 1);
        RETURN;
    END;
    
    IF @DryRun = 1
    BEGIN
        -- Preview changes
        SELECT 
            'PREVIEW' AS Mode,
            curr.TensorAtomCoefficientID,
            curr.CoefficientValue AS CurrentValue,
            hist.CoefficientValue AS RollbackValue,
            curr.CoefficientValue - hist.CoefficientValue AS Delta
        FROM dbo.TensorAtomCoefficients curr
        INNER JOIN (
            SELECT * FROM dbo.TensorAtomCoefficients 
            FOR SYSTEM_TIME AS OF @RollbackTimestamp
        ) hist ON curr.TensorAtomCoefficientID = hist.TensorAtomCoefficientID
        WHERE ABS(curr.CoefficientValue - hist.CoefficientValue) > 0.0001;
        
        RETURN;
    END;
    
    -- Actual rollback
    BEGIN TRANSACTION;
    
    UPDATE curr
    SET curr.CoefficientValue = hist.CoefficientValue,
        curr.LastModified = SYSUTCDATETIME()
    FROM dbo.TensorAtomCoefficients curr
    INNER JOIN (
        SELECT * FROM dbo.TensorAtomCoefficients 
        FOR SYSTEM_TIME AS OF @RollbackTimestamp
    ) hist ON curr.TensorAtomCoefficientID = hist.TensorAtomCoefficientID
    WHERE ABS(curr.CoefficientValue - hist.CoefficientValue) > 0.0001;
    
    SET @RowsAffected = @@ROWCOUNT;
    
    COMMIT TRANSACTION;
    
    PRINT 'Rolled back ' + CAST(@RowsAffected AS NVARCHAR(10)) + ' coefficients to ' + 
          CONVERT(NVARCHAR(50), @RollbackTimestamp, 120);
END;
GO
```

### Usage

```sql
-- Preview rollback to yesterday
EXEC System.sp_RollbackWeightsToTimestamp 
    @RollbackTimestamp = '2025-11-07 12:00:00',
    @DryRun = 1;

-- Actually rollback
EXEC System.sp_RollbackWeightsToTimestamp 
    @RollbackTimestamp = '2025-11-07 12:00:00',
    @DryRun = 0;
```

---

## Success Criteria

Phase 3 complete when:
- ✅ TensorAtomCoefficients is temporal table
- ✅ History table created and linked
- ✅ Weight analysis views created
- ✅ Rollback procedure created and tested
- ✅ Verification queries run successfully
- ✅ Migration script committed to git

## Next Phase

After Phase 3 complete → `04-ORPHANED-FILES.md`
