-- =============================================
-- vw_ModelsSummary: Database-level view for model listing
-- Replaces hard-coded SQL in ModelsController.GetModels (lines 61-73)
-- WITH SCHEMABINDING enables indexed views and query optimizer benefits
-- =============================================
CREATE VIEW [dbo].[vw_ModelsSummary]
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
    (SELECT COUNT_BIG(*) FROM dbo.ModelLayer ml WHERE ml.ModelId = m.ModelId) AS LayerCount
FROM dbo.Model m;
GO

-- Optional: Create clustered index for materialized view (significant perf boost for large datasets)
-- CREATE UNIQUE CLUSTERED INDEX IX_vw_ModelsSummary_ModelId ON dbo.vw_ModelsSummary(ModelId);
-- GO
