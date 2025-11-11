-- Procedure: Get Recent Weight Changes
-- Purpose: Find most recently updated tensor atom coefficients

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
