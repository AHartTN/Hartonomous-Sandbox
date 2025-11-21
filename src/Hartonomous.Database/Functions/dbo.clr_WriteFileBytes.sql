-- =============================================
-- CLR Function: clr_WriteFileBytes
-- Description: Writes bytes to file on disk (UNSAFE permission required)
-- =============================================
CREATE FUNCTION [dbo].[clr_WriteFileBytes]
(
    @filePath NVARCHAR(MAX),
    @content VARBINARY(MAX)
)
RETURNS BIGINT
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.FileSystemFunctions].[WriteFileBytes]
GO
