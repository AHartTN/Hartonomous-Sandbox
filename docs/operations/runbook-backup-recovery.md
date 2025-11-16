# Backup and Recovery Runbook

## Overview

This runbook provides comprehensive procedures for backing up and recovering the Hartonomous platform. The system uses SQL Server for the core database, Neo4j for provenance graphs, and file storage for models and artifacts.

## Backup Strategy

### Backup Types

**Full Backup**
- Complete copy of all databases and files
- Required: Weekly (minimum)
- Recommended: Daily for production

**Differential Backup**
- Changes since last full backup
- Recommended: Every 6 hours

**Transaction Log Backup**
- Point-in-time recovery capability
- Required: Every 15 minutes for production
- Retention: 7 days minimum

## SQL Server Database Backup

### 1. Full Database Backup

```sql
-- Full backup with compression
BACKUP DATABASE Hartonomous
TO DISK = 'D:\Backups\Hartonomous_Full_YYYYMMDD_HHMM.bak'
WITH 
    COMPRESSION,
    CHECKSUM,
    FORMAT,
    INIT,
    NAME = 'Hartonomous-Full Database Backup',
    STATS = 10;

-- Verify backup
RESTORE VERIFYONLY 
FROM DISK = 'D:\Backups\Hartonomous_Full_YYYYMMDD_HHMM.bak'
WITH CHECKSUM;
```

### 2. Differential Backup

```sql
-- Differential backup (requires recent full backup)
BACKUP DATABASE Hartonomous
TO DISK = 'D:\Backups\Hartonomous_Diff_YYYYMMDD_HHMM.bak'
WITH 
    DIFFERENTIAL,
    COMPRESSION,
    CHECKSUM,
    FORMAT,
    INIT,
    NAME = 'Hartonomous-Differential Backup',
    STATS = 10;
```

### 3. Transaction Log Backup

```sql
-- Transaction log backup (for point-in-time recovery)
BACKUP LOG Hartonomous
TO DISK = 'D:\Backups\Hartonomous_Log_YYYYMMDD_HHMM.trn'
WITH 
    COMPRESSION,
    CHECKSUM,
    FORMAT,
    INIT,
    NAME = 'Hartonomous-Transaction Log Backup',
    STATS = 10;
```

### 4. Automated Backup Script

```powershell
# Schedule this with Windows Task Scheduler
param(
    [string]$BackupPath = "D:\Backups",
    [string]$Server = "localhost",
    [int]$RetentionDays = 30
)

$timestamp = Get-Date -Format "yyyyMMdd_HHmm"
$fullBackup = "$BackupPath\Hartonomous_Full_$timestamp.bak"

# Full backup
$sql = @"
BACKUP DATABASE Hartonomous
TO DISK = '$fullBackup'
WITH COMPRESSION, CHECKSUM, FORMAT, INIT, STATS = 10;
"@

Invoke-Sqlcmd -ServerInstance $Server -Query $sql -QueryTimeout 3600

# Verify
$verifySql = "RESTORE VERIFYONLY FROM DISK = '$fullBackup' WITH CHECKSUM;"
Invoke-Sqlcmd -ServerInstance $Server -Query $verifySql

# Clean up old backups
Get-ChildItem $BackupPath -Filter "*.bak" | 
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-$RetentionDays) } |
    Remove-Item -Force

Write-Host "Backup completed: $fullBackup"
```

## Neo4j Provenance Graph Backup

### 1. Online Backup (Enterprise Edition)

```bash
# Using neo4j-admin backup
neo4j-admin backup \
    --backup-dir=/backups/neo4j \
    --name=hartonomous-$(date +%Y%m%d-%H%M) \
    --from=localhost:6362

# Verify backup
neo4j-admin check-consistency \
    --database=/backups/neo4j/hartonomous-YYYYMMDD-HHMM
```

### 2. Offline Backup (All Editions)

```bash
# Stop Neo4j
systemctl stop neo4j

# Copy database files
tar -czf /backups/neo4j/hartonomous-$(date +%Y%m%d-%H%M).tar.gz \
    /var/lib/neo4j/data/databases/hartonomous

# Start Neo4j
systemctl start neo4j
```

### 3. Export to Cypher Script

```cypher
// Export all nodes and relationships
CALL apoc.export.cypher.all(
    '/backups/neo4j/hartonomous-export-' + toString(datetime()) + '.cypher',
    {
        format: 'cypher-shell',
        useOptimizations: {type: 'UNWIND_BATCH', unwindBatchSize: 20}
    }
)
YIELD file, nodes, relationships, properties
RETURN file, nodes, relationships, properties;
```

## File Storage Backup

### Model Files and Artifacts

```powershell
# Backup model files and artifacts
$sourceDir = "D:\Hartonomous\Models"
$backupDir = "D:\Backups\Models\$(Get-Date -Format 'yyyyMMdd')"

# Create incremental backup using robocopy
robocopy $sourceDir $backupDir /MIR /Z /W:5 /R:3 /LOG:backup.log

# Compress older backups
Get-ChildItem "D:\Backups\Models" -Directory | 
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-7) } |
    ForEach-Object {
        $archive = "$($_.FullName).zip"
        if (!(Test-Path $archive)) {
            Compress-Archive -Path $_.FullName -DestinationPath $archive
            Remove-Item $_.FullName -Recurse -Force
        }
    }
```

## Complete System Backup Procedure

### Daily Backup Workflow

```powershell
# Complete daily backup script
param(
    [string]$BackupRoot = "D:\Backups"
)

$date = Get-Date -Format "yyyyMMdd"
$time = Get-Date -Format "HHmm"

# 1. SQL Server Full Backup
Write-Host "Starting SQL Server backup..."
$sqlBackup = "$BackupRoot\SQL\Hartonomous_Full_${date}_${time}.bak"
Invoke-Sqlcmd -Query @"
BACKUP DATABASE Hartonomous TO DISK = '$sqlBackup' 
WITH COMPRESSION, CHECKSUM, FORMAT, INIT;
"@

# 2. Neo4j Backup
Write-Host "Starting Neo4j backup..."
neo4j-admin backup --backup-dir="$BackupRoot\Neo4j" --name="hartonomous-$date-$time"

# 3. File Storage Backup
Write-Host "Starting file storage backup..."
robocopy "D:\Hartonomous\Models" "$BackupRoot\Files\$date" /MIR /Z

# 4. Configuration Backup
Write-Host "Backing up configuration..."
Copy-Item "C:\Program Files\Microsoft SQL Server\*\sql_instance\MSSQL\Binn\*.ini" "$BackupRoot\Config\SQL"
Copy-Item "/etc/neo4j/neo4j.conf" "$BackupRoot\Config\Neo4j"

# 5. Verify all backups
Write-Host "Verifying backups..."
Invoke-Sqlcmd -Query "RESTORE VERIFYONLY FROM DISK = '$sqlBackup' WITH CHECKSUM;"

Write-Host "Backup completed successfully"
```

## Recovery Procedures

### 1. Complete Database Recovery

**Scenario**: Complete database loss, need to restore from backup

```sql
-- Step 1: Restore full backup (NORECOVERY to allow log restores)
USE master;
GO

ALTER DATABASE Hartonomous SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
GO

RESTORE DATABASE Hartonomous
FROM DISK = 'D:\Backups\Hartonomous_Full_20251115_0200.bak'
WITH 
    NORECOVERY,
    REPLACE,
    STATS = 10;

-- Step 2: Restore differential backup (if available)
RESTORE DATABASE Hartonomous
FROM DISK = 'D:\Backups\Hartonomous_Diff_20251115_1400.bak'
WITH 
    NORECOVERY,
    STATS = 10;

-- Step 3: Restore transaction logs in sequence
RESTORE LOG Hartonomous
FROM DISK = 'D:\Backups\Hartonomous_Log_20251115_1415.trn'
WITH 
    NORECOVERY,
    STATS = 10;

RESTORE LOG Hartonomous
FROM DISK = 'D:\Backups\Hartonomous_Log_20251115_1430.trn'
WITH 
    NORECOVERY,
    STATS = 10;

-- Step 4: Final restore with RECOVERY
RESTORE DATABASE Hartonomous WITH RECOVERY;
GO

ALTER DATABASE Hartonomous SET MULTI_USER;
GO

-- Step 5: Verify database integrity
DBCC CHECKDB (Hartonomous) WITH NO_INFOMSGS;
GO
```

### 2. Point-in-Time Recovery

**Scenario**: Recover to specific point before data corruption

```sql
-- Backup tail of current log (if database is accessible)
BACKUP LOG Hartonomous
TO DISK = 'D:\Backups\Hartonomous_TailLog.trn'
WITH NO_TRUNCATE, NORECOVERY;

-- Restore to specific point in time
RESTORE DATABASE Hartonomous
FROM DISK = 'D:\Backups\Hartonomous_Full_20251115_0200.bak'
WITH NORECOVERY, REPLACE;

-- Restore logs up to the point in time
RESTORE LOG Hartonomous
FROM DISK = 'D:\Backups\Hartonomous_Log_20251115_1415.trn'
WITH 
    NORECOVERY,
    STOPAT = '2025-11-15 14:23:00';

-- Final recovery
RESTORE DATABASE Hartonomous WITH RECOVERY;
```

### 3. Neo4j Recovery

```bash
# Stop Neo4j
systemctl stop neo4j

# Remove corrupted database
rm -rf /var/lib/neo4j/data/databases/hartonomous

# Restore from backup
neo4j-admin restore \
    --from=/backups/neo4j/hartonomous-20251115-0200 \
    --database=hartonomous \
    --force

# Start Neo4j
systemctl start neo4j

# Verify consistency
neo4j-admin check-consistency --database=hartonomous
```

### 4. CLR Assembly Recovery

**Scenario**: CLR assemblies corrupted or missing after recovery

```sql
-- Drop corrupted assemblies
DROP FUNCTION IF EXISTS dbo.fn_ProjectTo3D;
DROP FUNCTION IF EXISTS dbo.clr_ComputeHilbertValue;
DROP ASSEMBLY IF EXISTS [Hartonomous.Clr];

-- Redeploy from DACPAC or manual deployment
-- Using DACPAC:
sqlpackage /Action:Publish \
    /SourceFile:Hartonomous.Database.dacpac \
    /TargetConnectionString:"Server=localhost;Database=Hartonomous;..."

-- Or manual:
CREATE ASSEMBLY [Hartonomous.Clr]
FROM 'D:\Backups\Assemblies\Hartonomous.Database.dll'
WITH PERMISSION_SET = SAFE;

-- Recreate functions
CREATE FUNCTION dbo.fn_ProjectTo3D(@vector VARBINARY(MAX))
RETURNS GEOMETRY
AS EXTERNAL NAME [Hartonomous.Clr].[Hartonomous.Clr.Core.LandmarkProjection].[ProjectTo3D];
```

## Disaster Recovery Scenarios

### Scenario 1: Server Hardware Failure

**Recovery Time Objective (RTO)**: 4 hours  
**Recovery Point Objective (RPO)**: 15 minutes

1. Provision new server hardware
2. Install SQL Server and Neo4j
3. Restore latest full + differential + logs
4. Restore Neo4j from backup
5. Restore file storage
6. Update connection strings in application
7. Run smoke tests

### Scenario 2: Data Corruption

**RTO**: 2 hours  
**RPO**: Point-in-time

1. Identify corruption point
2. Perform point-in-time recovery to before corruption
3. Verify data integrity with DBCC CHECKDB
4. Replay transactions after recovery point (if safe)

### Scenario 3: Accidental Data Deletion

**RTO**: 1 hour  
**RPO**: Point-in-time

1. Stop all write operations
2. Backup current state (tail log)
3. Restore to point before deletion
4. Extract deleted data
5. Merge with current state if needed

## Backup Verification

### Weekly Verification Procedure

```sql
-- Restore to test server and verify
RESTORE DATABASE Hartonomous_Test
FROM DISK = 'D:\Backups\Hartonomous_Full_Latest.bak'
WITH 
    MOVE 'Hartonomous' TO 'D:\TestRestore\Hartonomous.mdf',
    MOVE 'Hartonomous_log' TO 'D:\TestRestore\Hartonomous_log.ldf',
    REPLACE;

-- Run integrity checks
DBCC CHECKDB (Hartonomous_Test) WITH NO_INFOMSGS;

-- Verify critical tables have data
SELECT 
    'Atoms' AS TableName, COUNT(*) AS RowCount FROM Hartonomous_Test.dbo.Atoms
UNION ALL
SELECT 'AtomEmbeddings', COUNT(*) FROM Hartonomous_Test.dbo.AtomEmbeddings
UNION ALL
SELECT 'Sources', COUNT(*) FROM Hartonomous_Test.dbo.Sources;

-- Verify CLR functions work
DECLARE @testVec VARBINARY(MAX) = CAST(REPLICATE(0x3F800000, 1998) AS VARBINARY(MAX));
SELECT dbo.fn_ProjectTo3D(@testVec).STAsText();

-- Clean up
DROP DATABASE Hartonomous_Test;
```

## Backup Monitoring

### Key Metrics to Monitor

```sql
-- Check backup history
SELECT TOP 10
    database_name,
    backup_start_date,
    backup_finish_date,
    DATEDIFF(MINUTE, backup_start_date, backup_finish_date) AS duration_minutes,
    backup_size / 1024 / 1024 AS backup_size_mb,
    compressed_backup_size / 1024 / 1024 AS compressed_size_mb,
    type -- D=Full, I=Differential, L=Log
FROM msdb.dbo.backupset
WHERE database_name = 'Hartonomous'
ORDER BY backup_start_date DESC;

-- Check for failed backups
SELECT 
    session_id,
    start_time,
    percent_complete,
    estimated_completion_time,
    command
FROM sys.dm_exec_requests
WHERE command LIKE 'BACKUP%';
```

### Alert Thresholds

- No full backup in 24 hours: CRITICAL
- No log backup in 30 minutes: WARNING
- Backup size increased >50%: INVESTIGATE
- Backup duration >2x normal: WARNING
- Backup verification failed: CRITICAL

## Retention Policy

**Production**:
- Full backups: 30 days
- Differential backups: 7 days
- Transaction logs: 7 days
- Monthly archival: 1 year

**Development/Test**:
- Full backups: 7 days
- Differential backups: 3 days
- Transaction logs: 1 day

## Compliance and Security

### Encryption

```sql
-- Backup with encryption (requires certificate)
CREATE CERTIFICATE Hartonomous_Backup_Cert
WITH SUBJECT = 'Hartonomous Backup Encryption Certificate';

BACKUP DATABASE Hartonomous
TO DISK = 'D:\Backups\Hartonomous_Encrypted.bak'
WITH 
    COMPRESSION,
    ENCRYPTION (
        ALGORITHM = AES_256,
        SERVER CERTIFICATE = Hartonomous_Backup_Cert
    );
```

### Offsite Storage

```powershell
# Copy to offsite/cloud storage
$backupFiles = Get-ChildItem "D:\Backups" -Recurse -File
foreach ($file in $backupFiles) {
    # Azure Blob Storage example
    az storage blob upload `
        --account-name hartonomousbackups `
        --container-name backups `
        --name $file.Name `
        --file $file.FullName `
        --tier Cool
}
```

## Recovery Testing Schedule

- **Monthly**: Restore to test environment, verify data integrity
- **Quarterly**: Full disaster recovery drill
- **Annually**: Complete system rebuild from backups

## Checklist

**Daily**:
- [ ] Verify automated backups completed
- [ ] Check backup logs for errors
- [ ] Monitor backup storage space

**Weekly**:
- [ ] Test restore to test environment
- [ ] Verify backup integrity with RESTORE VERIFYONLY
- [ ] Review backup sizes and durations

**Monthly**:
- [ ] Full disaster recovery test
- [ ] Update documentation
- [ ] Review retention policies
- [ ] Archive to offsite storage

**Quarterly**:
- [ ] Disaster recovery drill with stakeholders
- [ ] Update RTO/RPO metrics
- [ ] Review and update procedures
