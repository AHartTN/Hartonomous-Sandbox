-- Registers the provenance.ComponentStream CLR user-defined type for component bill of materials storage.

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'provenance')
BEGIN
    EXEC('CREATE SCHEMA provenance AUTHORIZATION dbo');
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.types WHERE name = 'ComponentStream' AND schema_id = SCHEMA_ID('provenance'))
BEGIN
    CREATE TYPE provenance.ComponentStream
    EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.ComponentStream];

    PRINT 'provenance.ComponentStream type created.';
END
ELSE
BEGIN
    PRINT 'provenance.ComponentStream type already exists; skipping creation.';
END;
GO
