-- ============================================================================
-- Tier 5: Third-Party Libraries
-- ============================================================================

-- MathNet.Numerics
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'MathNet.Numerics')
BEGIN
    PRINT '  Deploying: MathNet.Numerics';
    CREATE ASSEMBLY [MathNet.Numerics]
    FROM '$(DependenciesPath)\MathNet.Numerics.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '  ✓ MathNet.Numerics deployed';
END
ELSE
    PRINT '  ✓ MathNet.Numerics already exists';
GO

-- Newtonsoft.Json
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'Newtonsoft.Json')
BEGIN
    PRINT '  Deploying: Newtonsoft.Json';
    CREATE ASSEMBLY [Newtonsoft.Json]
    FROM '$(DependenciesPath)\Newtonsoft.Json.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '  ✓ Newtonsoft.Json deployed';
END
ELSE
    PRINT '  ✓ Newtonsoft.Json already exists';
GO

-- Microsoft.SqlServer.Types
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'Microsoft.SqlServer.Types')
BEGIN
    PRINT '  Deploying: Microsoft.SqlServer.Types';
    CREATE ASSEMBLY [Microsoft.SqlServer.Types]
    FROM '$(DependenciesPath)\Microsoft.SqlServer.Types.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '  ✓ Microsoft.SqlServer.Types deployed';
END
ELSE
    PRINT '  ✓ Microsoft.SqlServer.Types already exists';
GO
