# Deployment Guide# Deployment Guide# Hartonomous Deployment Guide



## Overview



Deploy Hartonomous autonomous AI system to production SQL Server environments.## Overview**Version:** 1.0



**Components:****Last Updated:** November 6, 2025

- SQL Server 2025 database with CLR assemblies

- ASP.NET Core Web API (.NET 10)This guide covers deploying Hartonomous to production environments. The system consists of:**Target Environment:** Azure Arc-enabled SQL Server (On-Premises)

- Background services (ModelIngestion, CesConsumer, Neo4jSync)



## Prerequisites

1. **SQL Server Database** - Core data and CLR assemblies---

**Required:**

- SQL Server 2025 Enterprise/Developer Edition2. **Web API** - REST API layer (.NET 10)

- .NET 10 SDK

- PowerShell 7+3. **Background Services** - Model ingestion, CDC consumer, Neo4j sync## Prerequisites

- 16GB+ RAM (32GB recommended)

4. **Neo4j** - Graph database (optional)

**Optional:**

- Neo4j 5.x for graph features5. **Azure Event Hubs** - CDC event streaming (optional)### Infrastructure Requirements

- Azure Event Hubs for CDC events



## Quick Deployment

## Prerequisites**SQL Server:**

### 1. Enable SQL Server Features

- SQL Server 2025 (or 2022 with compatibility level 160+)

```sql

EXEC sp_configure 'clr enabled', 1;### Required- Azure Arc-enabled for production deployments

RECONFIGURE;

- FILESTREAM enabled

EXEC sp_configure 'clr strict security', 0;

RECONFIGURE;- SQL Server 2025 (Enterprise or Developer Edition)- CLR integration enabled



EXEC sp_configure 'filestream access level', 2;  - CLR integration enabled- Service Broker enabled

RECONFIGURE;

```  - FILESTREAM enabled- Minimum 32GB RAM recommended



Restart SQL Server service after FILESTREAM configuration.  - Service Broker enabled- SSD storage for optimal spatial index performance



### 2. Run Automated Deployment- .NET 10 SDK



```powershell- PowerShell 7+**Development Tools:**

.\scripts\deploy\deploy-database.ps1 `

    -ServerInstance "localhost" `- Windows Server 2022 or later- .NET 9 SDK

    -Database "Hartonomous"

```- PowerShell Core 7.4+



### 3. Verify Database### Optional- SQL Server Management Studio 20+ or Azure Data Studio



```sql
SELECT name, permission_set_desc 
FROM sys.assemblies
WHERE name = 'SqlClrFunctions';

SELECT is_broker_enabled 
FROM sys.databases
WHERE name = 'Hartonomous';
```



### 4. Deploy Applications## Database Deployment- Azure DevOps (CI/CD pipeline)



```powershell- Azure Arc Agent (SQL Server registration)

# Build all projects

dotnet build Hartonomous.sln -c Release### Step 1: Enable SQL Server Features



# Publish Web API---

dotnet publish src\Hartonomous.Api\Hartonomous.Api.csproj `

    -c Release -o "C:\Services\Hartonomous\Api"```sql



# Run API-- Enable CLR integration## Deployment Architecture

cd C:\Services\Hartonomous\Api

dotnet Hartonomous.Api.dllEXEC sp_configure 'clr enabled', 1;

```

RECONFIGURE;### Target Servers

## Configuration



### Connection String

-- Enable CLR strict security (SQL Server 2017+)**Primary:** HART-DESKTOP

```json

{EXEC sp_configure 'clr strict security', 0;- Azure Arc-enabled SQL Server

  "ConnectionStrings": {

    "DefaultConnection": "Server=localhost;Database=Hartonomous;Integrated Security=true;TrustServerCertificate=true;"RECONFIGURE;- Deployment group configured

  }

}- Development and staging workloads

```

-- Enable FILESTREAM

### Neo4j (Optional)

EXEC sp_configure 'filestream access level', 2;**Secondary:** HART-SERVER

```json

{RECONFIGURE;- Azure Arc agent installed

  "Neo4j": {

    "Uri": "bolt://localhost:7687",- Deployment group configuration pending

    "Username": "neo4j",

    "Password": "password"-- Restart SQL Server service for FILESTREAM changes- Future production candidate

  }

}```

```

---

## Verification

### Step 2: Run Deployment Script

```bash

# Health check## Deployment Process

curl http://localhost:5000/health

```powershell

# Test embedding

curl -X POST http://localhost:5000/api/embedding/generate \.\scripts\deploy\deploy-database.ps1 `### Phase 1: Pre-Deployment Validation

  -H "Content-Type: application/json" \

  -d '{"text":"test embedding"}'    -ServerInstance "localhost" `

```

    -Database "Hartonomous"**1.1 Verify Prerequisites**

## Performance Tuning

``````powershell

```sql

-- Configure memory# Run prerequisite validation

EXEC sp_configure 'max server memory (MB)', 28672;

RECONFIGURE;The deployment script executes:.\scripts\deploy\01-prerequisites.ps1 `



-- Enable Query Store    -ServerName "HART-DESKTOP" `

ALTER DATABASE Hartonomous SET QUERY_STORE = ON;

1. Prerequisites validation    -DatabaseName "Hartonomous"

-- Create spatial index

CREATE SPATIAL INDEX IX_Embeddings_Spatial2. Database creation```

ON dbo.Embeddings(EmbeddingGeometry)

WITH (GRIDS = (LEVEL_1 = HIGH, LEVEL_2 = HIGH));3. FILESTREAM configuration

```

4. CLR assembly deployment**Validates:**

## Monitoring

5. Service Broker setup- SQL Server version and edition

```sql

-- Check CLR execution6. Stored procedure installation- CLR integration capability

SELECT 

    er.session_id,- FILESTREAM configuration

    er.cpu_time,

    qt.text### Step 3: Verify Deployment- Service Broker availability

FROM sys.dm_exec_requests er

CROSS APPLY sys.dm_exec_sql_text(er.sql_handle) qt- Required PowerShell modules

WHERE qt.text LIKE '%clr%';

```sql

-- Monitor Query Store

SELECT TOP 10-- Verify assemblies are loaded---

    qt.query_sql_text,

    rs.avg_duration / 1000.0 AS avg_msSELECT name, permission_set_desc 

FROM sys.query_store_query q

JOIN sys.query_store_query_text qt ON q.query_text_id = qt.query_text_idFROM sys.assemblies### Phase 2: Database Initialization

JOIN sys.query_store_plan p ON q.query_id = p.query_id

JOIN sys.query_store_runtime_stats rs ON p.plan_id = rs.plan_id
WHERE name = 'SqlClrFunctions';
ORDER BY rs.avg_duration DESC;
```**2.1 Create Database**



## Backup Strategy-- Verify Service Broker is running```powershell



```sqlSELECT is_broker_enabled .\scripts\deploy\02-database-create.ps1 `

-- Full backup weekly

BACKUP DATABASE HartonomousFROM sys.databases     -ServerName "HART-DESKTOP" `

TO DISK = 'E:\Backups\Hartonomous_Full.bak'

WITH COMPRESSION, CHECKSUM;WHERE name = 'Hartonomous';    -DatabaseName "Hartonomous"



-- Transaction log every 15 minutes``````

BACKUP LOG Hartonomous

TO DISK = 'E:\Backups\Hartonomous_Log.trn'

WITH COMPRESSION, CHECKSUM;

```## Application Deployment**Actions:**



## Troubleshooting- Creates database with correct collation



### CLR Assembly Issues### Web API- Sets compatibility level to 160



```sql- Configures database options (SNAPSHOT isolation, temporal tables)

-- Add trusted assembly

DECLARE @hash VARBINARY(64);```powershell- Initializes metadata tables

SELECT @hash = HASHBYTES('SHA2_512', BulkColumn)

FROM OPENROWSET(BULK 'C:\Path\SqlClrFunctions.dll', SINGLE_BLOB) AS x;# Publish API



EXEC sp_add_trusted_assembly @hash, N'SqlClrFunctions';cd src\Hartonomous.Api**2.2 Configure FILESTREAM**

```

dotnet publish -c Release -o "C:\Services\Hartonomous\Api"```powershell

### Service Broker Issues

.\scripts\deploy\03-filestream.ps1 `

```sql

-- Enable Service Broker# Run API    -ServerName "HART-DESKTOP" `

ALTER DATABASE Hartonomous SET ENABLE_BROKER;

dotnet Hartonomous.Api.dll    -DatabaseName "Hartonomous" `

-- Check queue

SELECT * FROM sys.service_queues WHERE name = 'OODAQueue';```    -FilestreamPath "/var/opt/mssql/data/filestream"

```

```

## Security

### Background Services

```sql

-- Create application login**Actions:**

CREATE LOGIN hartonomous_app WITH PASSWORD = 'STRONG_PASSWORD';

CREATE USER hartonomous_app FOR LOGIN hartonomous_app;```powershell- Enables FILESTREAM at instance level

GRANT EXECUTE ON SCHEMA::dbo TO hartonomous_app;

# Model Ingestion- Creates FILESTREAM filegroup

-- Enable TDE

CREATE MASTER KEY ENCRYPTION BY PASSWORD = 'MASTER_KEY_PASSWORD';cd src\ModelIngestion- Configures directory structure

CREATE CERTIFICATE TDE_Cert WITH SUBJECT = 'Hartonomous TDE';

CREATE DATABASE ENCRYPTION KEY WITH ALGORITHM = AES_256dotnet publish -c Release -o "C:\Services\Hartonomous\ModelIngestion"

ENCRYPTION BY SERVER CERTIFICATE TDE_Cert;

ALTER DATABASE Hartonomous SET ENCRYPTION ON;---

```

# CDC Consumer

## High Availability

cd src\CesConsumer### Phase 3: CLR Assembly Deployment

```sql

-- Always On Availability Groupdotnet publish -c Release -o "C:\Services\Hartonomous\CesConsumer"

CREATE AVAILABILITY GROUP HartonomousAG

FOR DATABASE Hartonomous**3.1 Build CLR Assembly**

REPLICA ON 

    N'SQL-PRIMARY' WITH (# Neo4j Sync```powershell

        ENDPOINT_URL = N'TCP://sql-primary:5022',

        AVAILABILITY_MODE = SYNCHRONOUS_COMMIT,cd src\Neo4jSync# Build with UNSAFE permission set

        FAILOVER_MODE = AUTOMATIC

    ),dotnet publish -c Release -o "C:\Services\Hartonomous\Neo4jSync"dotnet build src/SqlClr/SqlClrFunctions.csproj --configuration Release

    N'SQL-SECONDARY' WITH (

        ENDPOINT_URL = N'TCP://sql-secondary:5022',``````

        AVAILABILITY_MODE = ASYNCHRONOUS_COMMIT,

        FAILOVER_MODE = MANUAL

    );

```## Configuration**3.2 Deploy Assembly**



## Support```powershell



- Issues: https://github.com/AHartTN/Hartonomous-Sandbox/issues### Connection Strings.\scripts\deploy\04-clr-assembly.ps1 `

- Email: support@hartonomous.dev

    -ServerName "HART-DESKTOP" `

**appsettings.Production.json**:    -DatabaseName "Hartonomous" `

    -AssemblyPath "src/SqlClr/bin/Release/SqlClrFunctions.dll"

```json```

{

  "ConnectionStrings": {**Actions:**

    "DefaultConnection": "Server=localhost;Database=Hartonomous;Integrated Security=true;TrustServerCertificate=true;"- Computes SHA-512 hash for trusted assembly registration

  },- Registers assembly in `sys.trusted_assemblies` (if CLR strict security enabled)

  "Neo4j": {- Creates CLR assembly with UNSAFE permission set

    "Uri": "bolt://localhost:7687",- Drops and recreates if assembly hash changed

    "Username": "neo4j",

    "Password": "password"**Security Note:** UNSAFE assemblies require on-premises SQL Server. Azure SQL Managed Instance supports only SAFE assemblies.

  }

}---

```

### Phase 4: Schema Migration

## Verification

**4.1 Run EF Core Migrations**

```bash```powershell

# Health check.\scripts\deploy\05-ef-migrations.ps1 `

curl http://localhost:5000/health    -ServerName "HART-DESKTOP" `

    -DatabaseName "Hartonomous" `

# Test embedding endpoint    -ProjectPath "src/Hartonomous.Data/Hartonomous.Data.csproj"

curl -X POST http://localhost:5000/api/embedding/generate \```

  -H "Content-Type: application/json" \

  -d '{"text":"test"}'**Actions:**

```- Generates idempotent migration script

- Applies schema changes

## Monitoring- Creates tables, indexes, constraints

- Configures temporal tables

### Application Insights

---

```json

{### Phase 5: SQL Procedures Installation

  "ApplicationInsights": {

    "ConnectionString": "InstrumentationKey=...;IngestionEndpoint=https://..."**⚠️ CRITICAL: Current Gap in Deployment**

  }

}**Status:** Manual workaround required until script `08-create-procedures.ps1` is implemented.

```

**Manual Procedure Installation:**

### SQL Server Monitoring```powershell

# Execute all procedure files in dependency order

```sql$procedureOrder = @(

-- Query Store    "Common.ClrBindings.sql",

ALTER DATABASE Hartonomous SET QUERY_STORE = ON;    "Common.Helpers.sql",

    "Common.CreateSpatialIndexes.sql",

-- Monitor performance    "dbo.*.sql",

SELECT     "Spatial.*.sql",

    qt.query_sql_text,    "Inference.*.sql",

    rs.avg_duration / 1000.0 AS avg_duration_ms    "Generation.*.sql",

FROM sys.query_store_query q    "Embedding.*.sql",

JOIN sys.query_store_query_text qt ON q.query_text_id = qt.query_text_id    "Billing.*.sql",

JOIN sys.query_store_plan p ON q.query_id = p.query_id    "Autonomy.*.sql",

JOIN sys.query_store_runtime_stats rs ON p.plan_id = rs.plan_id    "Attention.*.sql",

ORDER BY rs.avg_duration DESC;    "Functions.*.sql"

```)



## Troubleshootingforeach ($pattern in $procedureOrder) {

    Get-ChildItem -Path "sql/procedures" -Filter $pattern |

### CLR Assembly Load Failures        ForEach-Object {

            Write-Host "Executing: $($_.Name)"

```sql            sqlcmd -S HART-DESKTOP -d Hartonomous -i $_.FullName -b

-- Add assembly hash to trusted assemblies            if ($LASTEXITCODE -ne 0) {

DECLARE @hash VARBINARY(64);                throw "Failed to execute $($_.Name)"

SELECT @hash = HASHBYTES('SHA2_512', BulkColumn)            }

FROM OPENROWSET(BULK 'C:\Path\To\SqlClrFunctions.dll', SINGLE_BLOB) AS x;        }

}

EXEC sp_add_trusted_assembly @hash, N'SqlClrFunctions';```

```

**Note:** This step will be automated in Sprint 2025-Q1. See [KNOWN_ISSUES.md](../KNOWN_ISSUES.md) Issue #1.

### Service Broker Issues

---

```sql

-- Enable Service Broker### Phase 6: Service Broker Configuration

ALTER DATABASE Hartonomous SET ENABLE_BROKER;

**6.1 Enable and Configure Service Broker**

-- Check queue status```powershell

SELECT * FROM sys.service_queues WHERE name = 'OODAQueue';.\scripts\deploy\06-service-broker.ps1 `

```    -ServerName "HART-DESKTOP" `

    -DatabaseName "Hartonomous" `

## Support    -SetupScriptPath "scripts/setup-service-broker.sql"

```

For deployment issues:

- [GitHub Issues](https://github.com/AHartTN/Hartonomous-Sandbox/issues)**Actions:**

- Email: support@hartonomous.dev- Enables Service Broker on database

- Creates message types for OODA loop (ObservationMessage, OrientationMessage, DecisionMessage, ActionMessage)
- Creates contracts and queues
- Configures activation procedures
- Initializes conversation groups

---

### Phase 7: Deployment Verification

**7.1 Run Verification Script**
```powershell
.\scripts\deploy\07-verification.ps1 `
    -ServerName "HART-DESKTOP" `
    -DatabaseName "Hartonomous"
```

**Verifies:**
- Stored procedures count (expected: 40+)
- CLR aggregates count (expected: 75+)
- Spatial indexes existence
- Service Broker enabled
- CLR assembly loaded with UNSAFE permission
- Temporal tables configured

**7.2 Manual Verification**
```sql
-- Procedure count
SELECT COUNT(*) FROM sys.procedures WHERE schema_id = SCHEMA_ID('dbo');
-- Expected: 40+

-- CLR aggregates
SELECT COUNT(*) FROM sys.objects WHERE type = 'AF';
-- Expected: 75+ (Currently 0 due to Issue #2)

-- Spatial indexes
SELECT
    OBJECT_NAME(object_id) AS TableName,
    name AS IndexName,
    type_desc
FROM sys.spatial_indexes;
-- Expected: At least SpatialGeometry and SpatialCoarse indexes

-- Service Broker
SELECT is_broker_enabled FROM sys.databases WHERE name = 'Hartonomous';
-- Expected: 1

-- CLR assembly
SELECT
    name,
    permission_set_desc,
    clr_name
FROM sys.assemblies
WHERE name = 'SqlClrFunctions';
-- Expected: 1 row, permission_set_desc = 'UNSAFE'
```

---

## CI/CD Pipeline Deployment

### Azure DevOps Pipeline

**Pipeline Definition:** `azure-pipelines.yml`

**Stages:**
1. **Build** - Compile all projects, run tests
2. **DeployDatabase** - Execute modular deployment to HART-DESKTOP
3. **DeployToProduction** - Deploy services to HART-SERVER (when configured)

**Service Connections Required:**
- `hart-server-ssh` - SSH connection for file transfer and command execution
- `hart-server-database` - SQL Server connection for database operations

**Pipeline Variables:**
- `SQL_SERVER_NAME` - Target SQL Server instance
- `SQL_DATABASE` - Database name
- `SQL_USERNAME` - SQL authentication user (from Key Vault)
- `SQL_PASSWORD` - SQL authentication password (from Key Vault)
- `FILESTREAM_PATH` - FILESTREAM directory path

### Deployment Execution

**Trigger:** Push to `main` branch

**Workflow:**
1. Build stage compiles code and creates artifacts
2. Database deployment stage:
   - Copies deployment package to HART-DESKTOP via SSH
   - Executes `scripts/deploy/deploy-database.ps1` remotely
   - Verifies deployment success
3. Service deployment stage:
   - Installs systemd service files
   - Deploys API, CES Consumer, Neo4j Sync, Model Ingestion services
   - Starts and verifies services

---

## Post-Deployment Configuration

### 1. Initialize Spatial Anchors

```sql
-- Initialize 3 anchor points for trilateration
EXEC dbo.sp_InitializeSpatialAnchors;

-- Verify anchor configuration
SELECT * FROM dbo.SpatialProjectionAnchors;
```

### 2. Create Initial Indexes

```sql
-- Create spatial indexes on embedding tables
EXEC dbo.sp_ManageHartonomousIndexes @Action = 'CREATE';

-- Verify index creation
SELECT * FROM sys.spatial_indexes;
```

### 3. Configure OODA Loop

```sql
-- Activate autonomous optimization
BEGIN
    CONVERSATION TIMER (NEWID())
        TIMEOUT = 3600;  -- Run every hour
END
```

### 4. Seed Reference Data

```sql
-- Insert default deduplication policies
INSERT INTO dbo.DeduplicationPolicies (PolicyName, SemanticThreshold, IsActive)
VALUES ('Default', 0.95, 1);

-- Insert default models registry
INSERT INTO dbo.Models (ModelName, ModelType, IsActive)
VALUES ('default-embedding', 'embedding', 1);
```

---

## Troubleshooting

### Common Issues

**Issue:** CLR assembly deployment fails with "Assembly hash not trusted"

**Solution:**
```sql
-- Add assembly to trusted assemblies
EXEC sys.sp_add_trusted_assembly
    @hash = 0x...,  -- Hash from deployment script output
    @description = N'Hartonomous CLR Functions';
```

---

**Issue:** Spatial indexes not created

**Solution:**
```sql
-- Verify GEOMETRY columns exist
SELECT
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE DATA_TYPE = 'geometry';

-- Create indexes manually if needed
CREATE SPATIAL INDEX idx_AtomEmbeddings_SpatialGeometry
ON dbo.AtomEmbeddings(SpatialGeometry)
USING GEOMETRY_GRID
WITH (BOUNDING_BOX = (-1000, -1000, 1000, 1000));
```

---

**Issue:** Stored procedures not found after deployment

**Cause:** Known issue - see [KNOWN_ISSUES.md](../KNOWN_ISSUES.md) Issue #1

**Solution:** Execute manual procedure installation script above

---

**Issue:** CLR aggregates fail at runtime

**Cause:** Known issue - see [KNOWN_ISSUES.md](../KNOWN_ISSUES.md) Issue #2

**Solution:** Awaiting fix in Sprint 2025-Q1

---

## Security Hardening

### Production Checklist

- [ ] Replace hardcoded credentials with Azure Key Vault references
- [ ] Configure Managed Identity for Arc SQL Server authentication
- [ ] Enable SQL Server Audit for compliance
- [ ] Configure TDE (Transparent Data Encryption)
- [ ] Set up firewall rules for SQL Server port
- [ ] Configure SSL/TLS for SQL connections
- [ ] Enable row-level security for multi-tenant scenarios
- [ ] Set up Azure Monitor alerts for failed deployments

### Credential Management

**Environment File Pattern:**
```ini
# /etc/hartonomous/env (not in source control)
AZURE_CLIENT_ID=<from-key-vault>
AZURE_TENANT_ID=<from-key-vault>
AZURE_CLIENT_SECRET=<from-key-vault>
SQL_CONNECTION_STRING=<from-key-vault>
```

**Service File Configuration:**
```ini
[Service]
EnvironmentFile=/etc/hartonomous/env
User=hartonomous
Group=hartonomous
```

---

## Rollback Procedures

### Database Rollback

**1. Identify Target Migration**
```powershell
dotnet ef migrations list --project src/Hartonomous.Data
```

**2. Rollback to Previous State**
```powershell
dotnet ef database update <PreviousMigrationName> `
    --project src/Hartonomous.Data `
    --connection "Server=HART-DESKTOP;Database=Hartonomous;..."
```

**3. Verify Rollback**
```sql
SELECT * FROM __EFMigrationsHistory ORDER BY MigrationId DESC;
```

### CLR Assembly Rollback

```sql
-- Drop current assembly (drops all dependent objects)
DROP ASSEMBLY [SqlClrFunctions];

-- Redeploy previous version
-- (Use deployment script with previous DLL)
```

---

## Monitoring

### Health Checks

```sql
-- Database health
SELECT
    name,
    state_desc,
    is_broker_enabled,
    recovery_model_desc
FROM sys.databases
WHERE name = 'Hartonomous';

-- OODA loop status
SELECT TOP 10
    conversation_handle,
    message_type_name,
    message_body,
    queuing_order
FROM dbo.AnalysisQueue
ORDER BY queuing_order DESC;

-- Spatial index statistics
EXEC sp_helpindex 'dbo.AtomEmbeddings';

-- Performance metrics
SELECT
    COUNT(*) AS TotalEmbeddings,
    AVG(DATALENGTH(EmbeddingVector)) AS AvgVectorSize,
    COUNT(SpatialGeometry) AS ProjectedCount
FROM dbo.AtomEmbeddings;
```

### Performance Baselines

| Metric | Baseline | Threshold |
|--------|----------|-----------|
| Hybrid search (1M embeddings) | 100ms | 150ms |
| Spatial projection computation | 12ms | 20ms |
| Embedding insertion | 5ms | 10ms |
| Billing record insert | 0.4ms | 1ms |

---

## Support

**Documentation:** [docs/INDEX.md](INDEX.md)
**Known Issues:** [KNOWN_ISSUES.md](../KNOWN_ISSUES.md)
**Technical Audit:** [TECHNICAL_AUDIT_2025-11-06.md](TECHNICAL_AUDIT_2025-11-06.md)

**Next Review:** Sprint 2025-Q1 Planning

