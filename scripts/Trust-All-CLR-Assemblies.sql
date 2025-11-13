-- ===============================================================================
-- Trust ALL CLR assemblies (dependencies folder + GAC) for Hartonomous.Clr
-- ===============================================================================
-- Required by CLR strict security (SQL Server 2017+)
-- ALL assemblies must be trusted before CREATE ASSEMBLY with UNSAFE permission
-- IDEMPOTENT: Only adds assemblies that are not already trusted
-- ===============================================================================

PRINT '=== Trusting CLR Assemblies (Dependencies + GAC) ===';
PRINT '';

DECLARE @hash VARBINARY(64);

-- ========================================
-- NON-GAC DEPENDENCIES (from dependencies folder)
-- ========================================
PRINT '--- Trusting Non-GAC Dependencies (from dependencies folder) ---';

-- System.Runtime.CompilerServices.Unsafe (4.0.4.1)
SET @hash = (SELECT HASHBYTES('SHA2_512', BulkColumn) FROM OPENROWSET(BULK N'D:\Repositories\Hartonomous\dependencies\System.Runtime.CompilerServices.Unsafe.dll', SINGLE_BLOB) AS HashData);
IF NOT EXISTS (SELECT 1 FROM sys.trusted_assemblies WHERE hash = @hash)
BEGIN
    EXEC sys.sp_add_trusted_assembly @hash = @hash, @description = N'System.Runtime.CompilerServices.Unsafe 4.0.4.1 [DEPS]';
    PRINT '  ✓ System.Runtime.CompilerServices.Unsafe trusted';
END
ELSE PRINT '  ○ System.Runtime.CompilerServices.Unsafe already trusted';

-- System.Buffers (4.0.3.0)
SET @hash = (SELECT HASHBYTES('SHA2_512', BulkColumn) FROM OPENROWSET(BULK N'D:\Repositories\Hartonomous\dependencies\System.Buffers.dll', SINGLE_BLOB) AS HashData);
IF NOT EXISTS (SELECT 1 FROM sys.trusted_assemblies WHERE hash = @hash)
BEGIN
    EXEC sys.sp_add_trusted_assembly @hash = @hash, @description = N'System.Buffers 4.0.3.0 [DEPS]';
    PRINT '  ✓ System.Buffers trusted';
END
ELSE PRINT '  ○ System.Buffers already trusted';

-- System.Memory (4.0.1.1)
SET @hash = (SELECT HASHBYTES('SHA2_512', BulkColumn) FROM OPENROWSET(BULK N'D:\Repositories\Hartonomous\dependencies\System.Memory.dll', SINGLE_BLOB) AS HashData);
IF NOT EXISTS (SELECT 1 FROM sys.trusted_assemblies WHERE hash = @hash)
BEGIN
    EXEC sys.sp_add_trusted_assembly @hash = @hash, @description = N'System.Memory 4.0.1.1 [DEPS]';
    PRINT '  ✓ System.Memory trusted';
END
ELSE PRINT '  ○ System.Memory already trusted';

-- System.Collections.Immutable (1.2.5.0)
SET @hash = (SELECT HASHBYTES('SHA2_512', BulkColumn) FROM OPENROWSET(BULK N'D:\Repositories\Hartonomous\dependencies\System.Collections.Immutable.dll', SINGLE_BLOB) AS HashData);
IF NOT EXISTS (SELECT 1 FROM sys.trusted_assemblies WHERE hash = @hash)
BEGIN
    EXEC sys.sp_add_trusted_assembly @hash = @hash, @description = N'System.Collections.Immutable 1.2.5.0 [DEPS]';
    PRINT '  ✓ System.Collections.Immutable trusted';
END
ELSE PRINT '  ○ System.Collections.Immutable already trusted';

-- System.Reflection.Metadata (1.4.5.0)
SET @hash = (SELECT HASHBYTES('SHA2_512', BulkColumn) FROM OPENROWSET(BULK N'D:\Repositories\Hartonomous\dependencies\System.Reflection.Metadata.dll', SINGLE_BLOB) AS HashData);
IF NOT EXISTS (SELECT 1 FROM sys.trusted_assemblies WHERE hash = @hash)
BEGIN
    EXEC sys.sp_add_trusted_assembly @hash = @hash, @description = N'System.Reflection.Metadata 1.4.5.0 [DEPS]';
    PRINT '  ✓ System.Reflection.Metadata trusted';
END
ELSE PRINT '  ○ System.Reflection.Metadata already trusted';

-- MathNet.Numerics (5.0.0.0)
SET @hash = (SELECT HASHBYTES('SHA2_512', BulkColumn) FROM OPENROWSET(BULK N'D:\Repositories\Hartonomous\dependencies\MathNet.Numerics.dll', SINGLE_BLOB) AS HashData);
IF NOT EXISTS (SELECT 1 FROM sys.trusted_assemblies WHERE hash = @hash)
BEGIN
    EXEC sys.sp_add_trusted_assembly @hash = @hash, @description = N'MathNet.Numerics 5.0.0.0 [DEPS]';
    PRINT '  ✓ MathNet.Numerics trusted';
END
ELSE PRINT '  ○ MathNet.Numerics already trusted';

-- System.Numerics.Vectors (4.1.4.0) - GAC has 4.0.0.0, we need 4.1.4.0
SET @hash = (SELECT HASHBYTES('SHA2_512', BulkColumn) FROM OPENROWSET(BULK N'D:\Repositories\Hartonomous\dependencies\System.Numerics.Vectors.dll', SINGLE_BLOB) AS HashData);
IF NOT EXISTS (SELECT 1 FROM sys.trusted_assemblies WHERE hash = @hash)
BEGIN
    EXEC sys.sp_add_trusted_assembly @hash = @hash, @description = N'System.Numerics.Vectors 4.1.4.0 [DEPS]';
    PRINT '  ✓ System.Numerics.Vectors 4.1.4.0 trusted';
END
ELSE PRINT '  ○ System.Numerics.Vectors 4.1.4.0 already trusted';

PRINT '';

-- ========================================
-- GAC DEPENDENCIES (from Global Assembly Cache)
-- ========================================
PRINT '--- Trusting GAC Dependencies (from Global Assembly Cache) ---';

-- System.Drawing (4.0.0.0)
SET @hash = (SELECT HASHBYTES('SHA2_512', BulkColumn) FROM OPENROWSET(BULK N'C:\Windows\Microsoft.NET\assembly\GAC_MSIL\System.Drawing\v4.0_4.0.0.0__b03f5f7f11d50a3a\System.Drawing.dll', SINGLE_BLOB) AS HashData);
IF NOT EXISTS (SELECT 1 FROM sys.trusted_assemblies WHERE hash = @hash)
BEGIN
    EXEC sys.sp_add_trusted_assembly @hash = @hash, @description = N'System.Drawing 4.0.0.0 [GAC]';
    PRINT '  ✓ System.Drawing trusted';
END
ELSE PRINT '  ○ System.Drawing already trusted';

-- Newtonsoft.Json (13.0.0.0)
SET @hash = (SELECT HASHBYTES('SHA2_512', BulkColumn) FROM OPENROWSET(BULK N'C:\Windows\Microsoft.NET\assembly\GAC_MSIL\Newtonsoft.Json\v4.0_13.0.0.0__30ad4fe6b2a6aeed\Newtonsoft.Json.dll', SINGLE_BLOB) AS HashData);
IF NOT EXISTS (SELECT 1 FROM sys.trusted_assemblies WHERE hash = @hash)
BEGIN
    EXEC sys.sp_add_trusted_assembly @hash = @hash, @description = N'Newtonsoft.Json 13.0.0.0 [GAC]';
    PRINT '  ✓ Newtonsoft.Json trusted';
END
ELSE PRINT '  ○ Newtonsoft.Json already trusted';

-- SMDiagnostics (4.0.0.0)
SET @hash = (SELECT HASHBYTES('SHA2_512', BulkColumn) FROM OPENROWSET(BULK N'C:\Windows\Microsoft.NET\assembly\GAC_MSIL\SMDiagnostics\v4.0_4.0.0.0__b77a5c561934e089\SMDiagnostics.dll', SINGLE_BLOB) AS HashData);
IF NOT EXISTS (SELECT 1 FROM sys.trusted_assemblies WHERE hash = @hash)
BEGIN
    EXEC sys.sp_add_trusted_assembly @hash = @hash, @description = N'SMDiagnostics 4.0.0.0 [GAC]';
    PRINT '  ✓ SMDiagnostics trusted';
END
ELSE PRINT '  ○ SMDiagnostics already trusted';

-- System.Runtime.Serialization (4.0.0.0)
SET @hash = (SELECT HASHBYTES('SHA2_512', BulkColumn) FROM OPENROWSET(BULK N'C:\Windows\Microsoft.NET\assembly\GAC_MSIL\System.Runtime.Serialization\v4.0_4.0.0.0__b77a5c561934e089\System.Runtime.Serialization.dll', SINGLE_BLOB) AS HashData);
IF NOT EXISTS (SELECT 1 FROM sys.trusted_assemblies WHERE hash = @hash)
BEGIN
    EXEC sys.sp_add_trusted_assembly @hash = @hash, @description = N'System.Runtime.Serialization 4.0.0.0 [GAC]';
    PRINT '  ✓ System.Runtime.Serialization trusted';
END
ELSE PRINT '  ○ System.Runtime.Serialization already trusted';

-- System.ServiceModel.Internals (4.0.0.0)
SET @hash = (SELECT HASHBYTES('SHA2_512', BulkColumn) FROM OPENROWSET(BULK N'C:\Windows\Microsoft.NET\assembly\GAC_MSIL\System.ServiceModel.Internals\v4.0_4.0.0.0__31bf3856ad364e35\System.ServiceModel.Internals.dll', SINGLE_BLOB) AS HashData);
IF NOT EXISTS (SELECT 1 FROM sys.trusted_assemblies WHERE hash = @hash)
BEGIN
    EXEC sys.sp_add_trusted_assembly @hash = @hash, @description = N'System.ServiceModel.Internals 4.0.0.0 [GAC]';
    PRINT '  ✓ System.ServiceModel.Internals trusted';
END
ELSE PRINT '  ○ System.ServiceModel.Internals already trusted';

-- System.ValueTuple (4.0.0.0)
SET @hash = (SELECT HASHBYTES('SHA2_512', BulkColumn) FROM OPENROWSET(BULK N'C:\Windows\Microsoft.NET\assembly\GAC_MSIL\System.ValueTuple\v4.0_4.0.0.0__cc7b13ffcd2ddd51\System.ValueTuple.dll', SINGLE_BLOB) AS HashData);
IF NOT EXISTS (SELECT 1 FROM sys.trusted_assemblies WHERE hash = @hash)
BEGIN
    EXEC sys.sp_add_trusted_assembly @hash = @hash, @description = N'System.ValueTuple 4.0.0.0 [GAC]';
    PRINT '  ✓ System.ValueTuple trusted';
END
ELSE PRINT '  ○ System.ValueTuple already trusted';

PRINT '';

-- ========================================
-- MAIN ASSEMBLY: Hartonomous.Clr
-- ========================================
PRINT '--- Trusting Main Assembly ---';
SET @hash = (SELECT HASHBYTES('SHA2_512', BulkColumn) FROM OPENROWSET(BULK N'D:\Repositories\Hartonomous\src\Hartonomous.Database\bin\Output\Hartonomous.Database.dll', SINGLE_BLOB) AS HashData);
IF NOT EXISTS (SELECT 1 FROM sys.trusted_assemblies WHERE hash = @hash)
BEGIN
    EXEC sys.sp_add_trusted_assembly @hash = @hash, @description = N'Hartonomous.Clr 1.0.0.0 (Hartonomous.Database.dll)';
    PRINT '  ✓ Hartonomous.Clr trusted';
END
ELSE PRINT '  ○ Hartonomous.Clr already trusted';

PRINT '';
PRINT '=== All CLR Assemblies Trusted Successfully ===';
