-- sp_DiscoverAndBindConcepts: Orchestration procedure
-- Runs unsupervised learning pipeline end-to-end
-- Phase 1: Discover concepts via clustering
-- Phase 2: Bind atoms to discovered concepts

CREATE OR ALTER PROCEDURE dbo.sp_DiscoverAndBindConcepts
    @MinClusterSize INT = 10,
    @CoherenceThreshold FLOAT = 0.7,
    @MaxConcepts INT = 100,
    @SimilarityThreshold FLOAT = 0.6,
    @MaxConceptsPerAtom INT = 5,
    @TenantId INT = 0,
    @DryRun BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @DiscoveredCount INT = 0;
    DECLARE @BoundCount INT = 0;
    DECLARE @StartTime DATETIME2 = SYSUTCDATETIME();
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Phase 1: Discover concepts
        DECLARE @DiscoveredConcepts TABLE (
            ConceptId UNIQUEIDENTIFIER,
            Centroid VARBINARY(MAX),
            AtomCount INT,
            Coherence FLOAT,
            SpatialBucket INT
        );
        
        INSERT INTO @DiscoveredConcepts
        SELECT * FROM dbo.fn_DiscoverConcepts(
            @MinClusterSize,
            @CoherenceThreshold,
            @MaxConcepts,
            @TenantId
        );
        
        SET @DiscoveredCount = @@ROWCOUNT;
        
        IF @DryRun = 0
        BEGIN
            -- Persist discovered concepts
            INSERT INTO provenance.Concepts (
                ConceptId,
                Centroid,
                AtomCount,
                Coherence,
                SpatialBucket,
                TenantId
            )
            SELECT 
                ConceptId,
                Centroid,
                AtomCount,
                Coherence,
                SpatialBucket,
                @TenantId
            FROM @DiscoveredConcepts;
            
            -- Track discovery in evolution log
            INSERT INTO provenance.ConceptEvolution (
                ConceptId,
                PreviousCentroid,
                NewCentroid,
                CentroidShift,
                AtomCountDelta,
                CoherenceDelta,
                EvolutionType,
                TenantId
            )
            SELECT 
                ConceptId,
                NULL, -- No previous centroid for new discoveries
                Centroid,
                NULL, -- No shift for new concepts
                AtomCount,
                NULL, -- No delta for new concepts
                'Discovered',
                @TenantId
            FROM @DiscoveredConcepts;
        END
        
        -- Phase 2: Bind atoms to concepts
        DECLARE @ConceptsToBind TABLE (ConceptId UNIQUEIDENTIFIER);
        INSERT INTO @ConceptsToBind SELECT ConceptId FROM @DiscoveredConcepts;
        
        DECLARE @AtomBindings TABLE (
            AtomId BIGINT,
            ConceptId UNIQUEIDENTIFIER,
            Similarity FLOAT,
            IsPrimary BIT
        );
        
        -- For each concept, find matching atoms
        DECLARE @CurrentConceptId UNIQUEIDENTIFIER;
        DECLARE @ConceptCentroid VARBINARY(MAX);
        
        DECLARE concept_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT ConceptId, Centroid FROM @DiscoveredConcepts;
        
        OPEN concept_cursor;
        FETCH NEXT FROM concept_cursor INTO @CurrentConceptId, @ConceptCentroid;
        
        WHILE @@FETCH_STATUS = 0
        BEGIN
            -- Find atoms similar to this concept
            INSERT INTO @AtomBindings
            SELECT 
                ae.AtomId,
                @CurrentConceptId AS ConceptId,
                1.0 - VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @ConceptCentroid) AS Similarity,
                0 AS IsPrimary -- Will set primary in second pass
            FROM dbo.AtomEmbeddings ae
            WHERE ae.TenantId = @TenantId
                  AND (1.0 - VECTOR_DISTANCE('cosine', ae.EmbeddingVector, @ConceptCentroid)) >= @SimilarityThreshold;
            
            FETCH NEXT FROM concept_cursor INTO @CurrentConceptId, @ConceptCentroid;
        END
        
        CLOSE concept_cursor;
        DEALLOCATE concept_cursor;
        
        -- Set primary concept (highest similarity per atom)
        WITH RankedBindings AS (
            SELECT 
                AtomId,
                ConceptId,
                Similarity,
                ROW_NUMBER() OVER (PARTITION BY AtomId ORDER BY Similarity DESC) AS Rank
            FROM @AtomBindings
        )
        UPDATE ab
        SET IsPrimary = CASE WHEN rb.Rank = 1 THEN 1 ELSE 0 END
        FROM @AtomBindings ab
        INNER JOIN RankedBindings rb ON ab.AtomId = rb.AtomId AND ab.ConceptId = rb.ConceptId;
        
        SET @BoundCount = (SELECT COUNT(*) FROM @AtomBindings);
        
        IF @DryRun = 0
        BEGIN
            -- Persist atom-concept bindings
            INSERT INTO provenance.AtomConcepts (
                AtomId,
                ConceptId,
                Similarity,
                IsPrimary,
                TenantId
            )
            SELECT 
                AtomId,
                ConceptId,
                Similarity,
                IsPrimary,
                @TenantId
            FROM @AtomBindings;
            
            -- Update concept atom counts
            UPDATE c
            SET AtomCount = (
                SELECT COUNT(DISTINCT ac.AtomId)
                FROM provenance.AtomConcepts ac
                WHERE ac.ConceptId = c.ConceptId
            )
            FROM provenance.Concepts c
            WHERE c.ConceptId IN (SELECT ConceptId FROM @DiscoveredConcepts);
        END
        
        COMMIT TRANSACTION;
        
        -- Return summary
        SELECT 
            @DiscoveredCount AS ConceptsDiscovered,
            @BoundCount AS AtomBindings,
            DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME()) AS DurationMs,
            @DryRun AS WasDryRun;
        
        PRINT 'Concept discovery complete: ' + 
              CAST(@DiscoveredCount AS VARCHAR(10)) + ' concepts, ' +
              CAST(@BoundCount AS VARCHAR(10)) + ' bindings';
              
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        PRINT 'sp_DiscoverAndBindConcepts ERROR: ' + @ErrorMessage;
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
    END CATCH
END;
GO
