-- =============================================
-- sp_AtomizeModel_Governed: Governed, Chunked Model Weight Atomization
-- =============================================
-- Implements the T-SQL Governor state machine for resumable, quota-enforced ingestion
-- Uses IngestionJobs table to track progress and enforce governance
-- =============================================

CREATE PROCEDURE [dbo].[sp_AtomizeModel_Governed]
    @IngestionJobId BIGINT,
    @ModelData VARBINARY(MAX),
    @ModelFormat VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @JobStatus VARCHAR(50), @AtomChunkSize INT, @CurrentAtomOffset BIGINT;
    DECLARE @AtomQuota BIGINT, @TotalAtomsProcessed BIGINT;
    DECLARE @ParentAtomId BIGINT, @ModelId INT;
    DECLARE @RowsInChunk BIGINT;
    DECLARE @TenantId INT; -- V3: Added for tenancy

    -- 1. Load job state and governance parameters
    SELECT
        @JobStatus = JobStatus,
        @AtomChunkSize = AtomChunkSize,
        @CurrentAtomOffset = CurrentAtomOffset,
        @AtomQuota = AtomQuota,
        @TotalAtomsProcessed = TotalAtomsProcessed,
        @ParentAtomId = ParentAtomId,
        @ModelId = ModelId,
        @TenantId = TenantId -- V3: Retrieve TenantId from the job
    FROM dbo.IngestionJobs
    WHERE IngestionJobId = @IngestionJobId;

    IF @JobStatus IS NULL
    BEGIN
        RAISERROR('IngestionJobId not found.', 16, 1);
        RETURN -1;
    END

    IF @JobStatus = 'Complete' OR @JobStatus = 'Processing'
    BEGIN
        RAISERROR('Job is already complete or in progress.', 16, 1);
        RETURN -1;
    END

    UPDATE dbo.IngestionJobs 
    SET JobStatus = 'Processing', LastUpdatedAt = SYSUTCDATETIME() 
    WHERE IngestionJobId = @IngestionJobId;

    -- 2. Create temp tables for batch processing
    CREATE TABLE #ChunkWeights (
        [LayerIdx] INT,
        [PositionX] INT,
        [PositionY] INT,
        [PositionZ] INT,
        [Value] REAL
    );
    
    CREATE TABLE #UniqueWeights (
        [Value] REAL PRIMARY KEY,
        [AtomicValue] VARBINARY(4) NOT NULL,
        [ContentHash] BINARY(32) NOT NULL
    );
    
    CREATE TABLE #WeightToAtomId (
        [Value] REAL PRIMARY KEY,
        [AtomId] BIGINT NOT NULL
    );
    
    CREATE TABLE #ChunkCounts (
        [Value] REAL PRIMARY KEY,
        [Count] BIGINT NOT NULL
    );

    -- 3. Begin Governed State Machine Loop
    WHILE (1 = 1)
    BEGIN
        BEGIN TRY
            -- 3a. Check Governance
            IF @TotalAtomsProcessed > @AtomQuota
            BEGIN
                UPDATE dbo.IngestionJobs 
                SET JobStatus = 'Failed', 
                    ErrorMessage = 'Atom quota exceeded.',
                    LastUpdatedAt = SYSUTCDATETIME()
                WHERE IngestionJobId = @IngestionJobId;
                
                RAISERROR('Atom quota exceeded.', 16, 1);
                BREAK;
            END

            -- 3b. Clear temp tables for this chunk
            TRUNCATE TABLE #ChunkWeights;
            TRUNCATE TABLE #UniqueWeights;
            TRUNCATE TABLE #WeightToAtomId;
            TRUNCATE TABLE #ChunkCounts;

            -- 3c. Get ONE chunk from CLR streaming function
            INSERT INTO #ChunkWeights ([LayerIdx], [PositionX], [PositionY], [PositionZ], [Value])
            SELECT [LayerIdx], [PositionX], [PositionY], [PositionZ], [Value]
            FROM [dbo].[clr_StreamAtomicWeights_Chunked](
                @ModelData, 
                @ModelFormat, 
                @CurrentAtomOffset, 
                @AtomChunkSize
            );

            SET @RowsInChunk = @@ROWCOUNT;
            
            IF @RowsInChunk = 0
                BREAK; -- Finished streaming all weights

            -- 3d. Get unique atoms and counts *for this chunk*
            INSERT INTO #UniqueWeights ([Value], [AtomicValue], [ContentHash])
            SELECT DISTINCT 
                [Value], 
                CAST([Value] AS VARBINARY(4)), 
                HASHBYTES('SHA2_256', CAST([Value] AS VARBINARY(4)))
            FROM #ChunkWeights;

            INSERT INTO #ChunkCounts ([Value], [Count])
            SELECT [Value], COUNT_BIG(*) 
            FROM #ChunkWeights 
            GROUP BY [Value];

            -- 3e. Begin small, fast transaction
            BEGIN TRANSACTION;

                -- 3f. Merge unique weights into dbo.Atoms (deduplication)
                MERGE [dbo].[Atoms] AS T
                USING #UniqueWeights AS S
                ON T.[ContentHash] = S.[ContentHash]
                WHEN NOT MATCHED BY TARGET THEN
                    INSERT ([Modality], [Subtype], [ContentHash], [AtomicValue], [ReferenceCount], [TenantId])
                    VALUES ('model', 'float32-weight', S.[ContentHash], S.[AtomicValue], 0, @TenantId);

                -- 3g. Update reference counts atomically
                UPDATE a
                SET a.[ReferenceCount] = a.[ReferenceCount] + cc.[Count]
                FROM [dbo].[Atoms] a
                JOIN #UniqueWeights uw ON a.[ContentHash] = uw.[ContentHash]
                JOIN #ChunkCounts cc ON uw.[Value] = cc.[Value]
                WHERE a.[Modality] = 'model' AND a.[Subtype] = 'float32-weight';

                -- 3h. Get AtomIds for this chunk
                INSERT INTO #WeightToAtomId ([Value], [AtomId])
                SELECT uw.[Value], a.[AtomId]
                FROM #UniqueWeights uw
                JOIN [dbo].[Atoms] a ON a.[ContentHash] = uw.[ContentHash]
                WHERE a.[Modality] = 'model' AND a.[Subtype] = 'float32-weight';

                -- 3i. Insert the reconstruction data for this chunk into TensorAtomCoefficients
                INSERT INTO [dbo].[TensorAtomCoefficients] (
                    [TensorAtomId], 
                    [ModelId], 
                    [LayerIdx], 
                    [PositionX], 
                    [PositionY], 
                    [PositionZ]
                )
                SELECT 
                    wta.[AtomId],
                    @ModelId,
                    s.[LayerIdx],
                    s.[PositionX],
                    s.[PositionY],
                    s.[PositionZ]
                FROM #ChunkWeights s
                JOIN #WeightToAtomId wta ON s.[Value] = wta.[Value];

            COMMIT TRANSACTION;

            -- 3j. Update state and log progress
            SET @CurrentAtomOffset = @CurrentAtomOffset + @AtomChunkSize;
            SET @TotalAtomsProcessed = @TotalAtomsProcessed + @RowsInChunk;
            
            UPDATE dbo.IngestionJobs 
            SET CurrentAtomOffset = @CurrentAtomOffset, 
                TotalAtomsProcessed = @TotalAtomsProcessed, 
                LastUpdatedAt = SYSUTCDATETIME()
            WHERE IngestionJobId = @IngestionJobId;

        END TRY
        BEGIN CATCH
            IF (XACT_STATE() <> 0) ROLLBACK TRANSACTION;
            
            DECLARE @Error NVARCHAR(MAX) = ERROR_MESSAGE();
            UPDATE dbo.IngestionJobs 
            SET JobStatus = 'Failed', 
                ErrorMessage = @Error,
                LastUpdatedAt = SYSUTCDATETIME()
            WHERE IngestionJobId = @IngestionJobId;
            
            -- Cleanup temp tables
            DROP TABLE IF EXISTS #ChunkWeights;
            DROP TABLE IF EXISTS #UniqueWeights;
            DROP TABLE IF EXISTS #WeightToAtomId;
            DROP TABLE IF EXISTS #ChunkCounts;
            
            RAISERROR(@Error, 16, 1);
            RETURN -1;
        END CATCH
    END -- End WHILE

    -- Mark job as complete
    IF @JobStatus <> 'Failed'
        UPDATE dbo.IngestionJobs 
        SET JobStatus = 'Complete', 
            LastUpdatedAt = SYSUTCDATETIME() 
        WHERE IngestionJobId = @IngestionJobId;

    -- Cleanup temp tables
    DROP TABLE #ChunkWeights;
    DROP TABLE #UniqueWeights;
    DROP TABLE #WeightToAtomId;
    DROP TABLE #ChunkCounts;

    RETURN 0;
END
GO
