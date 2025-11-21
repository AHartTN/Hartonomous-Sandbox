-- =============================================
-- CLR Function: clr_DirectoryExists
-- Description: Checks if directory exists on disk
-- =============================================
CREATE FUNCTION [dbo].[clr_DirectoryExists]
(
    @directoryPath NVARCHAR(MAX)
)
RETURNS BIT
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.FileSystemFunctions].[DirectoryExists]
GO
