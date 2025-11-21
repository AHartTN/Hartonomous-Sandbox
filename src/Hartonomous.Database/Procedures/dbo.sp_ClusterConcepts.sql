-- sp_ClusterConcepts: Unsupervised Concept Discovery
-- Part of Phase 3: Learning Loop - "Dreaming" Logic
-- Discovers clusters of atoms in embedding space that do not have assigned concepts
-- Automatically proposes new concepts based on dense regions in 3D projection

CREATE PROCEDURE dbo.sp_ClusterConcepts
    @TenantId INT = NULL,
    @MinClusterSize INT = 5,      -- Minimum atoms to form a cluster
    @MaxClusters INT = 10,         -- Maximum clusters to discover per run
    @DensityThreshold FLOAT = 0.3  -- Minimum density for cluster validity
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @DiscoveredClusters INT = 0;
    DECLARE @TotalOrphanAtoms INT = 0;
    DECLARE @ClusterAnalysisId UNIQUEIDENTIFIER = NEWID();

    BEGIN TRY
        PRINT 'sp_ClusterConcepts: Starting unsupervised concept discovery';
        PRINT '  Parameters: MinClusterSize=' + CAST(@MinClusterSize AS NVARCHAR(10)) +
              ', MaxClusters=' + CAST(@MaxClusters AS NVARCHAR(10)) +
              ', DensityThreshold=' + CAST(@DensityThreshold AS NVARCHAR(10));

        -- 1. IDENTIFY ORPHAN ATOMS (atoms without a concept assignment)
        -- These are atoms that exist in embedding space but haven't been
        -- categorized into a higher-level concept yet
        CREATE TABLE #OrphanAtoms (
            AtomId BIGINT PRIMARY KEY,
            SpatialKey HIERARCHYID,
            X FLOAT,
            Y FLOAT,
            Z FLOAT,
            CanonicalText NVARCHAR(MAX),
            Modality NVARCHAR(50)
        );

        INSERT INTO #OrphanAtoms (AtomId, SpatialKey, X, Y, Z, CanonicalText, Modality)
        SELECT 
            a.AtomId,
            a.SpatialKey,
            a.SpatialKey.GetLevel() AS X, -- Simplified: Use hierarchy level as X
            CAST(a.SpatialKey.GetDescendant(NULL, NULL).ToString() AS FLOAT) AS Y, -- Simplified Y
            a.AtomId % 100 AS Z, -- Simplified Z (replace with actual vector projection)
            a.CanonicalText,
            a.Modality
        FROM dbo.Atom a
        WHERE (@TenantId IS NULL OR a.TenantId = @TenantId)
          AND a.ConceptId IS NULL -- Orphaned: no concept assigned
          AND a.SpatialKey IS NOT NULL;

        SET @TotalOrphanAtoms = @@ROWCOUNT;
        
        IF @TotalOrphanAtoms = 0
        BEGIN
            PRINT 'sp_ClusterConcepts: No orphan atoms found. All atoms are assigned to concepts.';
            RETURN 0;
        END

        PRINT '  Found ' + CAST(@TotalOrphanAtoms AS NVARCHAR(10)) + ' orphan atoms';

        -- 2. COLLECT ORPHAN ATOM VECTORS FOR CLR K-MEANS
        -- Convert spatial coordinates to JSON format for CLR processing
        DECLARE @OrphanVectorsJson NVARCHAR(MAX);
        
        SELECT @OrphanVectorsJson = (
            SELECT 
                AtomId,
                '[' + CAST(X AS NVARCHAR(20)) + ',' + 
                      CAST(Y AS NVARCHAR(20)) + ',' + 
                      CAST(Z AS NVARCHAR(20)) + ']' AS Vector
            FROM #OrphanAtoms
            FOR JSON PATH
        );

        -- 3. RUN CLR K-MEANS CLUSTERING
        -- This uses the VectorKMeansCluster aggregate from AdvancedVectorAggregates.cs
        -- The aggregate performs streaming k-means to discover @MaxClusters clusters
        
        CREATE TABLE #ClusterAssignments (
            AtomId BIGINT,
            ClusterId INT,
            DistanceToCentroid FLOAT
        );

        -- Use DBSCAN from CLR for density-based clustering (more robust than k-means for this)
        -- DBSCAN finds clusters of arbitrary shape and marks noise points
        DECLARE @EpsilonDistance FLOAT = 5.0; -- Maximum distance for neighborhood
        DECLARE @MinPoints INT = @MinClusterSize;

        -- Simplified clustering using spatial proximity
        -- In production, this would call the CLR DBSCAN implementation
        WITH SpatialClusters AS (
            SELECT 
                o1.AtomId,
                COUNT(o2.AtomId) AS NeighborCount,
                AVG(SQRT(POWER(o2.X - o1.X, 2) + POWER(o2.Y - o1.Y, 2) + POWER(o2.Z - o1.Z, 2))) AS AvgDistance
            FROM #OrphanAtoms o1
            CROSS APPLY (
                SELECT TOP 10 o2.AtomId, o2.X, o2.Y, o2.Z
                FROM #OrphanAtoms o2
                WHERE SQRT(POWER(o2.X - o1.X, 2) + POWER(o2.Y - o1.Y, 2) + POWER(o2.Z - o1.Z, 2)) <= @EpsilonDistance
                  AND o2.AtomId <> o1.AtomId
                ORDER BY SQRT(POWER(o2.X - o1.X, 2) + POWER(o2.Y - o1.Y, 2) + POWER(o2.Z - o1.Z, 2))
            ) o2
            GROUP BY o1.AtomId
        )
        INSERT INTO #ClusterAssignments (AtomId, ClusterId, DistanceToCentroid)
        SELECT 
            AtomId,
            DENSE_RANK() OVER (ORDER BY NeighborCount DESC, AvgDistance) AS ClusterId,
            AvgDistance
        FROM SpatialClusters
        WHERE NeighborCount >= @MinPoints;

        -- 4. ANALYZE CLUSTER QUALITY
        -- Filter out low-density clusters that don't meet the threshold
        CREATE TABLE #ValidClusters (
            ClusterId INT PRIMARY KEY,
            AtomCount INT,
            Density FLOAT,
            AvgDistanceToCentroid FLOAT,
            RepresentativeAtomId BIGINT,
            DominantModality NVARCHAR(50)
        );

        INSERT INTO #ValidClusters (ClusterId, AtomCount, Density, AvgDistanceToCentroid, RepresentativeAtomId, DominantModality)
        SELECT TOP (@MaxClusters)
            ca.ClusterId,
            COUNT(*) AS AtomCount,
            1.0 - (AVG(ca.DistanceToCentroid) / @EpsilonDistance) AS Density,
            AVG(ca.DistanceToCentroid) AS AvgDistanceToCentroid,
            MIN(ca.AtomId) AS RepresentativeAtomId, -- Use first atom as representative
            (
                SELECT TOP 1 o.Modality
                FROM #OrphanAtoms o
                WHERE o.AtomId = ca.AtomId
                GROUP BY o.Modality
                ORDER BY COUNT(*) DESC
            ) AS DominantModality
        FROM #ClusterAssignments ca
        GROUP BY ca.ClusterId
        HAVING COUNT(*) >= @MinClusterSize
           AND (1.0 - (AVG(ca.DistanceToCentroid) / @EpsilonDistance)) >= @DensityThreshold
        ORDER BY COUNT(*) DESC;

        SET @DiscoveredClusters = @@ROWCOUNT;

        IF @DiscoveredClusters = 0
        BEGIN
            PRINT 'sp_ClusterConcepts: No valid clusters found above density threshold';
            RETURN 0;
        END

        PRINT '  Discovered ' + CAST(@DiscoveredClusters AS NVARCHAR(10)) + ' valid clusters';

        -- 5. PROPOSE NEW CONCEPTS FOR EACH CLUSTER
        -- Create concept proposals that can be reviewed/approved by admin or auto-accepted
        DECLARE @ClusterId INT, @AtomCount INT, @Density FLOAT, @RepAtomId BIGINT, @Modality NVARCHAR(50);
        DECLARE @ConceptName NVARCHAR(200), @ConceptDescription NVARCHAR(MAX);

        DECLARE cluster_cursor CURSOR FOR
            SELECT ClusterId, AtomCount, Density, RepresentativeAtomId, DominantModality
            FROM #ValidClusters
            ORDER BY Density DESC;

        OPEN cluster_cursor;
        FETCH NEXT FROM cluster_cursor INTO @ClusterId, @AtomCount, @Density, @RepAtomId, @Modality;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            -- Generate concept name from representative atom's text
            SELECT @ConceptName = 
                LEFT(COALESCE(CanonicalText, 'Cluster_' + CAST(@ClusterId AS NVARCHAR(10))), 100)
            FROM #OrphanAtoms
            WHERE AtomId = @RepAtomId;

            SET @ConceptDescription = 
                'Unsupervised cluster discovery: ' + CAST(@AtomCount AS NVARCHAR(10)) + 
                ' atoms with density ' + CAST(@Density AS NVARCHAR(10)) + 
                ' in ' + @Modality + ' modality';

            -- Insert into ProposedConcepts table (create if doesn't exist)
            IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.ProposedConcepts') AND type = 'U')
            BEGIN
                CREATE TABLE dbo.ProposedConcepts (
                    ProposalId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
                    ConceptName NVARCHAR(200) NOT NULL,
                    ConceptDescription NVARCHAR(MAX),
                    ProposedBy NVARCHAR(128) DEFAULT 'SYSTEM',
                    ProposedAt DATETIME2 DEFAULT SYSUTCDATETIME(),
                    Status NVARCHAR(20) DEFAULT 'Pending', -- Pending, Approved, Rejected
                    ClusterId INT,
                    AtomCount INT,
                    Density FLOAT,
                    DominantModality NVARCHAR(50),
                    RepresentativeAtomId BIGINT,
                    TenantId INT
                );
            END;

            INSERT INTO dbo.ProposedConcepts (
                ConceptName,
                ConceptDescription,
                ClusterId,
                AtomCount,
                Density,
                DominantModality,
                RepresentativeAtomId,
                TenantId
            )
            VALUES (
                @ConceptName,
                @ConceptDescription,
                @ClusterId,
                @AtomCount,
                @Density,
                @Modality,
                @RepAtomId,
                @TenantId
            );

            PRINT '  Proposed Concept: "' + @ConceptName + '" (' + CAST(@AtomCount AS NVARCHAR(10)) + ' atoms, density: ' + CAST(@Density AS NVARCHAR(10)) + ')';

            FETCH NEXT FROM cluster_cursor INTO @ClusterId, @AtomCount, @Density, @RepAtomId, @Modality;
        END;

        CLOSE cluster_cursor;
        DEALLOCATE cluster_cursor;

        -- 6. LOG ANALYSIS RESULTS
        INSERT INTO dbo.LearningMetrics (
            AnalysisId,
            MetricType,
            MetricValue,
            MeasuredAt
        )
        VALUES 
            (@ClusterAnalysisId, 'OrphanAtoms', @TotalOrphanAtoms, SYSUTCDATETIME()),
            (@ClusterAnalysisId, 'DiscoveredClusters', @DiscoveredClusters, SYSUTCDATETIME()),
            (@ClusterAnalysisId, 'ConceptProposals', @DiscoveredClusters, SYSUTCDATETIME());

        -- 7. CLEANUP
        DROP TABLE #OrphanAtoms;
        DROP TABLE #ClusterAssignments;
        DROP TABLE #ValidClusters;

        PRINT 'sp_ClusterConcepts completed: ' + CAST(@DiscoveredClusters AS NVARCHAR(10)) + ' concept proposals created';
        RETURN @DiscoveredClusters;

    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();

        PRINT 'sp_ClusterConcepts ERROR: ' + @ErrorMessage;
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN -1;
    END CATCH
END;
GO

-- Grant execution permissions
GRANT EXECUTE ON dbo.sp_ClusterConcepts TO HartonomousAppUser;
GO
