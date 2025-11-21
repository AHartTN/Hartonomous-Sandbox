CREATE OR ALTER PROCEDURE dbo.sp_CrossModalQuery
    @text_query NVARCHAR(MAX) = NULL,
    @spatial_query_x FLOAT = NULL,
    @spatial_query_y FLOAT = NULL,
    @spatial_query_z FLOAT = NULL,
    @modality_filter NVARCHAR(50) = NULL,
    @top_k INT = 10,
    @TenantId INT = 0  -- PHASE 7.2: Multi-tenancy
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
            a.CanonicalText,
            ae.EmbeddingType,
            ae.ModelId,
            ae.SpatialKey.STDistance(@query_pt) AS SpatialDistance
        FROM dbo.AtomEmbedding AS ae
        INNER JOIN dbo.Atom AS a ON a.AtomId = ae.AtomId
        WHERE ae.SpatialKey IS NOT NULL
          AND (@modality_filter IS NULL OR a.Modality = @modality_filter)
          AND (@text_query IS NULL OR a.CanonicalText LIKE '%' + @text_query + '%')
          -- PHASE 7.2: Multi-tenancy filter
          AND (a.TenantId = @TenantId OR EXISTS (SELECT 1 FROM dbo.TenantAtoms ta WHERE ta.AtomId = a.AtomId AND ta.TenantId = @TenantId))
        ORDER BY ae.SpatialKey.STDistance(@query_pt);
    END
    ELSE
    BEGIN
        -- Default: Return recent atoms (not random)
        SELECT TOP (@top_k)
            ae.AtomEmbeddingId,
            ae.AtomId,
            a.Modality,
            a.Subtype,
            a.CanonicalText,
            ae.EmbeddingType,
            ae.ModelId
        FROM dbo.Atom a
        INNER JOIN dbo.AtomEmbedding ae ON a.AtomId = ae.AtomId
        WHERE (@modality_filter IS NULL OR a.Modality = @modality_filter)
          AND (@text_query IS NULL OR a.CanonicalText LIKE '%' + @text_query + '%')
          -- PHASE 7.2: Multi-tenancy filter
          AND (a.TenantId = @TenantId OR EXISTS (SELECT 1 FROM dbo.TenantAtoms ta WHERE ta.AtomId = a.AtomId AND ta.TenantId = @TenantId))
        ORDER BY a.CreatedAt DESC;  -- Recent atoms, not random
    END;

    PRINT '✓ Cross-modal results returned';
END;
GO