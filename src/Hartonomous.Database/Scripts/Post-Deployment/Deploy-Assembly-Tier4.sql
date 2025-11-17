-- ============================================================================
-- Tier 4: Serialization
-- ============================================================================
-- Purpose: XML/Binary serialization and WCF diagnostics support
-- Dependencies: Tier 2 (System.Runtime.CompilerServices.Unsafe)
-- Count: 3 assemblies
-- ============================================================================

PRINT 'Tier 4: Serialization (3 assemblies)';

-- System.Runtime.Serialization (DataContract serialization)
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'System.Runtime.Serialization')
BEGIN
    PRINT '  [1/3] Deploying: System.Runtime.Serialization';
    CREATE ASSEMBLY [System.Runtime.Serialization]
    FROM '$(DependenciesPath)\System.Runtime.Serialization.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '        ✓ Deployed';
END
ELSE
    PRINT '  [1/3] ○ System.Runtime.Serialization already exists';
GO

-- System.ServiceModel.Internals (WCF internal utilities)
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'System.ServiceModel.Internals')
BEGIN
    PRINT '  [2/3] Deploying: System.ServiceModel.Internals';
    CREATE ASSEMBLY [System.ServiceModel.Internals]
    FROM '$(DependenciesPath)\System.ServiceModel.Internals.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '        ✓ Deployed';
END
ELSE
    PRINT '  [2/3] ○ System.ServiceModel.Internals already exists';
GO

-- SMDiagnostics (System.ServiceModel diagnostics)
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'SMDiagnostics')
BEGIN
    PRINT '  [3/3] Deploying: SMDiagnostics';
    CREATE ASSEMBLY [SMDiagnostics]
    FROM '$(DependenciesPath)\SMDiagnostics.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '        ✓ Deployed';
END
ELSE
    PRINT '  [3/3] ○ SMDiagnostics already exists';
GO

PRINT '  ✓ Tier 4 complete';
PRINT '';
GO
