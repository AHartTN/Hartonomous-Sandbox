-- =============================================
-- Reconstruct VECTOR from atomic components
-- Fast reconstruction using indexed view (O(n) single scan)
-- =============================================
CREATE PROCEDURE dbo.sp_ReconstructVector
    @SourceAtomId BIGINT,
    @VectorJson NVARCHAR(MAX) OUTPUT,
    @Dimension INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Fast reconstruction using indexed view (contiguous clustered scan)
    DECLARE @Components TABLE (
        ComponentIndex INT NOT NULL,
        ComponentValue FLOAT NOT NULL,
        PRIMARY KEY CLUSTERED (ComponentIndex)
    );
    
    INSERT INTO @Components (ComponentIndex, ComponentValue)
    SELECT ComponentIndex, ComponentValue
    FROM dbo.vw_EmbeddingVectors
    WHERE SourceAtomId = @SourceAtomId
    ORDER BY ComponentIndex;
    
    SET @Dimension = @@ROWCOUNT;
    
    IF @Dimension = 0
    BEGIN
        SET @VectorJson = NULL;
        RETURN -1;
    END
    
    -- Build JSON array: [0.123, -0.456, ...]
    -- Use FOR JSON PATH for enterprise-grade escaping
    SELECT @VectorJson = (
        SELECT ComponentValue
        FROM @Components
        ORDER BY ComponentIndex
        FOR JSON PATH
    );
    
    -- Strip outer object wrapper to get pure array
    SET @VectorJson = JSON_QUERY(@VectorJson, '$');
    
    RETURN 0;
END
