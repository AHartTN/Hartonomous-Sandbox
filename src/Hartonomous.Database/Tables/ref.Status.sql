-- ==================================================================
-- ref.Status: Temporal Reference Table for Status Codes
-- ==================================================================
-- Purpose: Replace hardcoded status strings with FK-based lookups
-- Values: PENDING, RUNNING, COMPLETED, FAILED, CANCELLED, etc.
-- ==================================================================

CREATE TABLE [ref].[Status]
(
    [StatusId] INT NOT NULL IDENTITY(1,1),
    [Code] VARCHAR(50) NOT NULL,               -- UPPERCASE code for API/code
    [Name] NVARCHAR(100) NOT NULL,             -- Display name
    [Description] NVARCHAR(500) NULL,
    [SortOrder] INT NOT NULL DEFAULT 0,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt] DATETIME2(7) NULL,
    
    -- Temporal columns
    [ValidFrom] DATETIME2(7) GENERATED ALWAYS AS ROW START NOT NULL,
    [ValidTo] DATETIME2(7) GENERATED ALWAYS AS ROW END NOT NULL,
    
    CONSTRAINT [PK_Status] PRIMARY KEY CLUSTERED ([StatusId]),
    CONSTRAINT [UQ_Status_Code] UNIQUE NONCLUSTERED ([Code]),
    CONSTRAINT [UQ_Status_Name] UNIQUE NONCLUSTERED ([Name]),
    CONSTRAINT [CK_Status_Code] CHECK ([Code] = UPPER([Code]) AND [Code] NOT LIKE '%[^A-Z0-9_]%'),
    
    PERIOD FOR SYSTEM_TIME ([ValidFrom], [ValidTo])
) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [ref].[Status_History]));
GO

-- Covering index for active status lookups
CREATE NONCLUSTERED INDEX [IX_Status_Active_Code]
    ON [ref].[Status]([IsActive], [Code])
    INCLUDE ([StatusId], [Name])
    WHERE [IsActive] = 1;
GO

-- Insert reference data
SET IDENTITY_INSERT [ref].[Status] ON;
INSERT INTO [ref].[Status] (StatusId, Code, Name, Description, SortOrder)
VALUES
    (1, 'PENDING', 'Pending', 'Waiting to start', 1),
    (2, 'RUNNING', 'Running', 'Currently executing', 2),
    (3, 'COMPLETED', 'Completed', 'Successfully finished', 3),
    (4, 'FAILED', 'Failed', 'Execution failed', 4),
    (5, 'CANCELLED', 'Cancelled', 'User cancelled', 5),
    (6, 'EXECUTED', 'Executed', 'Command executed', 6),
    (7, 'HIGH_SUCCESS', 'High Success', 'High confidence success', 7),
    (8, 'SUCCESS', 'Success', 'Operation successful', 8),
    (9, 'REGRESSED', 'Regressed', 'Performance regression detected', 9),
    (10, 'WARN', 'Warning', 'Completed with warnings', 10);
SET IDENTITY_INSERT [ref].[Status] OFF;
GO
