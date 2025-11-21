-- =============================================
-- Table: dbo.InferenceRequests
-- Description: Master table for all inference requests (alias for InferenceRequest)
-- Purpose: Compatibility table that references the main InferenceRequest table
-- Note: This is a VIEW to maintain compatibility with older code
-- =============================================

-- This table reference should actually point to InferenceRequest
-- Creating as a synonym instead
CREATE SYNONYM [dbo].[InferenceRequests]
    FOR [dbo].[InferenceRequest];
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Synonym for InferenceRequest table. Provides backward compatibility for code using plural table name.',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'SYNONYM',  @level1name = N'InferenceRequests';
GO
