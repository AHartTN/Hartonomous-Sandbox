-- =============================================
-- CLR Function: clr_WriteFileText
-- Description: Writes text to file on disk (UNSAFE permission required)
-- =============================================
CREATE FUNCTION [dbo].[clr_WriteFileText]
(
    @filePath NVARCHAR(MAX),
    @content NVARCHAR(MAX)
)
RETURNS BIGINT
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.FileSystemFunctions].[WriteFileText]
GO
