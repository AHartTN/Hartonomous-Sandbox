-- =============================================
-- CLR Function: clr_SvdDecompose
-- Description: Performs SVD decomposition on weight matrix
-- =============================================
CREATE FUNCTION [dbo].[clr_SvdDecompose]
(
    @weightArrayJson NVARCHAR(MAX),
    @rows INT,
    @cols INT,
    @maxRank INT
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.SVDGeometryFunctions].[clr_SvdDecompose]
GO
