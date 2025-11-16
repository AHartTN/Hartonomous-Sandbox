-- Register System.Drawing dependency in SQL Server
USE master;
GO

-- Register System.Drawing (required by Hartonomous.Clr)
IF NOT EXISTS (SELECT * FROM sys.assemblies WHERE name = 'System.Drawing')
BEGIN
    CREATE ASSEMBLY [System.Drawing]
    FROM 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Drawing.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT 'System.Drawing registered';
END
ELSE
BEGIN
    PRINT 'System.Drawing already registered';
END
GO

-- Register Newtonsoft.Json
IF NOT EXISTS (SELECT * FROM sys.assemblies WHERE name = 'Newtonsoft.Json')
BEGIN
    CREATE ASSEMBLY [Newtonsoft.Json]
    FROM 'C:\Users\ahart\.nuget\packages\newtonsoft.json\13.0.3\lib\net45\Newtonsoft.Json.dll'
    WITH PERMISSION_SET = UNSAFE;
    PRINT 'Newtonsoft.Json registered';
END
ELSE
BEGIN
    PRINT 'Newtonsoft.Json already registered';
END
GO

-- Configure database for CLR deployment with UNSAFE assemblies
USE master;
GO

-- Grant current login UNSAFE ASSEMBLY permission
DECLARE @login NVARCHAR(128) = SYSTEM_USER;
DECLARE @sql NVARCHAR(MAX) = N'ALTER SERVER ROLE sysadmin ADD MEMBER [' + @login + ']';
IF NOT IS_SRVROLEMEMBER('sysadmin') = 1
BEGIN
    EXEC sp_executesql @sql;
    PRINT 'Granted sysadmin role to ' + @login;
END
ELSE
BEGIN
    PRINT @login + ' already has sysadmin role';
END
GO

-- Set database to TRUSTWORTHY
USE [Hartonomous];
GO

IF DATABASEPROPERTYEX('Hartonomous', 'IsTrustworthy') = 0
BEGIN
    ALTER DATABASE [Hartonomous] SET TRUSTWORTHY ON;
    PRINT 'Database set to TRUSTWORTHY';
END
ELSE
BEGIN
    PRINT 'Database already TRUSTWORTHY';
END
GO

USE [Hartonomous];
GO

PRINT 'Dependencies registered and configuration complete';
GO
