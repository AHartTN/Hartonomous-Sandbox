-- =============================================
-- CLR Function: clr_ExecuteModelInference
-- Description: Executes inference using a model on embedding vector
-- =============================================
CREATE FUNCTION [dbo].[clr_ExecuteModelInference]
(
    @modelId INT,
    @embeddingVector VARBINARY(MAX)
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.ModelInference].[ExecuteModelInference]
GO
