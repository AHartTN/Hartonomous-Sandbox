-- =============================================
-- Enable Change Data Capture (CDC)
-- =============================================

USE Hartonomous;
GO

-- Enable CDC on the database
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'Hartonomous' AND is_cdc_enabled = 1)
BEGIN
    EXEC sys.sp_cdc_enable_db;
    PRINT 'CDC enabled on database Hartonomous.';
END
GO

-- Enable CDC on the Models table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Models' AND is_tracked_by_cdc = 1)
BEGIN
    EXEC sys.sp_cdc_enable_table
        @source_schema = N'dbo',
        @source_name   = N'Models',
        @role_name     = NULL; -- Use NULL for the role to allow public access
    PRINT 'CDC enabled on table dbo.Models.';
END
GO
