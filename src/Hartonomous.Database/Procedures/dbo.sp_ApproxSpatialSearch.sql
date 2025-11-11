CREATE PROCEDURE dbo.sp_ApproxSpatialSearch
    @query_x FLOAT,
    @query_y FLOAT,
    @query_z FLOAT,
    @top_k INT = 10,
    @use_coarse BIT = 0,
    @embedding_type NVARCHAR(128) = NULL,
    @ModelId INT = NULL,
    @srid INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @wkt NVARCHAR(200) = CONCAT('POINT (', @query_x, ' ', @query_y, ' ', @query_z, ')');
    DECLARE @query_point GEOMETRY = geometry::STGeomFromText(@wkt, @srid);

    IF @use_coarse = 1
    BEGIN
        SELECT TOP (@top_k)
            ae.AtomEmbeddingId,
            ae.AtomId,
            a.Modality,
            a.Subtype,
            a.SourceUri,
            a.SourceType,
            ae.EmbeddingType,
            ae.ModelId,
            ae.SpatialCoarse.STDistance(@query_point) AS spatial_distance
        FROM dbo.AtomEmbeddings AS ae
        INNER JOIN dbo.Atoms AS a ON a.AtomId = ae.AtomId
        WHERE ae.SpatialCoarse IS NOT NULL
          AND (@embedding_type IS NULL OR ae.EmbeddingType = @embedding_type)
          AND (@ModelId IS NULL OR ae.ModelId = @ModelId)
        ORDER BY ae.SpatialCoarse.STDistance(@query_point);
    END
    ELSE
    BEGIN
        SELECT TOP (@top_k)
            ae.AtomEmbeddingId,
            ae.AtomId,
            a.Modality,
            a.Subtype,
            a.SourceUri,
            a.SourceType,
            ae.EmbeddingType,
            ae.ModelId,
            ae.SpatialGeometry.STDistance(@query_point) AS spatial_distance
        FROM dbo.AtomEmbeddings AS ae
        INNER JOIN dbo.Atoms AS a ON a.AtomId = ae.AtomId
        WHERE ae.SpatialGeometry IS NOT NULL
          AND (@embedding_type IS NULL OR ae.EmbeddingType = @embedding_type)
          AND (@ModelId IS NULL OR ae.ModelId = @ModelId)
        ORDER BY ae.SpatialGeometry.STDistance(@query_point);
    END;
END;
GO