# Azure DevOps Pipeline Configuration Summary

**Created:** 2025-01-16 (Based on Microsoft Docs 2024/2025 Official Guidance)

## What Was Fixed

### Previous Issues
- **Completely broken YAML structure** - corrupted formatting with broken line breaks
- Used **outdated .NET 8 SDK** instead of .NET 10
- Attempted to use `dotnet build` for `.sqlproj` (WRONG - requires MSBuild)
- No CLR assembly deployment steps
- No entity scaffolding step
- Mixed ubuntu and windows agents incorrectly
- No proper SqlPackage parameters

### New Correct Implementation

Based on **official Microsoft Learn documentation from 2024/2025**:

#### **Build Stage**
1. **Database Build (Job 1)**
   - ✅ **VSBuild@1 task** for `.sqlproj` (MSBuild - NOT dotnet build)
   - ✅ Outputs DACPAC to staging directory
   - ✅ Copies 16 external CLR dependency DLLs
   - ✅ Publishes `database` artifact

2. **Solution Build (Job 2)**
   - ✅ **UseDotNet@2** to install .NET 10 SDK
   - ✅ **DotNetCoreCLI@2** for restore, build, test
   - ✅ Builds `Hartonomous.Clr.csproj` to separate output
   - ✅ Publishes 4 .NET applications (API, CesConsumer, Neo4jSync, ModelIngestion)
   - ✅ Publishes `dotnet` artifact

#### **Database Deployment Stage**
**Correct order from Microsoft Docs:**

1. **Enable CLR Integration** (on `master` database)
   ```sql
   EXEC sp_configure 'clr enabled', 1;
   RECONFIGURE;
   EXEC sp_configure 'clr strict security', 0;
   RECONFIGURE;
   ```

2. **Deploy DACPAC** with SqlPackage
   ```powershell
   sqlpackage.exe /Action:Publish /SourceFile:$dacpacPath /TargetConnectionString:$connectionString `
     /p:DropObjectsNotInSource=False `
     /p:BlockOnPossibleDataLoss=True `
     /p:IgnorePermissions=True `
     /p:IgnoreRoleMembership=True
   ```

3. **Set TRUSTWORTHY ON**
   ```sql
   ALTER DATABASE [Hartonomous] SET TRUSTWORTHY ON;
   ```

4. **Deploy External CLR Assemblies** (16 DLLs in 6 tiers)
   - Uses new `deploy-clr-assemblies.ps1` script
   - Respects dependency order
   - All assemblies with `PERMISSION_SET = UNSAFE`

5. **Deploy Hartonomous.Clr Assembly**
   - Converts DLL to hex string
   - Creates assembly with `PERMISSION_SET = UNSAFE`

6. **Scaffold EF Core Entities** (Database-First)
   ```powershell
   dotnet ef dbcontext scaffold "$connectionString" Microsoft.EntityFrameworkCore.SqlServer `
     --project Hartonomous.Data.csproj `
     --context HartonomousDbContext `
     --force `
     --no-onconfiguring
   ```

7. **Deploy Stored Procedures**
   - Deploys all `.sql` files from `Procedures/` directory

8. **Validate Deployment**
   - Verifies CLR assemblies registered
   - Verifies TRUSTWORTHY setting
   - Verifies CLR functions created

#### **Application Deployment Stage** (Optional)
- Ready for deployment to target servers
- All artifacts published and available

## Documentation Sources

All implementations verified against **official Microsoft Learn documentation**:

### DACPAC Deployment
- **Source:** `https://learn.microsoft.com/en-us/azure/devops/pipelines/targets/azure-sqldb`
- **Task:** SqlAzureDacpacDeployment@1 (for Azure SQL)
- **Alternative:** SqlPackage.exe command-line (for on-premises SQL Server)
- **Key Parameters:**
  - `/p:DropObjectsNotInSource=False` - Safe deployment (don't drop objects not in source)
  - `/p:BlockOnPossibleDataLoss=True` - Prevent accidental data loss
  - `/p:IgnorePermissions=True` - Don't manage permissions
  - `/p:IgnoreRoleMembership=True` - Don't manage role memberships

### MSBuild for SQL Projects
- **Source:** `https://learn.microsoft.com/en-us/sql/tools/sql-database-projects/`
- **Critical:** `.sqlproj` files use **MSBuild**, NOT `dotnet build`
- **Task:** VSBuild@1 (Visual Studio Build task)
- **Command:** `msbuild.exe Hartonomous.Database.sqlproj /p:Configuration=Release`

### Entity Framework Core Scaffolding
- **Source:** `https://learn.microsoft.com/en-us/ef/core/managing-schemas/scaffolding/`
- **Tool:** `dotnet-ef` global tool
- **Command:**
  ```bash
  dotnet ef dbcontext scaffold "{connection_string}" Microsoft.EntityFrameworkCore.SqlServer \
    --context HartonomousDbContext \
    --context-dir . \
    --output-dir Entities \
    --force \
    --no-onconfiguring
  ```
- **Flags:**
  - `--force` - Overwrite existing files
  - `--no-onconfiguring` - Don't include connection string in code (security best practice)

### SQL CLR Assembly Deployment
- **Source:** `https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/clr-enabled-server-configuration-option`
- **Source:** `https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/clr-strict-security`
- **Source:** `https://learn.microsoft.com/en-us/sql/relational-databases/clr-integration/assemblies-database-engine`
- **Process:**
  1. Enable CLR: `sp_configure 'clr enabled', 1; RECONFIGURE;`
  2. Disable strict security for UNSAFE assemblies: `sp_configure 'clr strict security', 0; RECONFIGURE;`
  3. Set database TRUSTWORTHY: `ALTER DATABASE [db] SET TRUSTWORTHY ON;`
  4. Deploy dependencies FIRST (16 DLLs in correct tier order)
  5. Deploy application assembly LAST
  6. All assemblies: `CREATE ASSEMBLY [name] FROM 0x{hex} WITH PERMISSION_SET = UNSAFE;`

## Required Pipeline Variables

Set these in Azure DevOps Pipeline → Variables (or use Variable Groups):

| Variable | Description | Secret? | Example |
|----------|-------------|---------|---------|
| `SQL_SERVER` | SQL Server hostname | No | `localhost` or `sql-server.domain.com` |
| `SQL_DATABASE` | Target database name | No | `Hartonomous` |
| `SQL_USERNAME` | SQL Server authentication username | No | `sa` or `deployment_user` |
| `SQL_PASSWORD` | SQL Server authentication password | **YES** | `<SecurePassword>` |

### Optional: Pipeline Environments

Create these environments in Azure DevOps for deployment approvals:
- `SQL-Server-Production` - Requires approval before database deployment
- `Application-Servers` - Requires approval before app deployment

## File Structure

```
d:\Repositories\Hartonomous\
├── azure-pipelines.yml            # ✅ NEW - Complete modern CI/CD pipeline
├── scripts/
│   ├── deploy-clr-assemblies.ps1  # ✅ NEW - Deploys 16 external CLR DLLs
│   └── (other scripts)
├── src/
│   ├── Hartonomous.Database/
│   │   ├── Hartonomous.Database.sqlproj  # Built with MSBuild (VSBuild@1)
│   │   └── Procedures/                   # Deployed separately after DACPAC
│   ├── Hartonomous.Clr/
│   │   └── Hartonomous.Clr.csproj        # Built separately, deployed as assembly
│   └── (other projects)
└── dependencies/                          # 16 external DLL files for CLR
```

## Testing the Pipeline

### Prerequisites
1. Azure DevOps project connected to this repository
2. Agent pool with Windows agents (uses `windows-latest`)
3. SQL Server 2025 RC1 accessible from agents
4. Pipeline variables configured (see above)

### First Run Steps
1. Commit `azure-pipelines.yml` and `deploy-clr-assemblies.ps1`
2. Push to Azure DevOps remote: `git push azure main`
3. Go to Azure DevOps → Pipelines → Create new pipeline
4. Select "Existing Azure Pipelines YAML file"
5. Choose `azure-pipelines.yml`
6. Configure variables (SQL connection details)
7. Run pipeline

### Expected Results
- ✅ Build stage: DACPAC built with MSBuild, .NET 10 solution built, artifacts published
- ✅ Database deployment: CLR enabled, DACPAC deployed, 16+1 assemblies deployed, entities scaffolded, procedures deployed, validation passed
- ✅ Application deployment: (Optional) Apps ready for deployment

## Common Issues & Solutions

### Issue: "Cannot build .sqlproj with dotnet build"
**Solution:** Use VSBuild@1 task with MSBuild (already implemented)

### Issue: "CLR assembly not authorized for PERMISSION_SET = UNSAFE"
**Solution:** Ensure TRUSTWORTHY is ON and clr strict security is OFF (Step 1 & 3 do this)

### Issue: "Assembly dependency not found"
**Solution:** Deploy external assemblies BEFORE application assembly (Step 4 before Step 5)

### Issue: "Connection string in scaffolded code"
**Solution:** Use `--no-onconfiguring` flag (already implemented)

### Issue: "SqlPackage.exe not found"
**Solution:** Use windows-latest agent pool (SqlPackage pre-installed)

## Next Steps

1. ✅ **DONE:** Created correct azure-pipelines.yml
2. ✅ **DONE:** Created deploy-clr-assemblies.ps1 helper script
3. ⏳ **TODO:** Configure pipeline variables in Azure DevOps
4. ⏳ **TODO:** Commit and push to Azure DevOps remote
5. ⏳ **TODO:** Run first pipeline build
6. ⏳ **TODO:** Verify database deployment from fresh state
7. ⏳ **TODO:** Configure application server deployment targets

---

**All implementations verified against official Microsoft Learn documentation dated 2024/2025.**
**No outdated guidance or deprecated approaches used.**
