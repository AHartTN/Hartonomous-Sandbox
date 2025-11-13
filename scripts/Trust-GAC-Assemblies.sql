-- Trust ALL CLR dependencies for Hartonomous.Clr deployment
-- Required by CLR strict security (SQL Server 2017+)
-- ALL assemblies from dependencies folder must be trusted before CREATE ASSEMBLY

PRINT '=== Trusting CLR Dependencies from Dependencies Folder ===';
PRINT '';

DECLARE @DependenciesPath NVARCHAR(500) = N'D:\Repositories\Hartonomous\dependencies\';

-- Trust System.Runtime.CompilerServices.Unsafe (4.0.4.1)
DECLARE @hash VARBINARY(64);
SET @hash = (SELECT HASHBYTES('SHA2_512', BulkColumn) FROM OPENROWSET(BULK N'D:\Repositories\Hartonomous\dependencies\System.Runtime.CompilerServices.Unsafe.dll', SINGLE_BLOB) AS HashData);
IF NOT EXISTS (SELECT 1 FROM sys.trusted_assemblies WHERE hash = @hash)
BEGIN
    PRINT 'Trusting System.Runtime.CompilerServices.Unsafe...';
    EXEC sys.sp_add_trusted_assembly @hash = @hash, @description = N'System.Runtime.CompilerServices.Unsafe 4.0.4.1';
    PRINT '✓ System.Runtime.CompilerServices.Unsafe trusted';
END
ELSE PRINT '○ System.Runtime.CompilerServices.Unsafe already trusted';

-- Trust System.Buffers (4.0.3.0)
SET @hash = (SELECT HASHBYTES('SHA2_512', BulkColumn) FROM OPENROWSET(BULK N'D:\Repositories\Hartonomous\dependencies\System.Buffers.dll', SINGLE_BLOB) AS HashData);
IF NOT EXISTS (SELECT 1 FROM sys.trusted_assemblies WHERE hash = @hash)
BEGIN
    PRINT 'Trusting System.Buffers...';
    EXEC sys.sp_add_trusted_assembly @hash = @hash, @description = N'System.Buffers 4.0.3.0';
    PRINT '✓ System.Buffers trusted';
END
ELSE PRINT '○ System.Buffers already trusted';

-- Trust System.Memory (4.0.1.1)
SET @hash = (SELECT HASHBYTES('SHA2_512', BulkColumn) FROM OPENROWSET(BULK N'D:\Repositories\Hartonomous\dependencies\System.Memory.dll', SINGLE_BLOB) AS HashData);
IF NOT EXISTS (SELECT 1 FROM sys.trusted_assemblies WHERE hash = @hash)
BEGIN
    PRINT 'Trusting System.Memory...';
    EXEC sys.sp_add_trusted_assembly @hash = @hash, @description = N'System.Memory 4.0.1.1';
    PRINT '✓ System.Memory trusted';
END
ELSE PRINT '○ System.Memory already trusted';

-- Trust System.Numerics.Vectors (4.1.4.0) - CORRECT version
SET @hash = (SELECT HASHBYTES('SHA2_512', BulkColumn) FROM OPENROWSET(BULK N'D:\Repositories\Hartonomous\dependencies\System.Numerics.Vectors.dll', SINGLE_BLOB) AS HashData);
IF NOT EXISTS (SELECT 1 FROM sys.trusted_assemblies WHERE hash = @hash)
BEGIN
    PRINT 'Trusting System.Numerics.Vectors (4.1.4.0)...';
    EXEC sys.sp_add_trusted_assembly @hash = @hash, @description = N'System.Numerics.Vectors 4.1.4.0';
    PRINT '✓ System.Numerics.Vectors trusted';
END
ELSE PRINT '○ System.Numerics.Vectors already trusted';

-- Trust System.Collections.Immutable (1.2.5.0)
SET @hash = (SELECT HASHBYTES('SHA2_512', BulkColumn) FROM OPENROWSET(BULK N'D:\Repositories\Hartonomous\dependencies\System.Collections.Immutable.dll', SINGLE_BLOB) AS HashData);
IF NOT EXISTS (SELECT 1 FROM sys.trusted_assemblies WHERE hash = @hash)
BEGIN
    PRINT 'Trusting System.Collections.Immutable...';
    EXEC sys.sp_add_trusted_assembly @hash = @hash, @description = N'System.Collections.Immutable 1.2.5.0';
    PRINT '✓ System.Collections.Immutable trusted';
END
ELSE PRINT '○ System.Collections.Immutable already trusted';

-- Trust System.Reflection.Metadata (1.4.5.0)
SET @hash = (SELECT HASHBYTES('SHA2_512', BulkColumn) FROM OPENROWSET(BULK N'D:\Repositories\Hartonomous\dependencies\System.Reflection.Metadata.dll', SINGLE_BLOB) AS HashData);
IF NOT EXISTS (SELECT 1 FROM sys.trusted_assemblies WHERE hash = @hash)
BEGIN
    PRINT 'Trusting System.Reflection.Metadata...';
    EXEC sys.sp_add_trusted_assembly @hash = @hash, @description = N'System.Reflection.Metadata 1.4.5.0';
    PRINT '✓ System.Reflection.Metadata trusted';
END
ELSE PRINT '○ System.Reflection.Metadata already trusted';

-- Trust System.Drawing (from dependencies, not GAC)
SET @hash = (SELECT HASHBYTES('SHA2_512', BulkColumn) FROM OPENROWSET(BULK N'D:\Repositories\Hartonomous\dependencies\System.Drawing.dll', SINGLE_BLOB) AS HashData);
IF NOT EXISTS (SELECT 1 FROM sys.trusted_assemblies WHERE hash = @hash)
BEGIN
    PRINT 'Trusting System.Drawing...';
    EXEC sys.sp_add_trusted_assembly @hash = @hash, @description = N'System.Drawing from dependencies';
    PRINT '✓ System.Drawing trusted';
END
ELSE PRINT '○ System.Drawing already trusted';

-- Trust MathNet.Numerics (5.0.0.0)
SET @hash = (SELECT HASHBYTES('SHA2_512', BulkColumn) FROM OPENROWSET(BULK N'D:\Repositories\Hartonomous\dependencies\MathNet.Numerics.dll', SINGLE_BLOB) AS HashData);
IF NOT EXISTS (SELECT 1 FROM sys.trusted_assemblies WHERE hash = @hash)
BEGIN
    PRINT 'Trusting MathNet.Numerics...';
    EXEC sys.sp_add_trusted_assembly @hash = @hash, @description = N'MathNet.Numerics 5.0.0.0';
    PRINT '✓ MathNet.Numerics trusted';
END
ELSE PRINT '○ MathNet.Numerics already trusted';

-- Trust Microsoft.SqlServer.Types (16.0.0.0)
SET @hash = (SELECT HASHBYTES('SHA2_512', BulkColumn) FROM OPENROWSET(BULK N'D:\Repositories\Hartonomous\dependencies\Microsoft.SqlServer.Types.dll', SINGLE_BLOB) AS HashData);
IF NOT EXISTS (SELECT 1 FROM sys.trusted_assemblies WHERE hash = @hash)
BEGIN
    PRINT 'Trusting Microsoft.SqlServer.Types...';
    EXEC sys.sp_add_trusted_assembly @hash = @hash, @description = N'Microsoft.SqlServer.Types 16.0.0.0';
    PRINT '✓ Microsoft.SqlServer.Types trusted';
END
ELSE PRINT '○ Microsoft.SqlServer.Types already trusted';

-- Trust Newtonsoft.Json (13.0.0.0)
SET @hash = (SELECT HASHBYTES('SHA2_512', BulkColumn) FROM OPENROWSET(BULK N'D:\Repositories\Hartonomous\dependencies\Newtonsoft.Json.dll', SINGLE_BLOB) AS HashData);
IF NOT EXISTS (SELECT 1 FROM sys.trusted_assemblies WHERE hash = @hash)
BEGIN
    PRINT 'Trusting Newtonsoft.Json...';
    EXEC sys.sp_add_trusted_assembly @hash = @hash, @description = N'Newtonsoft.Json 13.0.0.0';
    PRINT '✓ Newtonsoft.Json trusted';
END
ELSE PRINT '○ Newtonsoft.Json already trusted';

PRINT '';
PRINT '=== All CLR Dependencies Trusted Successfully ===';
