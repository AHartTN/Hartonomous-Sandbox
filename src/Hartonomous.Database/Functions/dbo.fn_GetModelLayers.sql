-- =============================================
-- fn_GetModelLayers: Inline TVF for model layer queries with stats
-- Replaces hard-coded SQL in ModelsController.GetModelLayers
-- INLINE TVFs get full query optimizer benefits
-- =============================================
CREATE FUNCTION dbo.fn_GetModelLayers(
    @ModelId INT
)
RETURNS TABLE
AS
RETURN
(
    SELECT 
        LayerId,
        LayerIdx,
        LayerName,
        LayerType,
        ParameterCount,
        TensorShape,
        TensorDtype,
        CacheHitRate,
        AvgComputeTimeMs,
        TensorAtomCount,
        AvgImportance
    FROM dbo.vw_ModelLayersWithStats
    WHERE ModelId = @ModelId
);
GO
