-- =============================================
-- vw_ReconstructModelLayerWeights: OLAP-Queryable Weight Reconstruction
-- Provides materialized view of all model weights for analytics
-- NOTE: AtomicValue is VARBINARY(64), binary-to-float conversion requires CLR or client-side processing
-- This view returns the binary representation; decode client-side or use CLR functions
-- =============================================
CREATE VIEW [dbo].[vw_ReconstructModelLayerWeights] AS
SELECT 
    tac.[ModelId],
    m.[ModelName],
    tac.[LayerIdx],
    ml.[LayerName],
    tac.[PositionX],
    tac.[PositionY],
    tac.[PositionZ],
    a.[AtomicValue] AS [WeightValueBinary]  -- VARBINARY(64) containing IEEE 754 float32
FROM [dbo].[TensorAtomCoefficient] tac
JOIN [dbo].[Atom] a ON tac.[TensorAtomId] = a.[AtomId]
JOIN [dbo].[Model] m ON tac.[ModelId] = m.[ModelId]
LEFT JOIN [dbo].[ModelLayer] ml ON tac.[ModelId] = ml.[ModelId] AND tac.[LayerIdx] = ml.[LayerIdx]
WHERE a.[Modality] = 'model' AND a.[Subtype] = 'float32-weight';
GO
