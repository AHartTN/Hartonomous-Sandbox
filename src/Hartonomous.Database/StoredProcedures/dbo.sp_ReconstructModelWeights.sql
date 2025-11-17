-- =============================================
-- sp_ReconstructModelWeights: Reconstruct model layer weights from atomic float32 values
-- Returns weight matrix in row-major order for specific layer
-- =============================================
CREATE PROCEDURE [dbo].[sp_ReconstructModelWeights]
    @layerAtomId BIGINT,
    @includeMetadata BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    -- Get layer shape from metadata
    DECLARE @rows INT, @cols INT;
    SELECT 
        @rows = JSON_VALUE(Metadata, '$.shape[0]'),
        @cols = JSON_VALUE(Metadata, '$.shape[1]')
    FROM dbo.Atom
    WHERE AtomId = @layerAtomId;

    -- Return weight coefficients ordered by position
    SELECT 
        ac.DimensionY AS Row,
        ac.DimensionZ AS Col,
        w.WeightValue,
        ac.PositionKey.STX AS LayerId,
        CASE WHEN @includeMetadata = 1 THEN w.WeightHash ELSE NULL END AS WeightHash,
        CASE WHEN @includeMetadata = 1 THEN ac.Metadata ELSE NULL END AS Metadata
    FROM dbo.AtomCompositions ac
    INNER JOIN dbo.Atom a ON ac.ComponentAtomId = a.AtomId
    INNER JOIN dbo.AtomicWeights w ON w.WeightHash = a.ContentHash
    WHERE ac.SourceAtomId = @layerAtomId
      AND ac.ComponentType = 'weight'
    ORDER BY ac.DimensionY, ac.DimensionZ;  -- Row-major order

    -- Return layer metadata
    SELECT 
        AtomId AS LayerAtomId,
        @rows AS Rows,
        @cols AS Cols,
        (@rows * @cols) AS TotalWeights,
        JSON_VALUE(Metadata, '$.layerName') AS LayerName,
        JSON_VALUE(Metadata, '$.layerType') AS LayerType,
        JSON_VALUE(Metadata, '$.modelName') AS ModelName,
        JSON_VALUE(Metadata, '$.dtype') AS DataType,
        CreatedAt
    FROM dbo.Atom
    WHERE AtomId = @layerAtomId;
END;
GO
