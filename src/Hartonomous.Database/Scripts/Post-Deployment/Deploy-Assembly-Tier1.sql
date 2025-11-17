-- ============================================================================
-- Tier 1: Base Runtime Dependencies
-- ============================================================================

-- System.Numerics.Vectors
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'System.Numerics.Vectors')
BEGIN
    PRINT '  Deploying: System.Numerics.Vectors';
    CREATE ASSEMBLY [System.Numerics.Vectors]
    FROM '$(DependenciesPath)\System.Numerics.Vectors.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '  ✓ System.Numerics.Vectors deployed';
END
ELSE
    PRINT '  ✓ System.Numerics.Vectors already exists';
GO

-- System.ValueTuple
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'System.ValueTuple')
BEGIN
    PRINT '  Deploying: System.ValueTuple';
    CREATE ASSEMBLY [System.ValueTuple]
    FROM '$(DependenciesPath)\System.ValueTuple.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '  ✓ System.ValueTuple deployed';
END
ELSE
    PRINT '  ✓ System.ValueTuple already exists';
GO
