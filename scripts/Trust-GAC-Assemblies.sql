-- Trust GAC assemblies for CLR deployment
-- This adds System.Drawing and System.Numerics.Vectors to trusted assembly list

PRINT '=== Trusting GAC Assemblies for CLR Deployment ===';
PRINT '';

-- Trust System.Drawing
DECLARE @hash_drawing VARBINARY(64);
SET @hash_drawing = (
    SELECT HASHBYTES('SHA2_512', BulkColumn) 
    FROM OPENROWSET(BULK N'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Drawing.dll', SINGLE_BLOB) AS HashData
);

IF NOT EXISTS (SELECT 1 FROM sys.trusted_assemblies WHERE hash = @hash_drawing)
BEGIN
    PRINT 'Trusting System.Drawing...';
    EXEC sys.sp_add_trusted_assembly @hash = @hash_drawing, @description = N'System.Drawing from .NET Framework GAC';
    PRINT '✓ System.Drawing trusted';
END
ELSE
    PRINT '○ System.Drawing already trusted';

PRINT '';

-- Trust System.Numerics.Vectors
DECLARE @hash_vectors VARBINARY(64);
SET @hash_vectors = (
    SELECT HASHBYTES('SHA2_512', BulkColumn) 
    FROM OPENROWSET(BULK N'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Numerics.Vectors.dll', SINGLE_BLOB) AS HashData
);

IF NOT EXISTS (SELECT 1 FROM sys.trusted_assemblies WHERE hash = @hash_vectors)
BEGIN
    PRINT 'Trusting System.Numerics.Vectors...';
    EXEC sys.sp_add_trusted_assembly @hash = @hash_vectors, @description = N'System.Numerics.Vectors from .NET Framework GAC';
    PRINT '✓ System.Numerics.Vectors trusted';
END
ELSE
    PRINT '○ System.Numerics.Vectors already trusted';

PRINT '';
PRINT '=== GAC Assemblies Trusted Successfully ===';
