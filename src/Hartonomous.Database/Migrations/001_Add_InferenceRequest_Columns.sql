-- =====================================================
-- Migration: Add Missing Columns to InferenceRequest
-- =====================================================
-- Adds columns required by sp_RunInference:
-- - TenantId: Multi-tenancy support
-- - Temperature: Sampling temperature (0.0-2.0)
-- - TopK: Top-K sampling parameter
-- - TopP: Nucleus sampling parameter (0.0-1.0)
-- - MaxTokens: Maximum sequence length
-- =====================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.InferenceRequest') 
    AND name = 'TenantId'
)
BEGIN
    ALTER TABLE dbo.InferenceRequest
    ADD TenantId INT NOT NULL DEFAULT 0;
    
    PRINT 'Added TenantId column to InferenceRequest';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.InferenceRequest') 
    AND name = 'Temperature'
)
BEGIN
    ALTER TABLE dbo.InferenceRequest
    ADD Temperature FLOAT NULL;
    
    PRINT 'Added Temperature column to InferenceRequest';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.InferenceRequest') 
    AND name = 'TopK'
)
BEGIN
    ALTER TABLE dbo.InferenceRequest
    ADD TopK INT NULL;
    
    PRINT 'Added TopK column to InferenceRequest';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.InferenceRequest') 
    AND name = 'TopP'
)
BEGIN
    ALTER TABLE dbo.InferenceRequest
    ADD TopP FLOAT NULL;
    
    PRINT 'Added TopP column to InferenceRequest';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.InferenceRequest') 
    AND name = 'MaxTokens'
)
BEGIN
    ALTER TABLE dbo.InferenceRequest
    ADD MaxTokens INT NULL;
    
    PRINT 'Added MaxTokens column to InferenceRequest';
END
GO

-- Add indexes for query performance
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE object_id = OBJECT_ID('dbo.InferenceRequest') 
    AND name = 'IX_InferenceRequest_TenantId_RequestTimestamp'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_InferenceRequest_TenantId_RequestTimestamp
    ON dbo.InferenceRequest(TenantId, RequestTimestamp DESC)
    INCLUDE (Status, TotalDurationMs);
    
    PRINT 'Created index IX_InferenceRequest_TenantId_RequestTimestamp';
END
GO

PRINT 'InferenceRequest schema migration completed successfully';
GO
