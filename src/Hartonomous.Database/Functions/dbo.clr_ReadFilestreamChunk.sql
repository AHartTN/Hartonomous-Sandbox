-- =============================================
-- CLR Function: clr_ReadFilestreamChunk
-- Description: Reads a chunk from FILESTREAM storage
-- =============================================
CREATE FUNCTION [dbo].[clr_ReadFilestreamChunk]
(
    @payloadId UNIQUEIDENTIFIER,
    @offset BIGINT,
    @size BIGINT
)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.ModelIngestionFunctions].[ReadFilestreamChunk]
GO
