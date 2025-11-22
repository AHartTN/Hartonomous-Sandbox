# Hartonomous Backup & Recovery Guide

**SQL Server Backup Strategies | Neo4j Backup | Disaster Recovery | Point-in-Time Recovery**

---

## Table of Contents

1. [Overview](#overview)
2. [SQL Server Backup Strategies](#sql-server-backup-strategies)
3. [Neo4j Backup Procedures](#neo4j-backup-procedures)
4. [Disaster Recovery Plans](#disaster-recovery-plans)
5. [Point-in-Time Recovery](#point-in-time-recovery)
6. [Cross-Database Consistency](#cross-database-consistency)
7. [Backup Verification](#backup-verification)
8. [Automated Backup Scripts](#automated-backup-scripts)
9. [Recovery Procedures](#recovery-procedures)
10. [Testing & Validation](#testing--validation)

---

## Overview

Hartonomous backup strategy implements **multi-tier backup approach**:

- **SQL Server**: Full + Differential + Transaction Log backups
- **Neo4j**: Graph database snapshots + transaction logs
- **Cross-Database**: Coordinated backup timestamps for consistency
- **Azure Blob Storage**: Off-site backup replication (GRS)

**Recovery Objectives**:
- **RPO (Recovery Point Objective)**: <15 minutes (transaction log backups every 15 min)
- **RTO (Recovery Time Objective)**: <2 hours (full database restore)
- **Data Loss Tolerance**: Max 15 minutes of data (log backup frequency)

**Backup Schedule**:
```
Full Backup:       Weekly (Sunday 2:00 AM)
Differential:      Daily (2:00 AM, except Sunday)
Transaction Log:   Every 15 minutes
Neo4j Snapshot:    Daily (2:30 AM)
Azure Replication: Continuous (GRS storage)
```

---

## SQL Server Backup Strategies

### Full Backup Strategy

#### Weekly Full Backup

**PowerShell Script**: `scripts/backup/full-backup.ps1`

```powershell
<#
.SYNOPSIS
    Perform full database backup to local and Azure Blob Storage
.PARAMETER Database
    Database name (default: Hartonomous)
.PARAMETER BackupPath
    Local backup path (default: D:\Backups\SQL\Full)
.PARAMETER AzureStorageAccount
    Azure Storage Account name for off-site replication
#>
param(
    [string]$Server = "localhost",
    [string]$Database = "Hartonomous",
    [string]$BackupPath = "D:\Backups\SQL\Full",
    [string]$AzureStorageAccount = "sahartonomousbackup",
    [string]$AzureContainer = "sql-backups"
)

$ErrorActionPreference = "Stop"

# Create backup directory if not exists
if (-not (Test-Path $BackupPath)) {
    New-Item -Path $BackupPath -ItemType Directory -Force | Out-Null
}

# Generate backup filename with timestamp
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = Join-Path $BackupPath "$Database`_Full_$timestamp.bak"

Write-Host "Starting full backup: $Database" -ForegroundColor Cyan
Write-Host "Backup file: $backupFile" -ForegroundColor Gray

# Execute full backup
$backupQuery = @"
BACKUP DATABASE [$Database]
TO DISK = N'$backupFile'
WITH 
    COMPRESSION,
    CHECKSUM,
    STATS = 10,
    NAME = N'$Database Full Backup $timestamp',
    DESCRIPTION = N'Full database backup with compression and checksum';
"@

Invoke-Sqlcmd -ServerInstance $Server -Query $backupQuery -QueryTimeout 3600 -Verbose

# Verify backup
Write-Host "Verifying backup integrity..." -ForegroundColor Cyan
$verifyQuery = @"
RESTORE VERIFYONLY 
FROM DISK = N'$backupFile'
WITH CHECKSUM;
"@

Invoke-Sqlcmd -ServerInstance $Server -Query $verifyQuery -QueryTimeout 600

# Get backup file size
$backupSize = (Get-Item $backupFile).Length / 1MB
Write-Host "✓ Backup completed: $($backupSize.ToString('F2')) MB" -ForegroundColor Green

# Copy to Azure Blob Storage (off-site replication)
if ($AzureStorageAccount) {
    Write-Host "Uploading to Azure Blob Storage..." -ForegroundColor Cyan
    
    $blobName = "$Database/Full/$timestamp/$Database`_Full_$timestamp.bak"
    
    az storage blob upload `
        --account-name $AzureStorageAccount `
        --container-name $AzureContainer `
        --name $blobName `
        --file $backupFile `
        --tier Hot
    
    Write-Host "✓ Uploaded to Azure: $blobName" -ForegroundColor Green
}

# Cleanup old backups (keep last 4 weeks = 28 days)
$retentionDays = 28
$oldBackups = Get-ChildItem -Path $BackupPath -Filter "$Database`_Full_*.bak" |
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-$retentionDays) }

if ($oldBackups) {
    Write-Host "Cleaning up $($oldBackups.Count) old backups..." -ForegroundColor Yellow
    $oldBackups | Remove-Item -Force
}

Write-Host "`n=== Full Backup Summary ===" -ForegroundColor Cyan
Write-Host "Database:    $Database" -ForegroundColor White
Write-Host "Backup File: $backupFile" -ForegroundColor White
Write-Host "Size:        $($backupSize.ToString('F2')) MB" -ForegroundColor White
Write-Host "Status:      ✓ Verified" -ForegroundColor Green
```

### Differential Backup Strategy

#### Daily Differential Backup

**T-SQL Script**: `scripts/backup/differential-backup.sql`

```sql
DECLARE @Database NVARCHAR(128) = 'Hartonomous';
DECLARE @BackupPath NVARCHAR(512) = 'D:\Backups\SQL\Differential';
DECLARE @Timestamp NVARCHAR(20) = CONVERT(NVARCHAR(20), GETDATE(), 112) + '_' + REPLACE(CONVERT(NVARCHAR(8), GETDATE(), 108), ':', '');
DECLARE @BackupFile NVARCHAR(512);

-- Create backup directory (SQL Server service account needs permissions)
EXEC xp_create_subdir @BackupPath;

-- Generate backup filename
SET @BackupFile = @BackupPath + '\' + @Database + '_Diff_' + @Timestamp + '.bak';

PRINT 'Starting differential backup: ' + @Database;
PRINT 'Backup file: ' + @BackupFile;

-- Execute differential backup
BACKUP DATABASE @Database
TO DISK = @BackupFile
WITH 
    DIFFERENTIAL,
    COMPRESSION,
    CHECKSUM,
    STATS = 10,
    NAME = @Database + ' Differential Backup ' + @Timestamp,
    DESCRIPTION = 'Differential backup (changes since last full backup)';

-- Verify backup
PRINT 'Verifying backup integrity...';
RESTORE VERIFYONLY 
FROM DISK = @BackupFile
WITH CHECKSUM;

PRINT '✓ Differential backup completed and verified';
GO
```

### Transaction Log Backup Strategy

#### 15-Minute Transaction Log Backups

**PowerShell Script**: `scripts/backup/log-backup.ps1`

```powershell
<#
.SYNOPSIS
    Perform transaction log backup every 15 minutes (run as scheduled task)
#>
param(
    [string]$Server = "localhost",
    [string]$Database = "Hartonomous",
    [string]$BackupPath = "D:\Backups\SQL\Logs"
)

$ErrorActionPreference = "Stop"

# Create backup directory
if (-not (Test-Path $BackupPath)) {
    New-Item -Path $BackupPath -ItemType Directory -Force | Out-Null
}

# Generate backup filename with precise timestamp
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = Join-Path $BackupPath "$Database`_Log_$timestamp.trn"

# Execute transaction log backup
$backupQuery = @"
BACKUP LOG [$Database]
TO DISK = N'$backupFile'
WITH 
    COMPRESSION,
    CHECKSUM,
    STATS = 10,
    NAME = N'$Database Transaction Log Backup $timestamp';
"@

Invoke-Sqlcmd -ServerInstance $Server -Query $backupQuery -QueryTimeout 300

# Verify backup
$verifyQuery = "RESTORE VERIFYONLY FROM DISK = N'$backupFile' WITH CHECKSUM;"
Invoke-Sqlcmd -ServerInstance $Server -Query $verifyQuery -QueryTimeout 60

# Log backup completed
$logSize = (Get-Item $backupFile).Length / 1KB
Write-Host "✓ Log backup: $($logSize.ToString('F2')) KB" -ForegroundColor Green

# Cleanup logs older than 7 days
$retentionDays = 7
Get-ChildItem -Path $BackupPath -Filter "$Database`_Log_*.trn" |
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-$retentionDays) } |
    Remove-Item -Force
```

**Schedule as Windows Task**:

```powershell
# Create scheduled task for transaction log backups (every 15 minutes)
$action = New-ScheduledTaskAction `
    -Execute "powershell.exe" `
    -Argument "-ExecutionPolicy Bypass -File `"D:\Repositories\Hartonomous\scripts\backup\log-backup.ps1`""

$trigger = New-ScheduledTaskTrigger -Once -At (Get-Date) -RepetitionInterval (New-TimeSpan -Minutes 15) -RepetitionDuration ([TimeSpan]::MaxValue)

$principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -RunLevel Highest

Register-ScheduledTask `
    -TaskName "Hartonomous_LogBackup" `
    -Action $action `
    -Trigger $trigger `
    -Principal $principal `
    -Description "Transaction log backup every 15 minutes for point-in-time recovery"
```

---

## Neo4j Backup Procedures

### Neo4j Snapshot Backup

**Bash Script**: `scripts/backup/neo4j-backup.sh`

```bash
#!/bin/bash
#
# Neo4j graph database backup (Linux)
# Run daily at 2:30 AM (after SQL full/differential backup)
#

DATABASE="neo4j"
BACKUP_DIR="/var/backups/neo4j"
NEO4J_HOME="/var/lib/neo4j"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_NAME="neo4j_backup_${TIMESTAMP}"

echo "Starting Neo4j backup: $DATABASE"
echo "Backup directory: $BACKUP_DIR/$BACKUP_NAME"

# Create backup directory
mkdir -p "$BACKUP_DIR"

# Execute Neo4j backup (online backup - requires Neo4j Enterprise)
# Option 1: Neo4j Enterprise (online backup)
if command -v neo4j-admin &> /dev/null; then
    neo4j-admin database backup \
        --database=$DATABASE \
        --to-path="$BACKUP_DIR/$BACKUP_NAME" \
        --verbose
else
    # Option 2: Community Edition (stop database, copy files)
    echo "Stopping Neo4j service..."
    sudo systemctl stop neo4j
    
    # Copy database files
    cp -r "$NEO4J_HOME/data/databases/$DATABASE" "$BACKUP_DIR/$BACKUP_NAME"
    cp -r "$NEO4J_HOME/data/transactions/$DATABASE" "$BACKUP_DIR/$BACKUP_NAME/transactions"
    
    # Restart Neo4j
    echo "Restarting Neo4j service..."
    sudo systemctl start neo4j
fi

# Compress backup
cd "$BACKUP_DIR"
tar -czf "${BACKUP_NAME}.tar.gz" "$BACKUP_NAME"
rm -rf "$BACKUP_NAME"

# Get backup size
BACKUP_SIZE=$(du -h "${BACKUP_NAME}.tar.gz" | cut -f1)
echo "✓ Neo4j backup completed: $BACKUP_SIZE"

# Upload to Azure Blob Storage
AZURE_STORAGE_ACCOUNT="sahartonomousbackup"
AZURE_CONTAINER="neo4j-backups"
BLOB_NAME="neo4j/$TIMESTAMP/${BACKUP_NAME}.tar.gz"

echo "Uploading to Azure Blob Storage..."
az storage blob upload \
    --account-name "$AZURE_STORAGE_ACCOUNT" \
    --container-name "$AZURE_CONTAINER" \
    --name "$BLOB_NAME" \
    --file "$BACKUP_DIR/${BACKUP_NAME}.tar.gz" \
    --tier Hot

echo "✓ Uploaded to Azure: $BLOB_NAME"

# Cleanup old backups (keep last 14 days)
find "$BACKUP_DIR" -name "neo4j_backup_*.tar.gz" -mtime +14 -delete

echo "=== Neo4j Backup Summary ==="
echo "Database:    $DATABASE"
echo "Backup File: ${BACKUP_NAME}.tar.gz"
echo "Size:        $BACKUP_SIZE"
echo "Status:      ✓ Completed"
```

**Schedule via Cron**:

```bash
# Edit crontab
crontab -e

# Add daily backup at 2:30 AM
30 2 * * * /var/scripts/neo4j-backup.sh >> /var/log/neo4j-backup.log 2>&1
```

---

## Disaster Recovery Plans

### Scenario 1: Database Corruption

**Detection**:
```sql
-- Check database integrity
DBCC CHECKDB (Hartonomous) WITH NO_INFOMSGS;
```

**Recovery Steps**:

1. **Assess Damage**:
```sql
-- Identify corrupted objects
DBCC CHECKDB (Hartonomous) WITH TABLERESULTS;
```

2. **Restore from Last Good Backup**:
```powershell
# Restore latest full backup + differentials + logs
.\scripts\recovery\restore-to-latest.ps1 -Database Hartonomous
```

3. **Verify Data Integrity**:
```sql
-- Re-check database
DBCC CHECKDB (Hartonomous) WITH NO_INFOMSGS;

-- Verify critical tables
SELECT COUNT(*) FROM dbo.Atom;
SELECT COUNT(*) FROM dbo.AtomEmbedding;
SELECT COUNT(*) FROM dbo.TensorAtom;

-- Verify spatial indexes
SELECT name, type_desc FROM sys.indexes WHERE type_desc = 'SPATIAL';
```

### Scenario 2: Hardware Failure (SQL Server Host)

**Recovery Steps**:

1. **Provision New Hardware**:
   - Install Windows Server 2025
   - Install SQL Server 2025 RC1
   - Install Azure Arc agent
   - Configure same drive letters (D:\, E:\, etc.)

2. **Restore System Databases**:
```sql
-- Restore master database (single-user mode)
sqlservr.exe -m

RESTORE DATABASE master 
FROM DISK = 'D:\Backups\System\master.bak'
WITH REPLACE;
```

3. **Restore Hartonomous Database**:
```powershell
# Full restore chain (full + diff + logs)
.\scripts\recovery\full-restore.ps1 `
    -Server "NEW-SERVER" `
    -Database "Hartonomous" `
    -FullBackup "D:\Backups\SQL\Full\Hartonomous_Full_20250119_020000.bak" `
    -DifferentialBackup "D:\Backups\SQL\Differential\Hartonomous_Diff_20250125_020000.bak" `
    -LogBackupsPath "D:\Backups\SQL\Logs"
```

4. **Deploy CLR Assemblies**:
```powershell
# Re-deploy CLR dependencies + main assembly
.\scripts\deploy-clr-assemblies.ps1 -Server "NEW-SERVER" -Database "master"
```

5. **Verify Recovery**:
```sql
-- Check database status
SELECT name, state_desc, recovery_model_desc FROM sys.databases WHERE name = 'Hartonomous';

-- Verify CLR assemblies
SELECT name, permission_set_desc FROM sys.assemblies WHERE name NOT IN ('master', 'msdb', 'model', 'tempdb');

-- Verify spatial indexes
SELECT OBJECT_NAME(object_id) AS TableName, name AS IndexName, type_desc
FROM sys.indexes
WHERE type_desc = 'SPATIAL';

-- Test inference query
EXEC dbo.sp_SpatialNextToken @context_atom_ids = '1,2,3', @temperature = 0.7, @top_k = 10;
```

### Scenario 3: Complete Site Disaster (Fire, Flood, etc.)

**Recovery Steps** (Azure Geo-Redundant Storage):

1. **Access Azure Backup**:
```powershell
# List available backups in Azure Blob Storage
az storage blob list `
    --account-name "sahartonomousbackup" `
    --container-name "sql-backups" `
    --prefix "Hartonomous/Full" `
    --output table
```

2. **Download Latest Backups**:
```powershell
# Download full backup
az storage blob download `
    --account-name "sahartonomousbackup" `
    --container-name "sql-backups" `
    --name "Hartonomous/Full/20250119_020000/Hartonomous_Full_20250119_020000.bak" `
    --file "D:\Restore\Hartonomous_Full.bak"

# Download differential backup
az storage blob download `
    --account-name "sahartonomousbackup" `
    --container-name "sql-backups" `
    --name "Hartonomous/Differential/20250125_020000/Hartonomous_Diff_20250125_020000.bak" `
    --file "D:\Restore\Hartonomous_Diff.bak"

# Download transaction logs (last 24 hours)
az storage blob download-batch `
    --account-name "sahartonomousbackup" `
    --source "sql-backups" `
    --pattern "Hartonomous/Logs/202501*" `
    --destination "D:\Restore\Logs"
```

3. **Restore to New Location**:
```powershell
# Restore to temporary Azure SQL VM or new on-prem server
.\scripts\recovery\full-restore.ps1 `
    -Server "DISASTER-RECOVERY-SERVER" `
    -Database "Hartonomous" `
    -FullBackup "D:\Restore\Hartonomous_Full.bak" `
    -DifferentialBackup "D:\Restore\Hartonomous_Diff.bak" `
    -LogBackupsPath "D:\Restore\Logs" `
    -RecoveryMode "RECOVERY"  # Bring database online
```

4. **Restore Neo4j**:
```bash
# Download Neo4j backup from Azure
az storage blob download \
    --account-name "sahartonomousbackup" \
    --container-name "neo4j-backups" \
    --name "neo4j/20250125_023000/neo4j_backup_20250125_023000.tar.gz" \
    --file "/var/restore/neo4j_backup.tar.gz"

# Extract and restore
cd /var/restore
tar -xzf neo4j_backup.tar.gz

# Stop Neo4j
sudo systemctl stop neo4j

# Replace database files
sudo rm -rf /var/lib/neo4j/data/databases/neo4j
sudo cp -r neo4j_backup_20250125_023000 /var/lib/neo4j/data/databases/neo4j
sudo chown -R neo4j:neo4j /var/lib/neo4j/data

# Start Neo4j
sudo systemctl start neo4j
```

5. **Update DNS/Load Balancer**:
```powershell
# Point production DNS to disaster recovery server
az network traffic-manager endpoint update `
    --name "disaster-recovery" `
    --profile-name "hartonomous-tm" `
    --resource-group "rg-hartonomous-prod" `
    --type azureEndpoints `
    --priority 1
```

---

## Point-in-Time Recovery

### Restore to Specific Date/Time

**PowerShell Script**: `scripts/recovery/restore-to-point-in-time.ps1`

```powershell
<#
.SYNOPSIS
    Restore database to specific point in time
.PARAMETER PointInTime
    Recovery target date/time (e.g., "2025-01-25 14:30:00")
#>
param(
    [string]$Server = "localhost",
    [string]$Database = "Hartonomous",
    [Parameter(Mandatory=$true)]
    [datetime]$PointInTime,
    [string]$RestoreDatabase = "Hartonomous_PITR",
    [string]$FullBackupPath = "D:\Backups\SQL\Full",
    [string]$DifferentialBackupPath = "D:\Backups\SQL\Differential",
    [string]$LogBackupPath = "D:\Backups\SQL\Logs"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Point-in-Time Recovery ===" -ForegroundColor Cyan
Write-Host "Target Time: $($PointInTime.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor White
Write-Host "Restore As:  $RestoreDatabase" -ForegroundColor White

# Find latest full backup before target time
$fullBackup = Get-ChildItem -Path $FullBackupPath -Filter "$Database`_Full_*.bak" |
    Where-Object { 
        $_.LastWriteTime -le $PointInTime 
    } |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $fullBackup) {
    throw "No full backup found before $PointInTime"
}

Write-Host "`nStep 1: Restoring full backup..." -ForegroundColor Yellow
Write-Host "  File: $($fullBackup.Name)" -ForegroundColor Gray

# Restore full backup with NORECOVERY
$restoreFullQuery = @"
RESTORE DATABASE [$RestoreDatabase]
FROM DISK = N'$($fullBackup.FullName)'
WITH 
    MOVE N'Hartonomous' TO N'D:\SQLData\$RestoreDatabase.mdf',
    MOVE N'Hartonomous_log' TO N'D:\SQLData\$RestoreDatabase`_log.ldf',
    NORECOVERY,
    REPLACE,
    STATS = 10;
"@

Invoke-Sqlcmd -ServerInstance $Server -Query $restoreFullQuery -QueryTimeout 3600

# Find latest differential backup (if exists)
$diffBackup = Get-ChildItem -Path $DifferentialBackupPath -Filter "$Database`_Diff_*.bak" |
    Where-Object { 
        $_.LastWriteTime -gt $fullBackup.LastWriteTime -and
        $_.LastWriteTime -le $PointInTime 
    } |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if ($diffBackup) {
    Write-Host "`nStep 2: Restoring differential backup..." -ForegroundColor Yellow
    Write-Host "  File: $($diffBackup.Name)" -ForegroundColor Gray
    
    $restoreDiffQuery = @"
RESTORE DATABASE [$RestoreDatabase]
FROM DISK = N'$($diffBackup.FullName)'
WITH 
    NORECOVERY,
    STATS = 10;
"@
    
    Invoke-Sqlcmd -ServerInstance $Server -Query $restoreDiffQuery -QueryTimeout 1800
    $lastRestoredTime = $diffBackup.LastWriteTime
} else {
    $lastRestoredTime = $fullBackup.LastWriteTime
}

# Find all transaction log backups after last restored backup
$logBackups = Get-ChildItem -Path $LogBackupPath -Filter "$Database`_Log_*.trn" |
    Where-Object { 
        $_.LastWriteTime -gt $lastRestoredTime -and
        $_.LastWriteTime -le $PointInTime.AddMinutes(15)  # Allow 15-min buffer
    } |
    Sort-Object LastWriteTime

Write-Host "`nStep 3: Restoring transaction logs..." -ForegroundColor Yellow
Write-Host "  Log count: $($logBackups.Count)" -ForegroundColor Gray

$logCount = 0
foreach ($log in $logBackups) {
    $logCount++
    Write-Host "  [$logCount/$($logBackups.Count)] $($log.Name)" -ForegroundColor Gray
    
    # Determine if this is the last log (use STOPAT)
    $isLastLog = ($log -eq $logBackups[-1])
    
    if ($isLastLog) {
        # Last log - restore with STOPAT and RECOVERY
        $restoreLogQuery = @"
RESTORE LOG [$RestoreDatabase]
FROM DISK = N'$($log.FullName)'
WITH 
    STOPAT = N'$($PointInTime.ToString('yyyy-MM-dd HH:mm:ss'))',
    RECOVERY,
    STATS = 10;
"@
    } else {
        # Intermediate log - restore with NORECOVERY
        $restoreLogQuery = @"
RESTORE LOG [$RestoreDatabase]
FROM DISK = N'$($log.FullName)'
WITH 
    NORECOVERY,
    STATS = 10;
"@
    }
    
    Invoke-Sqlcmd -ServerInstance $Server -Query $restoreLogQuery -QueryTimeout 600
}

# If no logs were restored, bring database online
if ($logBackups.Count -eq 0) {
    Write-Host "`nBringing database online..." -ForegroundColor Yellow
    $recoveryQuery = "RESTORE DATABASE [$RestoreDatabase] WITH RECOVERY;"
    Invoke-Sqlcmd -ServerInstance $Server -Query $recoveryQuery -QueryTimeout 300
}

Write-Host "`n=== Recovery Summary ===" -ForegroundColor Cyan
Write-Host "Database:    $RestoreDatabase" -ForegroundColor White
Write-Host "Target Time: $($PointInTime.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor White
Write-Host "Full Backup: $($fullBackup.Name)" -ForegroundColor White
if ($diffBackup) {
    Write-Host "Diff Backup: $($diffBackup.Name)" -ForegroundColor White
}
Write-Host "Log Backups: $($logBackups.Count)" -ForegroundColor White
Write-Host "Status:      ✓ Database Online" -ForegroundColor Green

# Verify data
Write-Host "`nVerifying restored data..." -ForegroundColor Cyan
$verifyQuery = @"
USE [$RestoreDatabase];
SELECT 
    'Atom' AS TableName, COUNT(*) AS RowCount FROM dbo.Atom
UNION ALL
SELECT 'AtomEmbedding', COUNT(*) FROM dbo.AtomEmbedding
UNION ALL
SELECT 'TensorAtom', COUNT(*) FROM dbo.TensorAtom;
"@

Invoke-Sqlcmd -ServerInstance $Server -Query $verifyQuery | Format-Table
```

---

## Cross-Database Consistency

### Coordinated Backup Timestamps

**Ensure SQL Server and Neo4j backups use same timestamp**:

**Master Backup Script**: `scripts/backup/coordinated-backup.ps1`

```powershell
<#
.SYNOPSIS
    Coordinated backup of SQL Server + Neo4j with same timestamp
#>
param(
    [string]$SqlServer = "localhost",
    [string]$Database = "Hartonomous",
    [string]$Neo4jHost = "localhost"
)

$ErrorActionPreference = "Stop"

# Generate shared timestamp
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$utcTimestamp = (Get-Date).ToUniversalTime()

Write-Host "=== Coordinated Backup ===" -ForegroundColor Cyan
Write-Host "Timestamp:   $timestamp" -ForegroundColor White
Write-Host "UTC:         $($utcTimestamp.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor White

# Step 1: Backup SQL Server
Write-Host "`nStep 1: SQL Server full backup..." -ForegroundColor Yellow
& "$PSScriptRoot\full-backup.ps1" -Server $SqlServer -Database $Database

# Step 2: Backup Neo4j
Write-Host "`nStep 2: Neo4j snapshot backup..." -ForegroundColor Yellow
if ($IsLinux) {
    & "$PSScriptRoot\neo4j-backup.sh"
} else {
    # Windows Neo4j backup
    neo4j-admin database backup --database=neo4j --to-path="D:\Backups\Neo4j\$timestamp"
}

# Step 3: Record coordinated backup metadata
Write-Host "`nStep 3: Recording backup metadata..." -ForegroundColor Yellow
$metadataQuery = @"
INSERT INTO dbo.BackupHistory (BackupTimestamp, BackupType, SqlBackupFile, Neo4jBackupFile, BackupStatus)
VALUES (
    '$($utcTimestamp.ToString('yyyy-MM-dd HH:mm:ss'))',
    'Coordinated Full',
    'Hartonomous_Full_$timestamp.bak',
    'neo4j_backup_$timestamp.tar.gz',
    'Completed'
);
"@

Invoke-Sqlcmd -ServerInstance $SqlServer -Database $Database -Query $metadataQuery

Write-Host "`n✓ Coordinated backup completed" -ForegroundColor Green
Write-Host "Timestamp: $timestamp" -ForegroundColor White
```

### Cross-Database Restore Validation

```sql
-- Verify cross-database consistency after restore

-- 1. Check atom counts match between SQL and Neo4j
DECLARE @SqlAtomCount BIGINT;
DECLARE @Neo4jAtomCount BIGINT;

SELECT @SqlAtomCount = COUNT(*) FROM dbo.Atom;

-- Query Neo4j via sp_invoke_external_rest_endpoint (SQL Server 2025)
DECLARE @Neo4jResponse NVARCHAR(MAX);
EXEC sp_invoke_external_rest_endpoint 
    @url = 'http://localhost:7474/db/neo4j/tx/commit',
    @method = 'POST',
    @headers = '{"Content-Type": "application/json", "Accept": "application/json"}',
    @payload = '{"statements":[{"statement":"MATCH (a:Atom) RETURN count(a) AS atomCount"}]}',
    @response = @Neo4jResponse OUTPUT;

SELECT @Neo4jAtomCount = JSON_VALUE(@Neo4jResponse, '$.results[0].data[0].row[0]');

IF @SqlAtomCount = @Neo4jAtomCount
    PRINT '✓ Atom counts match: ' + CAST(@SqlAtomCount AS NVARCHAR(20));
ELSE
    RAISERROR('Atom count mismatch: SQL=%d, Neo4j=%d', 16, 1, @SqlAtomCount, @Neo4jAtomCount);

-- 2. Verify provenance graph consistency
-- (Check that all atoms with provenance in SQL exist in Neo4j)
SELECT COUNT(*) AS MissingAtoms
FROM dbo.AtomProvenance ap
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Neo4jSyncLog nsl
    WHERE nsl.EntityType = 'Atom' 
        AND nsl.EntityId = CAST(ap.AtomId AS NVARCHAR(50))
        AND nsl.Status = 'Synced'
);
```

---

## Backup Verification

### Automated Backup Testing

**T-SQL Verification Script**: `scripts/backup/verify-backup.sql`

```sql
-- Comprehensive backup verification procedure
CREATE OR ALTER PROCEDURE dbo.sp_VerifyBackup
    @BackupFile NVARCHAR(512)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ErrorMsg NVARCHAR(MAX);
    DECLARE @StartTime DATETIME2 = GETDATE();
    
    PRINT 'Verifying backup: ' + @BackupFile;
    
    -- 1. RESTORE VERIFYONLY (checks backup integrity)
    BEGIN TRY
        RESTORE VERIFYONLY 
        FROM DISK = @BackupFile
        WITH CHECKSUM;
        
        PRINT '✓ Backup file integrity: PASS';
    END TRY
    BEGIN CATCH
        SET @ErrorMsg = 'Backup verification failed: ' + ERROR_MESSAGE();
        RAISERROR(@ErrorMsg, 16, 1);
        RETURN;
    END CATCH
    
    -- 2. RESTORE HEADERONLY (backup metadata)
    CREATE TABLE #BackupHeader (
        BackupName NVARCHAR(128),
        BackupDescription NVARCHAR(255),
        BackupType SMALLINT,
        ExpirationDate DATETIME,
        Compressed BIT,
        Position SMALLINT,
        DeviceType SMALLINT,
        UserName NVARCHAR(128),
        ServerName NVARCHAR(128),
        DatabaseName NVARCHAR(128),
        DatabaseVersion INT,
        DatabaseCreationDate DATETIME,
        BackupSize NUMERIC(20,0),
        FirstLSN NUMERIC(25,0),
        LastLSN NUMERIC(25,0),
        CheckpointLSN NUMERIC(25,0),
        DatabaseBackupLSN NUMERIC(25,0),
        BackupStartDate DATETIME,
        BackupFinishDate DATETIME,
        SortOrder SMALLINT,
        CodePage SMALLINT,
        UnicodeLocaleId INT,
        UnicodeComparisonStyle INT,
        CompatibilityLevel TINYINT,
        SoftwareVendorId INT,
        SoftwareVersionMajor INT,
        SoftwareVersionMinor INT,
        SoftwareVersionBuild INT,
        MachineName NVARCHAR(128),
        Flags INT,
        BindingID UNIQUEIDENTIFIER,
        RecoveryForkID UNIQUEIDENTIFIER,
        Collation NVARCHAR(128),
        FamilyGUID UNIQUEIDENTIFIER,
        HasBulkLoggedData BIT,
        IsSnapshot BIT,
        IsReadOnly BIT,
        IsSingleUser BIT,
        HasBackupChecksums BIT,
        IsDamaged BIT,
        BeginsLogChain BIT,
        HasIncompleteMetaData BIT,
        IsForceOffline BIT,
        IsCopyOnly BIT,
        FirstRecoveryForkID UNIQUEIDENTIFIER,
        ForkPointLSN NUMERIC(25,0),
        RecoveryModel NVARCHAR(60),
        DifferentialBaseLSN NUMERIC(25,0),
        DifferentialBaseGUID UNIQUEIDENTIFIER,
        BackupTypeDescription NVARCHAR(60),
        BackupSetGUID UNIQUEIDENTIFIER,
        CompressedBackupSize BIGINT,
        containment TINYINT,
        KeyAlgorithm NVARCHAR(32),
        EncryptorThumbprint VARBINARY(20),
        EncryptorType NVARCHAR(32)
    );
    
    INSERT INTO #BackupHeader
    RESTORE HEADERONLY 
    FROM DISK = @BackupFile;
    
    -- Display backup metadata
    SELECT 
        DatabaseName,
        BackupTypeDescription,
        BackupStartDate,
        BackupFinishDate,
        DATEDIFF(SECOND, BackupStartDate, BackupFinishDate) AS DurationSeconds,
        BackupSize / 1048576.0 AS BackupSizeMB,
        CompressedBackupSize / 1048576.0 AS CompressedSizeMB,
        CAST((1 - (CompressedBackupSize * 1.0 / BackupSize)) * 100 AS DECIMAL(5,2)) AS CompressionPercent,
        Compressed,
        HasBackupChecksums,
        IsDamaged
    FROM #BackupHeader;
    
    -- Check for damage
    IF EXISTS (SELECT 1 FROM #BackupHeader WHERE IsDamaged = 1)
    BEGIN
        RAISERROR('Backup is damaged!', 16, 1);
    END
    ELSE
    BEGIN
        PRINT '✓ Backup metadata: PASS';
    END
    
    -- 3. RESTORE FILELISTONLY (database files in backup)
    CREATE TABLE #FileList (
        LogicalName NVARCHAR(128),
        PhysicalName NVARCHAR(260),
        Type CHAR(1),
        FileGroupName NVARCHAR(128),
        Size NUMERIC(20,0),
        MaxSize NUMERIC(20,0),
        FileId BIGINT,
        CreateLSN NUMERIC(25,0),
        DropLSN NUMERIC(25,0),
        UniqueId UNIQUEIDENTIFIER,
        ReadOnlyLSN NUMERIC(25,0),
        ReadWriteLSN NUMERIC(25,0),
        BackupSizeInBytes BIGINT,
        SourceBlockSize INT,
        FileGroupId INT,
        LogGroupGUID UNIQUEIDENTIFIER,
        DifferentialBaseLSN NUMERIC(25,0),
        DifferentialBaseGUID UNIQUEIDENTIFIER,
        IsReadOnly BIT,
        IsPresent BIT,
        TDEThumbprint VARBINARY(32),
        SnapshotUrl NVARCHAR(360)
    );
    
    INSERT INTO #FileList
    RESTORE FILELISTONLY 
    FROM DISK = @BackupFile;
    
    -- Display file list
    SELECT 
        LogicalName,
        Type AS FileType,
        FileGroupName,
        Size / 1048576.0 AS SizeMB,
        BackupSizeInBytes / 1048576.0 AS BackupSizeMB
    FROM #FileList;
    
    PRINT '✓ File list: PASS';
    
    -- Summary
    DECLARE @ElapsedSeconds INT = DATEDIFF(SECOND, @StartTime, GETDATE());
    PRINT '';
    PRINT '=== Verification Summary ===';
    PRINT 'Backup File: ' + @BackupFile;
    PRINT 'Status:      ✓ VERIFIED';
    PRINT 'Duration:    ' + CAST(@ElapsedSeconds AS NVARCHAR(10)) + ' seconds';
    
    -- Cleanup
    DROP TABLE #BackupHeader;
    DROP TABLE #FileList;
END
GO

-- Example usage
EXEC dbo.sp_VerifyBackup @BackupFile = 'D:\Backups\SQL\Full\Hartonomous_Full_20250119_020000.bak';
```

---

## Summary

**Key Backup Components**:

1. ✅ **Full Backups**: Weekly (Sunday 2:00 AM), retention 28 days
2. ✅ **Differential Backups**: Daily (2:00 AM), retention 7 days
3. ✅ **Transaction Log Backups**: Every 15 minutes, retention 7 days
4. ✅ **Neo4j Snapshots**: Daily (2:30 AM), retention 14 days
5. ✅ **Azure Replication**: Continuous (GRS storage)

**Recovery Capabilities**:
- RPO: <15 minutes (transaction log frequency)
- RTO: <2 hours (full restore)
- Point-in-Time: Any 15-minute interval
- Cross-Database: Coordinated SQL + Neo4j backups

**Next Steps**:
- See `docs/operations/performance-tuning.md` for optimization after restore
- See `docs/operations/troubleshooting.md` for backup/restore error resolution
