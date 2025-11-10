USE Hartonomous;
GO

-- Drop the table if it already exists to ensure a clean slate.
IF OBJECT_ID('dbo.TensorAtomPayloads', 'U') IS NOT NULL
    DROP TABLE dbo.TensorAtomPayloads;
GO

-- =============================================
-- Table: dbo.TensorAtomPayloads
-- =============================================
-- Stores the raw binary payload for a single tensor atom component.
-- This typically represents a vector (e.g., a row from the V matrix in SVD)
-- that is part of a larger decomposed model layer.
--
-- This table uses FILESTREAM to efficiently store the VARBINARY data
-- outside of the main database file, enabling fast, transactional,
-- stream-based access from the SQL CLR.
-- =============================================
CREATE TABLE dbo.TensorAtomPayloads
(
    -- A unique identifier for this payload record.
    PayloadId BIGINT IDENTITY(1,1) NOT NULL,

    -- Foreign key linking this payload back to the main TensorAtom record.
    -- This establishes the one-to-one relationship between the spatial signature
    -- and the actual tensor data.
    TensorAtomId BIGINT NOT NULL,

    -- A ROWGUIDCOL is required for any table with a FILESTREAM column.
    -- It must be a UNIQUEIDENTIFIER column with the UNIQUE constraint.
    RowGuid UNIQUEIDENTIFIER ROWGUIDCOL NOT NULL UNIQUE DEFAULT NEWID(),

    -- The FILESTREAM column itself, which stores the raw tensor data.
    Payload VARBINARY(MAX) FILESTREAM NULL,

    -- Metadata about the payload, such as data type and dimensions.
    -- Stored as JSON for flexibility.
    Metadata NVARCHAR(MAX) NULL,

    CreatedAt DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_TensorAtomPayloads PRIMARY KEY CLUSTERED (PayloadId),

    -- Establishes the foreign key relationship to the main TensorAtom table.
    -- Ensures that a payload cannot exist without a corresponding atom.
    CONSTRAINT FK_TensorAtomPayloads_TensorAtom FOREIGN KEY (TensorAtomId)
        REFERENCES dbo.TensorAtom(TensorAtomId)
        ON DELETE CASCADE -- If the parent atom is deleted, its payload is also deleted.
);
GO

-- Create a unique index on TensorAtomId to enforce the one-to-one relationship
-- and provide fast lookups.
CREATE UNIQUE NONCLUSTERED INDEX IX_TensorAtomPayloads_TensorAtomId
    ON dbo.TensorAtomPayloads(TensorAtomId);
GO

PRINT 'Successfully created dbo.TensorAtomPayloads table with FILESTREAM support.';
GO
