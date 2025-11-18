-- =============================================
-- vw_ModelDetails: Database-level view for model detail queries
-- Replaces hard-coded SQL in ModelsController.GetModel (lines 119-127)
-- WITH SCHEMABINDING enables indexed views and query optimizer benefits
-- =============================================
CREATE VIEW [dbo].[vw_ModelDetails]
WITH SCHEMABINDING
AS
SELECT 
    m.ModelId,
    m.ModelName,
    m.ModelType,
    m.ParameterCount,
    m.IngestionDate,
    m.Architecture,
    m.UsageCount,
    m.LastUsed,
    mm.SupportedTasks,
    mm.SupportedModalities,
    mm.MaxInputLength,
    mm.MaxOutputLength,
    mm.EmbeddingDimension,
    (SELECT COUNT_BIG(*) FROM dbo.ModelLayer ml WHERE ml.ModelId = m.ModelId) AS LayerCount
FROM dbo.Model m
LEFT JOIN dbo.ModelMetadata mm ON mm.ModelId = m.ModelId;
GO
