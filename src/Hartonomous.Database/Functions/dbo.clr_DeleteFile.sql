-- =============================================
-- CLR Function: clr_DeleteFile
-- Description: Deletes file from disk (UNSAFE permission required)
-- =============================================
CREATE FUNCTION [dbo].[clr_DeleteFile]
(
    @filePath NVARCHAR(MAX)
)
RETURNS BIT
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.FileSystemFunctions].[DeleteFile]
GO
