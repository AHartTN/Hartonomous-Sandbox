-- =============================================
-- Projection System for High-Dimensional Vectors
-- =============================================
-- This script registers the CLR function responsible for projecting
-- high-dimensional vectors into a 3D space for spatial indexing.
-- =============================================

-- Drop the old function if it exists
IF OBJECT_ID('dbo.fn_ProjectTo3D', 'FS') IS NOT NULL
    DROP FUNCTION dbo.fn_ProjectTo3D;
GO

-- Create the function, linking it to the C# method in the deployed assembly
CREATE FUNCTION dbo.fn_ProjectTo3D(@vector VARBINARY(MAX))
RETURNS GEOMETRY
AS EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.SpatialOperations].fn_ProjectTo3D;
GO

PRINT 'Created CLR function dbo.fn_ProjectTo3D for spatial projection.';
GO