# Hartonomous Deployment Guide

**Version:** 1.0
**Last Updated:** November 6, 2025
**Target Environment:** Azure Arc-enabled SQL Server (On-Premises)

---

## Prerequisites

### Infrastructure Requirements

**SQL Server:**
- SQL Server 2025 (or 2022 with compatibility level 160+)
- Azure Arc-enabled for production deployments
- FILESTREAM enabled
- CLR integration enabled
- Service Broker enabled
- Minimum 32GB RAM recommended
- SSD storage for optimal spatial index performance

**Development Tools:**
- .NET 9 SDK
- PowerShell Core 7.4+
- SQL Server Management Studio 20+ or Azure Data Studio
- `sqlcmd` CLI tool

**Azure Resources (for production):**
- Azure Key Vault (credential management)
- Azure App Configuration (environment settings)
- Azure DevOps (CI/CD pipeline)
- Azure Arc Agent (SQL Server registration)

---

## Deployment Architecture

### Target Servers

**Primary:** HART-DESKTOP
- Azure Arc-enabled SQL Server
- Deployment group configured
- Development and staging workloads

**Secondary:** HART-SERVER
- Azure Arc agent installed
- Deployment group configuration pending
- Future production candidate

---

## Deployment Process

### Phase 1: Pre-Deployment Validation

**1.1 Verify Prerequisites**
```powershell
# Run prerequisite validation
.\scripts\deploy\01-prerequisites.ps1 `
    -ServerName "HART-DESKTOP" `
    -DatabaseName "Hartonomous"
```

**Validates:**
- SQL Server version and edition
- CLR integration capability
- FILESTREAM configuration
- Service Broker availability
- Required PowerShell modules

---

### Phase 2: Database Initialization

**2.1 Create Database**
```powershell
.\scripts\deploy\02-database-create.ps1 `
    -ServerName "HART-DESKTOP" `
    -DatabaseName "Hartonomous"
```

**Actions:**
- Creates database with correct collation
- Sets compatibility level to 160
- Configures database options (SNAPSHOT isolation, temporal tables)
- Initializes metadata tables

**2.2 Configure FILESTREAM**
```powershell
.\scripts\deploy\03-filestream.ps1 `
    -ServerName "HART-DESKTOP" `
    -DatabaseName "Hartonomous" `
    -FilestreamPath "/var/opt/mssql/data/filestream"
```

**Actions:**
- Enables FILESTREAM at instance level
- Creates FILESTREAM filegroup
- Configures directory structure

---

### Phase 3: CLR Assembly Deployment

**3.1 Build CLR Assembly**
```powershell
# Build with UNSAFE permission set
dotnet build src/SqlClr/SqlClrFunctions.csproj --configuration Release
```

**3.2 Deploy Assembly**
```powershell
.\scripts\deploy\04-clr-assembly.ps1 `
    -ServerName "HART-DESKTOP" `
    -DatabaseName "Hartonomous" `
    -AssemblyPath "src/SqlClr/bin/Release/SqlClrFunctions.dll"
```

**Actions:**
- Computes SHA-512 hash for trusted assembly registration
- Registers assembly in `sys.trusted_assemblies` (if CLR strict security enabled)
- Creates CLR assembly with UNSAFE permission set
- Drops and recreates if assembly hash changed

**Security Note:** UNSAFE assemblies require on-premises SQL Server. Azure SQL Managed Instance supports only SAFE assemblies.

---

### Phase 4: Schema Migration

**4.1 Run EF Core Migrations**
```powershell
.\scripts\deploy\05-ef-migrations.ps1 `
    -ServerName "HART-DESKTOP" `
    -DatabaseName "Hartonomous" `
    -ProjectPath "src/Hartonomous.Data/Hartonomous.Data.csproj"
```

**Actions:**
- Generates idempotent migration script
- Applies schema changes
- Creates tables, indexes, constraints
- Configures temporal tables

---

### Phase 5: SQL Procedures Installation

**⚠️ CRITICAL: Current Gap in Deployment**

**Status:** Manual workaround required until script `08-create-procedures.ps1` is implemented.

**Manual Procedure Installation:**
```powershell
# Execute all procedure files in dependency order
$procedureOrder = @(
    "Common.ClrBindings.sql",
    "Common.Helpers.sql",
    "Common.CreateSpatialIndexes.sql",
    "dbo.*.sql",
    "Spatial.*.sql",
    "Inference.*.sql",
    "Generation.*.sql",
    "Embedding.*.sql",
    "Billing.*.sql",
    "Autonomy.*.sql",
    "Attention.*.sql",
    "Functions.*.sql"
)

foreach ($pattern in $procedureOrder) {
    Get-ChildItem -Path "sql/procedures" -Filter $pattern |
        ForEach-Object {
            Write-Host "Executing: $($_.Name)"
            sqlcmd -S HART-DESKTOP -d Hartonomous -i $_.FullName -b
            if ($LASTEXITCODE -ne 0) {
                throw "Failed to execute $($_.Name)"
            }
        }
}
```

**Note:** This step will be automated in Sprint 2025-Q1. See [KNOWN_ISSUES.md](../KNOWN_ISSUES.md) Issue #1.

---

### Phase 6: Service Broker Configuration

**6.1 Enable and Configure Service Broker**
```powershell
.\scripts\deploy\06-service-broker.ps1 `
    -ServerName "HART-DESKTOP" `
    -DatabaseName "Hartonomous" `
    -SetupScriptPath "scripts/setup-service-broker.sql"
```

**Actions:**
- Enables Service Broker on database
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

