-- =============================================
-- CLR Function: clr_ReadFileText
-- Description: Reads file text from disk (UNSAFE permission required)
-- =============================================
CREATE FUNCTION [dbo].[clr_ReadFileText]
(
    @filePath NVARCHAR(MAX)
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.FileSystemFunctions].[ReadFileText]
GO
