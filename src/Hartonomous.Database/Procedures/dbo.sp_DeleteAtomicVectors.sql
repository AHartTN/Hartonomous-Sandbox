-- =============================================
-- Batch delete atomic vectors with cleanup
-- Optionally removes orphaned atoms (ReferenceCount = 0)
-- =============================================
CREATE PROCEDURE dbo.sp_DeleteAtomicVectors
    @SourceAtomIds NVARCHAR(MAX),  -- Comma-separated list
    @RelationType NVARCHAR(128) = 'embedding_dimension',
    @CleanupOrphans BIT = 1,
    @DeletedRelations INT OUTPUT,
    @DeletedAtoms INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    
    BEGIN TRANSACTION;
    
    -- Parse atom IDs
    DECLARE @AtomIds TABLE (AtomId BIGINT PRIMARY KEY);
    
    INSERT INTO @AtomIds (AtomId)
    SELECT CAST([value] AS BIGINT)
    FROM STRING_SPLIT(@SourceAtomIds, ',')
    WHERE TRY_CAST([value] AS BIGINT) IS NOT NULL;
    
    -- Track affected target atoms for cleanup
    DECLARE @AffectedAtoms TABLE (AtomId BIGINT PRIMARY KEY);
    
    INSERT INTO @AffectedAtoms (AtomId)
    SELECT DISTINCT ar.TargetAtomId
    FROM dbo.AtomRelations ar
    INNER JOIN @AtomIds src ON src.AtomId = ar.SourceAtomId
    WHERE ar.RelationType = @RelationType;
    
    -- Delete relations
    DELETE ar
    FROM dbo.AtomRelations ar
    INNER JOIN @AtomIds src ON src.AtomId = ar.SourceAtomId
    WHERE ar.RelationType = @RelationType;
    
    SET @DeletedRelations = @@ROWCOUNT;
    
    -- Update reference counts
    UPDATE a
    SET ReferenceCount = ReferenceCount - 1
    FROM dbo.Atoms a
    INNER JOIN @AffectedAtoms aa ON aa.AtomId = a.AtomId;
    
    -- Cleanup orphaned atoms (optional)
    SET @DeletedAtoms = 0;
    IF @CleanupOrphans = 1
    BEGIN
        DELETE a
        FROM dbo.Atoms a
        INNER JOIN @AffectedAtoms aa ON aa.AtomId = a.AtomId
        WHERE a.ReferenceCount <= 0
          AND a.IsDeleted = 0;
        
        SET @DeletedAtoms = @@ROWCOUNT;
    END
    
    COMMIT TRANSACTION;
    
    RETURN 0;
END
