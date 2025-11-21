-- =============================================
-- CLR Function: clr_ComputeSemanticFeatures
-- Description: Computes semantic features from input text
-- =============================================
CREATE FUNCTION [dbo].[clr_ComputeSemanticFeatures]
(
    @input NVARCHAR(MAX)
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.SemanticAnalysis].[ComputeSemanticFeatures]
GO
