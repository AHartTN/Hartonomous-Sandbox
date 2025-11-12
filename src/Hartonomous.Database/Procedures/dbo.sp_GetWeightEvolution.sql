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