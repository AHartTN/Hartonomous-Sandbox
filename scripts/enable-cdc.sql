-- Enable Change Data Capture (CDC) for event streaming to Azure Event Hubs
-- Tracks changes to Atoms, Models, and InferenceRequests for real-time notifications

-- Enable CDC on database
EXEC sys.sp_cdc_enable_db;
GO

-- Enable CDC on Atoms table (track all inserts/updates/deletes)
EXEC sys.sp_cdc_enable_table
    @source_schema = N'dbo',
    @source_name = N'Atoms',
    @role_name = NULL, -- No role-based security
    @supports_net_changes = 1; -- Enable net changes queries
GO

-- Enable CDC on Models table
EXEC sys.sp_cdc_enable_table
    @source_schema = N'dbo',
    @source_name = N'Models',
    @role_name = NULL,
    @supports_net_changes = 1;
GO

-- Enable CDC on InferenceRequests table
EXEC sys.sp_cdc_enable_table
    @source_schema = N'dbo',
    @source_name = N'InferenceRequests',
    @role_name = NULL,
    @supports_net_changes = 1;
GO

-- Verify CDC is enabled
SELECT 
    t.name AS TableName,
    is_tracked_by_cdc,
    OBJECT_NAME(cdc_tables.source_object_id) AS CDCTableName
FROM sys.tables t
LEFT JOIN cdc.change_tables cdc_tables ON t.object_id = cdc_tables.source_object_id
WHERE t.name IN ('Atoms', 'Models', 'InferenceRequests')
ORDER BY t.name;
GO
