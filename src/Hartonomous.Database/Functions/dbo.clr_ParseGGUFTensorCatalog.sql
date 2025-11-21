-- =============================================
-- CLR Function: clr_ParseGGUFTensorCatalog (TVF)
-- Description: Parses GGUF model format tensor catalog
-- =============================================
CREATE FUNCTION [dbo].[clr_ParseGGUFTensorCatalog]
(
    @payloadId UNIQUEIDENTIFIER
)
RETURNS TABLE
(
    TensorName NVARCHAR(255),
    TensorType NVARCHAR(50),
    Offset BIGINT,
    Size BIGINT,
    Shape NVARCHAR(MAX)
)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.ModelIngestionFunctions].[ParseGGUFTensorCatalog]
GO
