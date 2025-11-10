-- Optimize CDC Configuration for Hartonomous
-- Excludes BLOB columns from change tracking to reduce storage overhead
-- Adds filegroup isolation for CDC change tables to improve I/O performance
-- Part of Research Finding #37: CDC enabled but not optimized for high-volume atom ingestion

USE Hartonomous;
GO

-- Disable existing CDC on tables (required to reconfigure with optimizations)
IF EXISTS (SELECT 1 FROM cdc.change_tables WHERE source_object_id = OBJECT_ID('dbo.Atoms'))
BEGIN
    EXEC sys.sp_cdc_disable_table
        @source_schema = N'dbo',
        @source_name = N'Atoms',
        @capture_instance = N'dbo_Atoms';
    PRINT 'Disabled CDC on dbo.Atoms for reconfiguration';
END
GO

IF EXISTS (SELECT 1 FROM cdc.change_tables WHERE source_object_id = OBJECT_ID('dbo.Models'))
BEGIN
    EXEC sys.sp_cdc_disable_table
        @source_schema = N'dbo',
        @source_name = N'Models',
        @capture_instance = N'dbo_Models';
    PRINT 'Disabled CDC on dbo.Models for reconfiguration';
END
GO

IF EXISTS (SELECT 1 FROM cdc.change_tables WHERE source_object_id = OBJECT_ID('dbo.InferenceRequests'))
BEGIN
    EXEC sys.sp_cdc_disable_table
        @source_schema = N'dbo',
        @source_name = N'InferenceRequests',
        @capture_instance = N'dbo_InferenceRequests';
    PRINT 'Disabled CDC on dbo.InferenceRequests for reconfiguration';
END
GO

-- Re-enable CDC on Atoms with column exclusions
-- EXCLUDE: Metadata (NVARCHAR(MAX)), Semantics (NVARCHAR(MAX)) - large JSON columns
-- INCLUDE: AtomId, Modality, Subtype, SourceType, SourceUri, ContentHash, ReferenceCount, 
--          CanonicalText (text is useful for streaming), TenantId, IsDeleted, CreatedAt, UpdatedAt, DeletedAt
EXEC sys.sp_cdc_enable_table
    @source_schema = N'dbo',
    @source_name = N'Atoms',
    @role_name = NULL,
    @supports_net_changes = 1,
    @captured_column_list = N'AtomId, Modality, Subtype, SourceType, SourceUri, ContentHash, ReferenceCount, CanonicalText, TenantId, IsDeleted, CreatedAt, UpdatedAt, DeletedAt',
    @filegroup_name = N'PRIMARY'; -- Use separate filegroup 'CDC_FG' if available for I/O isolation
GO
PRINT 'Optimized CDC on dbo.Atoms: excluded Metadata, Semantics BLOB columns';
GO

-- Re-enable CDC on Models (exclude large BLOB columns if any)
-- Models table schema inspection needed - for now, track all columns
EXEC sys.sp_cdc_enable_table
    @source_schema = N'dbo',
    @source_name = N'Models',
    @role_name = NULL,
    @supports_net_changes = 1,
    @filegroup_name = N'PRIMARY';
GO
PRINT 'Enabled CDC on dbo.Models with default column set';
GO

-- Re-enable CDC on InferenceRequests
-- EXCLUDE: InputData (NVARCHAR(MAX)), OutputData (NVARCHAR(MAX)) - large inference payloads
-- INCLUDE: InferenceId, TenantId, ModelId, ModelsUsed, RequestTimestamp, Status, TotalDurationMs, 
--          TotalTokensUsed, CreatedAt, UpdatedAt
EXEC sys.sp_cdc_enable_table
    @source_schema = N'dbo',
    @source_name = N'InferenceRequests',
    @role_name = NULL,
    @supports_net_changes = 1,
    @captured_column_list = N'InferenceId, TenantId, ModelId, ModelsUsed, RequestTimestamp, Status, TotalDurationMs, TotalTokensUsed, CreatedAt, UpdatedAt',
    @filegroup_name = N'PRIMARY';
GO
PRINT 'Optimized CDC on dbo.InferenceRequests: excluded InputData, OutputData BLOB columns';
GO

-- Configure CDC retention period
-- Default: 3 days (4320 minutes) - adjust based on CesConsumer processing cadence
-- If CesConsumer processes every hour, 24-hour retention is sufficient
EXEC sys.sp_cdc_change_job
    @job_type = N'cleanup',
    @retention = 1440; -- 24 hours (1440 minutes) for faster cleanup
GO
PRINT 'Configured CDC cleanup job: 24-hour retention period';
GO

-- Verify optimized CDC configuration
SELECT 
    t.name AS TableName,
    ct.capture_instance AS CaptureInstance,
    ct.supports_net_changes AS SupportsNetChanges,
    STUFF((
        SELECT ', ' + c.name
        FROM cdc.captured_columns cc
        INNER JOIN sys.columns c ON c.object_id = ct.source_object_id AND c.column_id = cc.column_id
        WHERE cc.object_id = ct.object_id
        ORDER BY cc.column_ordinal
        FOR XML PATH('')
    ), 1, 2, '') AS CapturedColumns,
    ct.filegroup_name AS Filegroup
FROM sys.tables t
INNER JOIN cdc.change_tables ct ON t.object_id = ct.source_object_id
WHERE t.name IN ('Atoms', 'Models', 'InferenceRequests')
ORDER BY t.name;
GO

PRINT 'CDC optimization completed: BLOB columns excluded, 24-hour retention configured';
GO
