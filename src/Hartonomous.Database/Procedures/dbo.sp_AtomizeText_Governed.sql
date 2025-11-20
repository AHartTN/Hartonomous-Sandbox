-- =============================================
-- sp_AtomizeText_Governed: Governed Text Tokenization and Atomization
-- =============================================
-- Implements chunked, resumable text atomization with XYZM structural storage
-- =============================================

CREATE PROCEDURE [dbo].[sp_AtomizeText_Governed]
    @IngestionJobId BIGINT,
    @TextData NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @JobStatus VARCHAR(50), @AtomChunkSize INT, @CurrentAtomOffset BIGINT;
    DECLARE @AtomQuota BIGINT, @TotalAtomsProcessed BIGINT;
    DECLARE @ParentAtomId BIGINT;
    DECLARE @RowsInChunk BIGINT;
    DECLARE @TenantId INT; -- V3: Added for tenancy

    -- 1. Load job state
    SELECT
        @JobStatus = JobStatus,
        @AtomChunkSize = AtomChunkSize,
        @CurrentAtomOffset = CurrentAtomOffset,
        @AtomQuota = AtomQuota,
        @TotalAtomsProcessed = TotalAtomsProcessed,
        @ParentAtomId = ParentAtomId,
        @TenantId = TenantId -- V3: Retrieve TenantId from the job
    FROM dbo.IngestionJob
    WHERE IngestionJobId = @IngestionJobId;

    IF @JobStatus IS NULL OR @JobStatus IN ('Complete', 'Processing')
    BEGIN
        RAISERROR('Invalid job state.', 16, 1);
        RETURN -1;
    END

    UPDATE dbo.IngestionJob 
    SET JobStatus = 'Processing', LastUpdatedAt = SYSUTCDATETIME() 
    WHERE IngestionJobId = @IngestionJobId;

    -- 2. Create temp tables
    CREATE TABLE #ChunkTokens (
        [TokenId] INT,
        [TokenText] NVARCHAR(256),
        [SequenceIndex] BIGINT
    );
    
    CREATE TABLE #UniqueTokens (
        [TokenText] NVARCHAR(256) PRIMARY KEY,
        [AtomicValue] VARBINARY(64) NOT NULL,
        [ContentHash] BINARY(32) NOT NULL
    );
    
    CREATE TABLE #TokenToAtomId (
        [TokenText] NVARCHAR(256) PRIMARY KEY,
        [AtomId] BIGINT NOT NULL
    );

    -- 3. State machine loop
    WHILE (1 = 1)
    BEGIN
        BEGIN TRY
            -- Check governance
            IF @TotalAtomsProcessed > @AtomQuota
            BEGIN
                UPDATE dbo.IngestionJob 
                SET JobStatus = 'Failed', ErrorMessage = 'Atom quota exceeded.'
                WHERE IngestionJobId = @IngestionJobId;
                RAISERROR('Atom quota exceeded.', 16, 1);
                BREAK;
            END

            -- Clear temp tables
            TRUNCATE TABLE #ChunkTokens;
            TRUNCATE TABLE #UniqueTokens;
            TRUNCATE TABLE #TokenToAtomId;

            -- Tokenize text chunk (simplified - production would use proper tokenizer)
            -- For now, split by whitespace as proof of concept
            DECLARE @ChunkStart BIGINT = @CurrentAtomOffset;
            DECLARE @ChunkEnd BIGINT = @ChunkStart + @AtomChunkSize;
            DECLARE @TextLength BIGINT = LEN(@TextData);
            
            IF @ChunkStart >= @TextLength
                BREAK;

            -- Simple whitespace tokenization (placeholder for real tokenizer)
            DECLARE @ChunkText NVARCHAR(MAX) = SUBSTRING(@TextData, @ChunkStart + 1, @AtomChunkSize);
            DECLARE @Pos INT = 1;
            DECLARE @SeqIdx BIGINT = @ChunkStart;
            
            WHILE @Pos <= LEN(@ChunkText)
            BEGIN
                DECLARE @SpacePos INT = CHARINDEX(' ', @ChunkText, @Pos);
                IF @SpacePos = 0
                    SET @SpacePos = LEN(@ChunkText) + 1;
                
                DECLARE @Token NVARCHAR(256) = SUBSTRING(@ChunkText, @Pos, @SpacePos - @Pos);
                
                IF LEN(@Token) > 0
                BEGIN
                    INSERT INTO #ChunkTokens ([TokenId], [TokenText], [SequenceIndex])
                    VALUES (0, @Token, @SeqIdx);
                    SET @SeqIdx = @SeqIdx + 1;
                END
                
                SET @Pos = @SpacePos + 1;
            END

            SET @RowsInChunk = @@ROWCOUNT;
            
            IF @RowsInChunk = 0
                BREAK;

            -- Get unique tokens
            INSERT INTO #UniqueTokens ([TokenText], [AtomicValue], [ContentHash])
            SELECT DISTINCT 
                [TokenText],
                CAST([TokenText] AS VARBINARY(64)),
                HASHBYTES('SHA2_256', CAST([TokenText] AS VARBINARY(64)))
            FROM #ChunkTokens;

            BEGIN TRANSACTION;

                -- Merge into Atoms
                MERGE [dbo].[Atom] AS T
                USING #UniqueTokens AS S
                ON T.[ContentHash] = S.[ContentHash]
                WHEN NOT MATCHED BY TARGET THEN
                    INSERT ([Modality], [Subtype], [ContentHash], [AtomicValue], [ReferenceCount], [TenantId])
                    VALUES ('text', 'token', S.[ContentHash], S.[AtomicValue], 0, @TenantId);

                -- Get AtomIds
                INSERT INTO #TokenToAtomId ([TokenText], [AtomId])
                SELECT ut.[TokenText], a.[AtomId]
                FROM #UniqueTokens ut
                JOIN [dbo].[Atom] a ON a.[ContentHash] = ut.[ContentHash];

                -- Update reference counts
                UPDATE a
                SET a.[ReferenceCount] = a.[ReferenceCount] + 1
                FROM [dbo].[Atom] a
                JOIN #TokenToAtomId tta ON a.[AtomId] = tta.[AtomId];

                -- Insert structural representation with XYZM spatial key
                INSERT INTO [dbo].[AtomComposition] (
                    [ParentAtomId], 
                    [ComponentAtomId], 
                    [SequenceIndex], 
                    [SpatialKey]
                )
                SELECT 
                    @ParentAtomId,
                    tta.[AtomId],
                    ct.[SequenceIndex],
                    [sys].[geometry]::Point(
                        ct.[SequenceIndex],  -- X = Position
                        tta.[AtomId] % 10000, -- Y = Value (modulo for scaling)
                        0                     -- Z = unused
                    )
                FROM #ChunkTokens ct
                JOIN #TokenToAtomId tta ON ct.[TokenText] = tta.[TokenText];

            COMMIT TRANSACTION;

            -- Update progress
            SET @CurrentAtomOffset = @CurrentAtomOffset + @AtomChunkSize;
            SET @TotalAtomsProcessed = @TotalAtomsProcessed + @RowsInChunk;
            
            UPDATE dbo.IngestionJob 
            SET CurrentAtomOffset = @CurrentAtomOffset, 
                TotalAtomsProcessed = @TotalAtomsProcessed,
                LastUpdatedAt = SYSUTCDATETIME()
            WHERE IngestionJobId = @IngestionJobId;

        END TRY
        BEGIN CATCH
            IF (XACT_STATE() <> 0) ROLLBACK TRANSACTION;
            
            DECLARE @Error NVARCHAR(MAX) = ERROR_MESSAGE();
            UPDATE dbo.IngestionJob 
            SET JobStatus = 'Failed', ErrorMessage = @Error
            WHERE IngestionJobId = @IngestionJobId;
            
            DROP TABLE IF EXISTS #ChunkTokens;
            DROP TABLE IF EXISTS #UniqueTokens;
            DROP TABLE IF EXISTS #TokenToAtomId;
            
            RAISERROR(@Error, 16, 1);
            RETURN -1;
        END CATCH
    END

    -- Mark complete
    UPDATE dbo.IngestionJob 
    SET JobStatus = 'Complete', LastUpdatedAt = SYSUTCDATETIME() 
    WHERE IngestionJobId = @IngestionJobId;

    DROP TABLE #ChunkTokens;
    DROP TABLE #UniqueTokens;
    DROP TABLE #TokenToAtomId;

    RETURN 0;
END
GO
