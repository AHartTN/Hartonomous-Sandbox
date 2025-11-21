-- =============================================
-- CLR Function: clr_FindPrimes
-- Description: Finds prime numbers in a range (used by OODA loop)
-- =============================================
CREATE FUNCTION [dbo].[clr_FindPrimes]
(
    @rangeStart BIGINT,
    @rangeEnd BIGINT
)
RETURNS NVARCHAR(MAX)
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.PrimeNumberSearch].[clr_FindPrimes]
GO
