# Deployment Guide

**Production Deployment for Hartonomous Platform**

## Deployment Architecture

Hartonomous supports three deployment models:

1. **Local Development**: Windows 11 + SQL Server + Neo4j (this guide's starting point)
2. **On-Premises Production**: Windows Server + Azure Arc management
3. **Hybrid Cloud**: On-prem database + Azure-hosted API/workers

## Prerequisites

### Hardware Requirements

**Database Server**:
- **Minimum**: 16 cores, 64GB RAM, 500GB NVMe SSD
- **Recommended**: 32+ cores, 128GB RAM, 2TB NVMe SSD
- **Production**: 64+ cores, 256GB RAM, 10TB NVMe SSD array

**API/Worker Servers**:
- **Minimum**: 8 cores, 16GB RAM, 100GB SSD
- **Recommended**: 16 cores, 32GB RAM, 500GB SSD

### Software Requirements

- **OS**: Windows Server 2022 or later
- **SQL Server**: 2025 (preferred) or 2022
- **Neo4j**: 5.x Community or Enterprise
- **.NET**: 10.0 Runtime (ASP.NET Core + hosting bundle)
- **PowerShell**: 7.0 or later

### Network Requirements

- **SQL Server**: Port 1433 (internal network only)
- **Neo4j**: Port 7687 (Bolt), 7474 (HTTP) - internal only
- **API**: Port 443 (HTTPS public), 5001 (HTTPS internal)
- **Workers**: No inbound ports (outbound SQL/Neo4j only)

## Step 1: Prepare Database Server

### Install SQL Server 2025

```powershell
# Download SQL Server 2025 installer
Invoke-WebRequest -Uri "https://go.microsoft.com/fwlink/?linkid=866662" `
    -OutFile "SQLServer2025-x64-ENU.exe"

# Run silent install (Developer Edition for testing, Standard/Enterprise for production)
.\SQLServer2025-x64-ENU.exe /Q /ACTION=Install `
    /FEATURES=SQLEngine,FullText,Replication `
    /INSTANCENAME=MSSQLSERVER `
    /SQLSYSADMINACCOUNTS="BUILTIN\Administrators" `
    /SECURITYMODE=SQL `
    /SAPWD="YourStrongPassword123!" `
    /TCPENABLED=1 `
    /IACCEPTSQLSERVERLICENSETERMS
```

### Enable CLR Integration

```sql
-- Connect to SQL Server
sqlcmd -S localhost -U sa -P "YourStrongPassword123!"

-- Enable CLR
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;

-- Disable strict security (required for SAFE assemblies with external dependencies)
EXEC sp_configure 'clr strict security', 0;
RECONFIGURE;

-- Enable Service Broker (required for CES worker)
ALTER DATABASE Hartonomous SET ENABLE_BROKER;
```

### Create Database

```sql
CREATE DATABASE Hartonomous
ON PRIMARY (
    NAME = Hartonomous_Data,
    FILENAME = 'D:\SQLData\Hartonomous_Data.mdf',
    SIZE = 10GB,
    FILEGROWTH = 1GB
)
LOG ON (
    NAME = Hartonomous_Log,
    FILENAME = 'D:\SQLLog\Hartonomous_Log.ldf',
    SIZE = 5GB,
    FILEGROWTH = 500MB
);

-- Configure for performance
ALTER DATABASE Hartonomous SET RECOVERY SIMPLE;  -- Or FULL for production with backups
ALTER DATABASE Hartonomous SET AUTO_UPDATE_STATISTICS_ASYNC ON;
ALTER DATABASE Hartonomous SET PAGE_VERIFY CHECKSUM;
```

## Step 2: Deploy Database Schema

### Option A: DACPAC Deployment (Recommended)

```powershell
# Build DACPAC from source
cd D:\Repositories\Hartonomous

.\scripts\build-dacpac.ps1 `
    -ProjectPath "src/Hartonomous.Database/Hartonomous.Database.sqlproj" `
    -OutputDir "deploy/" `
    -Configuration Release

# Deploy DACPAC
sqlpackage.exe /Action:Publish `
    /SourceFile:"deploy/Hartonomous.Database.dacpac" `
    /TargetServerName:"DB-SERVER.domain.local" `
    /TargetDatabaseName:"Hartonomous" `
    /TargetUser:"sa" `
    /TargetPassword:"YourStrongPassword123!" `
    /p:BlockOnPossibleDataLoss=false `
    /p:DropObjectsNotInSource=false `
    /p:IgnoreUserSettingsObjects=true
```

### Option B: PowerShell Deployment Script

```powershell
.\scripts\deploy\Deploy-Production.ps1 `
    -ServerName "DB-SERVER.domain.local" `
    -DatabaseName "Hartonomous" `
    -Username "sa" `
    -Password "YourStrongPassword123!" `
    -SkipBackup:$false
```

### Verify Deployment

```sql
-- Check table count (should be 93+)
SELECT COUNT(*) AS TableCount FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';

-- Check stored procedures (should be 77+)
SELECT COUNT(*) AS ProcedureCount FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_TYPE = 'PROCEDURE';

-- Check CLR functions (should be 93+)
SELECT COUNT(*) AS CLRFunctionCount FROM sys.objects WHERE type = 'FS';

-- Verify spatial indices
SELECT
    OBJECT_NAME(object_id) AS TableName,
    name AS IndexName,
    type_desc
FROM sys.indexes
WHERE type_desc = 'SPATIAL';
-- Should show: SIX_AtomEmbedding_Semantic, SIX_AtomComposition_Position, etc.
```

## Step 3: Configure Neo4j

### Install Neo4j

```powershell
# Download Neo4j 5.x Windows installer
Invoke-WebRequest -Uri "https://neo4j.com/artifact.php?name=neo4j-community-5.x-windows.zip" `
    -OutFile "neo4j.zip"

# Extract
Expand-Archive -Path "neo4j.zip" -DestinationPath "C:\Neo4j"

# Set initial password
C:\Neo4j\bin\neo4j-admin.bat set-initial-password "YourNeo4jPassword123!"

# Install as Windows Service
C:\Neo4j\bin\neo4j.bat install-service

# Start service
Start-Service Neo4j
```

### Configure Neo4j

Edit `C:\Neo4j\conf\neo4j.conf`:
```conf
# Enable Bolt connector
server.bolt.enabled=true
server.bolt.listen_address=0.0.0.0:7687

# Memory settings (adjust for your hardware)
server.memory.heap.initial_size=4g
server.memory.heap.max_size=8g
server.memory.pagecache.size=16g

# Security
dbms.security.auth_enabled=true
```

Restart Neo4j:
```powershell
Restart-Service Neo4j
```

## Step 4: Deploy API

### Build API

```powershell
cd D:\Repositories\Hartonomous

# Publish API (self-contained for deployment)
dotnet publish src/Hartonomous.Api/Hartonomous.Api.csproj `
    -c Release `
    -o publish/api `
    --self-contained true `
    -r win-x64
```

### Configure appsettings.Production.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=DB-SERVER.domain.local;Database=Hartonomous;User Id=hartonomous_api;Password=SecurePassword123!;TrustServerCertificate=true;",
    "Neo4jConnection": "bolt://NEO4J-SERVER.domain.local:7687"
  },
  "Neo4j": {
    "Uri": "bolt://NEO4J-SERVER.domain.local:7687",
    "Username": "neo4j",
    "Password": "YourNeo4jPassword123!",
    "Database": "neo4j",
    "MaxConnectionPoolSize": 100,
    "ConnectionTimeoutSeconds": 30
  },
  "AzureAd": {
    "Enabled": true,
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "your-tenant-id-here",
    "ClientId": "your-client-id-here",
    "Audience": "api://your-api-id-here"
  },
  "Azure": {
    "KeyVault": {
      "VaultUri": "https://your-keyvault.vault.azure.net/"
    },
    "ApplicationInsights": {
      "ConnectionString": "InstrumentationKey=your-key-here"
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft": "Warning",
      "Hartonomous": "Information"
    }
  },
  "RateLimiting": {
    "GlobalLimit": 1000,
    "IngestionLimit": 100,
    "SearchLimit": 500
  }
}
```

### Install as Windows Service

```powershell
# Install using NSSM (Non-Sucking Service Manager)
nssm install HartonomousApi "C:\Hartonomous\api\Hartonomous.Api.exe"
nssm set HartonomousApi AppDirectory "C:\Hartonomous\api"
nssm set HartonomousApi AppEnvironmentExtra "ASPNETCORE_ENVIRONMENT=Production"
nssm set HartonomousApi DisplayName "Hartonomous API"
nssm set HartonomousApi Description "Hartonomous Database-Centric AI Platform API"
nssm set HartonomousApi Start SERVICE_AUTO_START

# Start service
Start-Service HartonomousApi
```

### Configure IIS (Alternative to Windows Service)

```powershell
# Install IIS with ASP.NET Core hosting bundle
Install-WindowsFeature -Name Web-Server -IncludeManagementTools
Install-WindowsFeature -Name Web-Asp-Net45

# Download and install ASP.NET Core hosting bundle
Invoke-WebRequest -Uri "https://download.visualstudio.microsoft.com/download/pr/.../dotnet-hosting-10.0-win.exe" `
    -OutFile "dotnet-hosting.exe"
.\dotnet-hosting.exe /quiet /install

# Create IIS site
Import-Module WebAdministration
New-WebAppPool -Name "HartonomousApi" -Force
Set-ItemProperty IIS:\AppPools\HartonomousApi -Name managedRuntimeVersion -Value ""
New-Website -Name "HartonomousApi" `
    -Port 443 `
    -Ssl `
    -PhysicalPath "C:\Hartonomous\api" `
    -ApplicationPool "HartonomousApi"
```

## Step 5: Deploy Worker Services

### CES Consumer Worker

```powershell
# Publish
dotnet publish src/Hartonomous.Workers.CesConsumer/Hartonomous.Workers.CesConsumer.csproj `
    -c Release `
    -o publish/worker-ces `
    --self-contained true `
    -r win-x64

# Install as service
nssm install HartonomousCesWorker "C:\Hartonomous\workers\ces\Hartonomous.Workers.CesConsumer.exe"
nssm set HartonomousCesWorker AppDirectory "C:\Hartonomous\workers\ces"
nssm set HartonomousCesWorker AppEnvironmentExtra "ASPNETCORE_ENVIRONMENT=Production"
nssm set HartonomousCesWorker DisplayName "Hartonomous CES Consumer"
nssm set HartonomousCesWorker Start SERVICE_AUTO_START

Start-Service HartonomousCesWorker
```

### Embedding Generator Worker

```powershell
# Publish
dotnet publish src/Hartonomous.Workers.EmbeddingGenerator/Hartonomous.Workers.EmbeddingGenerator.csproj `
    -c Release `
    -o publish/worker-embedding `
    --self-contained true `
    -r win-x64

# Install as service
nssm install HartonomousEmbeddingWorker "C:\Hartonomous\workers\embedding\Hartonomous.Workers.EmbeddingGenerator.exe"
nssm set HartonomousEmbeddingWorker AppDirectory "C:\Hartonomous\workers\embedding"
nssm set HartonomousEmbeddingWorker AppEnvironmentExtra "ASPNETCORE_ENVIRONMENT=Production"
nssm set HartonomousEmbeddingWorker DisplayName "Hartonomous Embedding Generator"
nssm set HartonomousEmbeddingWorker Start SERVICE_AUTO_START

Start-Service HartonomousEmbeddingWorker
```

### Neo4j Sync Worker

```powershell
# Publish
dotnet publish src/Hartonomous.Workers.Neo4jSync/Hartonomous.Workers.Neo4jSync.csproj `
    -c Release `
    -o publish/worker-neo4j `
    --self-contained true `
    -r win-x64

# Install as service
nssm install HartonomousNeo4jWorker "C:\Hartonomous\workers\neo4j\Hartonomous.Workers.Neo4jSync.exe"
nssm set HartonomousNeo4jWorker AppDirectory "C:\Hartonomous\workers\neo4j"
nssm set HartonomousNeo4jWorker AppEnvironmentExtra "ASPNETCORE_ENVIRONMENT=Production"
nssm set HartonomousNeo4jWorker DisplayName "Hartonomous Neo4j Sync"
nssm set HartonomousNeo4jWorker Start SERVICE_AUTO_START

Start-Service HartonomousNeo4jWorker
```

## Step 6: Configure SSL/TLS

### Generate Self-Signed Certificate (Testing)

```powershell
$cert = New-SelfSignedCertificate -DnsName "hartonomous.local" `
    -CertStoreLocation "cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(5)

# Bind to IIS site
New-WebBinding -Name "HartonomousApi" -Protocol https -Port 443
$binding = Get-WebBinding -Name "HartonomousApi" -Protocol https
$binding.AddSslCertificate($cert.Thumbprint, "My")
```

### Use Production Certificate (Let's Encrypt or Commercial)

```powershell
# Import certificate
$cert = Import-PfxCertificate -FilePath "hartonomous.pfx" `
    -CertStoreLocation "cert:\LocalMachine\My" `
    -Password (ConvertTo-SecureString "CertPassword" -AsPlainText -Force)

# Bind to IIS
$binding = Get-WebBinding -Name "HartonomousApi" -Protocol https
$binding.AddSslCertificate($cert.Thumbprint, "My")
```

## Step 7: Configure OODA Loop Automation

### SQL Agent Jobs

```sql
USE msdb;

-- Job 1: Hourly incremental analysis
EXEC sp_add_job @job_name = 'Hartonomous_OODA_Hourly';
EXEC sp_add_jobstep @job_name = 'Hartonomous_OODA_Hourly',
    @step_name = 'Analyze',
    @subsystem = 'TSQL',
    @database_name = 'Hartonomous',
    @command = 'EXEC dbo.sp_Analyze @TenantId = 1, @AnalysisScope = ''incremental'', @LookbackHours = 1';

EXEC sp_add_schedule @schedule_name = 'Every Hour',
    @freq_type = 4,  -- Daily
    @freq_interval = 1,
    @freq_subday_type = 8,  -- Hours
    @freq_subday_interval = 1,
    @active_start_time = 0;

EXEC sp_attach_schedule @job_name = 'Hartonomous_OODA_Hourly', @schedule_name = 'Every Hour';
EXEC sp_add_jobserver @job_name = 'Hartonomous_OODA_Hourly';

-- Job 2: Daily full OODA loop
EXEC sp_add_job @job_name = 'Hartonomous_OODA_Full';
EXEC sp_add_jobstep @job_name = 'Hartonomous_OODA_Full',
    @step_name = 'Full OODA Cycle',
    @subsystem = 'TSQL',
    @database_name = 'Hartonomous',
    @command = '
        DECLARE @AnalysisId UNIQUEIDENTIFIER;
        EXEC dbo.sp_Analyze @TenantId = 1, @AnalysisScope = ''full'', @LookbackHours = 24;
        SET @AnalysisId = (SELECT TOP 1 AnalysisId FROM dbo.LearningMetrics ORDER BY CreatedAt DESC);
        EXEC dbo.sp_Hypothesize @AnalysisId = @AnalysisId, @TenantId = 1;
        EXEC dbo.sp_Act @TenantId = 1, @AutoApproveThreshold = 0.80;
    ';

EXEC sp_add_schedule @schedule_name = 'Daily 2AM',
    @freq_type = 4,
    @freq_interval = 1,
    @active_start_time = 020000;

EXEC sp_attach_schedule @job_name = 'Hartonomous_OODA_Full', @schedule_name = 'Daily 2AM';
EXEC sp_add_jobserver @job_name = 'Hartonomous_OODA_Full';
```

## Step 8: Monitoring and Health Checks

### Configure Application Insights (Optional)

```json
// appsettings.Production.json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=your-key;IngestionEndpoint=https://...",
    "EnableAdaptiveSampling": true,
    "EnablePerformanceCounterCollectionModule": true
  }
}
```

### Health Check Endpoint

Test: `https://your-api-domain.com/health`

Expected response:
```json
{
  "status": "Healthy",
  "results": {
    "database": "Healthy",
    "neo4j": "Healthy"
  },
  "totalDuration": "00:00:00.125"
}
```

### Windows Event Log Monitoring

```powershell
# View Hartonomous logs
Get-EventLog -LogName Application -Source "Hartonomous*" -Newest 50
```

## Step 9: Backup Configuration

### SQL Server Backups

```sql
-- Full backup daily at 1 AM
EXEC sp_add_job @job_name = 'Hartonomous_Backup_Full';
EXEC sp_add_jobstep @job_name = 'Hartonomous_Backup_Full',
    @step_name = 'Backup',
    @subsystem = 'TSQL',
    @command = '
        BACKUP DATABASE Hartonomous
        TO DISK = ''D:\Backups\Hartonomous_Full_'' + CONVERT(VARCHAR, GETDATE(), 112) + ''.bak''
        WITH COMPRESSION, CHECKSUM, STATS = 10;
    ';

EXEC sp_add_schedule @schedule_name = 'Daily 1AM',
    @freq_type = 4,
    @freq_interval = 1,
    @active_start_time = 010000;

EXEC sp_attach_schedule @job_name = 'Hartonomous_Backup_Full', @schedule_name = 'Daily 1AM';
EXEC sp_add_jobserver @job_name = 'Hartonomous_Backup_Full';
```

### Neo4j Backups

```powershell
# Create scheduled task for Neo4j backup
$action = New-ScheduledTaskAction -Execute "C:\Neo4j\bin\neo4j-admin.bat" `
    -Argument "dump --database=neo4j --to=D:\Backups\Neo4j\neo4j-$(Get-Date -Format 'yyyyMMdd').dump"

$trigger = New-ScheduledTaskTrigger -Daily -At "01:30AM"
Register-ScheduledTask -TaskName "Neo4jBackup" -Action $action -Trigger $trigger
```

## Step 10: Firewall Configuration

```powershell
# API HTTPS
New-NetFirewallRule -DisplayName "Hartonomous API HTTPS" `
    -Direction Inbound `
    -LocalPort 443 `
    -Protocol TCP `
    -Action Allow

# SQL Server (internal network only)
New-NetFirewallRule -DisplayName "SQL Server" `
    -Direction Inbound `
    -LocalPort 1433 `
    -Protocol TCP `
    -Action Allow `
    -RemoteAddress 10.0.0.0/8

# Neo4j Bolt (internal network only)
New-NetFirewallRule -DisplayName "Neo4j Bolt" `
    -Direction Inbound `
    -LocalPort 7687 `
    -Protocol TCP `
    -Action Allow `
    -RemoteAddress 10.0.0.0/8
```

## Deployment Verification Checklist

- [ ] SQL Server accessible and Hartonomous database exists
- [ ] 93+ tables, 77+ procedures, 93+ CLR functions deployed
- [ ] Spatial indices created (verify with sys.indexes)
- [ ] Neo4j running and accessible on port 7687
- [ ] API service running and health check returns Healthy
- [ ] CES Consumer worker processing Service Broker queue
- [ ] Embedding Generator worker generating embeddings
- [ ] Neo4j Sync worker syncing provenance graph
- [ ] OODA loop SQL Agent jobs scheduled
- [ ] Backup jobs configured and tested
- [ ] SSL certificate installed and HTTPS working
- [ ] Firewall rules configured
- [ ] Application Insights telemetry flowing (if configured)

---

**Document Version**: 2.0
**Last Updated**: January 2025
