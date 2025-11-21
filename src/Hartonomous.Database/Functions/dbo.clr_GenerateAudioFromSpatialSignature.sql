-- =============================================
-- CLR Function: clr_GenerateAudioFromSpatialSignature
-- Description: Generates audio from a spatial geometry signature
-- =============================================
CREATE FUNCTION [dbo].[clr_GenerateAudioFromSpatialSignature]
(
    @spatialSignature GEOMETRY
)
RETURNS VARBINARY(MAX)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.AudioProcessing].[GenerateAudioFromSpatialSignature]
GO
