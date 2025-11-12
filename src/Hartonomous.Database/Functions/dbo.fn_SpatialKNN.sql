CREATE FUNCTION dbo.fn_SpatialKNN(
    @query_point GEOMETRY,
    @top_k INT,
    @table_name NVARCHAR(128)
)
RETURNS TABLE
AS
RETURN
(
    -- Dynamic SQL would be needed for generic table parameter
    -- For now, specialized for AtomEmbeddings
    SELECT TOP (@top_k)
        ae.AtomEmbeddingId,
        ae.AtomId,
        ae.SpatialGeometry.STDistance(@query_point) AS SpatialDistance
    FROM dbo.AtomEmbeddings ae
    WHERE ae.SpatialGeometry IS NOT NULL
      AND ae.SpatialGeometry.STDistance(@query_point) IS NOT NULL
    ORDER BY ae.SpatialGeometry.STDistance(@query_point) ASC
);