-- Creates the provenance.GenerationStreams table to persist AtomicStream payloads.

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'provenance')
BEGIN
    EXEC('CREATE SCHEMA provenance AUTHORIZATION dbo');
END;
GO

IF OBJECT_ID('provenance.GenerationStreams', 'U') IS NULL
BEGIN
    CREATE TABLE provenance.GenerationStreams
    (
        StreamId UNIQUEIDENTIFIER NOT NULL,
        Scope NVARCHAR(128) NULL,
        Model NVARCHAR(128) NULL,
        CreatedUtc DATETIME2(3) NOT NULL CONSTRAINT DF_GenerationStreams_CreatedUtc DEFAULT (SYSUTCDATETIME()),
        Stream provenance.AtomicStream NOT NULL,
        PayloadSizeBytes AS CONVERT(BIGINT, DATALENGTH(Stream)) PERSISTED,
        CONSTRAINT PK_GenerationStreams PRIMARY KEY CLUSTERED (StreamId)
    );
END;
GO

IF COL_LENGTH('provenance.GenerationStreams', 'PayloadSizeBytes') IS NULL
BEGIN
    PRINT 'Adding persisted column provenance.GenerationStreams.PayloadSizeBytes';
    ALTER TABLE provenance.GenerationStreams
    ADD PayloadSizeBytes AS CONVERT(BIGINT, DATALENGTH(Stream)) PERSISTED;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GenerationStreams_Scope' AND object_id = OBJECT_ID('provenance.GenerationStreams'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_GenerationStreams_Scope ON provenance.GenerationStreams (Scope) INCLUDE (CreatedUtc);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_GenerationStreams_Model' AND object_id = OBJECT_ID('provenance.GenerationStreams'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_GenerationStreams_Model ON provenance.GenerationStreams (Model) INCLUDE (CreatedUtc);
END;
GO
