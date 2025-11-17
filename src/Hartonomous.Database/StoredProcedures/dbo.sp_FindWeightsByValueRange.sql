-- =============================================
-- sp_FindWeightsByValueRange: Find model layers with weights in specific range
-- Uses spatial index on weight values
-- =============================================
CREATE PROCEDURE [dbo].[sp_FindWeightsByValueRange]
    @minValue REAL,
    @maxValue REAL,
    @minOccurrences INT = 1,
    @maxResults INT = 100
AS
BEGIN
    SET NOCOUNT ON;

    -- Find weights in value range
    WITH MatchingWeights AS (
        SELECT 
            w.WeightHash,
            w.WeightValue,
            w.ReferenceCount
        FROM dbo.AtomicWeights w
        WHERE w.WeightValue BETWEEN @minValue AND @maxValue
    ),
    LayerMatches AS (
        SELECT 
            ac.SourceAtomId AS LayerAtomId,
            COUNT(*) AS WeightCount,
            AVG(mw.WeightValue) AS AvgValue,
            MIN(mw.WeightValue) AS MinValue,
            MAX(mw.WeightValue) AS MaxValue
        FROM dbo.AtomCompositions ac
        INNER JOIN dbo.Atom a ON ac.ComponentAtomId = a.AtomId
        INNER JOIN MatchingWeights mw ON mw.WeightHash = a.ContentHash
        WHERE ac.ComponentType = 'weight'
        GROUP BY ac.SourceAtomId
        HAVING COUNT(*) >= @minOccurrences
    )
    SELECT TOP (@maxResults)
        layer.AtomId AS LayerAtomId,
        JSON_VALUE(layer.Metadata, '$.layerName') AS LayerName,
        JSON_VALUE(layer.Metadata, '$.layerType') AS LayerType,
        JSON_VALUE(layer.Metadata, '$.modelName') AS ModelName,
        lm.WeightCount AS MatchingWeights,
        lm.AvgValue,
        lm.MinValue,
        lm.MaxValue,
        layer.CreatedAt
    FROM LayerMatches lm
    INNER JOIN dbo.Atom layer ON layer.AtomId = lm.LayerAtomId
    ORDER BY lm.WeightCount DESC, layer.CreatedAt DESC;
END;
GO
