-- =============================================
-- Insert atomic vector with deduplication
-- Automatically deduplicates float values across all vectors
-- =============================================
CREATE PROCEDURE dbo.sp_InsertAtomicVector
    @SourceAtomId BIGINT,
    @VectorJson NVARCHAR(MAX),
    @SpatialX FLOAT = NULL,
    @SpatialY FLOAT = NULL,
    @SpatialZ FLOAT = NULL,
    @RelationType NVARCHAR(128) = 'embedding_dimension',
    @TenantId INT = 0,
    @AtomCount INT OUTPUT,
    @RelationCount INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    
    BEGIN TRANSACTION;
    
    -- Parse vector components
    DECLARE @Components TABLE (
        ComponentIndex INT NOT NULL,
        ComponentValue FLOAT NOT NULL,
        ContentHash BINARY(32) NOT NULL,
        AtomId BIGINT NULL,
        PRIMARY KEY (ComponentIndex)
    );
    
    INSERT INTO @Components (ComponentIndex, ComponentValue, ContentHash)
    SELECT 
        ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS ComponentIndex,
        CAST([value] AS FLOAT) AS ComponentValue,
        HASHBYTES('SHA2_256', CAST(CAST([value] AS FLOAT) AS BINARY(8))) AS ContentHash
    FROM OPENJSON(@VectorJson);
    
    -- Find or create atomic float values (bulk upsert)
    MERGE dbo.Atoms AS target
    USING @Components AS source
    ON target.ContentHash = source.ContentHash
    WHEN NOT MATCHED THEN
        INSERT (ContentHash, Modality, Subtype, AtomicValue, CanonicalText, TenantId)
        VALUES (
            source.ContentHash,
            'numeric',
            'float64',
            CAST(source.ComponentValue AS VARBINARY(8)),
            CAST(source.ComponentValue AS NVARCHAR(50)),
            @TenantId
        );
    
    -- Get AtomIds for all components
    UPDATE c
    SET c.AtomId = a.AtomId
    FROM @Components c
    INNER JOIN dbo.Atoms a ON a.ContentHash = c.ContentHash;
    
    SET @AtomCount = @@ROWCOUNT;
    
    -- Compute spatial bucket if coordinates provided
    DECLARE @SpatialBucket BIGINT = NULL;
    IF @SpatialX IS NOT NULL AND @SpatialY IS NOT NULL AND @SpatialZ IS NOT NULL
    BEGIN
        SET @SpatialBucket = dbo.fn_ComputeSpatialBucket(@SpatialX, @SpatialY, @SpatialZ);
    END
    
    -- Create atomic relations
    INSERT INTO dbo.AtomRelations (
        SourceAtomId,
        TargetAtomId,
        RelationType,
        SequenceIndex,
        Weight,
        Importance,
        Confidence,
        SpatialBucket,
        CoordX,
        CoordY,
        CoordZ,
        TenantId
    )
    SELECT 
        @SourceAtomId,
        c.AtomId,
        @RelationType,
        c.ComponentIndex,
        1.0,
        ABS(c.ComponentValue),
        1.0,
        @SpatialBucket,
        @SpatialX,
        @SpatialY,
        @SpatialZ,
        @TenantId
    FROM @Components c
    WHERE c.AtomId IS NOT NULL;
    
    SET @RelationCount = @@ROWCOUNT;
    
    -- Update reference counts
    UPDATE a
    SET ReferenceCount = ReferenceCount + 1
    FROM dbo.Atoms a
    INNER JOIN @Components c ON c.AtomId = a.AtomId;
    
    COMMIT TRANSACTION;
    
    RETURN 0;
END
