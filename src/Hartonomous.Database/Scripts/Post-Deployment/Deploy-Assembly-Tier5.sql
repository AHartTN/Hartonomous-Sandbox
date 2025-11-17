-- ============================================================================
-- Tier 5: Third-Party Libraries
-- ============================================================================
-- Purpose: External NuGet packages and system spatial libraries
-- Dependencies: All previous tiers (System.Numerics.Vectors, System.Memory, etc.)
-- Count: 3 assemblies
-- ============================================================================

PRINT 'Tier 5: Third-Party Libraries (3 assemblies)';

-- MathNet.Numerics (Advanced mathematics library)
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'MathNet.Numerics')
BEGIN
    PRINT '  [1/3] Deploying: MathNet.Numerics';
    CREATE ASSEMBLY [MathNet.Numerics]
    FROM '$(DependenciesPath)\MathNet.Numerics.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '        ✓ Deployed';
END
ELSE
    PRINT '  [1/3] ○ MathNet.Numerics already exists';
GO

-- Newtonsoft.Json (JSON serialization/deserialization)
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'Newtonsoft.Json')
BEGIN
    PRINT '  [2/3] Deploying: Newtonsoft.Json';
    CREATE ASSEMBLY [Newtonsoft.Json]
    FROM '$(DependenciesPath)\Newtonsoft.Json.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '        ✓ Deployed';
END
ELSE
    PRINT '  [2/3] ○ Newtonsoft.Json already exists';
GO

-- Microsoft.SqlServer.Types (Spatial data types - geometry/geography)
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'Microsoft.SqlServer.Types')
BEGIN
    PRINT '  [3/3] Deploying: Microsoft.SqlServer.Types';
    CREATE ASSEMBLY [Microsoft.SqlServer.Types]
    FROM '$(DependenciesPath)\Microsoft.SqlServer.Types.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '        ✓ Deployed';
END
ELSE
    PRINT '  [3/3] ○ Microsoft.SqlServer.Types already exists';
GO

PRINT '  ✓ Tier 5 complete';
PRINT '';
GO
