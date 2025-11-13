-- sp_DiscoverAndBindConcepts: Orchestration procedure
-- Runs unsupervised learning pipeline end-to-end
-- Phase 1: Discover concepts via clustering
-- Phase 2: Bind atoms to discovered concepts

CREATE PROCEDURE dbo.sp_DiscoverAndBindConcepts
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
            -- Temp table to capture generated ConceptIds
            DECLARE @InsertedConcepts TABLE (
                ConceptId BIGINT,
                Centroid VARBINARY(MAX),
                AtomCount INT,
                Coherence FLOAT,
                SpatialBucket INT
            );

            -- Persist discovered concepts and capture generated IDs
            INSERT INTO provenance.Concepts (
                ConceptName,
                Description,
                CentroidVector,
                Centroid,
                VectorDimension,
                MemberCount,
                AtomCount,
                CoherenceScore,
                Coherence,
                SpatialBucket,
                DiscoveryMethod,
                ModelId,
                TenantId
            )
            OUTPUT 
                INSERTED.ConceptId,
                INSERTED.Centroid,
                INSERTED.AtomCount,
                INSERTED.Coherence,
                INSERTED.SpatialBucket
            INTO @InsertedConcepts
            SELECT 
                'Cluster_' + CAST(ROW_NUMBER() OVER (ORDER BY Coherence DESC) AS NVARCHAR(50)),
                'Auto-discovered concept cluster',
                Centroid,
                Centroid,
                0, -- VectorDimension - will be computed
                AtomCount,
                AtomCount,
                Coherence,
                Coherence,
                SpatialBucket,
                'DBSCAN_Spatial',
                1, -- Default ModelId - adjust as needed
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
            FROM @InsertedConcepts;
        END
        
        -- Phase 2: Bind atoms to concepts (use inserted concepts for binding)
        DECLARE @ConceptsToBind TABLE (ConceptId BIGINT);
        INSERT INTO @ConceptsToBind SELECT ConceptId FROM @InsertedConcepts;
        
        DECLARE @AtomBindings TABLE (
            AtomId BIGINT,
            ConceptId BIGINT,
            Similarity FLOAT,
            IsPrimary BIT
        );
        
        -- For each concept, find matching atoms
        DECLARE @CurrentConceptId BIGINT;
        DECLARE @ConceptCentroid VARBINARY(MAX);
        
        DECLARE concept_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT ConceptId, Centroid FROM @InsertedConcepts;
        
        OPEN concept_cursor;
        FETCH NEXT FROM concept_cursor INTO @CurrentConceptId, @ConceptCentroid;
        
        WHILE @@FETCH_STATUS = 0
        BEGIN
            -- Use enterprise CLR function that properly handles binary VECTOR format
            -- fn_BindAtomsToCentroid accepts VARBINARY and handles VECTOR_DISTANCE internally
            INSERT INTO @AtomBindings (AtomId, ConceptId, Similarity, IsPrimary)
            SELECT 
                AtomId,
                @CurrentConceptId AS ConceptId,
                Similarity,
                0 AS IsPrimary -- Will set primary in second pass
            FROM dbo.fn_BindAtomsToCentroid(@ConceptCentroid, @SimilarityThreshold, @TenantId);
            
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
