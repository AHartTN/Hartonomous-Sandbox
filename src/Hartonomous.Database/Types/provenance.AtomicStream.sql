-- AtomicStream UDT for provenance tracking
CREATE TYPE [provenance].[AtomicStream]
EXTERNAL NAME [SqlClrFunctions].[SqlClrFunctions.AtomicStream];
