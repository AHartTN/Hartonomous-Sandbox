-- ============================================================================
-- Tier 6: Application Support Libraries
-- ============================================================================

-- System.Drawing
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'System.Drawing')
BEGIN
    PRINT '  Deploying: System.Drawing';
    CREATE ASSEMBLY [System.Drawing]
    FROM '$(DependenciesPath)\System.Drawing.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '  ✓ System.Drawing deployed';
END
ELSE
    PRINT '  ✓ System.Drawing already exists';
GO

-- SqlClrFunctions
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'SqlClrFunctions')
BEGIN
    PRINT '  Deploying: SqlClrFunctions';
    CREATE ASSEMBLY [SqlClrFunctions]
    FROM '$(DependenciesPath)\SqlClrFunctions.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '  ✓ SqlClrFunctions deployed';
END
ELSE
    PRINT '  ✓ SqlClrFunctions already exists';
GO

-- Hartonomous.Database
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'Hartonomous.Database')
BEGIN
    PRINT '  Deploying: Hartonomous.Database';
    CREATE ASSEMBLY [Hartonomous.Database]
    FROM '$(DependenciesPath)\Hartonomous.Database.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '  ✓ Hartonomous.Database deployed';
END
ELSE
    PRINT '  ✓ Hartonomous.Database already exists';
GO
