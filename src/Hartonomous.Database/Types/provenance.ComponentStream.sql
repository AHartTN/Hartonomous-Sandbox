-- =============================================
-- User-Defined Type: provenance.ComponentStream
-- Description: CLR UDT for component stream provenance tracking
-- =============================================

CREATE TYPE [provenance].[ComponentStream]
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.ComponentStream];
GO
