-- =============================================
-- CLR Function: clr_GenerateCodeAstVector
-- Description: Generates AST vector from source code
-- =============================================
CREATE FUNCTION [dbo].[clr_GenerateCodeAstVector]
(
    @sourceCode NVARCHAR(MAX)
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.CodeAnalysis].[clr_GenerateCodeAstVector]
GO
