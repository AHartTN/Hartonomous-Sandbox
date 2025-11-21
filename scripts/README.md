# ?? **HARTONOMOUS DEPLOYMENT GUIDE**

**Single Command Deployment - No Confusion**

---

## **?? QUICK START**

### **Fresh Database Deployment**:
```powershell
.\scripts\Deploy-Master.ps1 -Server localhost
```

### **Update Existing Database**:
```powershell
.\scripts\Deploy-Master.ps1 -Server localhost -SkipDatabaseCreation
```

---

## **?? DEPLOYMENT ARCHITECTURE**

```
Deploy-Master.ps1 (Entry Point)
    ??? deploy-hartonomous.ps1 (Unified Deployment)
            ??? 1. Pre-Flight Checks
            ??? 2. Build DACPAC
            ??? 3. Configure CLR Security
            ??? 4. Deploy DACPAC (Schema + CLR)
            ??? 5. Deploy External CLR Dependencies
            ??? 6. Scaffold EF Core Entities
            ??? 7. Build .NET Solution
            ??? 8. Deploy Stored Procedures
            ??? 9. Validation
```

---

## **? WHAT YOU GET**

After running `Deploy-Master.ps1`:

1. ? **Database Created** (if needed)
2. ? **Schema Deployed** (all tables, views, functions)
3. ? **CLR Assemblies Registered** (Hartonomous.Clr + dependencies)
4. ? **Procedures Deployed** (all stored procedures)
5. ? **Entities Scaffolded** (EF Core entities from schema)
6. ? **Solution Built** (all projects compile)
7. ? **Validated** (post-deployment checks)

---

## **?? SCRIPT INVENTORY**

### **PRIMARY SCRIPTS** (Use These):
- `Deploy-Master.ps1` - **SINGLE ENTRY POINT** for all deployments
- `deploy-hartonomous.ps1` - Comprehensive unified deployment (called by Master)

### **SUPPORTING SCRIPTS** (Called Automatically):
- `Initialize-CLRSigning.ps1` - CLR certificate setup
- `Deploy-CLRCertificate.ps1` - Deploy cert to SQL Server
- `Build-WithSigning.ps1` - Build with strong-name signing
- `deploy-dacpac.ps1` - DACPAC deployment only
- `scaffold-entities.ps1` - EF Core scaffolding
- `Test-PipelineConfiguration.ps1` - Pipeline validation
- `Run-CoreTests.ps1` - Test execution

### **DEPRECATED** (Don't Use):
- `Deploy-All.ps1` - Replaced by Deploy-Master.ps1
- Individual deployment scripts - Now integrated into deploy-hartonomous.ps1

---

## **?? PREREQUISITES**

### **Required Tools**:
```powershell
# Check if you have all required tools
dotnet --version        # .NET 10 SDK
sqlcmd -?               # SQL Server CLI
sqlpackage /?           # DACPAC deployment
dotnet ef --version     # EF Core tools
```

### **Install Missing Tools**:
```powershell
# .NET SDK
winget install Microsoft.DotNet.SDK.10

# SQL Server command line tools
winget install Microsoft.SQLServer.Tools

# EF Core tools
dotnet tool install --global dotnet-ef --version 10.0.0
```

---

## **?? DETAILED OPTIONS**

### **Deploy-Master.ps1 Parameters**:

| Parameter | Default | Description |
|-----------|---------|-------------|
| `-Server` | localhost | SQL Server instance |
| `-Database` | Hartonomous | Database name |
| `-SkipDatabaseCreation` | false | Skip DB creation (for existing DB) |
| `-SkipCLR` | false | Skip CLR assembly deployment |
| `-SkipScaffold` | false | Skip EF Core entity scaffolding |
| `-SkipBuild` | false | Skip .NET solution build |

### **Examples**:

```powershell
# Deploy to production server
.\scripts\Deploy-Master.ps1 -Server prodserver.database.windows.net -Database Hartonomous

# Update existing database only (skip creation)
.\scripts\Deploy-Master.ps1 -Server localhost -SkipDatabaseCreation

# Fast deployment (skip scaffolding and build)
.\scripts\Deploy-Master.ps1 -Server localhost -SkipScaffold -SkipBuild

# Schema only (no CLR)
.\scripts\Deploy-Master.ps1 -Server localhost -SkipCLR
```

---

## **?? TROUBLESHOOTING**

### **"Cannot connect to SQL Server"**
```powershell
# Test connection
Test-NetConnection -ComputerName localhost -Port 1433

# Check SQL Server is running
Get-Service MSSQLSERVER
```

### **"MSBuild not found"**
- Install Visual Studio 2022 with SQL Server Data Tools (SSDT)
- Or install VS Build Tools 2022

### **"DACPAC deployment failed"**
```powershell
# Check DACPAC exists
Test-Path "src\Hartonomous.Database\bin\Release\Hartonomous.Database.dacpac"

# Rebuild DACPAC manually
.\scripts\build-dacpac.ps1
```

### **"Entity scaffolding failed"**
- Ensure database is deployed first
- Check connection string
- Verify dotnet-ef is installed

---

## **?? DEPLOYMENT SCENARIOS**

### **Scenario 1: Local Development (First Time)**
```powershell
# Fresh deployment
.\scripts\Deploy-Master.ps1 -Server localhost

# Start API
cd src\Hartonomous.Api
dotnet run
```

### **Scenario 2: Update After Code Changes**
```powershell
# Update existing database
.\scripts\Deploy-Master.ps1 -Server localhost -SkipDatabaseCreation
```

### **Scenario 3: Production Deployment**
```powershell
# 1. Backup production database first!
sqlcmd -S prodserver -Q "BACKUP DATABASE Hartonomous TO DISK='C:\Backups\Hartonomous_backup.bak'"

# 2. Deploy update
.\scripts\Deploy-Master.ps1 -Server prodserver -SkipDatabaseCreation

# 3. Validate
.\scripts\Test-HartonomousDeployment-Simple.ps1 -Server prodserver
```

### **Scenario 4: CI/CD Pipeline**
```yaml
# .github/workflows/deploy.yml
- name: Deploy Hartonomous
  run: |
    .\scripts\Deploy-Master.ps1 `
      -Server ${{ secrets.SQL_SERVER }} `
      -Database Hartonomous `
      -SkipDatabaseCreation
```

---

## **? POST-DEPLOYMENT CHECKLIST**

After deployment completes:

- [ ] Verify database exists: `sqlcmd -S localhost -Q "SELECT DB_ID('Hartonomous')"`
- [ ] Check table count: `sqlcmd -S localhost -d Hartonomous -Q "SELECT COUNT(*) FROM sys.tables"`
- [ ] Verify CLR enabled: `sqlcmd -S localhost -Q "EXEC sp_configure 'clr enabled'"`
- [ ] Test API: `curl http://localhost:5000/health`
- [ ] Run tests: `.\scripts\Run-CoreTests.ps1`

---

## **?? DEPLOYMENT TIMELINE**

Typical deployment duration (local SSD, localhost):

| Phase | Duration |
|-------|----------|
| Pre-Flight Checks | ~5s |
| Build DACPAC | ~30s |
| Deploy DACPAC | ~15s |
| Deploy CLR | ~20s |
| Scaffold Entities | ~10s |
| Build Solution | ~20s |
| Deploy Procedures | ~10s |
| Validation | ~5s |
| **Total** | **~2 minutes** |

---

## **?? SUPPORT**

### **Documentation**:
- `docs/CI_CD_PIPELINE_GUIDE.md` - Complete CI/CD guide
- `docs/ENTERPRISE_DEPLOYMENT.md` - Production deployment
- `docs/DEPLOYMENT_AUDIT_REFACTOR.md` - Script architecture

### **Common Issues**:
- Build errors: Check `docs/BUILD_TROUBLESHOOTING.md`
- Test failures: See `tests/README.md`
- Pipeline issues: Review `docs/CI_CD_PIPELINE_GUIDE.md`

---

**REMEMBER**: Always use `Deploy-Master.ps1` as the entry point. No other deployment script should be called directly.

