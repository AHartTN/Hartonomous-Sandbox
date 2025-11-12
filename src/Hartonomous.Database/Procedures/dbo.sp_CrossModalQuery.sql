CREATE PROCEDURE dbo.sp_CrossModalQuery
    @text_query NVARCHAR(MAX) = NULL,
    @spatial_query_x FLOAT = NULL,
    @spatial_query_y FLOAT = NULL,
    @spatial_query_z FLOAT = NULL,
    @modality_filter NVARCHAR(50) = NULL,
    @top_k INT = 10
AS
BEGIN
    SET NOCOUNT ON;

    PRINT 'CROSS-MODAL INFERENCE';
    PRINT '  Text filter: ' + ISNULL(@text_query, '(none)');
    PRINT '  Target modality: ' + ISNULL(@modality_filter, 'all');

    IF @spatial_query_x IS NOT NULL AND @spatial_query_y IS NOT NULL
    BEGIN
        DECLARE @z FLOAT = ISNULL(@spatial_query_z, 0);
        DECLARE @query_wkt NVARCHAR(200) = CONCAT('POINT (', @spatial_query_x, ' ', @spatial_query_y, ' ', @z, ')');
        DECLARE @query_pt GEOMETRY = geometry::STGeomFromText(@query_wkt, 0);

        SELECT TOP (@top_k)
            ae.AtomEmbeddingId,
            ae.AtomId,
            a.Modality,
            a.Subtype,
            a.SourceType,
            a.SourceUri,
            a.CanonicalText,
            ae.EmbeddingType,
            ae.ModelId,
            ae.SpatialGeometry.STDistance(@query_pt) AS SpatialDistance
        FROM dbo.AtomEmbeddings AS ae
        INNER JOIN dbo.Atoms AS a ON a.AtomId = ae.AtomId
        WHERE ae.SpatialGeometry IS NOT NULL
          AND (@modality_filter IS NULL OR a.SourceType = @modality_filter OR a.Modality = @modality_filter)
          AND (@text_query IS NULL OR a.CanonicalText LIKE '%' + @text_query + '%')
        ORDER BY ae.SpatialGeometry.STDistance(@query_pt);
    END
    ELSE
    BEGIN
        SELECT TOP (@top_k)
            ae.AtomEmbeddingId,
            ae.AtomId,
            a.Modality,
            a.Subtype,
            a.SourceType,
            a.SourceUri,
            a.CanonicalText,
            ae.EmbeddingType,
            ae.ModelId
        FROM dbo.AtomEmbeddings AS ae
        INNER JOIN dbo.Atoms AS a ON a.AtomId = ae.AtomId
        WHERE (@modality_filter IS NULL OR a.SourceType = @modality_filter OR a.Modality = @modality_filter)
          AND (@text_query IS NULL OR a.CanonicalText LIKE '%' + @text_query + '%')
        ORDER BY NEWID();
    END;

    PRINT '✓ Cross-modal results returned';
END