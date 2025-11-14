-- =============================================
-- Register CLR Functions for Model Parsing
-- =============================================
-- This script registers the CLR table-valued function (TVF)
-- used for extracting model weights.
-- =============================================

-- =============================================
-- Register CLR Functions for Model Parsing
-- =============================================
-- This script registers the CLR table-valued function (TVF)
-- used for extracting model weights.
-- =============================================

CREATE FUNCTION dbo.clr_ExtractModelWeights(@modelFormat nvarchar(50), @modelData varbinary(max))
RETURNS TABLE (
    TensorName nvarchar(255),
    LayerIndex int,
    WeightIndex bigint,
    WeightValue float
)
AS EXTERNAL NAME SqlClrFunctions.[Hartonomous.Clr.ModelWeightExtractor].ExtractModelWeights;
GO
