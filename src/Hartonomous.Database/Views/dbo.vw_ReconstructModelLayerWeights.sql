-- =============================================
-- vw_ReconstructModelLayerWeights: OLAP-Queryable Weight Reconstruction
-- Provides materialized view of all model weights for analytics
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
    CAST(a.[AtomicValue] AS REAL) AS [WeightValue]
FROM [dbo].[TensorAtomCoefficients] tac
JOIN [dbo].[Atoms] a ON tac.[TensorAtomId] = a.[AtomId]
JOIN [dbo].[Models] m ON tac.[ModelId] = m.[ModelId]
LEFT JOIN [dbo].[ModelLayers] ml ON tac.[ModelId] = ml.[ModelId] AND tac.[LayerIdx] = ml.[LayerIdx]
WHERE a.[Modality] = 'model' AND a.[Subtype] = 'float32-weight';
GO
