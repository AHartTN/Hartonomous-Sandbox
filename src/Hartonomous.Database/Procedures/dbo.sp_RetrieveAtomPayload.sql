-- sp_RetrieveAtomPayload: Read large payload from FILESTREAM storage
-- Takes RowGuid as input, returns payload data and metadata

CREATE OR ALTER PROCEDURE dbo.sp_RetrieveAtomPayload
    @RowGuid UNIQUEIDENTIFIER = NULL,
    @PayloadId BIGINT = NULL,
    @AtomId BIGINT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Retrieve payload by RowGuid (fastest), PayloadId (second), or AtomId (slowest)
    SELECT 
        PayloadId,
        RowGuid,
        AtomId,
        ContentType,
        ContentHash,
        SizeBytes,
        PayloadData,
        CreatedUtc,
        CreatedBy
    FROM dbo.AtomPayloadStore
    WHERE 
        (@RowGuid IS NOT NULL AND RowGuid = @RowGuid)
        OR (@PayloadId IS NOT NULL AND PayloadId = @PayloadId)
        OR (@AtomId IS NOT NULL AND AtomId = @AtomId);
    
    RETURN 0;
END;
GO
