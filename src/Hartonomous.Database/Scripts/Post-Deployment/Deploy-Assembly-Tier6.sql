-- ============================================================================
-- Tier 6: Application Assemblies
-- ============================================================================
-- Purpose: Application-specific CLR functions and utilities
-- Dependencies: All previous tiers (complete dependency tree)
-- Count: 3 assemblies
-- Note: Hartonomous.Clr is deployed via DACPAC schema, not here
-- ============================================================================

PRINT 'Tier 6: Application Assemblies (3 assemblies)';

-- System.Drawing (Graphics and imaging support)
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'System.Drawing')
BEGIN
    PRINT '  [1/3] Deploying: System.Drawing';
    CREATE ASSEMBLY [System.Drawing]
    FROM '$(DependenciesPath)\System.Drawing.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '        ✓ Deployed';
END
ELSE
    PRINT '  [1/3] ○ System.Drawing already exists';
GO

-- SqlClrFunctions (Legacy CLR functions library)
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'SqlClrFunctions')
BEGIN
    PRINT '  [2/3] Deploying: SqlClrFunctions';
    CREATE ASSEMBLY [SqlClrFunctions]
    FROM '$(DependenciesPath)\SqlClrFunctions.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '        ✓ Deployed';
END
ELSE
    PRINT '  [2/3] ○ SqlClrFunctions already exists';
GO

-- Hartonomous.Database (Database-specific utilities)
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'Hartonomous.Database')
BEGIN
    PRINT '  [3/3] Deploying: Hartonomous.Database';
    CREATE ASSEMBLY [Hartonomous.Database]
    FROM '$(DependenciesPath)\Hartonomous.Database.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '        ✓ Deployed';
END
ELSE
    PRINT '  [3/3] ○ Hartonomous.Database already exists';
GO

PRINT '  ✓ Tier 6 complete';
PRINT '';
GO
