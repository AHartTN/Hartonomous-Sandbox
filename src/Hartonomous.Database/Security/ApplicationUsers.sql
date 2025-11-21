-- =============================================
-- Security Principals Setup
-- Creates application users and roles for Hartonomous
-- =============================================

-- Application user for web/API access
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'HartonomousAppUser')
BEGIN
    CREATE USER [HartonomousAppUser] WITHOUT LOGIN;
    PRINT 'Created user: HartonomousAppUser';
END
GO

-- Grant base permissions
ALTER ROLE [db_datareader] ADD MEMBER [HartonomousAppUser];
ALTER ROLE [db_datawriter] ADD MEMBER [HartonomousAppUser];
GO

-- Grant execute on specific functions (if they exist)
IF EXISTS (SELECT * FROM sys.objects WHERE name = 'fn_FindNearestAtoms' AND type IN ('FN', 'IF', 'TF'))
    GRANT EXECUTE ON [dbo].[fn_FindNearestAtoms] TO [HartonomousAppUser];
GO

PRINT 'Security principals configured successfully';
GO
