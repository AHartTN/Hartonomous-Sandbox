-- Procedure: Compare Weights at Two Points in Time
-- Purpose: Compare tensor atom coefficients between two timestamps

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
