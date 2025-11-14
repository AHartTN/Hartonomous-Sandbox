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

