# Hartonomous Database Deployment Architecture

## Overview

This directory contains modular PowerShell deployment scripts for deploying the Hartonomous autonomous system database to **Azure Arc-enabled SQL Server** running on Ubuntu Linux. The deployment is orchestrated through Azure DevOps pipelines with SSH deployment to on-premises Arc servers.

## Architecture

```
┌─────────────────┐
│  GitHub Repo    │
│  (Source Code)  │
└────────┬────────┘
         │ git push
         ▼
┌─────────────────────────────────────────────────────────┐
│           Azure DevOps (aharttn org)                    │
│  ┌──────────────────────────────────────────────────┐   │
│  │  Build Stage (ubuntu-latest agent)              │   │
│  │  • Compile .NET 9 projects                       │   │
│  │  • Run tests                                     │   │
│  │  • Publish artifacts (API, CES, Neo4j, Models)   │   │
│  └──────────────────────────────────────────────────┘   │
│                       │                                  │
│                       ▼                                  │
│  ┌──────────────────────────────────────────────────┐   │
│  │  DeployDatabase Stage (ubuntu-latest agent)      │   │
│  │  • Build UNSAFE CLR assembly (SqlClrFunctions)   │   │
│  │  • Generate EF Core migrations (idempotent)      │   │
│  │  • Package deployment scripts                    │   │
│  │  • Publish database-deployment artifact          │   │
│  │  • SSH → hart-server Arc machine                 │   │
│  │  • Execute deploy-database.ps1 orchestrator      │   │
│  └──────────────────────────────────────────────────┘   │
│                       │                                  │
│                       ▼                                  │
│  ┌──────────────────────────────────────────────────┐   │
│  │  DeployToProduction Stage                        │   │
│  │  • Deploy systemd services to /srv/www          │   │
│  │  • Restart hartonomous-*.service units           │   │
│  └──────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
                       │ SSH (port 22)
                       ▼
         ┌─────────────────────────────┐
         │  hart-server (192.168.1.2)  │
         │  • Ubuntu 22.04.5 LTS       │
         │  • Intel i7-6850K, 125GB RAM│
         │  • Azure Arc agent 1.58.x   │
         │  ├─ SQL Server 2025 (Linux) │
         │  │  • Database: Hartonomous  │
         │  │  • CLR: UNSAFE (CPU SIMD) │
         │  │  • FILESTREAM enabled     │
         │  │  • Service Broker: OODA   │
         │  └─ systemd services:        │
         │     • hartonomous-api        │
         │     • hartonomous-ces        │
         │     • hartonomous-neo4j      │
         │     • hartonomous-models     │
         └─────────────────────────────┘
```

## Deployment Components

### 1. Modular PowerShell Scripts (`scripts/deploy/`)

Each script is idempotent and returns JSON for pipeline consumption:

| Script | Purpose | Key Features |
|--------|---------|--------------|
| `01-prerequisites.ps1` | Validates deployment readiness | • SQL Server version check (14.x+)<br>• Platform detection (Linux/Windows)<br>• CLR settings verification<br>• Azure Arc agent detection |
| `02-database-create.ps1` | Creates Hartonomous database | • Platform-specific paths<br>• Query Store enabled<br>• In-Memory OLTP support<br>• Compatibility level 160 |
| `03-filestream.ps1` | Configures FILESTREAM support | • Linux path: `/var/opt/mssql/data/filestream`<br>• SQL 2019+ version check<br>• `sp_configure` verification<br>• Filegroup + file creation |
| `04-clr-assembly.ps1` | Deploys UNSAFE CLR assembly | • SHA-512 hash computation<br>• `sys.sp_add_trusted_assembly` registration<br>• Dependency order: DROP funcs→aggs→types→assembly<br>• PERMISSION_SET = UNSAFE for CPU SIMD |
| `05-ef-migrations.ps1` | Applies EF Core migrations | • `dotnet ef migrations script --idempotent`<br>• NetTopologySuite spatial types<br>• `__EFMigrationsHistory` verification |
| `06-service-broker.ps1` | Sets up Service Broker | • OODA loop message types<br>• Queues: Observation, Orientation, Decision, Action<br>• `ALTER DATABASE SET ENABLE_BROKER` |
| `07-verification.ps1` | Validates deployment | • 6 comprehensive checks<br>• Critical: DB, CLR, Migrations<br>• Optional: FILESTREAM, Service Broker, Spatial |
| `deploy-database.ps1` | Orchestrates all scripts | • Sequential execution with error handling<br>• JSON aggregation<br>• Duration tracking<br>• Azure DevOps artifact output |

### 2. Azure DevOps Pipeline (`azure-pipelines.yml`)

Three stages:

1. **Build** - Compiles .NET 9 projects, runs tests, publishes artifacts
2. **DeployDatabase** - Builds CLR assembly, generates migrations, deploys to Arc SQL Server via SSH
3. **DeployToProduction** - Deploys systemd services to `/srv/www/hartonomous`

### 3. SQL Server Configuration

**On-Premises Only** (Azure Arc-enabled, NOT Azure SQL):

- **SQL Server 2025 on Linux** (Ubuntu 22.04)
- **UNSAFE CLR** - Required for ILGPU GPU acceleration with cuBLAS
- **FILESTREAM** - Supported on SQL Server 2019+ for Linux
- **Service Broker** - Autonomous OODA loop messaging
- **Spatial Types** - NetTopologySuite geometry columns with R-tree indexes
- **In-Memory OLTP** - `MEMORY_OPTIMIZED_ELEVATE_TO_SNAPSHOT`
- **Query Store** - Performance monitoring enabled

## Key Design Decisions

### Why UNSAFE CLR?

**UNSAFE CLR assemblies are ONLY deployed to on-premises Arc servers** for CPU SIMD acceleration:

- System.Numerics.Vectors requires unsafe memory operations for SIMD intrinsics (AVX2/SSE4)
- MathNet.Numerics uses unmanaged BLAS/LAPACK routines for matrix operations
- FILESTREAM requires unsafe memory-mapped file access
- Cannot use Azure SQL Database (no CLR support)
- Cannot use SAFE/EXTERNAL_ACCESS (SIMD requires UNSAFE)

**ILGPU Status**: ILGPU disabled/commented due to CLR verifier incompatibility with unmanaged GPU memory pointers. Code preserved in SqlClrFunctions project for potential future implementation outside SQL CLR (e.g., API/worker processes using .NET 10).

**Current Deployment**: 14 assemblies, CPU SIMD-only (AVX2/SSE4), .NET Framework 4.8.1.

**MS Docs Reference:** [CLR strict security](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/clr-strict-security)

### Why Azure Arc?

- **Unified management** - Manage on-prem SQL Server through Azure portal
- **Azure DevOps integration** - Use Arc extensions for deployments
- **Governance** - Azure Policy and RBAC for on-premises resources
- **Extensions** - `LinuxAgent.SqlServer` for SQL Server management on Linux
- **NO migration to cloud** - Database remains on-premises with GPU hardware

**MS Docs Reference:** [Azure Arc-enabled SQL Server](https://learn.microsoft.com/en-us/sql/sql-server/azure-arc/overview)

### Why Modular Scripts?

Replaces monolithic 1,238-line `deploy-database.ps1` with:

- **Idempotency** - Each script can be run independently
- **Testing** - Unit test individual deployment steps
- **Debugging** - Isolate failures to specific components
- **Reusability** - Scripts work in CI/CD and manual deployments
- **JSON output** - Structured logging for Azure DevOps
- **Error handling** - Fail fast with detailed error messages

## Prerequisites

### Azure DevOps Setup

1. **Service Connection**: SSH connection to `hart-server` (192.168.1.2)
   ```bash
   # In Azure DevOps Project Settings → Service connections
   # Add SSH service connection named "hart-server-ssh"
   # Host: 192.168.1.2
   # Port: 22
   # Username: <deployment-user>
   # Private key: <SSH private key>
   ```

2. **Pipeline Variables**:
   - `SQL_USERNAME` - SQL Server authentication username (Variable Group)
   - `SQL_PASSWORD` - SQL Server password (Secret, Variable Group)
   - `SQL_DATABASE` - Database name (default: `Hartonomous`)
   - `SQL_SERVER` - Server name (default: `hart-server` or `localhost` from Arc server)

3. **Azure Arc Registration**:
   ```bash
   # Arc agent already installed on hart-server
   # Verify:
   az connectedmachine show --name hart-server --resource-group rg-hartonomous
   az resource show --ids /subscriptions/<sub-id>/resourceGroups/rg-hartonomous/providers/Microsoft.AzureArcData/sqlServerInstances/hart-server
   ```

### hart-server Requirements

**Software:**
- Ubuntu 22.04 LTS (kernel 5.15+)
- SQL Server 2025 on Linux (version 17.x)
- PowerShell Core 7.x (`pwsh` command available)
- .NET SDK 9.x (for EF migrations)
- sqlcmd utility (SQL Server command-line tools)
- Azure Arc Connected Machine agent 1.58+

**Configuration:**
```bash
# Enable CLR integration
sudo /opt/mssql/bin/mssql-conf set hadr.hadrenabled 0
sudo systemctl restart mssql-server

# Enable FILESTREAM (requires instance restart)
sudo /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -Q "EXEC sp_configure 'filestream access level', 2; RECONFIGURE;"
sudo systemctl restart mssql-server

# Verify PowerShell Core
pwsh --version  # Should be 7.4+

# Verify dotnet CLI
dotnet --version  # Should be 9.0+
```

**Permissions:**
- SQL Server login with `sysadmin` role (for CLR deployment)
- File system access to `/var/opt/mssql/data` (for FILESTREAM)
- SSH access for deployment user

## Deployment Workflow

### Automated (Azure DevOps Pipeline)

1. **Commit to `main` branch** → Triggers `azure-pipelines.yml`
2. **Build Stage** → Compiles .NET 9 projects, runs tests
3. **DeployDatabase Stage**:
   - Builds `SqlClrFunctions.dll` with UNSAFE permission set
   - Generates idempotent EF Core migration script
   - Packages deployment artifacts
   - SSH to `hart-server`
   - Executes `scripts/deploy/deploy-database.ps1` orchestrator
   - Runs 7 modular scripts sequentially:
     - 01-prerequisites.ps1 ✓
     - 02-database-create.ps1 ✓
     - 03-filestream.ps1 ✓
     - 04-clr-assembly.ps1 ✓
     - 05-ef-migrations.ps1 ✓
     - 06-service-broker.ps1 ✓
     - 07-verification.ps1 ✓
   - Publishes JSON results to artifact staging directory
4. **DeployToProduction Stage** → Deploys systemd services

### Manual (Local Execution)

```powershell
# From repository root on hart-server
cd /srv/deployment/hartonomous/database

# Option 1: Full orchestrated deployment
pwsh scripts/deploy/deploy-database.ps1 \
  -ServerName "localhost" \
  -DatabaseName "Hartonomous" \
  -AssemblyPath "./clr/SqlClrFunctions.dll" \
  -ProjectPath "../src/Hartonomous.Infrastructure/Hartonomous.Infrastructure.csproj" \
  -SqlUser "sa" \
  -Verbose

# Option 2: Individual script execution (for testing)
pwsh scripts/deploy/01-prerequisites.ps1 -ServerName "localhost" -DatabaseName "Hartonomous"
pwsh scripts/deploy/02-database-create.ps1 -ServerName "localhost" -DatabaseName "Hartonomous"
# ... etc
```

## Troubleshooting

### Common Issues

#### 1. CLR Strict Security Blocking Assembly

**Error:** `CREATE ASSEMBLY failed because assembly 'SqlClrFunctions' is not authorized for PERMISSION_SET = UNSAFE`

**Solution:**
```sql
-- Option A: Add to trusted assemblies (recommended)
EXEC sys.sp_add_trusted_assembly @hash = 0x<SHA512-hash>;

-- Option B: Disable CLR strict security (NOT recommended for production)
EXEC sp_configure 'clr strict security', 0;
RECONFIGURE;
```

**Script:** `04-clr-assembly.ps1` handles this automatically via `sys.sp_add_trusted_assembly`.

#### 2. FILESTREAM Not Enabled

**Error:** `FILESTREAM feature is disabled`

**Solution:**
```bash
# On hart-server
sudo /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -Q "EXEC sp_configure 'filestream access level', 2; RECONFIGURE;"
sudo systemctl restart mssql-server
```

**Script:** `03-filestream.ps1` detects this and warns but continues (requires manual fix + restart).

#### 3. Service Broker Not Starting

**Error:** `Service Broker is disabled` or `Cannot enable Service Broker - database in use`

**Solution:**
```sql
-- Requires exclusive access
ALTER DATABASE [Hartonomous] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
ALTER DATABASE [Hartonomous] SET ENABLE_BROKER;
ALTER DATABASE [Hartonomous] SET MULTI_USER;
```

**Script:** `06-service-broker.ps1` attempts this automatically but may fail if connections exist.

#### 4. EF Migrations Fail on Spatial Types

**Error:** `NetTopologySuite.Geometries.Geometry` type not found

**Solution:**
```bash
# Ensure NetTopologySuite.IO.SqlServerBytes package installed
cd src/Hartonomous.Infrastructure
dotnet add package NetTopologySuite.IO.SqlServerBytes

# Verify DbContext configuration
# HartonomousDbContext.OnModelCreating should have:
# modelBuilder.HasPostgresExtension("postgis"); // If PostgreSQL
# OR
# Use .HasColumnType("geometry") for SQL Server
```

#### 5. SSH Connection Failures

**Error:** `Permission denied (publickey)` or `Connection refused`

**Solution:**
```bash
# On hart-server, verify SSH daemon running
sudo systemctl status ssh

# Check authorized_keys
cat ~/.ssh/authorized_keys

# In Azure DevOps, verify SSH service connection:
# Project Settings → Service connections → hart-server-ssh → Edit
# Test the connection

# Check firewall
sudo ufw status
sudo ufw allow 22/tcp
```

#### 6. Azure Arc Agent Offline

**Error:** `Arc agent not responding` or `Machine not connected`

**Solution:**
```bash
# On hart-server
sudo systemctl status himdsd  # Hybrid Instance Metadata Service

# Restart agent
sudo systemctl restart himdsd

# Verify in Azure
az connectedmachine show --name hart-server --resource-group rg-hartonomous --query "status"
```

## Performance Considerations

### CLR Assembly Size

- **Current size:** ~8-12 MB (14 assemblies: CPU SIMD dependencies)
- **SQL Server limit:** ~2 GB per assembly (theoretical)
- **Recommendation:** Keep under 100 MB for deployment speed
- **Deployment method:** Embedded hex string in T-SQL (04-clr-assembly.ps1 converts binary to `0x...`)

### FILESTREAM Performance

- **Path:** `/var/opt/mssql/data/filestream` on fast SSD
- **Use cases:** Large binary payloads (embeddings, model weights)
- **Avoid:** Frequent small writes (use varbinary(max) instead)

### Service Broker Throughput

- **OODA loop latency:** ~10-50ms per message (observe→orient→decide→act)
- **Queue depth monitoring:**
```sql
SELECT q.name, q.is_receive_enabled, COUNT(c.conversation_handle) AS message_count
FROM sys.service_queues q
LEFT JOIN sys.conversation_endpoints c ON q.object_id = c.service_id
WHERE q.name NOT LIKE 'sys%'
GROUP BY q.name, q.is_receive_enabled;
```

## Security

### Credentials Management

- **Azure DevOps Variable Groups** - Store `SQL_USERNAME` and `SQL_PASSWORD` as secrets
- **SSH Keys** - Use dedicated deployment user with limited sudo access
- **CLR Strict Security** - Enabled by default, use `sys.sp_add_trusted_assembly`
- **SQL Authentication** - Prefer Windows/Integrated auth where possible (requires AD join)

### Network Security

- **SSH:** Port 22 restricted to Azure DevOps agent IP ranges
- **SQL Server:** Port 1433 (default) - internal network only, no public exposure
- **Arc Agent:** HTTPS outbound to `*.guestconfiguration.azure.com`, `*.servicebus.windows.net`

## References

### Microsoft Docs

- [Azure Arc-enabled SQL Server Overview](https://learn.microsoft.com/en-us/sql/sql-server/azure-arc/overview)
- [CLR Integration Architecture](https://learn.microsoft.com/en-us/sql/relational-databases/clr-integration/clr-integration-architecture-clr-hosted-environment)
- [FILESTREAM on Linux](https://learn.microsoft.com/en-us/sql/relational-databases/blob/filestream-sql-server#filestream-on-linux)
- [Service Broker](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-service-broker)
- [Azure DevOps YAML Pipelines](https://learn.microsoft.com/en-us/azure/devops/pipelines/yaml-schema)

### Internal Documentation

- [RADICAL_ARCHITECTURE.md](../docs/RADICAL_ARCHITECTURE.md) - Autonomous system design
- [CLR_DEPLOYMENT_STRATEGY.md](../docs/CLR_DEPLOYMENT_STRATEGY.md) - CLR deployment patterns
- [EMERGENT_CAPABILITIES.md](../docs/EMERGENT_CAPABILITIES.md) - OODA loop autonomy

## Future Enhancements

1. **High Availability** - SQL Server Always On Availability Groups with Arc
2. **Backup Strategy** - Automated backups to Azure Blob Storage via Arc
3. **Monitoring** - Azure Monitor integration for Arc SQL Server metrics
4. **DR Testing** - Automated failover testing with secondary Arc server
5. **Performance Baselines** - Query Store analysis and automatic index tuning
6. **Security Hardening** - Certificate-based CLR assembly signing instead of trusted assemblies

---

**Last Updated:** 2024-11-11  
**Maintainer:** Azure Arc DevOps Team  
**Status:** Production-ready ✓
