-- ===============================================
-- Weight History Analysis Views & Procedures
-- Purpose: Query and analyze model weight evolution
-- ===============================================
-- Created: 2025-11-08
-- Part of: Phase 3 - Temporal Tables Implementation
-- Reference: docs/audit/03-TEMPORAL-TABLES.md
-- ===============================================

-- ===============================================
-- View: Current Weights Summary
-- ===============================================
IF OBJECT_ID('dbo.vw_CurrentWeights', 'V') IS NOT NULL
    DROP VIEW dbo.vw_CurrentWeights;

CREATE VIEW dbo.vw_CurrentWeights
AS
SELECT 
    tac.TensorAtomCoefficientId,
    tac.TensorAtomId,
    ta.AtomId,
    ta.ModelId,
    ta.LayerId,
    ta.AtomType,
    tac.ParentLayerId,
    tac.TensorRole,
    tac.Coefficient,
    tac.ValidFrom AS LastUpdated,
    -- Metadata
    ta.ImportanceScore,
    JSON_VALUE(ta.Metadata, '$.description') AS AtomDescription,
    JSON_VALUE(ta.Metadata, '$.source') AS AtomSource
FROM 
    dbo.TensorAtomCoefficients tac
    INNER JOIN dbo.TensorAtoms ta ON tac.TensorAtomId = ta.TensorAtomId;

-- ===============================================
-- View: Weight Change History
-- ===============================================
IF OBJECT_ID('dbo.vw_WeightChangeHistory', 'V') IS NOT NULL
    DROP VIEW dbo.vw_WeightChangeHistory;

CREATE VIEW dbo.vw_WeightChangeHistory
AS
SELECT 
    tac.TensorAtomCoefficientId,
    tac.TensorAtomId,
    tac.ParentLayerId,
    tac.TensorRole,
    tac.Coefficient,
    tac.ValidFrom AS ChangedAt,
    tac.ValidTo AS ValidUntil,
    -- Calculate duration of this weight value
    DATEDIFF(SECOND, tac.ValidFrom, tac.ValidTo) AS DurationSeconds,
    -- Calculate change from previous value (if available)
    LAG(tac.Coefficient) OVER (
        PARTITION BY tac.TensorAtomCoefficientId 
        ORDER BY tac.ValidFrom
    ) AS PreviousCoefficient,
    tac.Coefficient - LAG(tac.Coefficient) OVER (
        PARTITION BY tac.TensorAtomCoefficientId 
        ORDER BY tac.ValidFrom
    ) AS CoefficientDelta
FROM 
    dbo.TensorAtomCoefficients FOR SYSTEM_TIME ALL tac;

-- ===============================================
-- Procedure: Get Weight Evolution for Atom
-- ===============================================
IF OBJECT_ID('dbo.sp_GetWeightEvolution', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetWeightEvolution;

CREATE PROCEDURE dbo.sp_GetWeightEvolution
    @TensorAtomId BIGINT,
    @StartDate DATETIME2(7) = NULL,
    @EndDate DATETIME2(7) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Default to all history if dates not provided
    SET @StartDate = ISNULL(@StartDate, '2000-01-01');
    SET @EndDate = ISNULL(@EndDate, '9999-12-31 23:59:59.9999999');
    
    SELECT 
        tac.TensorAtomCoefficientId,
        tac.TensorAtomId,
        ta.AtomId,
        ta.AtomType,
        tac.ParentLayerId,
        tac.TensorRole,
        tac.Coefficient,
        tac.ValidFrom AS ChangedAt,
        tac.ValidTo AS ValidUntil,
        -- Delta from previous
        LAG(tac.Coefficient) OVER (
            PARTITION BY tac.TensorAtomCoefficientId 
            ORDER BY tac.ValidFrom
        ) AS PreviousValue,
        tac.Coefficient - LAG(tac.Coefficient) OVER (
            PARTITION BY tac.TensorAtomCoefficientId 
            ORDER BY tac.ValidFrom
        ) AS Delta,
        -- Percentage change
        CASE 
            WHEN LAG(tac.Coefficient) OVER (
                PARTITION BY tac.TensorAtomCoefficientId 
                ORDER BY tac.ValidFrom
            ) <> 0 
            THEN 100.0 * (tac.Coefficient - LAG(tac.Coefficient) OVER (
                PARTITION BY tac.TensorAtomCoefficientId 
                ORDER BY tac.ValidFrom
            )) / LAG(tac.Coefficient) OVER (
                PARTITION BY tac.TensorAtomCoefficientId 
                ORDER BY tac.ValidFrom
            )
            ELSE NULL
        END AS PercentChange
    FROM 
        dbo.TensorAtomCoefficients FOR SYSTEM_TIME ALL tac
        INNER JOIN dbo.TensorAtoms ta ON tac.TensorAtomId = ta.TensorAtomId
    WHERE 
        tac.TensorAtomId = @TensorAtomId
        AND tac.ValidFrom >= @StartDate
        AND tac.ValidFrom <= @EndDate
    ORDER BY 
        tac.TensorAtomCoefficientId,
        tac.ValidFrom;
END;

-- ===============================================
-- Procedure: Compare Weights at Two Points in Time
-- ===============================================
IF OBJECT_ID('dbo.sp_CompareWeightsAtTimes', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_CompareWeightsAtTimes;

CREATE PROCEDURE dbo.sp_CompareWeightsAtTimes
    @Time1 DATETIME2(7),
    @Time2 DATETIME2(7),
    @ModelId INT = NULL,
    @MinDeltaThreshold REAL = 0.0001  -- Only show weights that changed significantly
AS
BEGIN
    SET NOCOUNT ON;
    
    WITH WeightsAtTime1 AS (
        SELECT 
            tac.TensorAtomCoefficientId,
            tac.TensorAtomId,
            ta.ModelId,
            ta.AtomType,
            tac.TensorRole,
            tac.Coefficient AS Coefficient1
        FROM 
            dbo.TensorAtomCoefficients FOR SYSTEM_TIME AS OF @Time1 tac
            INNER JOIN dbo.TensorAtoms ta ON tac.TensorAtomId = ta.TensorAtomId
        WHERE 
            @ModelId IS NULL OR ta.ModelId = @ModelId
    ),
    WeightsAtTime2 AS (
        SELECT 
            tac.TensorAtomCoefficientId,
            tac.Coefficient AS Coefficient2
        FROM 
            dbo.TensorAtomCoefficients FOR SYSTEM_TIME AS OF @Time2 tac
    )
    SELECT 
        w1.TensorAtomCoefficientId,
        w1.TensorAtomId,
        w1.ModelId,
        w1.AtomType,
        w1.TensorRole,
        w1.Coefficient1,
        ISNULL(w2.Coefficient2, w1.Coefficient1) AS Coefficient2,
        ISNULL(w2.Coefficient2, w1.Coefficient1) - w1.Coefficient1 AS Delta,
        CASE 
            WHEN w1.Coefficient1 <> 0 
            THEN 100.0 * (ISNULL(w2.Coefficient2, w1.Coefficient1) - w1.Coefficient1) / w1.Coefficient1
            ELSE NULL
        END AS PercentChange
    FROM 
        WeightsAtTime1 w1
        LEFT JOIN WeightsAtTime2 w2 ON w1.TensorAtomCoefficientId = w2.TensorAtomCoefficientId
    WHERE 
        ABS(ISNULL(w2.Coefficient2, w1.Coefficient1) - w1.Coefficient1) >= @MinDeltaThreshold
    ORDER BY 
        ABS(ISNULL(w2.Coefficient2, w1.Coefficient1) - w1.Coefficient1) DESC;
END;

-- ===============================================
-- Procedure: Get Most Recently Changed Weights
-- ===============================================
IF OBJECT_ID('dbo.sp_GetRecentWeightChanges', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetRecentWeightChanges;

CREATE PROCEDURE dbo.sp_GetRecentWeightChanges
    @TopN INT = 100,
    @SinceDateTime DATETIME2(7) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Default to last 24 hours
    SET @SinceDateTime = ISNULL(@SinceDateTime, DATEADD(DAY, -1, SYSUTCDATETIME()));
    
    SELECT TOP (@TopN)
        tac.TensorAtomCoefficientId,
        tac.TensorAtomId,
        ta.AtomId,
        ta.ModelId,
        ta.AtomType,
        tac.TensorRole,
        tac.Coefficient AS CurrentValue,
        LAG(tac.Coefficient) OVER (
            PARTITION BY tac.TensorAtomCoefficientId 
            ORDER BY tac.ValidFrom
        ) AS PreviousValue,
        tac.Coefficient - LAG(tac.Coefficient) OVER (
            PARTITION BY tac.TensorAtomCoefficientId 
            ORDER BY tac.ValidFrom
        ) AS Delta,
        tac.ValidFrom AS ChangedAt
    FROM 
        dbo.TensorAtomCoefficients FOR SYSTEM_TIME ALL tac
        INNER JOIN dbo.TensorAtoms ta ON tac.TensorAtomId = ta.TensorAtomId
    WHERE 
        tac.ValidFrom >= @SinceDateTime
    ORDER BY 
        tac.ValidFrom DESC;
END;

