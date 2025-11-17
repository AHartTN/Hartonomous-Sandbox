-- ============================================================================
-- Tier 2: Memory Management
-- ============================================================================

-- System.Memory
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'System.Memory')
BEGIN
    PRINT '  Deploying: System.Memory';
    CREATE ASSEMBLY [System.Memory]
    FROM '$(DependenciesPath)\System.Memory.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '  ✓ System.Memory deployed';
END
ELSE
    PRINT '  ✓ System.Memory already exists';
GO

-- System.Buffers
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'System.Buffers')
BEGIN
    PRINT '  Deploying: System.Buffers';
    CREATE ASSEMBLY [System.Buffers]
    FROM '$(DependenciesPath)\System.Buffers.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '  ✓ System.Buffers deployed';
END
ELSE
    PRINT '  ✓ System.Buffers already exists';
GO

-- System.Runtime.CompilerServices.Unsafe
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'System.Runtime.CompilerServices.Unsafe')
BEGIN
    PRINT '  Deploying: System.Runtime.CompilerServices.Unsafe';
    CREATE ASSEMBLY [System.Runtime.CompilerServices.Unsafe]
    FROM '$(DependenciesPath)\System.Runtime.CompilerServices.Unsafe.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '  ✓ System.Runtime.CompilerServices.Unsafe deployed';
END
ELSE
    PRINT '  ✓ System.Runtime.CompilerServices.Unsafe already exists';
GO
