-- ============================================================================
-- Tier 4: Serialization
-- ============================================================================

-- System.Runtime.Serialization
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'System.Runtime.Serialization')
BEGIN
    PRINT '  Deploying: System.Runtime.Serialization';
    CREATE ASSEMBLY [System.Runtime.Serialization]
    FROM '$(DependenciesPath)\System.Runtime.Serialization.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '  ✓ System.Runtime.Serialization deployed';
END
ELSE
    PRINT '  ✓ System.Runtime.Serialization already exists';
GO

-- System.ServiceModel.Internals
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'System.ServiceModel.Internals')
BEGIN
    PRINT '  Deploying: System.ServiceModel.Internals';
    CREATE ASSEMBLY [System.ServiceModel.Internals]
    FROM '$(DependenciesPath)\System.ServiceModel.Internals.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '  ✓ System.ServiceModel.Internals deployed';
END
ELSE
    PRINT '  ✓ System.ServiceModel.Internals already exists';
GO

-- SMDiagnostics
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'SMDiagnostics')
BEGIN
    PRINT '  Deploying: SMDiagnostics';
    CREATE ASSEMBLY [SMDiagnostics]
    FROM '$(DependenciesPath)\SMDiagnostics.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '  ✓ SMDiagnostics deployed';
END
ELSE
    PRINT '  ✓ SMDiagnostics already exists';
GO
