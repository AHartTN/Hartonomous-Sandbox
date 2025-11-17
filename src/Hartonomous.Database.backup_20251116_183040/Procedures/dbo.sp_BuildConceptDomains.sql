-- =============================================
-- sp_BuildConceptDomains: Build Voronoi-like Semantic Domains
-- =============================================
-- Creates spatial domains for each concept using nearest-neighbor boundaries
-- Simplified version - production would use full 3D Voronoi (MIConvexHull CLR)
-- =============================================

CREATE PROCEDURE [dbo].[sp_BuildConceptDomains]
    @TenantId INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    -- Build domains for each concept by defining boundaries with nearest concepts
    -- This is a simplified "cell" approach - each concept gets a buffered region

    CREATE TABLE #ConceptCentroids (
        ConceptId INT PRIMARY KEY,
        Centroid GEOMETRY NOT NULL
    );

    -- Get all active concepts with spatial centroids
    INSERT INTO #ConceptCentroids (ConceptId, Centroid)
    SELECT ConceptId, CentroidSpatialKey
    FROM [provenance].[Concepts]
    WHERE [TenantId] = @TenantId
      AND [IsActive] = 1
      AND [CentroidSpatialKey] IS NOT NULL;

    IF NOT EXISTS (SELECT 1 FROM #ConceptCentroids)
    BEGIN
        PRINT 'No concepts with spatial centroids found.';
        RETURN 0;
    END

    -- For each concept, compute its domain as a buffer around centroid
    -- Size is determined by distance to nearest neighbor / 2
    CREATE TABLE #ConceptDomains (
        ConceptId INT PRIMARY KEY,
        Domain GEOMETRY NOT NULL,
        Radius FLOAT NOT NULL
    );

    DECLARE @ConceptId INT;
    DECLARE @Centroid GEOMETRY;
    DECLARE @NearestDistance FLOAT;

    DECLARE concept_cursor CURSOR FOR
        SELECT ConceptId, Centroid 
        FROM #ConceptCentroids;

    OPEN concept_cursor;
    FETCH NEXT FROM concept_cursor INTO @ConceptId, @Centroid;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Find distance to nearest neighbor concept
        SELECT TOP 1 @NearestDistance = @Centroid.STDistance(cc.Centroid)
        FROM #ConceptCentroids cc
        WHERE cc.ConceptId <> @ConceptId
        ORDER BY @Centroid.STDistance(cc.Centroid) ASC;

        IF @NearestDistance IS NULL
            SET @NearestDistance = 1.0; -- Single concept case

        -- Create domain as buffer with radius = half distance to nearest neighbor
        DECLARE @Radius FLOAT = @NearestDistance / 2.0;
        DECLARE @Domain GEOMETRY = @Centroid.STBuffer(@Radius);

        INSERT INTO #ConceptDomains (ConceptId, Domain, Radius)
        VALUES (@ConceptId, @Domain, @Radius);

        FETCH NEXT FROM concept_cursor INTO @ConceptId, @Centroid;
    END

    CLOSE concept_cursor;
    DEALLOCATE concept_cursor;

    -- Update Concepts table with computed domains
    UPDATE c
    SET 
        c.[ConceptDomain] = cd.Domain,
        c.[LastUpdatedAt] = SYSUTCDATETIME()
    FROM [provenance].[Concepts] c
    JOIN #ConceptDomains cd ON c.ConceptId = cd.ConceptId;

    DECLARE @UpdatedCount INT = @@ROWCOUNT;

    -- Ensure spatial indexes exist (will be skipped if already exists)
    IF NOT EXISTS (SELECT 1 FROM sys.spatial_indexes WHERE name = 'SIX_Concepts_ConceptDomain')
    BEGIN
        CREATE SPATIAL INDEX [SIX_Concepts_ConceptDomain] 
            ON [provenance].[Concepts]([ConceptDomain])
            WITH (BOUNDING_BOX = (-1, -1, 1, 1));
    END

    DROP TABLE #ConceptCentroids;
    DROP TABLE #ConceptDomains;

    PRINT 'Built ' + CAST(@UpdatedCount AS VARCHAR(10)) + ' concept domains.';
    RETURN 0;
END
GO
