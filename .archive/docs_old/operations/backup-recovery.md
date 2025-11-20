# Backup and Recovery

**Last Updated**: November 19, 2025  
**Status**: Production Ready

## Overview

Hartonomous backup strategy covers SQL Server databases (Hartonomous + HartonomousArchive), Neo4j provenance graph, CLR assemblies, and configuration files. Recovery procedures support point-in-time restore, disaster recovery, and CLR assembly recovery.

## Backup Architecture

```
┌────────────────────────────────────────────────────────┐
│              Backup Components                         │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐ │
│  │ SQL Server   │  │    Neo4j     │  │  CLR         │ │
│  │ Databases    │  │   Graph      │  │  Assemblies  │ │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘ │
│         │                  │                  │         │
│         ▼                  ▼                  ▼         │
│  ┌──────────────────────────────────────────────────┐  │
│  │         Backup Storage (Azure Blob)              │  │
│  │  - Full backups (weekly)                         │  │
│  │  - Differential backups (daily)                  │  │
│  │  - Transaction log backups (hourly)              │  │
│  │  - Neo4j snapshot (daily)                        │  │
│  │  - CLR DLLs + schemas (on change)                │  │
│  └──────────────────┬───────────────────────────────┘  │
│                     │                                   │
│                     ▼                                   │
│  ┌──────────────────────────────────────────────────┐  │
│  │           Retention Policy                       │  │
│  │  - Full: 4 weeks                                 │  │
│  │  - Differential: 7 days                          │  │
│  │  - Transaction log: 7 days                       │  │
│  │  - Neo4j: 30 days                                │  │
│  └──────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────┘
```

## SQL Server Backups

### Full Backup (Weekly)

**Manual execution**:
```sql
BACKUP DATABASE Hartonomous
TO URL = 'https://hartstorage.blob.core.windows.net/backups/Hartonomous_FULL_20251119.bak'
WITH 
    CREDENTIAL = 'AzureStorageCredential',
    COMPRESSION,
    STATS = 10,
    CHECKSUM,
    FORMAT;

BACKUP DATABASE HartonomousArchive
TO URL = 'https://hartstorage.blob.core.windows.net/backups/HartonomousArchive_FULL_20251119.bak'
WITH 
    CREDENTIAL = 'AzureStorageCredential',
    COMPRESSION,
    STATS = 10,
    CHECKSUM,
    FORMAT;
```

**Automated via SQL Agent Job**:
```sql
EXEC msdb.dbo.sp_add_job @job_name = 'Hartonomous_FullBackup';

EXEC msdb.dbo.sp_add_jobstep 
    @job_name = 'Hartonomous_FullBackup',
    @step_name = 'BackupHartonomous',
    @subsystem = 'TSQL',
    @command = N'
BACKUP DATABASE Hartonomous
TO URL = ''https://hartstorage.blob.core.windows.net/backups/Hartonomous_FULL_'' + 
    CONVERT(NVARCHAR(8), GETDATE(), 112) + ''.bak''
WITH CREDENTIAL = ''AzureStorageCredential'', COMPRESSION, CHECKSUM;';

EXEC msdb.dbo.sp_add_jobstep 
    @job_name = 'Hartonomous_FullBackup',
    @step_name = 'BackupArchive',
    @subsystem = 'TSQL',
    @command = N'
BACKUP DATABASE HartonomousArchive
TO URL = ''https://hartstorage.blob.core.windows.net/backups/HartonomousArchive_FULL_'' + 
    CONVERT(NVARCHAR(8), GETDATE(), 112) + ''.bak''
WITH CREDENTIAL = ''AzureStorageCredential'', COMPRESSION, CHECKSUM;';

EXEC msdb.dbo.sp_add_schedule 
    @schedule_name = 'WeeklySunday',
    @freq_type = 8,  -- Weekly
    @freq_interval = 1,  -- Sunday
    @active_start_time = 10000;  -- 1:00 AM

EXEC msdb.dbo.sp_attach_schedule 
    @job_name = 'Hartonomous_FullBackup',
    @schedule_name = 'WeeklySunday';
```

**Verification**:
```sql
-- Verify backup integrity
RESTORE VERIFYONLY
FROM URL = 'https://hartstorage.blob.core.windows.net/backups/Hartonomous_FULL_20251119.bak'
WITH CREDENTIAL = 'AzureStorageCredential';
```

### Differential Backup (Daily)

```sql
BACKUP DATABASE Hartonomous
TO URL = 'https://hartstorage.blob.core.windows.net/backups/Hartonomous_DIFF_20251119.bak'
WITH 
    CREDENTIAL = 'AzureStorageCredential',
    DIFFERENTIAL,
    COMPRESSION,
    CHECKSUM;
```

**Automated (daily at 2 AM)**:
```sql
EXEC msdb.dbo.sp_add_job @job_name = 'Hartonomous_DifferentialBackup';

EXEC msdb.dbo.sp_add_jobstep 
    @job_name = 'Hartonomous_DifferentialBackup',
    @step_name = 'DiffBackup',
    @subsystem = 'TSQL',
    @command = N'
BACKUP DATABASE Hartonomous
TO URL = ''https://hartstorage.blob.core.windows.net/backups/Hartonomous_DIFF_'' + 
    CONVERT(NVARCHAR(8), GETDATE(), 112) + ''.bak''
WITH CREDENTIAL = ''AzureStorageCredential'', DIFFERENTIAL, COMPRESSION;';

EXEC msdb.dbo.sp_add_schedule 
    @schedule_name = 'Daily2AM',
    @freq_type = 4,  -- Daily
    @active_start_time = 20000;  -- 2:00 AM

EXEC msdb.dbo.sp_attach_schedule 
    @job_name = 'Hartonomous_DifferentialBackup',
    @schedule_name = 'Daily2AM';
```

### Transaction Log Backup (Hourly)

```sql
BACKUP LOG Hartonomous
TO URL = 'https://hartstorage.blob.core.windows.net/backups/Hartonomous_LOG_20251119_14.trn'
WITH 
    CREDENTIAL = 'AzureStorageCredential',
    COMPRESSION,
    CHECKSUM;
```

**Automated (hourly)**:
```sql
EXEC msdb.dbo.sp_add_job @job_name = 'Hartonomous_TransactionLogBackup';

EXEC msdb.dbo.sp_add_jobstep 
    @job_name = 'Hartonomous_TransactionLogBackup',
    @step_name = 'LogBackup',
    @subsystem = 'TSQL',
    @command = N'
BACKUP LOG Hartonomous
TO URL = ''https://hartstorage.blob.core.windows.net/backups/Hartonomous_LOG_'' + 
    CONVERT(NVARCHAR(8), GETDATE(), 112) + ''_'' + 
    CONVERT(NVARCHAR(2), DATEPART(HOUR, GETDATE())) + ''.trn''
WITH CREDENTIAL = ''AzureStorageCredential'', COMPRESSION;';

EXEC msdb.dbo.sp_add_schedule 
    @schedule_name = 'HourlyBackup',
    @freq_type = 4,  -- Daily
    @freq_subday_type = 8,  -- Hours
    @freq_subday_interval = 1;  -- Every 1 hour

EXEC msdb.dbo.sp_attach_schedule 
    @job_name = 'Hartonomous_TransactionLogBackup',
    @schedule_name = 'HourlyBackup';
```

**Important**: Set recovery model to FULL
```sql
ALTER DATABASE Hartonomous SET RECOVERY FULL;
ALTER DATABASE HartonomousArchive SET RECOVERY FULL;
```

## Neo4j Backups

### Snapshot Backup (Daily)

**Neo4j Enterprise snapshot**:
```bash
# Stop Neo4j for consistent backup
sudo systemctl stop neo4j

# Create snapshot
sudo neo4j-admin backup \
    --backup-dir=/backups/neo4j \
    --name=hartonomous-$(date +%Y%m%d) \
    --database=neo4j \
    --verbose

# Restart Neo4j
sudo systemctl start neo4j

# Upload to Azure Blob
azcopy copy "/backups/neo4j/hartonomous-$(date +%Y%m%d)" \
    "https://hartstorage.blob.core.windows.net/neo4j-backups/" \
    --recursive
```

**Automated via cron** (daily at 3 AM):
```bash
# /etc/cron.d/neo4j-backup
0 3 * * * root /opt/scripts/neo4j-backup.sh
```

**neo4j-backup.sh**:
```bash
#!/bin/bash
set -e

BACKUP_DIR="/backups/neo4j"
BACKUP_NAME="hartonomous-$(date +\%Y\%m\%d)"
AZURE_CONTAINER="https://hartstorage.blob.core.windows.net/neo4j-backups/"

echo "Starting Neo4j backup: $BACKUP_NAME"

# Stop Neo4j
systemctl stop neo4j

# Create backup
neo4j-admin backup \
    --backup-dir=$BACKUP_DIR \
    --name=$BACKUP_NAME \
    --database=neo4j \
    --verbose

# Restart Neo4j
systemctl start neo4j

# Upload to Azure
azcopy copy "$BACKUP_DIR/$BACKUP_NAME" $AZURE_CONTAINER --recursive

# Cleanup old backups (keep 30 days)
find $BACKUP_DIR -name "hartonomous-*" -mtime +30 -exec rm -rf {} \;

echo "Neo4j backup completed: $BACKUP_NAME"
```

### Online Backup (No Downtime)

Neo4j Enterprise supports online backups:
```bash
neo4j-admin backup \
    --backup-dir=/backups/neo4j \
    --name=hartonomous-online-$(date +%Y%m%d) \
    --database=neo4j \
    --verbose \
    --fallback-to-full=true
```

**Note**: Requires `dbms.backup.enabled=true` in neo4j.conf

## CLR Assembly Backups

### Backup CLR DLLs

```powershell
# Backup-CLRAssemblies.ps1
$BackupDir = "D:\Backups\CLR\$(Get-Date -Format 'yyyyMMdd')"
New-Item -ItemType Directory -Path $BackupDir -Force

# Copy CLR assemblies
Copy-Item "D:\Assemblies\Hartonomous.Clr.dll" $BackupDir
Copy-Item "D:\Assemblies\System.Numerics.Tensors.dll" $BackupDir
Copy-Item "D:\Assemblies\System.Memory.dll" $BackupDir

# Copy CLR deployment scripts
Copy-Item "D:\Scripts\deploy-clr.sql" $BackupDir

# Upload to Azure Blob
azcopy copy $BackupDir "https://hartstorage.blob.core.windows.net/clr-backups/" --recursive

Write-Host "CLR assemblies backed up to $BackupDir and uploaded to Azure"
```

**Run after CLR deployment**:
```powershell
# Schedule after CLR changes
.\Backup-CLRAssemblies.ps1
```

### Backup CLR Schema

Export CLR object definitions:
```sql
-- Export CLR functions/aggregates
SELECT 
    'CREATE FUNCTION ' + OBJECT_SCHEMA_NAME(object_id) + '.' + name + 
    ' AS EXTERNAL NAME [' + a.name + '].[' + af.assembly_class + '].[' + af.assembly_method + '];' AS Definition
FROM sys.objects o
INNER JOIN sys.assembly_modules af ON o.object_id = af.object_id
INNER JOIN sys.assemblies a ON af.assembly_id = a.assembly_id
WHERE o.type = 'FS'  -- Scalar function
UNION ALL
SELECT 
    'CREATE AGGREGATE ' + OBJECT_SCHEMA_NAME(object_id) + '.' + name + 
    ' AS EXTERNAL NAME [' + a.name + '].[' + af.assembly_class + '];' AS Definition
FROM sys.objects o
INNER JOIN sys.assembly_modules af ON o.object_id = af.object_id
INNER JOIN sys.assemblies a ON af.assembly_id = a.assembly_id
WHERE o.type = 'AF';  -- Aggregate function

-- Save to file: D:\Backups\CLR\clr-schema.sql
```

## Restore Procedures

### Point-in-Time Restore

**Scenario**: Restore database to 2025-11-19 14:30

**Step 1: Restore full backup**
```sql
-- Restore with NORECOVERY to allow log replay
RESTORE DATABASE Hartonomous
FROM URL = 'https://hartstorage.blob.core.windows.net/backups/Hartonomous_FULL_20251117.bak'
WITH 
    CREDENTIAL = 'AzureStorageCredential',
    REPLACE,
    NORECOVERY;
```

**Step 2: Restore differential backup**
```sql
RESTORE DATABASE Hartonomous
FROM URL = 'https://hartstorage.blob.core.windows.net/backups/Hartonomous_DIFF_20251119.bak'
WITH 
    CREDENTIAL = 'AzureStorageCredential',
    NORECOVERY;
```

**Step 3: Restore transaction logs (up to 14:30)**
```sql
-- Restore log from 13:00
RESTORE LOG Hartonomous
FROM URL = 'https://hartstorage.blob.core.windows.net/backups/Hartonomous_LOG_20251119_13.trn'
WITH 
    CREDENTIAL = 'AzureStorageCredential',
    NORECOVERY;

-- Restore log from 14:00 with STOPAT
RESTORE LOG Hartonomous
FROM URL = 'https://hartstorage.blob.core.windows.net/backups/Hartonomous_LOG_20251119_14.trn'
WITH 
    CREDENTIAL = 'AzureStorageCredential',
    STOPAT = '2025-11-19 14:30:00',
    RECOVERY;
```

**Step 4: Verify restore**
```sql
-- Check database status
SELECT name, state_desc, recovery_model_desc
FROM sys.databases
WHERE name = 'Hartonomous';

-- Verify data at restore point
SELECT COUNT(*) AS TotalAtoms
FROM dbo.Atoms
WHERE IngestTimestamp <= '2025-11-19 14:30:00';
```

### Disaster Recovery (Complete Rebuild)

**Scenario**: Total database loss, restore from backups

**Step 1: Restore SQL Server databases**
```sql
-- Restore Hartonomous (full + differential + logs)
RESTORE DATABASE Hartonomous
FROM URL = 'https://hartstorage.blob.core.windows.net/backups/Hartonomous_FULL_20251117.bak'
WITH CREDENTIAL = 'AzureStorageCredential', REPLACE, NORECOVERY;

RESTORE DATABASE Hartonomous
FROM URL = 'https://hartstorage.blob.core.windows.net/backups/Hartonomous_DIFF_20251119.bak'
WITH CREDENTIAL = 'AzureStorageCredential', NORECOVERY;

RESTORE LOG Hartonomous
FROM URL = 'https://hartstorage.blob.core.windows.net/backups/Hartonomous_LOG_20251119_14.trn'
WITH CREDENTIAL = 'AzureStorageCredential', RECOVERY;

-- Restore HartonomousArchive
RESTORE DATABASE HartonomousArchive
FROM URL = 'https://hartstorage.blob.core.windows.net/backups/HartonomousArchive_FULL_20251117.bak'
WITH CREDENTIAL = 'AzureStorageCredential', REPLACE, RECOVERY;
```

**Step 2: Restore Neo4j graph**
```bash
# Stop Neo4j
sudo systemctl stop neo4j

# Download backup from Azure
azcopy copy "https://hartstorage.blob.core.windows.net/neo4j-backups/hartonomous-20251119" \
    /backups/neo4j/hartonomous-20251119 \
    --recursive

# Restore from backup
sudo neo4j-admin restore \
    --from=/backups/neo4j/hartonomous-20251119 \
    --database=neo4j \
    --force

# Start Neo4j
sudo systemctl start neo4j
```

**Step 3: Restore CLR assemblies**
```powershell
# Download CLR assemblies from Azure
azcopy copy "https://hartstorage.blob.core.windows.net/clr-backups/20251119" `
    D:\Assemblies\ `
    --recursive

# Deploy CLR assemblies
sqlcmd -S localhost -d Hartonomous -i D:\Assemblies\deploy-clr.sql
```

**Step 4: Verify system health**
```sql
-- Check OODA loop
SELECT COUNT(*) AS RecentOODAExecutions
FROM dbo.OODALogs
WHERE StartTime >= DATEADD(HOUR, -1, SYSDATETIME());

-- Check spatial queries
SELECT TOP 10 AtomId
FROM dbo.AtomEmbeddings
WHERE SpatialGeometry.STIntersects(GEOMETRY::Point(10, 20, 0).STBuffer(30.0)) = 1;

-- Check Neo4j sync
SELECT COUNT(*) AS PendingSync
FROM dbo.Neo4jSyncQueue
WHERE IsSynced = 0;
```

## CLR Assembly Recovery

**Scenario**: CLR functions broken after SQL Server update

**Step 1: Re-deploy CLR assemblies**
```sql
-- Drop existing CLR objects
DROP AGGREGATE dbo.clr_ChainOfThoughtCoherence;
DROP AGGREGATE dbo.clr_SelfConsistency;
DROP FUNCTION dbo.clr_CosineSimilarity;
DROP FUNCTION dbo.clr_ComputeEmbedding;
DROP FUNCTION dbo.clr_LandmarkProjection_ProjectTo3D;

-- Drop assemblies
DROP ASSEMBLY [Hartonomous.Clr];
DROP ASSEMBLY [System.Numerics.Tensors];
DROP ASSEMBLY [System.Memory];

-- Redeploy from backup
CREATE ASSEMBLY [System.Memory]
FROM 'D:\Assemblies\System.Memory.dll'
WITH PERMISSION_SET = SAFE;

CREATE ASSEMBLY [System.Numerics.Tensors]
FROM 'D:\Assemblies\System.Numerics.Tensors.dll'
WITH PERMISSION_SET = SAFE;

CREATE ASSEMBLY [Hartonomous.Clr]
FROM 'D:\Assemblies\Hartonomous.Clr.dll'
WITH PERMISSION_SET = UNSAFE;

-- Recreate CLR functions/aggregates
-- (Run D:\Assemblies\deploy-clr.sql)
```

**Step 2: Test CLR functions**
```sql
-- Test cosine similarity
DECLARE @Vec1 VARBINARY(MAX) = (SELECT TOP 1 EmbeddingVector FROM dbo.AtomEmbeddings);
DECLARE @Vec2 VARBINARY(MAX) = (SELECT TOP 1 EmbeddingVector FROM dbo.AtomEmbeddings WHERE AtomId > 1000);
SELECT dbo.clr_CosineSimilarity(@Vec1, @Vec2);

-- Test spatial projection
SELECT dbo.clr_LandmarkProjection_ProjectTo3D(
    @Vec1,
    (SELECT Vector FROM dbo.SpatialLandmarks WHERE AxisAssignment = 'X'),
    (SELECT Vector FROM dbo.SpatialLandmarks WHERE AxisAssignment = 'Y'),
    (SELECT Vector FROM dbo.SpatialLandmarks WHERE AxisAssignment = 'Z'),
    42
);
```

## Retention Policy

| Backup Type | Frequency | Retention | Storage |
|-------------|-----------|-----------|---------|
| SQL Full | Weekly (Sunday 1 AM) | 4 weeks | Azure Blob (Cool tier) |
| SQL Differential | Daily (2 AM) | 7 days | Azure Blob (Hot tier) |
| SQL Transaction Log | Hourly | 7 days | Azure Blob (Hot tier) |
| Neo4j Snapshot | Daily (3 AM) | 30 days | Azure Blob (Cool tier) |
| CLR Assemblies | On change | Indefinite | Azure Blob (Archive tier) |

**Cleanup script** (weekly):
```powershell
# Cleanup-OldBackups.ps1
$RetentionDays = @{
    'Hartonomous_FULL_' = 28
    'Hartonomous_DIFF_' = 7
    'Hartonomous_LOG_' = 7
    'hartonomous-' = 30  # Neo4j
}

foreach ($prefix in $RetentionDays.Keys) {
    $cutoffDate = (Get-Date).AddDays(-$RetentionDays[$prefix])
    
    # Delete from Azure Blob (requires Azure CLI)
    $blobs = az storage blob list `
        --account-name hartstorage `
        --container-name backups `
        --prefix $prefix `
        --output json | ConvertFrom-Json
    
    foreach ($blob in $blobs) {
        $blobDate = [DateTime]::ParseExact($blob.name.Substring($prefix.Length, 8), 'yyyyMMdd', $null)
        if ($blobDate -lt $cutoffDate) {
            Write-Host "Deleting old backup: $($blob.name)"
            az storage blob delete `
                --account-name hartstorage `
                --container-name backups `
                --name $blob.name
        }
    }
}
```

## Monitoring Backup Health

### View: Backup Status

```sql
CREATE VIEW dbo.vw_BackupStatus AS
SELECT 
    database_name,
    type,
    MAX(backup_finish_date) AS LastBackup,
    DATEDIFF(HOUR, MAX(backup_finish_date), SYSDATETIME()) AS HoursSinceLastBackup,
    AVG(DATEDIFF(SECOND, backup_start_date, backup_finish_date)) AS AvgDurationSeconds
FROM msdb.dbo.backupset
WHERE database_name IN ('Hartonomous', 'HartonomousArchive')
    AND backup_finish_date >= DATEADD(DAY, -7, SYSDATETIME())
GROUP BY database_name, type;
```

**Usage**:
```sql
SELECT * FROM dbo.vw_BackupStatus;
```

**Expected Output**:

| database_name | type | LastBackup | HoursSinceLastBackup | AvgDurationSeconds |
|---------------|------|------------|----------------------|---------------------|
| Hartonomous | D (Full) | 2025-11-17 01:00 | 50 | 3600 |
| Hartonomous | I (Differential) | 2025-11-19 02:00 | 12 | 600 |
| Hartonomous | L (Log) | 2025-11-19 14:00 | 0 | 45 |

**Alerts**:
- Full backup not taken in 8 days (weekly + 1 day buffer)
- Differential not taken in 26 hours (daily + 2 hour buffer)
- Log backup not taken in 2 hours (hourly + 1 hour buffer)

## Best Practices

1. **Test Restores**: Quarterly restore test to verify backup integrity
2. **Geo-Redundancy**: Use Azure Blob GRS (geo-redundant storage)
3. **Encryption**: Enable TDE (Transparent Data Encryption) for backups
4. **Monitoring**: Alert if backups fail or exceed expected duration
5. **Retention**: Keep 4 weeks of full backups, 7 days of logs
6. **CLR Versioning**: Tag CLR assemblies with version numbers in backup filenames

## Summary

Hartonomous backup and recovery:

- **SQL Server**: Full (weekly) + Differential (daily) + Log (hourly)
- **Neo4j**: Daily snapshots with online backup support
- **CLR Assemblies**: Version-controlled backups on change
- **Retention**: 4 weeks full, 7 days differential/logs, 30 days Neo4j
- **Recovery**: Point-in-time restore via transaction logs, disaster recovery via full restore
- **Monitoring**: Track backup health, alert on failures

All backups stored in Azure Blob Storage with geo-redundancy.
