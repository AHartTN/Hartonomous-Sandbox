-- =============================================
-- CLR Function: clr_StreamOrchestrator
-- Description: Orchestrates multi-modal stream processing
-- =============================================
CREATE FUNCTION [dbo].[clr_StreamOrchestrator]
(
    @streamIds NVARCHAR(MAX),
    @fusionType NVARCHAR(50),
    @weights NVARCHAR(MAX)
)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.StreamOrchestrator].[Orchestrate]
GO
