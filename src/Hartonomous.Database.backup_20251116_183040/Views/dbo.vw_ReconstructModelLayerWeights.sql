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
FROM [dbo].[TensorAtomCoefficients] tac
JOIN [dbo].[Atoms] a ON tac.[TensorAtomId] = a.[AtomId]
JOIN [dbo].[Models] m ON tac.[ModelId] = m.[ModelId]
LEFT JOIN [dbo].[ModelLayers] ml ON tac.[ModelId] = ml.[ModelId] AND tac.[LayerIdx] = ml.[LayerIdx]
WHERE a.[Modality] = 'model' AND a.[Subtype] = 'float32-weight';
GO
