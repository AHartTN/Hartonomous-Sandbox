-- ============================================================================
-- Tier 3: Collections and Reflection
-- ============================================================================

-- System.Collections.Immutable
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'System.Collections.Immutable')
BEGIN
    PRINT '  Deploying: System.Collections.Immutable';
    CREATE ASSEMBLY [System.Collections.Immutable]
    FROM '$(DependenciesPath)\System.Collections.Immutable.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '  ✓ System.Collections.Immutable deployed';
END
ELSE
    PRINT '  ✓ System.Collections.Immutable already exists';
GO

-- System.Reflection.Metadata
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'System.Reflection.Metadata')
BEGIN
    PRINT '  Deploying: System.Reflection.Metadata';
    CREATE ASSEMBLY [System.Reflection.Metadata]
    FROM '$(DependenciesPath)\System.Reflection.Metadata.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '  ✓ System.Reflection.Metadata deployed';
END
ELSE
    PRINT '  ✓ System.Reflection.Metadata already exists';
GO
