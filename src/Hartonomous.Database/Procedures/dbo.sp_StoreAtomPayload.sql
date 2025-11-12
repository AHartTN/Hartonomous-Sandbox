-- sp_StoreAtomPayload: Write large payload to FILESTREAM storage
-- Returns RowGuid for later retrieval
-- Uses transaction context required by SqlFileStream API

CREATE PROCEDURE dbo.sp_StoreAtomPayload
    @AtomId BIGINT,
    @ContentType NVARCHAR(256),
    @PayloadData VARBINARY(MAX),
    @CreatedBy NVARCHAR(256) = NULL,
    @RowGuid UNIQUEIDENTIFIER OUTPUT,
    @PayloadId BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Calculate content hash for deduplication
    DECLARE @ContentHash BINARY(32) = HASHBYTES('SHA2_256', @PayloadData);
    DECLARE @SizeBytes BIGINT = DATALENGTH(@PayloadData);
    
    -- Check if identical payload already exists (deduplication)
    SELECT TOP 1 
        @RowGuid = RowGuid,
        @PayloadId = PayloadId
    FROM dbo.AtomPayloadStore
    WHERE ContentHash = @ContentHash;
    
    IF @RowGuid IS NOT NULL
    BEGIN
        -- Payload already exists, return existing RowGuid
        RETURN 0;
    END
    
    -- Insert new payload
    INSERT INTO dbo.AtomPayloadStore (
        AtomId,
        ContentType,
        ContentHash,
        SizeBytes,
        PayloadData,
        CreatedBy
    )
    VALUES (
        @AtomId,
        @ContentType,
        @ContentHash,
        @SizeBytes,
        @PayloadData,
        @CreatedBy
    );
    
    -- Return RowGuid and PayloadId
    SELECT 
        @RowGuid = RowGuid,
        @PayloadId = PayloadId
    FROM dbo.AtomPayloadStore
    WHERE PayloadId = SCOPE_IDENTITY();
    
    RETURN 0;
END;
GO
