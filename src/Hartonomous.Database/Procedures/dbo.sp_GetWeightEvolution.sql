-- Procedure: Get Weight Evolution for Atom
-- Purpose: Track temporal changes of coefficients for a specific tensor atom

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
        tac.TensorRole,
        tac.Coefficient AS CurrentValue,
        tac.ValidFrom AS ChangedAt,
        tac.ValidTo AS ValidUntil,
        -- Previous value
        LAG(tac.Coefficient) OVER (
            PARTITION BY tac.TensorAtomCoefficientId 
            ORDER BY tac.ValidFrom
        ) AS PreviousValue,
        -- Change amount
        tac.Coefficient - LAG(tac.Coefficient) OVER (
            PARTITION BY tac.TensorAtomCoefficientId 
            ORDER BY tac.ValidFrom
        ) AS Delta,
        -- Change percentage (handle division by zero)
        CASE 
            WHEN LAG(tac.Coefficient) OVER (
                PARTITION BY tac.TensorAtomCoefficientId 
                ORDER BY tac.ValidFrom
            ) = 0 THEN NULL
            ELSE (tac.Coefficient - LAG(tac.Coefficient) OVER (
                PARTITION BY tac.TensorAtomCoefficientId 
                ORDER BY tac.ValidFrom
            )) / LAG(tac.Coefficient) OVER (
                PARTITION BY tac.TensorAtomCoefficientId 
                ORDER BY tac.ValidFrom
            ) * 100
        END AS PercentChange
    FROM 
        dbo.TensorAtomCoefficients FOR SYSTEM_TIME ALL tac
    WHERE 
        tac.TensorAtomId = @TensorAtomId
        AND tac.ValidFrom >= @StartDate
        AND tac.ValidFrom <= @EndDate
    ORDER BY 
        tac.TensorRole,
        tac.ValidFrom;
END;
