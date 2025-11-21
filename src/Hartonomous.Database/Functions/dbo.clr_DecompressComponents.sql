-- =============================================
-- CLR Function: fn_DecompressComponents (TVF)
-- Description: Decompresses stream components
-- =============================================
CREATE FUNCTION [dbo].[fn_DecompressComponents]
(
    @componentStream VARBINARY(MAX)
)
RETURNS TABLE
(
    ComponentIndex INT,
    ComponentType NVARCHAR(50),
    Timestamp DATETIME2,
    Data VARBINARY(MAX)
)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.StreamOrchestrator].[fn_DecompressComponents]
GO
