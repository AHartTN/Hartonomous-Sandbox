-- Ensures the provenance schema exists and binds the CLR AtomicStream UDT.

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'provenance')
BEGIN
    EXEC('CREATE SCHEMA provenance AUTHORIZATION dbo');
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.types WHERE name = 'AtomicStream' AND schema_id = SCHEMA_ID('provenance'))
BEGIN
    CREATE TYPE provenance.AtomicStream
    EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.AtomicStream];

    PRINT 'provenance.AtomicStream type created.';
END
ELSE
BEGIN
    PRINT 'provenance.AtomicStream type already exists; skipping creation.';
END;
GO
