# Deployment Runbook

## Prerequisites

- SQL Server 2019+ (Standard or Enterprise recommended)
- MSBuild with SQL Server Data Tools
- SqlPackage.exe (DacFx)
- Network access to target SQL Server

## Deployment Steps

### 1. Pre-Deployment Validation

```powershell
# Verify DACPAC exists
Test-Path "src\Hartonomous.Database\bin\Output\Hartonomous.Database.dacpac"

# Check SQL Server connectivity
sqlcmd -S YOUR_SERVER -Q "SELECT @@VERSION"
```

### 2. Database Backup (Production Only)

```sql
-- Create backup before deployment
BACKUP DATABASE Hartonomous
TO DISK = 'D:\Backups\Hartonomous_PreDeploy_YYYYMMDD.bak'
WITH FORMAT, INIT, COMPRESSION,
     NAME = 'Pre-Deployment Backup';
```

### 3. Deploy DACPAC

```powershell
# Using deployment script
.\scripts\Week1-Deploy-DACPAC.ps1 `
    -Server "YOUR_SERVER" `
    -Database "Hartonomous" `
    -IntegratedSecurity `
    -TrustServerCertificate

# OR manual SqlPackage
sqlpackage /Action:Publish `
    /SourceFile:src\Hartonomous.Database\bin\Output\Hartonomous.Database.dacpac `
    /TargetConnectionString:"Server=YOUR_SERVER;Database=Hartonomous;Integrated Security=True;" `
    /p:IncludeCompositeObjects=True `
    /p:BlockOnPossibleDataLoss=True
```

### 4. Configure SQL Server for CLR

```sql
-- Enable CLR
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;

-- Enable CLR strict security (SQL Server 2017+)
EXEC sp_configure 'clr strict security', 1;
RECONFIGURE;

-- Create asymmetric key from CLR DLL
CREATE ASYMMETRIC KEY Hartonomous_CLR_Key
FROM EXECUTABLE FILE = 'D:\Path\To\Hartonomous.Database.dll';

CREATE LOGIN Hartonomous_CLR_Login FROM ASYMMETRIC KEY Hartonomous_CLR_Key;
GRANT UNSAFE ASSEMBLY TO Hartonomous_CLR_Login;

-- Verify CLR assemblies loaded
SELECT name, permission_set_desc FROM sys.assemblies WHERE name LIKE 'Hartonomous%';
```

### 5. Enable Service Broker

```sql
-- Enable Service Broker for OODA loop
ALTER DATABASE Hartonomous SET ENABLE_BROKER;

-- Verify queues are active
SELECT name, is_activation_enabled 
FROM sys.service_queues 
WHERE name IN ('AnalyzeQueue', 'HypothesizeQueue', 'ActQueue', 'LearnQueue');
```

### 6. Create Spatial Indexes

```sql
-- Spatial indexes should be created by post-deployment scripts
-- Verify they exist:
SELECT name, type_desc FROM sys.indexes 
WHERE object_id = OBJECT_ID('dbo.AtomEmbeddings') 
  AND type_desc = 'SPATIAL';

-- Expected:
-- IX_AtomEmbeddings_SpatialGeometry
-- IX_AtomEmbeddings_SpatialCoarse
```

### 7. Run Smoke Tests

```powershell
sqlcmd -S YOUR_SERVER -d Hartonomous -i tests\smoke-tests.sql
```

Expected output:
```
Test 1: CLR Functions Exist...
  PASSED: CLR functions exist
Test 2: Spatial Projection Works...
  PASSED: Projection working
...
ALL SMOKE TESTS PASSED
```

### 8. Seed Sample Data (Dev/Test Only)

```powershell
sqlcmd -S YOUR_SERVER -d Hartonomous -i tests\seed-sample-data.sql
```

### 9. Verify Core Functionality

```sql
-- Test spatial projection
DECLARE @vec VARBINARY(MAX) = CAST(REPLICATE(0x3F800000, 1998) AS VARBINARY(MAX));
SELECT dbo.fn_ProjectTo3D(@vec).STAsText();

-- Test Hilbert curve
DECLARE @point GEOMETRY = geometry::Point(0.5, 0.5, 0.5, 0);
SELECT dbo.clr_ComputeHilbertValue(@point, 21);

-- Test spatial query
EXEC sp_SpatialNextToken @context_atom_ids = '1,2', @top_k = 5;
```

## Post-Deployment

### Update Connection Strings

Update application connection strings to point to new database:

```json
{
  "ConnectionStrings": {
    "Hartonomous": "Server=YOUR_SERVER;Database=Hartonomous;..."
  }
}
```

### Restart Application Services

```powershell
# Restart API
Restart-Service Hartonomous.Api

# Restart background workers
Restart-Service Hartonomous.Workers.CesConsumer
Restart-Service Hartonomous.Workers.Neo4jSync
```

### Monitor Initial Performance

```sql
-- Monitor query performance
SELECT * FROM dbo.vw_QueryPerformanceMetrics
ORDER BY last_execution_time DESC;

-- Check OODA loop activity
SELECT * FROM dbo.OODALoopMetrics
WHERE ExecutedAt >= DATEADD(HOUR, -1, GETUTCDATE())
ORDER BY ExecutedAt DESC;
```

## Rollback Procedure

If deployment fails:

```sql
-- Restore from backup
USE master;
ALTER DATABASE Hartonomous SET SINGLE_USER WITH ROLLBACK IMMEDIATE;

RESTORE DATABASE Hartonomous
FROM DISK = 'D:\Backups\Hartonomous_PreDeploy_YYYYMMDD.bak'
WITH REPLACE, RECOVERY;

ALTER DATABASE Hartonomous SET MULTI_USER;
```

## Troubleshooting

### DACPAC Deployment Fails

Check SqlPackage output for specific errors:
- Schema drift: Review differences, may need manual migration
- Data loss warning: Set `/p:BlockOnPossibleDataLoss=False` (with caution)

### CLR Assembly Won't Load

```sql
-- Check CLR configuration
EXEC sp_configure 'clr enabled';
EXEC sp_configure 'clr strict security';

-- Verify asymmetric key
SELECT name FROM sys.asymmetric_keys WHERE name = 'Hartonomous_CLR_Key';

-- Check assembly trust
SELECT l.name, p.permission_name
FROM sys.server_principals l
INNER JOIN sys.server_permissions p ON l.principal_id = p.grantee_principal_id
WHERE l.name = 'Hartonomous_CLR_Login';
```

### Service Broker Not Running

```sql
-- Check broker enabled
SELECT is_broker_enabled FROM sys.databases WHERE name = 'Hartonomous';

-- Check for errors
SELECT * FROM sys.transmission_queue;

-- Manually activate queue
EXEC sp_Analyze @TenantId = 0;
```

## Checklist

- [ ] DACPAC deployed successfully
- [ ] CLR assemblies loaded
- [ ] Service Broker enabled
- [ ] Spatial indexes created
- [ ] Smoke tests passed
- [ ] Sample data seeded (dev/test)
- [ ] Application connection strings updated
- [ ] Services restarted
- [ ] Initial monitoring checks completed
- [ ] Backup retention configured
