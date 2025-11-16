USE [Hartonomous];
GO

DROP ASSEMBLY IF EXISTS [System.Numerics.Vectors];
GO

CREATE ASSEMBLY [System.Numerics.Vectors]
FROM 'C:\Users\ahart\.nuget\packages\system.numerics.vectors\4.5.0\lib\net46\System.Numerics.Vectors.dll'
WITH PERMISSION_SET = UNSAFE;
GO

PRINT 'System.Numerics.Vectors 4.5.0 registered';
GO
