# Installation Guide

Complete installation guide for Hartonomous on Windows Server or Windows 11.

## System Requirements

### Minimum Requirements

- **OS**: Windows Server 2022 or Windows 11
- **Processor**: 4 cores, 2.5 GHz
- **RAM**: 16 GB
- **Storage**: 100 GB SSD (database and graph storage)
- **Network**: Internet connection for package downloads

### Recommended Requirements

- **OS**: Windows Server 2025
- **Processor**: 8+ cores, 3.0+ GHz  
- **RAM**: 32 GB+
- **Storage**: 500 GB NVMe SSD (spatial indices benefit from fast I/O)
- **Network**: Gigabit Ethernet or faster

## Installation Steps

### 1. Install .NET 10 SDK

Download and install the .NET 10 SDK:

```powershell
# Download .NET 10 SDK
$url = "https://dotnet.microsoft.com/download/dotnet/10.0"
Start-Process $url

# Verify installation
dotnet --version
# Expected: 10.0.x
```

### 2. Install SQL Server 2025

#### Option A: SQL Server 2025 (Recommended)

```powershell
# Download SQL Server 2025 Developer Edition
# https://www.microsoft.com/en-us/sql-server/sql-server-downloads

# Run installer with default options
# Select features:
# - Database Engine Services
# - Full-Text and Semantic Extractions for Search
# - Client Tools Connectivity

# During installation:
# - Mixed Mode Authentication (set SA password)
# - Add current user as SQL Server administrator
# - Enable CLR integration
```

#### Option B: SQL Server 2022 (Supported)

```powershell
# Download SQL Server 2022 Developer Edition
# https://www.microsoft.com/sql-server/sql-server-2022

# Same installation options as 2025
```

#### Post-Installation: Enable CLR

```sql
-- Connect with SQL Server Management Studio (SSMS) or sqlcmd
USE master;
GO

-- Enable CLR integration
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;

-- Disable CLR strict security (required for custom assemblies)
EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;

EXEC sp_configure 'clr strict security', 0;
RECONFIGURE;

-- Verify
SELECT name, value_in_use 
FROM sys.configurations 
WHERE name IN ('clr enabled', 'clr strict security');
```

### 3. Install Neo4j 5.x

```powershell
# Download Neo4j Community Edition
# https://neo4j.com/download-center/#community

# Install using .exe installer
# Or use Chocolatey:
choco install neo4j-community

# Configure Neo4j
# Edit: C:\Program Files\Neo4j\conf\neo4j.conf

# Set initial heap size (adjust based on RAM)
dbms.memory.heap.initial_size=2G
dbms.memory.heap.max_size=4G

# Enable bolt connector
dbms.connector.bolt.enabled=true
dbms.connector.bolt.listen_address=0.0.0.0:7687

# Enable http connector
dbms.connector.http.enabled=true
dbms.connector.http.listen_address=0.0.0.0:7474
```

Start Neo4j:

```powershell
# Start as console application (for testing)
neo4j console

# Or install as Windows service
neo4j install-service
neo4j start

# Access Neo4j Browser
# Navigate to: http://localhost:7474
# Default username: neo4j
# Default password: neo4j (will prompt to change on first login)
```

### 4. Install PowerShell 7+

```powershell
# Install PowerShell 7 using winget
winget install Microsoft.PowerShell

# Or download from GitHub
# https://github.com/PowerShell/PowerShell/releases

# Verify installation
pwsh --version
# Expected: PowerShell 7.x.x
```

### 5. Clone Hartonomous Repository

```powershell
# Navigate to desired directory
cd D:\Repositories  # Or your preferred location

# Clone repository
git clone https://github.com/AHartTN/Hartonomous.git
cd Hartonomous

# Verify repository structure
dir
# Expected: docs, scripts, src, tests, README.md, etc.
```

### 6. Configure Application

```powershell
# Copy template configuration
Copy-Item src/Hartonomous.Api/appsettings.json.template src/Hartonomous.Api/appsettings.json

# Edit configuration (use VS Code, notepad, or your preferred editor)
code src/Hartonomous.Api/appsettings.json
```

#### Required Configuration

Edit `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "HartonomousDb": "Server=localhost;Database=Hartonomous;Integrated Security=true;TrustServerCertificate=true;",
    "Neo4j": "neo4j://localhost:7687"
  },
  "Neo4jCredentials": {
    "Username": "neo4j",
    "Password": "your-secure-password"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Hartonomous": "Debug"
    }
  },
  "AllowedHosts": "*"
}
```

#### Optional: Azure Services Configuration

For production deployments with Azure integration:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "yourdomain.onmicrosoft.com",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id"
  },
  "AzureAppConfiguration": {
    "Endpoint": "https://your-appconfig.azconfig.io"
  },
  "KeyVault": {
    "VaultUri": "https://your-keyvault.vault.azure.net/"
  },
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=your-key;IngestionEndpoint=..."
  }
}
```

### 7. Deploy Database

```powershell
# Run database deployment script
.\scripts\Deploy-Database.ps1 -ServerName "localhost" -DatabaseName "Hartonomous"
```

The script performs:

1. **Create Database**: Creates `Hartonomous` database if it doesn't exist
2. **Deploy Schema**: Creates 40+ tables, indices, temporal tables
3. **Register CLR Assemblies**: Deploys 49 SIMD-optimized functions
4. **Create Spatial Indices**: R-tree indices for `GEOMETRY` columns
5. **Enable Service Broker**: Activates OODA queue infrastructure
6. **Set Permissions**: Configures row-level security

**Expected output:**

```
Starting Hartonomous database deployment...
✅ Database 'Hartonomous' created
✅ Schema deployed (42 tables created)
✅ CLR assemblies registered (16 assemblies, 49 functions)
✅ Spatial indices created (3 R-tree indices)
✅ Service Broker enabled (4 queues configured)
✅ Temporal tables configured (12 tables with history)
✅ Row-level security predicates created

Deployment completed successfully!
Database: Hartonomous
Server: localhost
CLR Permission Level: SAFE
Spatial Index Type: GEOMETRY_AUTO_GRID
```

### 8. (Optional) Seed Cognitive Kernel

For testing and validation:

```powershell
.\scripts\seed-cognitive-kernel.ps1 -ServerName "localhost" -DatabaseName "Hartonomous"
```

This creates:
- 3 orthogonal basis vectors (X/Y/Z axes)
- A* pathfinding test chain (6 atoms: START → STEP_1 → STEP_2 → GOAL + 2 noise)
- 1,000 landmark atoms for spatial trilateration
- 10 OODA loop test cycles

### 9. Build and Run

```powershell
# Restore NuGet packages and build
dotnet restore
dotnet build

# Run API
dotnet run --project src/Hartonomous.Api
```

**Expected output:**

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

Navigate to: https://localhost:5001/swagger

## Verification

### Verify SQL Server Connection

```sql
-- Connect with SSMS or sqlcmd
USE Hartonomous;

-- Verify tables
SELECT COUNT(*) AS TableCount 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE';
-- Expected: 42+

-- Verify CLR assemblies
SELECT name, permission_set_desc 
FROM sys.assemblies 
WHERE name LIKE 'Hartonomous%';
-- Expected: 16 assemblies, permission_set_desc = SAFE_ACCESS

-- Verify spatial indices
SELECT 
    OBJECT_NAME(object_id) AS TableName,
    name AS IndexName,
    type_desc AS IndexType
FROM sys.indexes
WHERE type_desc = 'SPATIAL';
-- Expected: 3 spatial indices on AtomEmbedding, SpatialLandmark, etc.
```

### Verify Neo4j Connection

```cypher
// Open Neo4j Browser: http://localhost:7474

// Verify connectivity
RETURN "Connection Successful" AS status;

// Check constraints
SHOW CONSTRAINTS;
// Expected: Constraints on Atom(id), Model(id), etc.
```

### Verify API Connectivity

```powershell
# Health check
curl https://localhost:5001/health

# Swagger UI (open in browser)
Start-Process https://localhost:5001/swagger
```

## Troubleshooting

### SQL Server Installation Issues

**Problem**: SQL Server service won't start

**Solutions**:
1. Check Windows Event Log: `eventvwr.msc` → Windows Logs → Application
2. Verify SQL Server Configuration Manager: TCP/IP enabled
3. Check port 1433 not in use: `netstat -ano | findstr :1433`

### CLR Assembly Registration Failed

**Problem**: `Assembly 'Hartonomous.Clr' could not be loaded`

**Solutions**:

```sql
-- 1. Verify CLR is enabled
SELECT value_in_use FROM sys.configurations WHERE name = 'clr enabled';
-- Must be 1

-- 2. Check assembly dependencies
SELECT * FROM sys.assembly_files;

-- 3. Manually register assembly (if deployment script failed)
CREATE ASSEMBLY [Hartonomous.Clr]
FROM 'D:\Repositories\Hartonomous\src\Hartonomous.Clr\bin\Release\net8.0\Hartonomous.Clr.dll'
WITH PERMISSION_SET = SAFE;
```

### Neo4j Connection Refused

**Problem**: `Unable to connect to neo4j://localhost:7687`

**Solutions**:

1. Verify Neo4j is running: `neo4j status`
2. Check configuration: `C:\Program Files\Neo4j\conf\neo4j.conf`
3. Verify firewall: Allow port 7687 (Bolt) and 7474 (HTTP)
4. Test connection: `cypher-shell -a neo4j://localhost:7687 -u neo4j -p password`

### Port Conflicts

**Problem**: `Port 5001 already in use`

**Solutions**:

```powershell
# Find process using port 5001
netstat -ano | findstr :5001

# Kill process (if safe to do so)
taskkill /PID <process-id> /F

# Or change API port in launchSettings.json
code src/Hartonomous.Api/Properties/launchSettings.json
# Change "applicationUrl": "https://localhost:5001;http://localhost:5000"
```

### Spatial Index Creation Timeout

**Problem**: Spatial index creation takes > 10 minutes

**Solutions**:

```sql
-- Check index creation progress
SELECT 
    r.command,
    r.percent_complete,
    CAST((r.estimated_completion_time / 60000.0) AS DECIMAL(10,2)) AS EstMinutesRemaining
FROM sys.dm_exec_requests r
WHERE r.command LIKE '%INDEX%';

-- If stuck, cancel and retry with smaller bounding box
DROP INDEX IX_AtomEmbedding_Spatial ON AtomEmbedding;

CREATE SPATIAL INDEX IX_AtomEmbedding_Spatial
ON AtomEmbedding(SpatialKey)
USING GEOMETRY_GRID
WITH (
    BOUNDING_BOX = (-100, -100, 100, 100),
    GRIDS = (MEDIUM, MEDIUM, MEDIUM, MEDIUM),
    CELLS_PER_OBJECT = 16
);
```

## Next Steps

- **[Quickstart Guide](quickstart.md)** - Complete 5-minute quickstart
- **[Configuration Guide](configuration.md)** - Advanced configuration options
- **[First Ingestion](first-ingestion.md)** - Ingest your first model
- **[Deployment Guide](../operations/deployment.md)** - Production deployment with Azure Arc

## Additional Resources

- **[SQL Server Installation](https://learn.microsoft.com/en-us/sql/database-engine/install-windows/install-sql-server)**
- **[Neo4j Installation](https://neo4j.com/docs/operations-manual/current/installation/windows/)**
- **[.NET Installation](https://learn.microsoft.com/en-us/dotnet/core/install/windows)**
- **[PowerShell Installation](https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell-on-windows)**
