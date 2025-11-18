-- =============================================
-- vw_ModelLayersWithStats: Database-level view for model layer queries with aggregations
-- Replaces hard-coded SQL in ModelsController.GetModelLayers (lines 401-423)
-- WITH SCHEMABINDING enables indexed views and query optimizer benefits
-- =============================================
CREATE VIEW [dbo].[vw_ModelLayersWithStats]
WITH SCHEMABINDING
AS
SELECT 
    l.LayerId,
    l.ModelId,
    l.LayerIdx,
    l.LayerName,
    l.LayerType,
    l.ParameterCount,
    l.TensorShape,
    l.TensorDtype,
    l.CacheHitRate,
    l.AvgComputeTimeMs,
    (SELECT COUNT_BIG(*) FROM dbo.TensorAtom ta WHERE ta.LayerId = l.LayerId) AS TensorAtomCount,
    (SELECT AVG(CAST(ta2.ImportanceScore AS FLOAT)) 
     FROM dbo.TensorAtom ta2 
     WHERE ta2.LayerId = l.LayerId) AS AvgImportance
FROM dbo.ModelLayer l;
GO

-- Optional: Create clustered index for materialized view
-- CREATE UNIQUE CLUSTERED INDEX IX_vw_ModelLayersWithStats_LayerId ON dbo.vw_ModelLayersWithStats(LayerId);
-- GO
