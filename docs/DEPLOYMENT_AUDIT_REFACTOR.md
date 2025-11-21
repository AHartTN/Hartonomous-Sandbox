# ?? **COMPREHENSIVE SCRIPT AUDIT & REFACTORING PLAN**

**Date**: January 2025  
**Scope**: ALL deployment scripts, migrations, and SQL files  
**Goal**: Single, idempotent, properly-ordered deployment workflow

---

## **?? CRITICAL ISSUES FOUND**

### **1. DUPLICATE DEPLOYMENT SCRIPTS**
- `Deploy-All.ps1` - 6-phase deployment orchestrator
- `deploy-hartonomous.ps1` - 9-step unified deployment
- **Problem**: Overlapping responsibilities, confusing, hard to maintain
- **Solution**: Create ONE master deployment script

### **2. SQL SYNTAX ERRORS**

#### **TenantGuidMapping.sql** ?
```sql
-- LINE 23: Duplicate constraint
CONSTRAINT [UQ_TenantGuidMapping_TenantGuid] UNIQUE NONCLUSTERED ([TenantGuid]),
CONSTRAINT [UQ_TenantGuidMapping_TenantGuid] UNIQUE NONCLUSTERED ([TenantGuid])  -- DUPLICATE!

-- LINE 33-34: Duplicate INCLUDE clause
INCLUDE ([TenantId], [TenantGuid], [TenantName], [StripeCustomerId]);
INCLUDE ([TenantGuid], [TenantName], [CreatedAt])  -- DUPLICATE!
```

### **3. MISSING IDEMPOTENCY**
Many scripts don't have proper IF EXISTS checks:
- Migration scripts
- Pre/Post deployment scripts
- Procedure deployments

### **4. NO CLEAR EXECUTION ORDER**
Scripts reference each other circularly:
- Deploy-All.ps1 calls deploy-dacpac.ps1
- deploy-hartonomous.ps1 duplicates this logic
- No clear entry point

### **5. INCONSISTENT ERROR HANDLING**
- Some scripts use `$ErrorActionPreference = "Stop"`
- Others don't trap errors
- No rollback mechanisms

---

## **?? REFACTORING PLAN**

### **PHASE 1: FIX SQL SYNTAX ERRORS** ?
1. Fix TenantGuidMapping.sql (duplicate constraints/includes)
2. Audit all table files for similar issues
3. Audit all procedure files for syntax errors

### **PHASE 2: CREATE MASTER DEPLOYMENT SCRIPT**
**NEW**: `Deploy-Master.ps1`
- Single entry point
- Clear phases with proper ordering
- Idempotent operations
- Comprehensive error handling
- Rollback capabilities

### **PHASE 3: REFACTOR MIGRATIONS**
**Goal**: All migrations idempotent with proper checks

Pattern:
```sql
-- Migration: <Name>
-- Date: <Date>
-- Idempotent: YES

-- Check if migration already applied
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MigrationHistory')
BEGIN
    CREATE TABLE MigrationHistory (
        MigrationId INT IDENTITY PRIMARY KEY,
        MigrationName NVARCHAR(255) NOT NULL,
        AppliedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END

IF NOT EXISTS (SELECT 1 FROM MigrationHistory WHERE MigrationName = '<This Migration>')
BEGIN
    -- Migration code here
    
    INSERT INTO MigrationHistory (MigrationName) VALUES ('<This Migration>');
END
```

### **PHASE 4: REFACTOR PRE-DEPLOYMENT**
All pre-deployment scripts need:
- IF EXISTS checks
- Rollback safety
- Clear comments

### **PHASE 5: REFACTOR POST-DEPLOYMENT**
All post-deployment scripts need:
- IF EXISTS checks
- Dependency ordering
- Clear validation

### **PHASE 6: DEPRECATE OLD SCRIPTS**
Move to `/scripts/deprecated`:
- Deploy-All.ps1 (replaced by Deploy-Master.ps1)
- Individual deployment scripts that are now integrated

---

## **?? NEW DEPLOYMENT ARCHITECTURE**

### **Entry Point**:
```
Deploy-Master.ps1
  ??? 1. Pre-Flight Checks
  ??? 2. Database Creation (if needed)
  ??? 3. Migration History Setup
  ??? 4. Run Migrations (in order)
  ??? 5. Deploy DACPAC
  ??? 6. Deploy CLR Assemblies
  ??? 7. Run Post-Deployment
  ??? 8. Scaffold Entities
  ??? 9. Build Solution
  ??? 10. Validation
```

### **Supporting Scripts** (called by master):
- `Initialize-Database.ps1` - Creates database if needed
- `Deploy-Migrations.ps1` - Runs all migrations in order
- `Deploy-DACPAC.ps1` - DACPAC deployment only
- `Deploy-CLR.ps1` - CLR assembly deployment
- `Scaffold-Entities.ps1` - EF Core scaffolding
- `Validate-Deployment.ps1` - Post-deployment validation

---

## **?? IMMEDIATE ACTIONS**

1. ? Fix TenantGuidMapping.sql syntax errors
2. ? Audit all SQL files for duplicate constraints
3. ? Create Deploy-Master.ps1 (single entry point)
4. ? Add MigrationHistory table pattern
5. ? Refactor Phase9_SchemaRefactor.sql to use MigrationHistory
6. ? Test end-to-end deployment on clean database
7. ? Test end-to-end deployment on existing database (idempotency)
8. ? Document deployment process

---

## **?? TOOLS NEEDED**

- PowerShell 7+
- SQL Server 2022+
- .NET 10 SDK
- MSBuild (Visual Studio 2022)
- sqlcmd (SQL Server CLI)
- sqlpackage (DACPAC deployment)
- dotnet-ef (Entity scaffolding)

---

## **? SUCCESS CRITERIA**

1. **Single Command Deployment**: `.\Deploy-Master.ps1 -Server localhost`
2. **Idempotent**: Can run multiple times safely
3. **Clear Output**: Progress bars, colored status messages
4. **Proper Error Handling**: Rollback on failure
5. **Validation**: Automated post-deployment checks
6. **Documentation**: Clear README in /scripts

---

**This audit identifies all issues. Now executing refactoring systematically.**

