# Hartonomous Deployment Guide

## Table of Contents

- [Prerequisites](#prerequisites)
- [Infrastructure Setup](#infrastructure-setup)
- [Database Deployment](#database-deployment)
- [Application Deployment](#application-deployment)
- [Post-Deployment Verification](#post-deployment-verification)
- [Configuration Reference](#configuration-reference)
- [Troubleshooting](#troubleshooting)

## Prerequisites

### Required Software

- **SQL Server 2025** (or compatible version with vector/spatial support)
  - Features: Database Engine, Full-Text Search
  - CLR integration enabled
  - Sufficient memory for vector indexes (recommended: 32GB+ for production)

- **.NET 10.0 SDK** (or later)
  - Download: https://dotnet.microsoft.com/download

- **Neo4j 5.x** (Community or Enterprise)
  - Download: https://neo4j.com/download/
  - Java 17+ required

- **Azure Event Hubs namespace** (for event streaming)
  - Standard or Premium tier recommended
  - Minimum 2 partitions for parallel processing

### Optional Tools

- **Visual Studio 2022** (17.8+) or **VS Code** with C# extensions
- **SQL Server Management Studio (SSMS)** or **Azure Data Studio**
- **Neo4j Desktop** or **Neo4j Browser**
- **Postman** or **curl** for API testing

### System Requirements

**Development Environment:**
- CPU: 4 cores
- RAM: 16GB
- Disk: 50GB SSD

**Production Environment:**
- CPU: 16+ cores
- RAM: 64GB+ (32GB for SQL Server, 16GB for Neo4j, 16GB for applications)
- Disk: 500GB+ SSD (NVMe recommended for vector indexes)

## Infrastructure Setup

### 1. SQL Server Configuration

#### Enable CLR Integration

```sql
-- Enable CLR
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;
GO

-- Enable CLR strict security (SQL Server 2017+)
EXEC sp_configure 'clr strict security', 0;
RECONFIGURE;
GO

-- Set max server memory (adjust based on your system)
EXEC sp_configure 'max server memory (MB)', 32768; -- 32GB
RECONFIGURE;
GO
```

#### Create Database

```sql
-- Create database with optimal settings for vector workloads
CREATE DATABASE Hartonomous
ON PRIMARY
(
    NAME = N'Hartonomous_Data',
    FILENAME = N'D:\SQLData\Hartonomous.mdf',
    SIZE = 10GB,
    MAXSIZE = UNLIMITED,
    FILEGROWTH = 1GB
)
LOG ON
(
    NAME = N'Hartonomous_Log',
    FILENAME = N'D:\SQLData\Hartonomous_log.ldf',
    SIZE = 2GB,
    MAXSIZE = UNLIMITED,
    FILEGROWTH = 512MB
);
GO

-- Set database options
ALTER DATABASE Hartonomous
SET RECOVERY SIMPLE,
    ALLOW_SNAPSHOT_ISOLATION ON,
    READ_COMMITTED_SNAPSHOT ON,
    AUTO_CREATE_STATISTICS ON,
    AUTO_UPDATE_STATISTICS ON;
GO

-- Enable Change Tracking (for CES)
ALTER DATABASE Hartonomous
SET CHANGE_TRACKING = ON
(CHANGE_RETENTION = 7 DAYS, AUTO_CLEANUP = ON);
GO
```

#### Configure Spatial Types

```sql
USE Hartonomous;
GO

-- Ensure spatial libraries are available
SELECT * FROM sys.assemblies WHERE name LIKE '%SqlServer%Spatial%';
GO
```

### 2. Neo4j Setup

#### Installation

```bash
# Using Neo4j Desktop (recommended for development)
# 1. Download and install Neo4j Desktop
# 2. Create a new project
# 3. Add a local DBMS (version 5.x)
# 4. Start the database

# Or using Docker
docker run -d \
  --name hartonomous-neo4j \
  -p 7474:7474 -p 7687:7687 \
  -e NEO4J_AUTH=neo4j/your_password \
  -e NEO4J_PLUGINS='["apoc"]' \
  -v $HOME/neo4j/data:/data \
  neo4j:5-community
```

#### Apply Schema

```bash
# Connect to Neo4j using cypher-shell
cypher-shell -u neo4j -p your_password < neo4j/schemas/CoreSchema.cypher
```

### 3. Azure Event Hubs

#### Create Namespace and Hub

```bash
# Using Azure CLI
az eventhubs namespace create \
  --name hartonomous-events \
  --resource-group hartonomous-rg \
  --location eastus \
  --sku Standard \
  --capacity 2

az eventhubs eventhub create \
  --name hartonomous-changes \
  --resource-group hartonomous-rg \
  --namespace-name hartonomous-events \
  --partition-count 4 \
  --message-retention 7

# Create consumer groups
az eventhubs eventhub consumer-group create \
  --name neo4j-sync \
  --resource-group hartonomous-rg \
  --namespace-name hartonomous-events \
  --eventhub-name hartonomous-changes
```

#### Get Connection Strings

```bash
# Get connection string for sending (CesConsumer)
az eventhubs namespace authorization-rule keys list \
  --resource-group hartonomous-rg \
  --namespace-name hartonomous-events \
  --name RootManageSharedAccessKey \
  --query primaryConnectionString -o tsv

# Note: Store these in Key Vault or environment variables
```

## Database Deployment

### Using PowerShell Script (Recommended)

```powershell
# Navigate to scripts directory
cd d:\Repositories\Hartonomous\scripts

# Run deployment script
.\deploy-database.ps1 `
  -ServerName "localhost" `
  -DatabaseName "Hartonomous" `
  -TrustedConnection $true `
  -Verbose

# Or with SQL authentication
.\deploy-database.ps1 `
  -ServerName "your-server.database.windows.net" `
  -DatabaseName "Hartonomous" `
  -Username "sa" `
  -Password "YourPassword" `
  -Verbose
```

The script will:
1. Validate connectivity
2. Run EF Core migrations
3. Deploy stored procedures from `sql/procedures/`
4. Build and deploy SQL CLR assembly
5. Verify deployment

### Manual Deployment

#### 1. Run EF Core Migrations

```bash
cd src/Hartonomous.Data

# Update database to latest migration
dotnet ef database update --connection "Server=localhost;Database=Hartonomous;Trusted_Connection=True;TrustServerCertificate=True;"
```

#### 2. Deploy Stored Procedures

```bash
# From sql/procedures directory
sqlcmd -S localhost -d Hartonomous -E -i 01_SemanticSearch.sql
sqlcmd -S localhost -d Hartonomous -E -i 03_MultiModelEnsemble.sql
sqlcmd -S localhost -d Hartonomous -E -i 05_SpatialInference.sql
# ... continue for all procedures
```

#### 3. Build and Deploy SQL CLR

```bash
cd src/SqlClr

# Build the assembly
dotnet build -c Release

# Deploy using SSMS or T-SQL
```

```sql
USE Hartonomous;
GO

-- Enable CLR if not already done
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;
GO

-- Create assembly from DLL
CREATE ASSEMBLY SqlClrFunctions
FROM 'd:\Repositories\Hartonomous\src\SqlClr\bin\Release\net10.0\SqlClrFunctions.dll'
WITH PERMISSION_SET = SAFE;
GO

-- Create functions (see SqlClr project for function definitions)
```

#### 4. Seed Reference Data

```sql
-- Seed token vocabulary (optional, for text generation)
EXEC sp_ExecuteSQL N'
    -- Your vocabulary seeding logic
';
GO

-- Initialize spatial anchors
EXEC sp_InitializeSpatialAnchors;
GO
```

## Application Deployment

### 1. Configure Connection Strings

Create `appsettings.Production.json` files for each project:

**ModelIngestion/appsettings.Production.json:**
```json
{
  "ConnectionStrings": {
    "HartonomousDb": "Server=your-server;Database=Hartonomous;User Id=app_user;Password=***;TrustServerCertificate=True;"
  },
  "SqlServer": {
    "CommandTimeoutSeconds": 300,
    "EnableRetryOnFailure": true,
    "MaxRetryCount": 5
  },
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=***"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

**CesConsumer/appsettings.Production.json:**
```json
{
  "ConnectionStrings": {
    "HartonomousDb": "Server=your-server;Database=Hartonomous;User Id=cdc_user;Password=***;",
    "EventHubSender": "Endpoint=sb://hartonomous-events.servicebus.windows.net/;SharedAccessKeyName=***;SharedAccessKey=***;"
  },
  "CdcListener": {
    "PollIntervalSeconds": 5,
    "BatchSize": 100,
    "EventHubName": "hartonomous-changes"
  }
}
```

**Neo4jSync/appsettings.Production.json:**
```json
{
  "Neo4j": {
    "Uri": "bolt://localhost:7687",
    "Username": "neo4j",
    "Password": "***"
  },
  "EventHub": {
    "ConnectionString": "Endpoint=sb://hartonomous-events.servicebus.windows.net/;SharedAccessKeyName=***;SharedAccessKey=***;",
    "EventHubName": "hartonomous-changes",
    "ConsumerGroup": "neo4j-sync",
    "BlobStorageConnectionString": "DefaultEndpointsProtocol=https;AccountName=***;AccountKey=***;",
    "BlobContainerName": "checkpoints"
  }
}
```

**Hartonomous.Admin/appsettings.Production.json:**
```json
{
  "ConnectionStrings": {
    "HartonomousDb": "Server=your-server;Database=Hartonomous;User Id=admin_user;Password=***;"
  },
  "DetailedErrors": false,
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

### 2. Build Applications

```bash
# Build all projects in Release mode
cd d:\Repositories\Hartonomous

dotnet build Hartonomous.sln -c Release
```

### 3. Publish Applications

```bash
# ModelIngestion (self-contained for portability)
dotnet publish src/ModelIngestion/ModelIngestion.csproj \
  -c Release \
  -o publish/ModelIngestion \
  --self-contained false

# CesConsumer
dotnet publish src/CesConsumer/CesConsumer.csproj \
  -c Release \
  -o publish/CesConsumer \
  --self-contained false

# Neo4jSync
dotnet publish src/Neo4jSync/Neo4jSync.csproj \
  -c Release \
  -o publish/Neo4jSync \
  --self-contained false

# Hartonomous.Admin
dotnet publish src/Hartonomous.Admin/Hartonomous.Admin.csproj \
  -c Release \
  -o publish/HartonomousAdmin \
  --self-contained false
```

### 4. Deploy as Windows Services (Optional)

```powershell
# Create Windows Service for CesConsumer
sc.exe create HartonomousCesConsumer `
  binPath="d:\Services\Hartonomous\CesConsumer\CesConsumer.exe" `
  start=auto `
  DisplayName="Hartonomous CDC Consumer"

# Start service
sc.exe start HartonomousCesConsumer

# Similarly for Neo4jSync
sc.exe create HartonomousNeo4jSync `
  binPath="d:\Services\Hartonomous\Neo4jSync\Neo4jSync.exe" `
  start=auto `
  DisplayName="Hartonomous Neo4j Sync"

sc.exe start HartonomousNeo4jSync
```

### 5. Deploy Admin Portal (IIS)

```powershell
# Install IIS with ASP.NET Core hosting bundle
# Download from: https://dotnet.microsoft.com/permalink/dotnetcore-current-windows-runtime-bundle-installer

# Create IIS site
Import-Module WebAdministration

New-WebAppPool -Name "HartonomousAdminPool"
Set-ItemProperty IIS:\AppPools\HartonomousAdminPool -Name "managedRuntimeVersion" -Value ""

New-Website -Name "HartonomousAdmin" `
  -Port 5000 `
  -PhysicalPath "d:\Services\Hartonomous\Admin" `
  -ApplicationPool "HartonomousAdminPool"

# Start site
Start-Website -Name "HartonomousAdmin"
```

## Post-Deployment Verification

### 1. Run System Verification Script

```sql
-- Execute verification queries
:r sql\verification\SystemVerification.sql
```

Expected output:
- Table counts (Atoms, Models, Embeddings, etc.)
- Stored procedure existence checks
- Sample vector search results
- Spatial query results
- CLR function tests

### 2. Test Model Ingestion

```bash
cd publish/ModelIngestion

# Test ONNX model ingestion
.\ModelIngestion.exe ingest-model "C:\Models\bert-base-uncased.onnx"

# Verify in database
sqlcmd -S localhost -d Hartonomous -Q "SELECT ModelId, ModelName, ParameterCount FROM Models ORDER BY IngestionDate DESC"
```

### 3. Verify Event Pipeline

```bash
# Check CesConsumer logs
Get-Content "C:\Logs\CesConsumer\log.txt" -Tail 50

# Check Neo4jSync logs
Get-Content "C:\Logs\Neo4jSync\log.txt" -Tail 50

# Query Neo4j for recent nodes
cypher-shell -u neo4j -p your_password "MATCH (m:Model) RETURN m.name, m.created ORDER BY m.created DESC LIMIT 10"
```

### 4. Access Admin Portal

Navigate to `http://localhost:5000` (or your configured URL)

Verify:
- Dashboard shows correct totals
- Model browser lists ingested models
- Real-time telemetry updates via SignalR

### 5. Health Checks

```bash
# Check application health endpoints
curl http://localhost:5000/health
curl http://localhost:5000/health/ready
```

## Configuration Reference

### Environment Variables

```bash
# SQL Server
HARTONOMOUS_DB_SERVER=localhost
HARTONOMOUS_DB_NAME=Hartonomous
HARTONOMOUS_DB_USER=app_user
HARTONOMOUS_DB_PASSWORD=***

# Neo4j
NEO4J_URI=bolt://localhost:7687
NEO4J_USER=neo4j
NEO4J_PASSWORD=***

# Azure Event Hubs
EVENTHUB_CONNECTION_STRING=Endpoint=sb://...
EVENTHUB_NAME=hartonomous-changes

# Application Insights
APPINSIGHTS_CONNECTION_STRING=InstrumentationKey=***

# Logging
ASPNETCORE_ENVIRONMENT=Production
LOG_LEVEL=Information
```

### SQL Server Users

Create dedicated users for each application:

```sql
USE Hartonomous;
GO

-- Application user (read/write)
CREATE LOGIN app_user WITH PASSWORD = '***';
CREATE USER app_user FOR LOGIN app_user;
ALTER ROLE db_datareader ADD MEMBER app_user;
ALTER ROLE db_datawriter ADD MEMBER app_user;
GRANT EXECUTE TO app_user;

-- CDC user (read-only + CDC tables)
CREATE LOGIN cdc_user WITH PASSWORD = '***';
CREATE USER cdc_user FOR LOGIN cdc_user;
ALTER ROLE db_datareader ADD MEMBER cdc_user;
GRANT VIEW CHANGE TRACKING TO cdc_user;

-- Admin user (elevated privileges)
CREATE LOGIN admin_user WITH PASSWORD = '***';
CREATE USER admin_user FOR LOGIN admin_user;
ALTER ROLE db_owner ADD MEMBER admin_user;
```

## Troubleshooting

### SQL Server Issues

**Error: "CLR is not enabled"**
```sql
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;
```

**Error: "Vector type not recognized"**
- Ensure SQL Server 2025 or compatible version
- Check compatibility level: `ALTER DATABASE Hartonomous SET COMPATIBILITY_LEVEL = 160;`

**DiskANN index creation fails**
- Verify sufficient memory (32GB+ recommended)
- Check table has enough rows (minimum 1000 for DiskANN)

### Neo4j Connection Issues

**Error: "Connection refused"**
- Verify Neo4j is running: `systemctl status neo4j` (Linux) or check Task Manager (Windows)
- Check firewall allows port 7687

**Error: "Authentication failed"**
- Reset password: `neo4j-admin set-initial-password your_password`

### Event Hub Issues

**Error: "Unauthorized access"**
- Verify connection string includes SharedAccessKey
- Check SAS policy has Send/Listen permissions

**Events not appearing in Neo4j**
- Verify consumer group exists
- Check Neo4jSync logs for errors
- Confirm blob storage for checkpoints is accessible

### Application Startup Issues

**Error: "Failed to bind to address"**
- Port already in use, change in `appsettings.json`
- Run with elevated privileges if using ports < 1024

**Error: "Connection string not found"**
- Verify `appsettings.Production.json` exists in output directory
- Check `ASPNETCORE_ENVIRONMENT` is set correctly

---

## Next Steps

- Review [Operations Guide](operations.md) for day-to-day procedures
- See [Development Guide](development.md) for local development setup
- Consult [API Reference](api-reference.md) for programmatic integration

