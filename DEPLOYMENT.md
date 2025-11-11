# Hartonomous Deployment Guide

**Comprehensive guide for deploying Hartonomous to development, staging, and production environments.**

## Overview

Hartonomous deployment consists of five components:

1. **SQL Server 2025 Database** - Core data substrate, CLR assemblies, Service Broker queues
2. **REST API** (`src/Hartonomous.Api`) - ASP.NET Core 10 gateway
3. **Admin Portal** (`src/Hartonomous.Admin`) - Blazor Server administration interface
4. **Background Workers** - CES consumer, Neo4j sync, ingestion/inference workers
5. **Neo4j** (Optional) - Graph database for provenance lineage

## Prerequisites

### SQL Server 2025

**Required Features**:

- SQL Server 2025 Enterprise or Developer Edition
- CLR integration enabled
- FILESTREAM enabled (for model weight storage)
- Service Broker enabled (for OODA loop messaging)
- Minimum 32GB RAM (64GB recommended for production)
- SSD storage for optimal spatial index performance

**Optional Features**:

- Always On Availability Groups (high availability)
- Readable secondaries (read scale-out)

### .NET and PowerShell

- .NET 10 SDK
- PowerShell 7+ (PowerShell Core)

### Azure (Production)

- Azure Arc agent (SQL Server registration)
- Azure Key Vault (credential management)
- Azure App Configuration (environment settings)
- Azure Monitor / Application Insights (telemetry export)

### Neo4j (Optional)

- Neo4j 5.x or later
- Minimum 16GB RAM
- Driver: Neo4j.Driver 5.x

## Quick Start (Local Development)

### 1. Clone and Build

```pwsh
git clone <repository-url> Hartonomous
cd Hartonomous
dotnet restore Hartonomous.sln
dotnet build Hartonomous.sln -c Debug
```

### 2. Enable SQL Server Features

Connect to SQL Server and run:

```sql
-- Enable CLR integration
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;

-- Disable CLR strict security (local dev only, DO NOT use in production)
EXEC sp_configure 'clr strict security', 0;
RECONFIGURE;

-- Enable FILESTREAM
EXEC sp_configure 'filestream access level', 2;
RECONFIGURE;
```

**Restart SQL Server service** after FILESTREAM configuration.

### 3. Run Unified Deployment Script

```pwsh
./scripts/deploy-database-unified.ps1 -Server "localhost" -Database "Hartonomous"
```

This script:

- Creates database and filegroups
- Executes EF Core migrations (domain tables)
- Deploys CLR assemblies (`SqlClrFunctions.dll` + dependencies)
- Creates advanced tables (attention, reasoning, graph, temporal)
- Sets up Service Broker (message types, contracts, queues, services)
- Binds CLR functions/aggregates/types
- Deploys stored procedures
- Runs verification tests

**Options**:

- `-SkipFilestream`: Skip FILESTREAM configuration
- `-SkipClr`: Skip CLR assembly deployment
- `-DryRun`: Show SQL commands without executing
- `-Verbose`: Detailed logging

### 4. Run Services

**API Server**:

```pwsh
cd src/Hartonomous.Api
dotnet run
```

**Admin Portal**:

```pwsh
cd src/Hartonomous.Admin
dotnet run
```

**Background Workers**:

```pwsh
# Terminal 1: CES Consumer
cd src/Hartonomous.Workers.CesConsumer
dotnet run

# Terminal 2: Neo4j Sync
cd src/Hartonomous.Workers.Neo4jSync
dotnet run
```

### 5. Verify Deployment

```sql
-- Check CLR assemblies
SELECT name, permission_set_desc 
FROM sys.assemblies 
WHERE is_user_defined = 1;

-- Check Service Broker
SELECT is_broker_enabled 
FROM sys.databases 
WHERE name = 'Hartonomous';

-- Test vector operation (CLR function)
DECLARE @vec1 VECTOR(1998) = CAST(REPLICATE(0x3F800000, 1998) AS VECTOR(1998));
DECLARE @vec2 VECTOR(1998) = CAST(REPLICATE(0x3F800000, 1998) AS VECTOR(1998));
SELECT dbo.clr_VectorDistance(@vec1, @vec2) AS Distance;

-- Check tables
SELECT COUNT(*) AS TableCount FROM sys.tables;
```

## Production Deployment

### Architecture Overview

**Production Environment**:

- **Database Tier**: SQL Server 2025 Always On Availability Group (primary + 2 readable secondaries)
- **API Tier**: 3+ API instances behind Azure Application Gateway
- **Worker Tier**: Dedicated worker pool (CES consumer, Neo4j sync, ingestion, inference)
- **Graph Tier**: Neo4j 3-node cluster (optional)
- **Monitoring**: Azure Monitor + Application Insights + Prometheus + Grafana

### Deployment Order

1. Database provisioning
2. CLR assembly deployment
3. Service deployment (API, Admin, Workers)
4. Verification and smoke tests

### Step 1: Database Provisioning

**On Primary Replica**:

```pwsh
# Use secure CLR deployment script
./scripts/deploy-clr-secure.ps1 `
    -ServerName "prod-sql-primary" `
    -DatabaseName "Hartonomous" `
    -BinDirectory "src/SqlClr/bin/Release"
```

**Secure CLR script** (`deploy-clr-secure.ps1`):

- Keeps `clr strict security = 1`
- Registers assembly hashes via `sp_add_trusted_assembly`
- Ensures `TRUSTWORTHY OFF`
- Validates strong-name signatures

**Enable Service Broker**:

```sql
ALTER DATABASE Hartonomous SET ENABLE_BROKER;
```

**Configure Always On** (if applicable):

```sql
-- Add database to availability group
ALTER AVAILABILITY GROUP [HartonomousAG]
ADD DATABASE Hartonomous;
```

**Verify on all replicas**:

```sql
-- Check replication lag
SELECT 
    ar.replica_server_name,
    drs.synchronization_state_desc,
    drs.synchronization_health_desc
FROM sys.dm_hadr_database_replica_states drs
INNER JOIN sys.availability_replicas ar ON drs.replica_id = ar.replica_id
WHERE drs.database_id = DB_ID('Hartonomous');
```

### Step 2: CLR Assembly Deployment

**Strong-Name Signing** (production requirement):

1. Generate strong-name key:

   ```pwsh
   sn -k HartonomousKey.snk
   ```

2. Add to `SqlClrFunctions.csproj`:

   ```xml
   <PropertyGroup>
     <SignAssembly>true</SignAssembly>
     <AssemblyOriginatorKeyFile>HartonomousKey.snk</AssemblyOriginatorKeyFile>
   </PropertyGroup>
   ```

3. Build signed assemblies:

   ```pwsh
   dotnet build src/SqlClr/SqlClrFunctions.csproj -c Release
   ```

4. Deploy via secure script:

   ```pwsh
   ./scripts/deploy-clr-secure.ps1 `
       -ServerName "prod-sql-primary" `
       -DatabaseName "Hartonomous" `
       -BinDirectory "src/SqlClr/bin/Release"
   ```

**Trusted Assembly Registration**:

The secure script automatically registers assembly hashes:

```sql
EXEC sys.sp_add_trusted_assembly 
    @hash = 0x<SHA512-hash>,
    @description = 'SqlClrFunctions.dll v1.0';
```

### Step 3: Service Deployment (Systemd)

**On Linux Servers** (Ubuntu 22.04+, RHEL 9+):

1. Copy deployment artifacts:

   ```bash
   # From local machine
   scp -r publish/* hart-server:/opt/hartonomous/
   ```

2. Install systemd units:

   ```bash
   sudo cp deploy/hartonomous-api.service /etc/systemd/system/
   sudo cp deploy/hartonomous-ces-consumer.service /etc/systemd/system/
   sudo cp deploy/hartonomous-neo4j-sync.service /etc/systemd/system/
   
   sudo systemctl daemon-reload
   ```

3. Configure environment:

   ```bash
   # Create environment file
   sudo nano /etc/hartonomous/api.env
   ```

   ```ini
   ConnectionStrings__DefaultConnection=Server=prod-sql-primary;Database=Hartonomous;User Id=hartonomous_api;Password=***;TrustServerCertificate=false;
   AzureAd__TenantId=<tenant-id>
   AzureAd__ClientId=<client-id>
   ApplicationInsights__ConnectionString=<app-insights-connection-string>
   Neo4j__Uri=bolt://neo4j-cluster:7687
   Neo4j__Username=neo4j
   Neo4j__Password=***
   ```

4. Start services:

   ```bash
   sudo systemctl enable hartonomous-api
   sudo systemctl start hartonomous-api
   
   sudo systemctl enable hartonomous-ces-consumer
   sudo systemctl start hartonomous-ces-consumer
   
   sudo systemctl enable hartonomous-neo4j-sync
   sudo systemctl start hartonomous-neo4j-sync
   ```

5. Verify:

   ```bash
   sudo systemctl status hartonomous-api
   sudo journalctl -u hartonomous-api -f
   ```

**Systemd Unit Example** (`deploy/hartonomous-api.service`):

```ini
[Unit]
Description=Hartonomous REST API
After=network.target

[Service]
Type=notify
User=hartonomous
WorkingDirectory=/opt/hartonomous/api
ExecStart=/usr/bin/dotnet /opt/hartonomous/api/Hartonomous.Api.dll
EnvironmentFile=/etc/hartonomous/api.env
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=hartonomous-api

[Install]
WantedBy=multi-user.target
```

### Step 4: Windows Service Deployment

**On Windows Servers**:

1. Publish applications:

   ```pwsh
   dotnet publish src/Hartonomous.Api/Hartonomous.Api.csproj -c Release -o C:\Services\Hartonomous\Api
   dotnet publish src/Hartonomous.Workers.CesConsumer/Hartonomous.Workers.CesConsumer.csproj -c Release -o C:\Services\Hartonomous\CesConsumer
   dotnet publish src/Hartonomous.Workers.Neo4jSync/Hartonomous.Workers.Neo4jSync.csproj -c Release -o C:\Services\Hartonomous\Neo4jSync
   ```

2. Install as Windows Services using `sc.exe`:

   ```pwsh
   sc.exe create "HartonomousApi" binPath="C:\Services\Hartonomous\Api\Hartonomous.Api.exe" start=auto
   sc.exe create "HartonomousCesConsumer" binPath="C:\Services\Hartonomous\CesConsumer\Hartonomous.Workers.CesConsumer.exe" start=auto
   sc.exe create "HartonomousNeo4jSync" binPath="C:\Services\Hartonomous\Neo4jSync\Hartonomous.Workers.Neo4jSync.exe" start=auto
   ```

3. Configure service recovery:

   ```pwsh
   sc.exe failure "HartonomousApi" reset=86400 actions=restart/5000/restart/10000/restart/30000
   ```

4. Start services:

   ```pwsh
   Start-Service HartonomousApi
   Start-Service HartonomousCesConsumer
   Start-Service HartonomousNeo4jSync
   ```

### Step 5: Azure Arc Configuration (Hybrid)

**For on-premises SQL Server registered with Azure Arc**:

1. Install Azure Arc agent:

   ```pwsh
   # Download and run Arc installer
   Invoke-WebRequest -Uri "https://aka.ms/AzureConnectedMachineAgent" -OutFile AzureConnectedMachineAgent.msi
   msiexec /i AzureConnectedMachineAgent.msi /qn
   
   # Connect to Azure
   azcmagent connect `
       --resource-group "Hartonomous-RG" `
       --tenant-id "<tenant-id>" `
       --location "eastus" `
       --subscription-id "<subscription-id>"
   ```

2. Enable SQL Server Arc extension:

   ```pwsh
   az connectedmachine extension create `
       --machine-name "HART-SERVER" `
       --resource-group "Hartonomous-RG" `
       --name "WindowsAgent.SqlServer" `
       --type "WindowsAgent.SqlServer" `
       --publisher "Microsoft.AzureData"
   ```

3. Configure managed identity for Key Vault access:

   ```pwsh
   # Assign managed identity to Arc-enabled server
   az connectedmachine update `
       --name "HART-SERVER" `
       --resource-group "Hartonomous-RG" `
       --assign-identity
   
   # Grant Key Vault access
   az keyvault set-policy `
       --name "hartonomous-kv" `
       --object-id <managed-identity-object-id> `
       --secret-permissions get list
   ```

## Configuration Management

### Connection Strings

**Development** (`appsettings.Development.json`):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=Hartonomous;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

**Production** (`appsettings.Production.json`):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-sql-primary;Database=Hartonomous;User Id=hartonomous_api;Password=<from-keyvault>;TrustServerCertificate=False;Encrypt=True;"
  }
}
```

**Azure Key Vault Integration**:

```csharp
// Program.cs
builder.Configuration.AddAzureKeyVault(
    new Uri("https://hartonomous-kv.vault.azure.net/"),
    new DefaultAzureCredential());
```

### Environment Variables

**Required**:

- `ASPNETCORE_ENVIRONMENT` (Development, Staging, Production)
- `ConnectionStrings__DefaultConnection`
- `AzureAd__TenantId`
- `AzureAd__ClientId`

**Optional**:

- `ApplicationInsights__ConnectionString`
- `Neo4j__Uri`, `Neo4j__Username`, `Neo4j__Password`
- `AzureStorage__ConnectionString`
- `OpenTelemetry__OtlpEndpoint`

## Verification and Smoke Tests

### Database Verification

```sql
-- 1. Check CLR assemblies
SELECT name, permission_set_desc, is_visible
FROM sys.assemblies 
WHERE is_user_defined = 1;
-- Expected: SqlClrFunctions, System.Numerics.Vectors, MathNet.Numerics, Newtonsoft.Json, etc.

-- 2. Check Service Broker
SELECT is_broker_enabled FROM sys.databases WHERE name = 'Hartonomous';
-- Expected: 1 (enabled)

-- 3. Test CLR vector operation
DECLARE @vec1 VECTOR(1998) = CAST(REPLICATE(0x3F800000, 1998) AS VECTOR(1998));
DECLARE @vec2 VECTOR(1998) = CAST(REPLICATE(0x3F800000, 1998) AS VECTOR(1998));
SELECT dbo.clr_VectorDistance(@vec1, @vec2) AS Distance;
-- Expected: 0 (identical vectors)

-- 4. Verify OODA loop queues
SELECT name, is_receive_enabled, is_enqueue_enabled
FROM sys.service_queues
WHERE name IN ('AnalyzeQueue', 'HypothesizeQueue', 'ActQueue', 'LearnQueue');
-- Expected: 4 rows, all enabled

-- 5. Check temporal tables
SELECT 
    t.name AS TableName,
    t.temporal_type_desc
FROM sys.tables t
WHERE t.temporal_type = 2;
-- Expected: TensorAtomCoefficients, Weights, etc.
```

### API Health Check

```bash
curl -X GET http://localhost:5000/health
```

Expected response:

```json
{
  "status": "Healthy",
  "checks": [
    {"name": "Database", "status": "Healthy"},
    {"name": "ServiceBroker", "status": "Healthy"},
    {"name": "Neo4j", "status": "Healthy"}
  ]
}
```

### End-to-End Smoke Test

```bash
# 1. Ingest atom
curl -X POST http://localhost:5000/api/atoms/ingest \
  -H "Content-Type: application/json" \
  -d '{
    "modality": "text",
    "content": "Hartonomous is an autonomous AI platform running in SQL Server.",
    "metadata": {}
  }'

# 2. Search for similar atoms
curl -X POST http://localhost:5000/api/search \
  -H "Content-Type: application/json" \
  -d '{
    "query": "autonomous AI database",
    "topK": 10
  }'

# 3. Trigger OODA loop analysis
curl -X POST http://localhost:5000/api/operations/analyze \
  -H "Content-Type: application/json" \
  -d '{
    "observationType": "PerformanceMetric",
    "observation": {"metricName": "ResponseTime", "value": 1250}
  }'
```

## Monitoring and Observability

### Application Insights

**Instrumentation** (`Program.cs`):

```csharp
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
});
```

**Custom Metrics**:

```csharp
telemetryClient.TrackMetric("Hartonomous.Inference.Duration", duration);
telemetryClient.TrackEvent("Hartonomous.OODA.HypothesisGenerated", properties);
```

### Prometheus Metrics

**Endpoint**: `http://localhost:5000/metrics`

**Key Metrics**:

- `hartonomous_inference_duration_seconds` (histogram)
- `hartonomous_atoms_ingested_total` (counter)
- `hartonomous_vector_searches_total` (counter)
- `hartonomous_ooda_cycles_total` (counter)

### SQL Server Monitoring

**Query Store**:

```sql
ALTER DATABASE Hartonomous SET QUERY_STORE = ON (
    OPERATION_MODE = READ_WRITE,
    MAX_STORAGE_SIZE_MB = 1024,
    INTERVAL_LENGTH_MINUTES = 60
);
```

**Extended Events** (track CLR operations):

```sql
CREATE EVENT SESSION [Hartonomous_CLR_Operations]
ON SERVER
ADD EVENT sqlserver.clr_assembly_load,
ADD EVENT sqlserver.clr_allocation_failure,
ADD EVENT sqlserver.sp_statement_completed (
    WHERE sqlserver.database_name = 'Hartonomous'
      AND object_name LIKE '%clr_%'
)
ADD TARGET package0.event_file (
    SET filename = N'Hartonomous_CLR_Operations.xel'
);

ALTER EVENT SESSION [Hartonomous_CLR_Operations] ON SERVER STATE = START;
```

## Troubleshooting

### CLR Assembly Deployment Failures

**Issue**: `CREATE ASSEMBLY failed because assembly <name> is not authorized for PERMISSION_SET = UNSAFE`

**Solution**:

1. Use secure deployment script: `./scripts/deploy-clr-secure.ps1`
2. Register assembly hash: `EXEC sp_add_trusted_assembly @hash = 0x<hash>`
3. Verify `clr strict security = 1` in production

**Issue**: `Could not find file 'SqlClrFunctions.dll'`

**Solution**: Build CLR project first: `dotnet build src/SqlClr/SqlClrFunctions.csproj -c Release`

### Service Broker Issues

**Issue**: Service Broker disabled after database restore

**Solution**: Re-enable broker:

```sql
ALTER DATABASE Hartonomous SET NEW_BROKER;
ALTER DATABASE Hartonomous SET ENABLE_BROKER;
```

**Issue**: Messages stuck in queue

**Solution**: Check activation procedure:

```sql
-- Verify activation status
SELECT name, is_activation_enabled, is_receive_enabled
FROM sys.service_queues
WHERE name = 'AnalyzeQueue';

-- Manually receive message
DECLARE @handle UNIQUEIDENTIFIER, @messageType NVARCHAR(256), @messageBody VARBINARY(MAX);
RECEIVE TOP(1) @handle = conversation_handle, @messageType = message_type_name, @messageBody = message_body
FROM AnalyzeQueue;
SELECT @messageType, CAST(@messageBody AS NVARCHAR(MAX));
```

### Performance Issues

**Issue**: Slow vector searches

**Solution**: Verify spatial indexes:

```sql
-- Check spatial index statistics
SELECT 
    i.name AS IndexName,
    s.user_scans,
    s.user_seeks,
    s.user_updates
FROM sys.indexes i
INNER JOIN sys.dm_db_index_usage_stats s ON i.object_id = s.object_id AND i.index_id = s.index_id
WHERE i.type_desc = 'SPATIAL'
  AND OBJECT_NAME(i.object_id) = 'AtomEmbeddings';
```

## Rollback Procedures

### Database Rollback

**Temporal table point-in-time recovery**:

```sql
-- View historical state
SELECT * FROM dbo.TensorAtomCoefficients
FOR SYSTEM_TIME AS OF '2025-11-01T10:00:00';

-- Restore to previous state
UPDATE dbo.TensorAtomCoefficients
SET CoefficientValue = h.CoefficientValue
FROM dbo.TensorAtomCoefficients c
INNER JOIN dbo.TensorAtomCoefficients FOR SYSTEM_TIME AS OF '2025-11-01T10:00:00' h
  ON c.CoefficientId = h.CoefficientId;
```

### CLR Assembly Rollback

```sql
-- Drop current assembly
DROP ASSEMBLY SqlClrFunctions;

-- Restore from backup
CREATE ASSEMBLY SqlClrFunctions
FROM 'D:\Backups\SqlClrFunctions_v1.0.dll'
WITH PERMISSION_SET = UNSAFE;
```

### Service Rollback

```bash
# Stop current version
sudo systemctl stop hartonomous-api

# Deploy previous version
sudo cp /opt/hartonomous/backups/api-v1.0/* /opt/hartonomous/api/

# Start service
sudo systemctl start hartonomous-api
```

## Security Best Practices

1. **Always use `clr strict security = 1` in production**
2. **Register assembly hashes via `sp_add_trusted_assembly`**
3. **Never enable `TRUSTWORTHY ON`**
4. **Use managed identities for Azure resources**
5. **Store credentials in Azure Key Vault**
6. **Enable TLS for all SQL connections (`Encrypt=True`)**
7. **Use Azure AD authentication where possible**
8. **Enable row-level security for multi-tenant isolation**
9. **Configure firewall rules to restrict SQL Server access**
10. **Regularly rotate credentials and certificates**

## References

- [README.md](README.md) - Getting started guide
- [ARCHITECTURE.md](ARCHITECTURE.md) - System architecture
- [API.md](API.md) - REST API reference
- [docs/CLR_GUIDE.md](docs/CLR_GUIDE.md) - CLR deployment and troubleshooting
- [docs/DATABASE_SCHEMA.md](docs/DATABASE_SCHEMA.md) - Database schema reference
- [Microsoft Docs: CLR Strict Security](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/clr-strict-security)
- [Microsoft Docs: Service Broker](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-service-broker)
