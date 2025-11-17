-- ============================================================================
-- Tier 3: Collections and Reflection
-- ============================================================================
-- Purpose: Advanced collection types and reflection metadata support
-- Dependencies: Tier 1 (System.Numerics.Vectors), Tier 2 (System.Memory)
-- Count: 2 assemblies
-- ============================================================================

PRINT 'Tier 3: Collections and Reflection (2 assemblies)';

-- System.Collections.Immutable (Immutable collections)
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'System.Collections.Immutable')
BEGIN
    PRINT '  [1/2] Deploying: System.Collections.Immutable';
    CREATE ASSEMBLY [System.Collections.Immutable]
    FROM '$(DependenciesPath)\System.Collections.Immutable.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '        ✓ Deployed';
END
ELSE
    PRINT '  [1/2] ○ System.Collections.Immutable already exists';
GO

-- System.Reflection.Metadata (Reflection and metadata APIs)
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'System.Reflection.Metadata')
BEGIN
    PRINT '  [2/2] Deploying: System.Reflection.Metadata';
    CREATE ASSEMBLY [System.Reflection.Metadata]
    FROM '$(DependenciesPath)\System.Reflection.Metadata.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '        ✓ Deployed';
END
ELSE
    PRINT '  [2/2] ○ System.Reflection.Metadata already exists';
GO

PRINT '  ✓ Tier 3 complete';
PRINT '';
GO
