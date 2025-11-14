-- ==================================================================
-- ref.Status_History: Temporal history for Status reference table
-- ==================================================================

CREATE TABLE [ref].[Status_History]
(
    [StatusId] INT NOT NULL,
    [Code] VARCHAR(50) NOT NULL,
    [Name] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [SortOrder] INT NOT NULL,
    [IsActive] BIT NOT NULL,
    [CreatedAt] DATETIME2(7) NOT NULL,
    [UpdatedAt] DATETIME2(7) NULL,
    [ValidFrom] DATETIME2(7) NOT NULL,
    [ValidTo] DATETIME2(7) NOT NULL
);
GO

-- Clustered index on ValidFrom for temporal queries
CREATE CLUSTERED INDEX [IX_Status_History_Period]
    ON [ref].[Status_History]([ValidFrom], [ValidTo]);
GO
