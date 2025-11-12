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