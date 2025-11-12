CREATE PROCEDURE dbo.sp_OptimizeEmbeddings
    @ModelId INT,
    @BatchSize INT = 100,
    @MaxAgeHours INT = 24,
    @TenantId INT = NULL -- Optional tenant filtering
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ProcessedCount INT = 0;
    DECLARE @StartTime DATETIME2 = SYSUTCDATETIME();
    
    BEGIN TRY
        -- Find atoms with outdated or missing embeddings
        DECLARE @AtomsToProcess TABLE (
            AtomId BIGINT PRIMARY KEY,
            Content NVARCHAR(MAX)
        );
        
        INSERT INTO @AtomsToProcess
        SELECT TOP (@BatchSize)
            a.AtomId,
            CAST(a.Content AS NVARCHAR(MAX)) AS Content
        FROM dbo.Atoms a
        LEFT JOIN dbo.TenantAtoms ta ON a.AtomId = ta.AtomId
        LEFT JOIN dbo.AtomEmbeddings ae ON a.AtomId = ae.AtomId AND ae.ModelId = @ModelId
        WHERE (@TenantId IS NULL OR ta.TenantId = @TenantId)
              AND a.IsDeleted = 0
              AND (
                  ae.AtomEmbeddingId IS NULL -- Missing embedding
                  OR ae.LastComputedUtc < DATEADD(HOUR, -@MaxAgeHours, SYSUTCDATETIME()) -- Outdated
              )
        ORDER BY a.AtomId;
        
        -- Process each atom
        DECLARE @CurrentAtomId BIGINT;
        DECLARE @CurrentContent NVARCHAR(MAX);
        DECLARE @NewEmbedding VARBINARY(MAX);
        
        DECLARE atom_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT AtomId, Content FROM @AtomsToProcess;
        
        OPEN atom_cursor;
        FETCH NEXT FROM atom_cursor INTO @CurrentAtomId, @CurrentContent;
        
        WHILE @@FETCH_STATUS = 0
        BEGIN
            -- Compute new embedding
            SET @NewEmbedding = dbo.fn_ComputeEmbedding(@CurrentAtomId, @ModelId, @TenantId);
            
            IF @NewEmbedding IS NOT NULL
            BEGIN
                -- Upsert embedding (AtomEmbeddings has no TenantId column)
                MERGE dbo.AtomEmbeddings AS target
                USING (SELECT @CurrentAtomId AS AtomId, @ModelId AS ModelId) AS source
                ON target.AtomId = source.AtomId AND target.ModelId = source.ModelId
                WHEN MATCHED THEN
                    UPDATE SET 
                        EmbeddingVector = @NewEmbedding,
                        LastComputedUtc = SYSUTCDATETIME(),
                        LastAccessedUtc = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT (AtomId, ModelId, EmbeddingVector)
                    VALUES (@CurrentAtomId, @ModelId, @NewEmbedding);
                
                SET @ProcessedCount += 1;
            END
            
            FETCH NEXT FROM atom_cursor INTO @CurrentAtomId, @CurrentContent;
        END
        
        CLOSE atom_cursor;
        DEALLOCATE atom_cursor;
        
        SELECT 
            @ProcessedCount AS EmbeddingsProcessed,
            @BatchSize AS BatchSize,
            DATEDIFF(MILLISECOND, @StartTime, SYSUTCDATETIME()) AS DurationMs;
        
        PRINT 'Embedding optimization complete: ' + CAST(@ProcessedCount AS VARCHAR(10)) + ' embeddings processed';
        
        RETURN 0;
    END TRY
    BEGIN CATCH
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        DECLARE @ErrorState INT = ERROR_STATE();
        
        PRINT 'sp_OptimizeEmbeddings ERROR: ' + @ErrorMessage;
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN -1;
    END CATCH
END;