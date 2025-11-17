-- ============================================================================
-- Tier 1: Base Runtime Dependencies
-- ============================================================================
-- Purpose: Foundation assemblies required by higher-tier dependencies
-- Dependencies: None (base layer)
-- Count: 2 assemblies
-- ============================================================================

PRINT 'Tier 1: Base Runtime Dependencies (2 assemblies)';

-- System.Numerics.Vectors (Required by MathNet.Numerics)
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'System.Numerics.Vectors')
BEGIN
    PRINT '  [1/2] Deploying: System.Numerics.Vectors';
    CREATE ASSEMBLY [System.Numerics.Vectors]
    FROM '$(DependenciesPath)\System.Numerics.Vectors.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '        ✓ Deployed';
END
ELSE
    PRINT '  [1/2] ○ System.Numerics.Vectors already exists';
GO

-- System.ValueTuple (Required by System.Memory)
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'System.ValueTuple')
BEGIN
    PRINT '  [2/2] Deploying: System.ValueTuple';
    CREATE ASSEMBLY [System.ValueTuple]
    FROM '$(DependenciesPath)\System.ValueTuple.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '        ✓ Deployed';
END
ELSE
    PRINT '  [2/2] ○ System.ValueTuple already exists';
GO

PRINT '  ✓ Tier 1 complete';
PRINT '';
GO
