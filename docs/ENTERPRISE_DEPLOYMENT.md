# ?? **ENTERPRISE DEPLOYMENT ARCHITECTURE**

**Purpose**: Database testing and deployment strategy for dev/staging/prod  
**Strategy**: Hybrid approach - right tool for each environment

---

## **?? DEPLOYMENT ENVIRONMENTS**

### **1. LOCAL DEVELOPMENT** ?????

**Database**: SQL Server LocalDB  
**Purpose**: Fast inner development loop  
**Deployment**: Manual via Visual Studio or DACPAC

```powershell
# Developer workflow
1. Write code
2. Run tests (LocalDB) ? Fast (<1 second startup)
3. Debug locally
4. Commit changes
```

**Benefits**:
- ? No Docker required
- ? Instant startup
- ? Full SQL Server features
- ? Easy debugging

**Connection String**:
```
Server=(localdb)\mssqllocaldb;
Database=Hartonomous;
Integrated Security=True;
TrustServerCertificate=True;
```

---

### **2. CI/CD PIPELINE** ??

**Database**: Docker Container (SQL Server 2022)  
**Purpose**: Automated testing on every commit  
**Deployment**: Testcontainers or docker-compose

```yaml
# .github/workflows/tests.yml
name: Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      
      - name: Run Tests
        run: dotnet test --configuration Release
        env:
          CI: true  # Triggers Docker mode in DatabaseTestBase
```

**How It Works**:
```csharp
// DatabaseTestBase.cs auto-detects CI environment
var isCI = Environment.GetEnvironmentVariable("CI") != null;
if (isCI) {
    // Use Testcontainers (Docker)
    _sqlContainer = new MsSqlBuilder().Build();
    await _sqlContainer.StartAsync();
}
```

**Benefits**:
- ? Consistent environment (every run identical)
- ? Isolated (no state leakage between runs)
- ? Cross-platform (Linux/Mac/Windows)
- ? Disposable (clean slate every time)

---

### **3. STAGING ENVIRONMENT** ??

**Database**: Azure SQL Database  
**Purpose**: Pre-production validation  
**Deployment**: Azure DevOps Pipelines or GitHub Actions

```yaml
# .github/workflows/deploy-staging.yml
name: Deploy to Staging

on:
  push:
    branches: [develop]

jobs:
  deploy:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v3
      
      - name: Deploy DACPAC to Azure SQL
        uses: azure/sql-action@v2
        with:
          connection-string: ${{ secrets.AZURE_SQL_STAGING_CONNECTION }}
          path: './src/Hartonomous.Database/bin/Release/Hartonomous.Database.dacpac'
          action: 'publish'
      
      - name: Run Integration Tests
        run: dotnet test tests/Hartonomous.IntegrationTests
        env:
          HARTONOMOUS_TEST_DB: ${{ secrets.AZURE_SQL_STAGING_CONNECTION }}
```

**Benefits**:
- ? Production-like environment
- ? Real Azure SQL features (geo-replication, auto-backup)
- ? Performance testing with realistic latency
- ? Integration testing with other Azure services

**Connection String** (from Azure Key Vault):
```
Server=hartonomous-staging.database.windows.net;
Database=Hartonomous;
Authentication=Active Directory Default;
Encrypt=True;
```

---

### **4. PRODUCTION ENVIRONMENT** ??

**Database**: Azure SQL Database (Premium/Hyperscale tier)  
**Purpose**: Real workload  
**Deployment**: Blue/Green deployment with validation

```yaml
# .github/workflows/deploy-production.yml
name: Deploy to Production

on:
  release:
    types: [published]

jobs:
  deploy:
    runs-on: ubuntu-latest
    environment: production  # Requires manual approval
    
    steps:
      - name: Download DACPAC
        uses: actions/download-artifact@v3
      
      - name: Validate DACPAC
        run: sqlpackage /Action:DeployReport /SourceFile:Hartonomous.Database.dacpac /TargetConnectionString:"$PROD_CONNECTION"
      
      - name: Deploy to Blue Slot
        uses: azure/sql-action@v2
        with:
          connection-string: ${{ secrets.AZURE_SQL_PROD_BLUE }}
          path: './Hartonomous.Database.dacpac'
          action: 'publish'
      
      - name: Run Smoke Tests
        run: dotnet test tests/Hartonomous.SmokeTests
        env:
          HARTONOMOUS_TEST_DB: ${{ secrets.AZURE_SQL_PROD_BLUE }}
      
      - name: Traffic Switch (Blue ? Green)
        if: success()
        run: az sql db update --name Hartonomous --resource-group Production --server prod-sql --set primarySlot=blue
```

**Benefits**:
- ? Zero-downtime deployments
- ? Automatic rollback on failure
- ? Production validation before traffic switch
- ? Full observability (Application Insights)

---

## **?? HYBRID TEST STRATEGY**

### **Environment Detection Logic**:

```csharp
public static class DatabaseTestEnvironment
{
    public static TestEnvironment Detect()
    {
        // Priority 1: Explicit Azure SQL connection string
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HARTONOMOUS_TEST_DB")))
            return TestEnvironment.AzureSql;
        
        // Priority 2: CI/CD environment
        if (IsCI())
            return TestEnvironment.CiCd;
        
        // Priority 3: Local development (default)
        return TestEnvironment.LocalDevelopment;
    }
    
    private static bool IsCI()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) ||
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")) ||
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TF_BUILD")) ||
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BUILD_BUILDID"));
    }
}
```

---

## **?? ENVIRONMENT MATRIX**

| Environment | Database | Connection | Startup Time | Cost | Consistency |
|-------------|----------|------------|--------------|------|-------------|
| **Local Dev** | LocalDB | Integrated Security | <1s | Free | Good |
| **CI/CD** | Docker | SA password | ~5-10s | Free | Excellent |
| **Staging** | Azure SQL | Managed Identity | N/A | $$ | Excellent |
| **Production** | Azure SQL | Managed Identity | N/A | $$$ | Excellent |

---

## **?? SETUP INSTRUCTIONS**

### **For Developers (LocalDB)**:
```powershell
# No setup required - LocalDB comes with Visual Studio
# Just run tests:
.\scripts\Run-CoreTests.ps1
```

### **For CI/CD (Docker)**:
```yaml
# GitHub Actions - automatic
# Sets environment variable CI=true
# DatabaseTestBase detects this and uses Docker
```

### **For Staging (Azure SQL)**:
```powershell
# Set environment variable
$env:HARTONOMOUS_TEST_DB = "Server=staging.database.windows.net;Database=Hartonomous;Authentication=Active Directory Default;"

# Run tests
dotnet test tests/Hartonomous.IntegrationTests
```

---

## **?? DACPAC DEPLOYMENT STRATEGY**

### **Local ? Staging ? Production**:

```mermaid
Developer (LocalDB)
    ?
Commit to Git
    ?
CI/CD Build (Docker tests)
    ?
Generate DACPAC artifact
    ?
Deploy to Staging (Azure SQL)
    ?
Run integration tests
    ?
Manual approval gate
    ?
Deploy to Production (Blue slot)
    ?
Smoke tests
    ?
Traffic switch (Blue ? Green)
```

### **DACPAC Deployment Commands**:

```powershell
# Staging deployment
sqlpackage /Action:Publish `
    /SourceFile:"Hartonomous.Database.dacpac" `
    /TargetConnectionString:"Server=staging.database.windows.net;Database=Hartonomous;Authentication=Active Directory Default;" `
    /p:BlockOnPossibleDataLoss=True `
    /p:BackupDatabaseBeforeChanges=True

# Production deployment (with validation)
sqlpackage /Action:DeployReport `
    /SourceFile:"Hartonomous.Database.dacpac" `
    /TargetConnectionString:"$PROD_CONNECTION" `
    /OutputPath:"deploy-report.xml"

# Review deploy-report.xml before proceeding

sqlpackage /Action:Publish `
    /SourceFile:"Hartonomous.Database.dacpac" `
    /TargetConnectionString:"$PROD_CONNECTION" `
    /p:BlockOnPossibleDataLoss=True `
    /p:BackupDatabaseBeforeChanges=True `
    /DeployScriptPath:"deploy-script.sql"  # Generate script for DBA review
```

---

## **?? BEST PRACTICES**

### **Database Versioning**:
```sql
-- Track DACPAC deployments
CREATE TABLE dbo.DatabaseVersion (
    VersionId INT IDENTITY PRIMARY KEY,
    DacpacVersion NVARCHAR(50) NOT NULL,
    DeployedBy NVARCHAR(100) NOT NULL,
    DeployedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    Environment NVARCHAR(50) NOT NULL,  -- 'Local', 'Staging', 'Production'
    GitCommitHash NVARCHAR(40)
);

-- After successful deployment
INSERT INTO dbo.DatabaseVersion (DacpacVersion, DeployedBy, Environment, GitCommitHash)
VALUES ('1.0.0', SYSTEM_USER, 'Staging', 'abc123...');
```

### **Schema Drift Detection**:
```powershell
# Compare staging vs production
sqlpackage /Action:DeployReport `
    /SourceConnectionString:"$STAGING_CONNECTION" `
    /TargetConnectionString:"$PROD_CONNECTION" `
    /OutputPath:"drift-report.xml"

# Review drift-report.xml
# Should show: "No differences detected" ?
```

### **Rollback Strategy**:
```sql
-- Option 1: Restore from backup
RESTORE DATABASE Hartonomous 
FROM DISK = 'D:\Backups\Hartonomous_PreDeployment.bak'
WITH REPLACE;

-- Option 2: Temporal tables (if enabled)
SELECT * FROM dbo.Atom FOR SYSTEM_TIME AS OF '2025-01-15 14:30:00';

-- Option 3: DACPAC rollback (deploy previous version)
sqlpackage /Action:Publish /SourceFile:"Hartonomous.Database.v1.0.0.dacpac" ...
```

---

## **?? DEPLOYMENT CHECKLIST**

### **Staging Deployment**:
- [ ] Generate DACPAC from main branch
- [ ] Review deploy report for breaking changes
- [ ] Deploy to staging Azure SQL
- [ ] Run integration tests
- [ ] Validate schema with test queries
- [ ] Check DatabaseVersion table
- [ ] Monitor performance for 24 hours

### **Production Deployment**:
- [ ] All staging tests passing
- [ ] DBA review of deploy script
- [ ] Production backup verified
- [ ] Deploy to blue slot (inactive)
- [ ] Run smoke tests on blue slot
- [ ] Switch traffic to blue slot
- [ ] Monitor errors/performance
- [ ] Keep green slot for 48 hours (rollback option)
- [ ] Decommission green slot

---

## **?? ANSWER TO YOUR QUESTION**

**"What about dev/staging/prod deployments?"**

### **The Hybrid Approach**:

| Environment | Database | Why |
|-------------|----------|-----|
| **Dev (Local)** | LocalDB | ? Fast iteration, no dependencies |
| **CI/CD** | Docker | ? Consistent, isolated, cross-platform |
| **Staging** | Azure SQL | ? Production-like, managed service |
| **Production** | Azure SQL | ? Enterprise features, HA, backups |

### **The Code Supports All Four**:
```csharp
// Automatically detects environment and uses appropriate database:
- Local dev: LocalDB (detected: no CI env variable)
- CI/CD: Docker (detected: CI=true)
- Staging: Azure SQL (detected: HARTONOMOUS_TEST_DB set)
- Production: Azure SQL (deployed via DACPAC)
```

---

## **? RECOMMENDED SETUP**

### **Now** (Phase 7):
```
? Local dev: Use LocalDB (fast, simple)
? Tests: Support both LocalDB and Docker (hybrid)
```

### **Phase 8** (CI/CD):
```
? Create GitHub Actions workflow
? Use Docker in CI/CD (consistent)
? Generate DACPAC artifacts
```

### **Phase 9** (Staging):
```
? Provision Azure SQL (staging)
? Deploy DACPAC to staging
? Run integration tests
```

### **Phase 10** (Production):
```
? Blue/Green deployment
? Automated rollback
? Production monitoring
```

---

## **?? THE ANSWER**

**Do you need Docker?**
- **For local dev**: ? No - LocalDB is faster
- **For CI/CD**: ? Yes - Consistency matters
- **For staging/prod**: ? No - Use Azure SQL

**Current implementation**: ? **Hybrid** - supports all scenarios with auto-detection

**Your tests now work**:
- ? Locally WITHOUT Docker (LocalDB)
- ? In CI/CD WITH Docker (Testcontainers)
- ? In staging/prod WITH Azure SQL (connection string)

**One codebase, all environments** ??

