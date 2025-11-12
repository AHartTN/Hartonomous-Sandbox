USE [$(DatabaseName)]
GO

-- Enable CDC on database
IF (SELECT is_cdc_enabled FROM sys.databases WHERE name = DB_NAME()) = 0
BEGIN
    EXEC sys.sp_cdc_enable_db;
    PRINT 'CDC enabled for database [$(DatabaseName)].';
END
ELSE
BEGIN
    PRINT 'CDC already enabled for database [$(DatabaseName)].';
END
GO

-- Enable CDC on Atoms table (track all inserts/updates/deletes)
IF NOT EXISTS (SELECT 1 FROM cdc.change_tables WHERE source_schema = N'dbo' AND source_name = N'Atoms')
BEGIN
    EXEC sys.sp_cdc_enable_table
        @source_schema = N'dbo',
        @source_name = N'Atoms',
        @role_name = NULL, -- No role-based security
        @supports_net_changes = 1; -- Enable net changes queries
    PRINT 'CDC enabled for table dbo.Atoms.';
END
ELSE
BEGIN
    PRINT 'CDC already enabled for table dbo.Atoms.';
END
GO

-- Enable CDC on Models table
IF NOT EXISTS (SELECT 1 FROM cdc.change_tables WHERE source_schema = N'dbo' AND source_name = N'Models')
BEGIN
    EXEC sys.sp_cdc_enable_table
        @source_schema = N'dbo',
        @source_name = N'Models',
        @role_name = NULL,
        @supports_net_changes = 1;
    PRINT 'CDC enabled for table dbo.Models.';
END
ELSE
BEGIN
    PRINT 'CDC already enabled for table dbo.Models.';
END
GO

-- Enable CDC on InferenceRequests table
IF NOT EXISTS (SELECT 1 FROM cdc.change_tables WHERE source_schema = N'dbo' AND source_name = N'InferenceRequests')
BEGIN
    EXEC sys.sp_cdc_enable_table
        @source_schema = N'dbo',
        @source_name = N'InferenceRequests',
        @role_name = NULL,
        @supports_net_changes = 1;
    PRINT 'CDC enabled for table dbo.InferenceRequests.';
END
ELSE
BEGIN
    PRINT 'CDC already enabled for table dbo.InferenceRequests.';
END
GO