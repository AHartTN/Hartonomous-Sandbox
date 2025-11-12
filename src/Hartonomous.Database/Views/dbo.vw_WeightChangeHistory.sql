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