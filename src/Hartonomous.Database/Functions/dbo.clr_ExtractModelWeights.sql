-- =============================================
-- Register CLR Functions for Model Parsing
-- =============================================
-- This script registers the CLR table-valued function (TVF)
-- used for extracting model weights.
-- =============================================

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE name = 'clr_ExtractModelWeights' AND type = 'FT')
BEGIN
    PRINT 'Creating CLR function dbo.clr_ExtractModelWeights...';
    CREATE FUNCTION dbo.clr_ExtractModelWeights(@modelFormat nvarchar(50), @modelData varbinary(max))
    RETURNS TABLE (
        TensorName nvarchar(255),
        LayerIndex int,
        WeightIndex bigint,
        WeightValue float
    )
    AS EXTERNAL NAME SqlClrFunctions.[Hartonomous.Clr.ModelWeightExtractor].ExtractModelWeights;
END
GO

PRINT 'CLR model parsing functions registered.';
GO
