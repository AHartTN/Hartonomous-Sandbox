-- ============================================================================
-- Tier 2: Memory Management
-- ============================================================================
-- Purpose: High-performance memory and buffer management
-- Dependencies: Tier 1 (System.ValueTuple)
-- Count: 3 assemblies
-- ============================================================================

PRINT 'Tier 2: Memory Management (3 assemblies)';

-- System.Memory (Span<T>, Memory<T> support)
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'System.Memory')
BEGIN
    PRINT '  [1/3] Deploying: System.Memory';
    CREATE ASSEMBLY [System.Memory]
    FROM '$(DependenciesPath)\System.Memory.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '        ✓ Deployed';
END
ELSE
    PRINT '  [1/3] ○ System.Memory already exists';
GO

-- System.Buffers (ArrayPool support)
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'System.Buffers')
BEGIN
    PRINT '  [2/3] Deploying: System.Buffers';
    CREATE ASSEMBLY [System.Buffers]
    FROM '$(DependenciesPath)\System.Buffers.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '        ✓ Deployed';
END
ELSE
    PRINT '  [2/3] ○ System.Buffers already exists';
GO

-- System.Runtime.CompilerServices.Unsafe (Low-level memory operations)
IF NOT EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'System.Runtime.CompilerServices.Unsafe')
BEGIN
    PRINT '  [3/3] Deploying: System.Runtime.CompilerServices.Unsafe';
    CREATE ASSEMBLY [System.Runtime.CompilerServices.Unsafe]
    FROM '$(DependenciesPath)\System.Runtime.CompilerServices.Unsafe.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT '        ✓ Deployed';
END
ELSE
    PRINT '  [3/3] ○ System.Runtime.CompilerServices.Unsafe already exists';
GO

PRINT '  ✓ Tier 2 complete';
PRINT '';
GO
