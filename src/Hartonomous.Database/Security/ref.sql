-- ==================================================================
-- ref Schema: Reference Data Schema
-- ==================================================================
-- Purpose: Temporal reference tables for enum replacements
-- ==================================================================

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'ref')
BEGIN
    EXEC('CREATE SCHEMA [ref] AUTHORIZATION dbo');
    PRINT 'Created schema: ref';
END
GO
