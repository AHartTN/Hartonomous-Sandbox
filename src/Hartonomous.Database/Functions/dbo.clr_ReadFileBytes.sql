-- =============================================
-- CLR Function: clr_ReadFileBytes
-- Description: Reads file bytes from disk (UNSAFE permission required)
-- =============================================
CREATE FUNCTION [dbo].[clr_ReadFileBytes]
(
    @filePath NVARCHAR(MAX)
)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.FileSystemFunctions].[ReadFileBytes]
GO
