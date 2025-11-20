# Hartonomous Troubleshooting Guide

**Common Errors | CLR Issues | Spatial Index Problems | OODA Diagnostics | Recovery Procedures**

---

## Table of Contents

1. [Overview](#overview)
2. [CLR Assembly Errors](#clr-assembly-errors)
3. [Spatial Index Corruption](#spatial-index-corruption)
4. [OODA Queue Blockage](#ooda-queue-blockage)
5. [Neo4j Connectivity Issues](#neo4j-connectivity-issues)
6. [Memory Pressure](#memory-pressure)
7. [Slow Query Diagnosis](#slow-query-diagnosis)
8. [Azure Arc Authentication](#azure-arc-authentication)
9. [DACPAC Deployment Failures](#dacpac-deployment-failures)
10. [Emergency Recovery](#emergency-recovery)

---

## Overview

This guide covers **most common production issues** with Hartonomous:

**Error Categories**:
- **CLR Errors**: Assembly load failures, permission denied, version mismatches
- **Spatial Errors**: Index corruption, fragmentation >50%, STIntersects failures
- **OODA Errors**: Queue stalled, phase timeout, deadlock in Service Broker
- **Connectivity**: Neo4j connection timeout, Azure Arc authentication failures
- **Performance**: High CPU, memory pressure, slow spatial queries

**Diagnostic Philosophy**:
1. **Identify Symptoms** → Check logs, DMVs, Application Insights
2. **Isolate Cause** → Run targeted diagnostic queries
3. **Apply Fix** → Execute resolution script
4. **Verify Resolution** → Validate with health checks

---

## CLR Assembly Errors

### Error: "Assembly Not Found" or "Could not load assembly"

**Symptoms**:
```
Msg 6522, Level 16, State 1, Procedure clr_HilbertEncode
A .NET Framework error occurred during execution of user-defined routine or aggregate "clr_HilbertEncode":
System.IO.FileNotFoundException: Could not load file or assembly 'HartonomousClr, Version=1.0.0.0, Culture=neutral, PublicKeyToken=...' or one of its dependencies.
```

**Diagnosis**:

```sql
-- Check if assemblies are registered
SELECT 
    a.name AS AssemblyName,
    a.permission_set_desc,
    a.is_visible,
    a.create_date,
    af.name AS FileName
FROM sys.assemblies a
LEFT JOIN sys.assembly_files af ON a.assembly_id = af.assembly_id
WHERE a.name NOT IN ('master', 'msdb', 'model', 'tempdb')
ORDER BY a.create_date DESC;

-- Expected assemblies:
-- HartonomousClr (main assembly)
-- + 16 external dependencies (MathNet.Numerics, System.Numerics.Vectors, etc.)
```

**Resolution**:

```powershell
# Re-deploy CLR assemblies in dependency order
cd D:\Repositories\Hartonomous\scripts

# Step 1: Drop existing assemblies (reverse dependency order)
.\scripts\deploy-clr-assemblies.ps1 -Server "localhost" -Database "master" -Action "Drop"

# Step 2: Re-deploy assemblies
.\scripts\deploy-clr-assemblies.ps1 -Server "localhost" -Database "master" -Action "Deploy"

# Verify deployment
sqlcmd -S localhost -Q "SELECT name, permission_set_desc FROM sys.assemblies WHERE name = 'HartonomousClr'"
```

### Error: "Permission Denied" (UNSAFE Assembly)

**Symptoms**:
```
Msg 10327, Level 14, State 1
CREATE ASSEMBLY failed because type 'HartonomousClr.SpatialUtils' in safe assembly 'HartonomousClr' has a static field 'cache'.
```

**Root Cause**: CLR assembly requires `UNSAFE` permission (file I/O, native code, static fields).

**Resolution**:

```sql
-- 1. Enable CLR integration
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;

-- 2. Set database TRUSTWORTHY ON (required for UNSAFE assemblies)
ALTER DATABASE Hartonomous SET TRUSTWORTHY ON;

-- 3. Drop and recreate assembly with UNSAFE permission
DROP ASSEMBLY IF EXISTS HartonomousClr;

CREATE ASSEMBLY HartonomousClr
FROM 'D:\Repositories\Hartonomous\src\Hartonomous.Clr\bin\Release\net481\HartonomousClr.dll'
WITH PERMISSION_SET = UNSAFE;  -- Change from SAFE to UNSAFE

-- 4. Verify permission set
SELECT name, permission_set_desc FROM sys.assemblies WHERE name = 'HartonomousClr';
-- Expected: UNSAFE_ACCESS
```

**Security Note**: `TRUSTWORTHY ON` + `UNSAFE` assemblies grant full system access. Use only in controlled environments.

### Error: "Assembly Version Mismatch"

**Symptoms**:
```
Msg 6285, Level 16, State 1
Could not load type 'HartonomousClr.SpatialUtils' from assembly 'HartonomousClr, Version=1.0.0.0' because the method 'HilbertEncode' has a different signature than the method in the type.
```

**Root Cause**: Assembly updated but SQL function signatures not updated.

**Resolution**:

```sql
-- 1. Drop all CLR functions/procedures/aggregates
DECLARE @SQL NVARCHAR(MAX) = '';

SELECT @SQL = @SQL + 'DROP FUNCTION ' + QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name) + ';' + CHAR(13)
FROM sys.objects
WHERE type IN ('FN', 'FS', 'FT')  -- Scalar/Table-valued/CLR functions
    AND is_ms_shipped = 0
    AND OBJECTPROPERTY(object_id, 'IsClrFunction') = 1;

PRINT @SQL;
EXEC sp_executesql @SQL;

-- 2. Drop assembly
DROP ASSEMBLY HartonomousClr;

-- 3. Re-deploy assembly + functions
-- (Use scripts/deploy-clr-assemblies.ps1)
```

---

## Spatial Index Corruption

### Error: "Spatial index is corrupt" or "STIntersects returned inconsistent results"

**Symptoms**:
```
Msg 8992, Level 16, State 1
Check Catalog Msg 3853, State 1: Attribute (owning_principal_id=...) of row (object_id=..., index_id=...) in sys.indexes does not have a matching row (principal_id=...) in sys.database_principals.
```

**Diagnosis**:

```sql
-- Run DBCC CHECKDB to detect corruption
DBCC CHECKDB (Hartonomous) WITH NO_INFOMSGS, ALL_ERRORMSGS;

-- Check spatial index health
SELECT 
    OBJECT_NAME(object_id) AS TableName,
    name AS IndexName,
    type_desc,
    is_disabled,
    has_filter,
    fill_factor
FROM sys.indexes
WHERE type_desc = 'SPATIAL' AND OBJECT_NAME(object_id) = 'Atom';

-- Check fragmentation
SELECT 
    avg_fragmentation_in_percent,
    page_count,
    ghost_record_count
FROM sys.dm_db_index_physical_stats(DB_ID(), OBJECT_ID('dbo.Atom'), NULL, NULL, 'DETAILED')
WHERE index_id = (SELECT index_id FROM sys.indexes WHERE name = 'IX_Atom_Location_Spatial');
```

**Resolution**:

```sql
-- Option 1: Rebuild spatial index (if fragmentation >30%)
ALTER INDEX IX_Atom_Location_Spatial ON dbo.Atom REBUILD
WITH (
    MAXDOP = 4,
    SORT_IN_TEMPDB = ON,
    ONLINE = OFF  -- Spatial indexes cannot rebuild online
);

-- Verify rebuild success
DBCC CHECKDB (Hartonomous) WITH NO_INFOMSGS;

-- Option 2: Drop and recreate (if corruption persists)
DROP INDEX IX_Atom_Location_Spatial ON dbo.Atom;

-- Recreate with optimized settings
CREATE SPATIAL INDEX IX_Atom_Location_Spatial
ON dbo.Atom(Location)
USING GEOMETRY_GRID
WITH (
    BOUNDING_BOX = (xmin = -100.0, ymin = -100.0, xmax = 100.0, ymax = 100.0),
    GRIDS = (LEVEL_1 = HIGH, LEVEL_2 = HIGH, LEVEL_3 = HIGH, LEVEL_4 = HIGH),
    CELLS_PER_OBJECT = 16,
    PAD_INDEX = ON,
    SORT_IN_TEMPDB = ON,
    DROP_EXISTING = OFF
);

-- Verify index usage
SET STATISTICS IO ON;
SELECT TOP 10 AtomId, Location
FROM dbo.Atom WITH (INDEX(IX_Atom_Location_Spatial))
WHERE Location.STIntersects(GEOMETRY::STGeomFromText('POINT(0 0)', 0)) = 1;
SET STATISTICS IO OFF;
```

### Error: "Geometry is not valid" (Invalid GEOMETRY objects)

**Symptoms**:
```
Msg 6522, Level 16, State 1
System.ArgumentException: 24200: The specified input does not represent a valid geography instance.
```

**Diagnosis**:

```sql
-- Find invalid geometries
SELECT 
    AtomId, 
    Location,
    Location.STIsValid() AS IsValid,
    Location.IsValidDetailed() AS InvalidReason
FROM dbo.Atom
WHERE Location IS NOT NULL AND Location.STIsValid() = 0;

-- Example invalid reasons:
-- "24200: The polygon input is not valid because the start and end points of the exterior ring are not the same."
-- "24201: The LineString input is not valid because it does not have enough points."
```

**Resolution**:

```sql
-- Fix invalid geometries using MakeValid()
UPDATE dbo.Atom
SET Location = Location.MakeValid()
WHERE Location IS NOT NULL AND Location.STIsValid() = 0;

-- Verify all geometries are now valid
SELECT COUNT(*) AS InvalidGeometryCount
FROM dbo.Atom
WHERE Location IS NOT NULL AND Location.STIsValid() = 0;
-- Expected: 0
```

---

## OODA Queue Blockage

### Error: OODA Cycle Stalled (Queue Not Draining)

**Symptoms**:
- OODA cycles take >5 seconds (normal: <200ms)
- Service Broker queues accumulating messages
- `sys.transmission_queue` shows large message backlog

**Diagnosis**:

```sql
-- Check queue depth
SELECT 
    sq.name AS QueueName,
    (SELECT COUNT(*) FROM sys.transmission_queue WHERE to_service_name LIKE '%' + sq.name + '%') AS MessageCount
FROM (
    SELECT 'ObserveQueue' AS name UNION ALL
    SELECT 'OrientQueue' UNION ALL
    SELECT 'DecideQueue' UNION ALL
    SELECT 'ActQueue' UNION ALL
    SELECT 'LearnQueue'
) sq
ORDER BY MessageCount DESC;

-- Check activation status
SELECT 
    name AS QueueName,
    is_activation_enabled,
    max_readers,
    activation_procedure
FROM sys.service_queues
WHERE name LIKE '%Queue';

-- Check for blocked sessions
SELECT 
    session_id,
    blocking_session_id,
    wait_type,
    wait_time / 1000.0 AS wait_time_sec,
    last_wait_type,
    text
FROM sys.dm_exec_requests r
CROSS APPLY sys.dm_exec_sql_text(r.sql_handle)
WHERE blocking_session_id != 0 OR wait_type LIKE '%BROKER%'
ORDER BY wait_time DESC;
```

**Resolution**:

```sql
-- 1. Check if activation is enabled
ALTER QUEUE ObserveQueue WITH ACTIVATION (STATUS = ON);
ALTER QUEUE OrientQueue WITH ACTIVATION (STATUS = ON);
ALTER QUEUE DecideQueue WITH ACTIVATION (STATUS = ON);
ALTER QUEUE ActQueue WITH ACTIVATION (STATUS = ON);
ALTER QUEUE LearnQueue WITH ACTIVATION (STATUS = ON);

-- 2. Increase max readers (if CPU allows)
ALTER QUEUE ObserveQueue WITH ACTIVATION (MAX_QUEUE_READERS = 20);
ALTER QUEUE OrientQueue WITH ACTIVATION (MAX_QUEUE_READERS = 20);
ALTER QUEUE DecideQueue WITH ACTIVATION (MAX_QUEUE_READERS = 20);
ALTER QUEUE ActQueue WITH ACTIVATION (MAX_QUEUE_READERS = 20);
ALTER QUEUE LearnQueue WITH ACTIVATION (MAX_QUEUE_READERS = 20);

-- 3. Manually drain queue (if automated activation fails)
DECLARE @ConversationHandle UNIQUEIDENTIFIER;
DECLARE @MessageBody VARBINARY(MAX);

WHILE 1 = 1
BEGIN
    WAITFOR (
        RECEIVE TOP(1)
            @ConversationHandle = conversation_handle,
            @MessageBody = message_body
        FROM ObserveQueue
    ), TIMEOUT 1000;  -- 1 second timeout
    
    IF @@ROWCOUNT = 0 BREAK;
    
    -- Process message manually (call activation procedure)
    EXEC dbo.sp_ProcessObserveQueue;
    
    END CONVERSATION @ConversationHandle;
END

PRINT '✓ Queue drained manually';
```

### Error: Deadlock in OODA Processing

**Symptoms**:
```
Msg 1205, Level 13, State 51
Transaction (Process ID 78) was deadlocked on lock resources with another process and has been chosen as the deadlock victim.
```

**Diagnosis**:

```sql
-- Enable deadlock trace flag (writes deadlock graphs to SQL error log)
DBCC TRACEON (1222, -1);

-- Query recent deadlocks from system health session
WITH DeadlockData AS (
    SELECT 
        CAST(target_data AS XML) AS DeadlockGraph
    FROM sys.dm_xe_session_targets st
    JOIN sys.dm_xe_sessions s ON s.address = st.event_session_address
    WHERE s.name = 'system_health'
        AND st.target_name = 'ring_buffer'
)
SELECT 
    DeadlockGraph.value('(//deadlock/@timestamp)[1]', 'DATETIME') AS DeadlockTime,
    DeadlockGraph.value('(//deadlock/process-list/process/@waitresource)[1]', 'NVARCHAR(256)') AS WaitResource,
    DeadlockGraph.query('.') AS DeadlockXML
FROM DeadlockData
WHERE DeadlockGraph.exist('//deadlock') = 1
ORDER BY DeadlockTime DESC;
```

**Resolution**:

```sql
-- Add NOLOCK hints to SELECT queries in OODA procedures (avoid shared locks)
-- Example: sp_ProcessOrientQueue

ALTER PROCEDURE dbo.sp_ProcessOrientQueue
AS
BEGIN
    SET NOCOUNT ON;
    
    -- BAD: Uses shared locks
    -- SELECT AtomId, Location FROM dbo.Atom WHERE ...
    
    -- GOOD: Uses NOLOCK (read uncommitted)
    SELECT AtomId, Location 
    FROM dbo.Atom WITH (NOLOCK)
    WHERE IsActive = 1;
    
    -- Process spatial query...
END
GO

-- Alternative: Use READ_COMMITTED_SNAPSHOT isolation
ALTER DATABASE Hartonomous SET READ_COMMITTED_SNAPSHOT ON;
```

---

## Neo4j Connectivity Issues

### Error: "Unable to connect to Neo4j" or Connection Timeout

**Symptoms**:
```
System.Net.Sockets.SocketException: A connection attempt failed because the connected party did not properly respond after a period of time
```

**Diagnosis**:

```bash
# Check Neo4j service status (Linux)
sudo systemctl status neo4j

# Check Neo4j logs
sudo tail -f /var/log/neo4j/neo4j.log

# Test connectivity from SQL Server host
curl -v http://localhost:7474/db/neo4j/tx/commit

# Test Bolt protocol (port 7687)
nc -zv localhost 7687
```

**Resolution**:

```bash
# Restart Neo4j service
sudo systemctl restart neo4j

# Verify service started
sudo systemctl status neo4j

# Check listening ports
sudo netstat -tulpn | grep neo4j
# Expected:
# tcp6       0      0 :::7474                 :::*                    LISTEN      12345/java (HTTP)
# tcp6       0      0 :::7687                 :::*                    LISTEN      12345/java (Bolt)
```

**Configuration Fix** (if firewall blocking):

```bash
# Allow Neo4j ports through firewall (Ubuntu)
sudo ufw allow 7474/tcp  # HTTP
sudo ufw allow 7687/tcp  # Bolt
sudo ufw reload

# Verify firewall rules
sudo ufw status
```

**SQL Server Configuration** (if using sp_invoke_external_rest_endpoint):

```sql
-- Enable external network access (SQL Server 2025)
EXEC sp_configure 'external scripts enabled', 1;
RECONFIGURE;

-- Test Neo4j connectivity from SQL Server
DECLARE @Response NVARCHAR(MAX);
DECLARE @StatusCode INT;

EXEC sp_invoke_external_rest_endpoint 
    @url = 'http://localhost:7474/db/neo4j/tx/commit',
    @method = 'POST',
    @headers = '{"Content-Type": "application/json"}',
    @payload = '{"statements":[{"statement":"RETURN 1 AS test"}]}',
    @response = @Response OUTPUT,
    @response_code = @StatusCode OUTPUT;

SELECT @StatusCode AS StatusCode, @Response AS Response;
-- Expected: StatusCode = 200
```

### Error: Neo4j Authentication Failed

**Symptoms**:
```
401 Unauthorized: Invalid username or password
```

**Resolution**:

```sql
-- Update Neo4j credentials in SQL Server (if using sp_invoke_external_rest_endpoint)
-- Store credentials in Azure Key Vault + reference via connection string

-- Create credential
CREATE DATABASE SCOPED CREDENTIAL Neo4jCredential
WITH IDENTITY = 'neo4j',
SECRET = 'your-new-password-here';

-- Update external REST endpoint to use credential
-- (Configuration-dependent - may require appsettings.json update)
```

---

## Memory Pressure

### Symptoms: High Memory Usage, Slow Performance

**Diagnosis**:

```sql
-- Check SQL Server memory usage
SELECT 
    physical_memory_in_use_kb / 1024 AS PhysicalMemoryUsedMB,
    locked_page_allocations_kb / 1024 AS LockedPagesMB,
    total_virtual_address_space_kb / 1024 AS VirtualAddressSpaceMB,
    available_commit_limit_kb / 1024 AS AvailableCommitMB
FROM sys.dm_os_process_memory;

-- Check memory clerks (largest consumers)
SELECT TOP 10
    type AS MemoryClerkType,
    SUM(pages_kb) / 1024.0 AS MemoryUsedMB
FROM sys.dm_os_memory_clerks
GROUP BY type
ORDER BY MemoryUsedMB DESC;

-- Check buffer pool hit ratio (should be >98%)
SELECT 
    (a.cntr_value * 1.0 / b.cntr_value) * 100.0 AS BufferCacheHitRatio
FROM sys.dm_os_performance_counters a
JOIN sys.dm_os_performance_counters b ON a.object_name = b.object_name
WHERE a.counter_name = 'Buffer cache hit ratio'
    AND b.counter_name = 'Buffer cache hit ratio base';
```

**Resolution**:

```sql
-- 1. Clear buffer pool cache (EMERGENCY ONLY - causes performance degradation)
CHECKPOINT;
DBCC DROPCLEANBUFFERS;  -- Removes clean pages from buffer pool
DBCC FREEPROCCACHE;     -- Clears procedure cache

-- 2. Configure max server memory (if not set correctly)
EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;

-- Example: 64GB server, leave 8GB for OS
EXEC sp_configure 'max server memory (MB)', 56320;  -- 55GB
RECONFIGURE;

-- 3. Identify memory-hogging queries
SELECT TOP 10
    SUBSTRING(qt.text, (qs.statement_start_offset/2)+1,
        ((CASE qs.statement_end_offset
            WHEN -1 THEN DATALENGTH(qt.text)
            ELSE qs.statement_end_offset
        END - qs.statement_start_offset)/2)+1) AS QueryText,
    qs.execution_count,
    qs.total_worker_time / 1000 AS TotalCPUTime_ms,
    qs.total_logical_reads,
    qs.total_logical_writes,
    qs.total_grant_kb / 1024 AS MemoryGrantMB
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) qt
ORDER BY qs.total_grant_kb DESC;
```

---

## Slow Query Diagnosis

### Identify Slow Queries

**Using Query Store**:

```sql
-- Top 10 slowest queries (last 24 hours)
SELECT TOP 10
    qsq.query_id,
    SUBSTRING(qsqt.query_sql_text, 1, 200) AS QueryText,
    qsrs.count_executions,
    CAST(qsrs.avg_duration / 1000.0 AS DECIMAL(10,2)) AS avg_duration_ms,
    CAST(qsrs.max_duration / 1000.0 AS DECIMAL(10,2)) AS max_duration_ms,
    CAST(qsrs.avg_cpu_time / 1000.0 AS DECIMAL(10,2)) AS avg_cpu_time_ms,
    qsrs.avg_logical_io_reads,
    qsrs.avg_physical_io_reads,
    qsrs.last_execution_time
FROM sys.query_store_query qsq
JOIN sys.query_store_query_text qsqt ON qsq.query_text_id = qsqt.query_text_id
JOIN sys.query_store_plan qsp ON qsq.query_id = qsp.query_id
JOIN sys.query_store_runtime_stats qsrs ON qsp.plan_id = qsrs.plan_id
WHERE qsrs.last_execution_time >= DATEADD(HOUR, -24, GETUTCDATE())
ORDER BY qsrs.avg_duration DESC;
```

**Using DMVs**:

```sql
-- Currently executing slow queries
SELECT 
    r.session_id,
    r.status,
    r.command,
    r.cpu_time AS CPUTime_ms,
    r.total_elapsed_time AS ElapsedTime_ms,
    r.logical_reads,
    r.writes,
    r.wait_type,
    r.wait_time AS WaitTime_ms,
    SUBSTRING(qt.text, (r.statement_start_offset/2)+1,
        ((CASE r.statement_end_offset
            WHEN -1 THEN DATALENGTH(qt.text)
            ELSE r.statement_end_offset
        END - r.statement_start_offset)/2)+1) AS QueryText,
    qp.query_plan
FROM sys.dm_exec_requests r
CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) qt
CROSS APPLY sys.dm_exec_query_plan(r.plan_handle) qp
WHERE r.session_id > 50  -- Exclude system sessions
    AND r.total_elapsed_time > 5000  -- Queries running >5 seconds
ORDER BY r.total_elapsed_time DESC;
```

**Query Plan Analysis**:

```sql
-- View execution plan for specific query
SET SHOWPLAN_XML ON;
GO

-- Run slow query (does not execute, only shows plan)
EXEC dbo.sp_SpatialNextToken @context_atom_ids = '1,2,3', @temperature = 0.7, @top_k = 10;
GO

SET SHOWPLAN_XML OFF;
GO

-- Look for:
-- - Missing indexes (green highlighting in SSMS)
-- - Table scans (should use spatial index for Location filters)
-- - High-cost operators (sort, hash join >30% cost)
```

---

## Azure Arc Authentication

### Error: "Azure Arc authentication failed" or OIDC Token Invalid

**Symptoms**:
```
Error: Failed to acquire Azure AD token for resource 'https://database.windows.net/'
```

**Diagnosis**:

```powershell
# Check Azure Arc agent status
azcmagent show

# Expected output:
# Resource Name:  HART-DESKTOP
# Agent Status:   Connected
# Agent Version:  1.x.x
```

**Resolution**:

```powershell
# Re-authenticate Azure Arc agent
azcmagent connect `
    --resource-group "rg-hartonomous" `
    --location "eastus" `
    --subscription-id "YOUR-SUBSCRIPTION-ID" `
    --tenant-id "YOUR-TENANT-ID"

# Verify SQL extension
az connectedmachine extension show `
    --machine-name "HART-DESKTOP" `
    --resource-group "rg-hartonomous" `
    --name "WindowsAgent.SqlServer"

# Restart SQL Server (to refresh Arc token)
Restart-Service MSSQLSERVER -Force
```

---

## DACPAC Deployment Failures

### Error: "Pre-deployment script failed" or "CLR Assembly Deploy Error"

**Symptoms**:
```
Error SQL72014: .Net SqlClient Data Provider: Msg 6285, Level 16, State 1
Could not load file or assembly 'System.Numerics.Vectors, Version=4.1.4.0' or one of its dependencies.
```

**Resolution**:

```powershell
# Run preflight check before deployment
.\scripts\preflight-check.ps1

# Check for missing dependencies
.\scripts\verify-dacpac.ps1 -DacpacPath ".\src\Hartonomous.Database\bin\Release\Hartonomous.dacpac"

# Re-build DACPAC with dependencies
.\scripts\build-dacpac.ps1 -Configuration Release

# Deploy with verbose logging
.\scripts\deploy-dacpac.ps1 `
    -Server "localhost" `
    -Database "Hartonomous" `
    -DacpacPath ".\src\Hartonomous.Database\bin\Release\Hartonomous.dacpac" `
    -Verbose
```

---

## Emergency Recovery

### Complete System Crash Recovery Checklist

```powershell
# 1. Verify hardware + services
Get-Service MSSQLSERVER, neo4j | Format-Table -AutoSize

# 2. Check database status
sqlcmd -S localhost -Q "SELECT name, state_desc FROM sys.databases WHERE name = 'Hartonomous'"

# 3. Run DBCC CHECKDB
sqlcmd -S localhost -Q "DBCC CHECKDB (Hartonomous) WITH NO_INFOMSGS"

# 4. Verify CLR assemblies
sqlcmd -S localhost -Q "SELECT name, permission_set_desc FROM sys.assemblies WHERE name = 'HartonomousClr'"

# 5. Verify spatial indexes
sqlcmd -S localhost -Q "SELECT OBJECT_NAME(object_id), name, type_desc FROM sys.indexes WHERE type_desc = 'SPATIAL'"

# 6. Test inference query
sqlcmd -S localhost -Q "EXEC dbo.sp_SpatialNextToken @context_atom_ids='1,2,3', @temperature=0.7, @top_k=10"

# 7. Check Application Insights connectivity
Test-NetConnection -ComputerName "dc.services.visualstudio.com" -Port 443

# If all checks pass: System healthy ✓
```

---

## Summary

**Common Issues Quick Reference**:

| Issue | Diagnostic Query | Resolution Script |
|-------|-----------------|-------------------|
| CLR Assembly Not Found | `SELECT * FROM sys.assemblies` | `deploy-clr-assemblies.ps1` |
| Spatial Index Corrupt | `DBCC CHECKDB` | `ALTER INDEX REBUILD` |
| OODA Queue Stalled | `SELECT * FROM sys.transmission_queue` | `ALTER QUEUE WITH MAX_READERS=20` |
| Neo4j Connection Timeout | `curl http://localhost:7474` | `systemctl restart neo4j` |
| High Memory Usage | `sys.dm_os_process_memory` | `sp_configure 'max server memory'` |
| Slow Query | Query Store analysis | Create missing indexes |
| Azure Arc Auth Failed | `azcmagent show` | `azcmagent connect` |
| DACPAC Deploy Failed | `preflight-check.ps1` | `build-dacpac.ps1 -Verbose` |

**Next Steps**:
- See `docs/operations/monitoring.md` for proactive issue detection
- See `docs/operations/performance-tuning.md` for optimization after fixes
