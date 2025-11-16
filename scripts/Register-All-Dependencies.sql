USE [Hartonomous];
GO

-- Register dependencies in correct order (base dependencies first)

-- System assemblies (GAC)
CREATE ASSEMBLY [System.Runtime.Serialization] FROM 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Runtime.Serialization.dll' WITH PERMISSION_SET = UNSAFE;
CREATE ASSEMBLY [System.Drawing] FROM 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Drawing.dll' WITH PERMISSION_SET = UNSAFE;
GO

-- Dependencies folder assemblies (exact versions)
DECLARE @sql NVARCHAR(MAX);
DECLARE @bytes VARBINARY(MAX);

-- System.Numerics.Vectors
SET @bytes = (SELECT BulkColumn FROM OPENROWSET(BULK 'D:\Repositories\Hartonomous\dependencies\System.Numerics.Vectors.dll', SINGLE_BLOB) AS x);
SET @sql = 'CREATE ASSEMBLY [System.Numerics.Vectors] FROM 0x' + CONVERT(NVARCHAR(MAX), @bytes, 2) + ' WITH PERMISSION_SET = UNSAFE';
EXEC sp_executesql @sql;

-- System.Runtime.CompilerServices.Unsafe
SET @bytes = (SELECT BulkColumn FROM OPENROWSET(BULK 'D:\Repositories\Hartonomous\dependencies\System.Runtime.CompilerServices.Unsafe.dll', SINGLE_BLOB) AS x);
SET @sql = 'CREATE ASSEMBLY [System.Runtime.CompilerServices.Unsafe] FROM 0x' + CONVERT(NVARCHAR(MAX), @bytes, 2) + ' WITH PERMISSION_SET = UNSAFE';
EXEC sp_executesql @sql;

-- System.Buffers
SET @bytes = (SELECT BulkColumn FROM OPENROWSET(BULK 'D:\Repositories\Hartonomous\dependencies\System.Buffers.dll', SINGLE_BLOB) AS x);
SET @sql = 'CREATE ASSEMBLY [System.Buffers] FROM 0x' + CONVERT(NVARCHAR(MAX), @bytes, 2) + ' WITH PERMISSION_SET = UNSAFE';
EXEC sp_executesql @sql;

-- System.Memory
SET @bytes = (SELECT BulkColumn FROM OPENROWSET(BULK 'D:\Repositories\Hartonomous\dependencies\System.Memory.dll', SINGLE_BLOB) AS x);
SET @sql = 'CREATE ASSEMBLY [System.Memory] FROM 0x' + CONVERT(NVARCHAR(MAX), @bytes, 2) + ' WITH PERMISSION_SET = UNSAFE';
EXEC sp_executesql @sql;

-- System.ValueTuple
SET @bytes = (SELECT BulkColumn FROM OPENROWSET(BULK 'D:\Repositories\Hartonomous\dependencies\System.ValueTuple.dll', SINGLE_BLOB) AS x);
SET @sql = 'CREATE ASSEMBLY [System.ValueTuple] FROM 0x' + CONVERT(NVARCHAR(MAX), @bytes, 2) + ' WITH PERMISSION_SET = UNSAFE';
EXEC sp_executesql @sql;

-- System.Collections.Immutable
SET @bytes = (SELECT BulkColumn FROM OPENROWSET(BULK 'D:\Repositories\Hartonomous\dependencies\System.Collections.Immutable.dll', SINGLE_BLOB) AS x);
SET @sql = 'CREATE ASSEMBLY [System.Collections.Immutable] FROM 0x' + CONVERT(NVARCHAR(MAX), @bytes, 2) + ' WITH PERMISSION_SET = UNSAFE';
EXEC sp_executesql @sql;

-- System.Reflection.Metadata
SET @bytes = (SELECT BulkColumn FROM OPENROWSET(BULK 'D:\Repositories\Hartonomous\dependencies\System.Reflection.Metadata.dll', SINGLE_BLOB) AS x);
SET @sql = 'CREATE ASSEMBLY [System.Reflection.Metadata] FROM 0x' + CONVERT(NVARCHAR(MAX), @bytes, 2) + ' WITH PERMISSION_SET = UNSAFE';
EXEC sp_executesql @sql;

-- Newtonsoft.Json
SET @bytes = (SELECT BulkColumn FROM OPENROWSET(BULK 'D:\Repositories\Hartonomous\dependencies\Newtonsoft.Json.dll', SINGLE_BLOB) AS x);
SET @sql = 'CREATE ASSEMBLY [Newtonsoft.Json] FROM 0x' + CONVERT(NVARCHAR(MAX), @bytes, 2) + ' WITH PERMISSION_SET = UNSAFE';
EXEC sp_executesql @sql;

-- MathNet.Numerics
SET @bytes = (SELECT BulkColumn FROM OPENROWSET(BULK 'D:\Repositories\Hartonomous\dependencies\MathNet.Numerics.dll', SINGLE_BLOB) AS x);
SET @sql = 'CREATE ASSEMBLY [MathNet.Numerics] FROM 0x' + CONVERT(NVARCHAR(MAX), @bytes, 2) + ' WITH PERMISSION_SET = UNSAFE';
EXEC sp_executesql @sql;

-- Microsoft.SqlServer.Types
SET @bytes = (SELECT BulkColumn FROM OPENROWSET(BULK 'D:\Repositories\Hartonomous\dependencies\Microsoft.SqlServer.Types.dll', SINGLE_BLOB) AS x);
SET @sql = 'CREATE ASSEMBLY [Microsoft.SqlServer.Types] FROM 0x' + CONVERT(NVARCHAR(MAX), @bytes, 2) + ' WITH PERMISSION_SET = UNSAFE';
EXEC sp_executesql @sql;

GO

PRINT 'All dependencies registered successfully';
GO
