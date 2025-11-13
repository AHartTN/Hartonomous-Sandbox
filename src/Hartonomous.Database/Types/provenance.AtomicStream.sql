-- =============================================
-- User-Defined Type: provenance.AtomicStream
-- Description: CLR UDT for atomic stream provenance tracking
-- =============================================

CREATE TYPE [provenance].[AtomicStream]
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.AtomicStream];
GO
