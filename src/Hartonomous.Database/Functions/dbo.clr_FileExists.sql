-- =============================================
-- CLR Function: clr_FileExists
-- Description: Checks if file exists on disk
-- =============================================
CREATE FUNCTION [dbo].[clr_FileExists]
(
    @filePath NVARCHAR(MAX)
)
RETURNS BIT
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.FileSystemFunctions].[FileExists]
GO
