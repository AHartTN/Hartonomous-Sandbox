# GitHub Actions Runner Architecture

## Overview
The Hartonomous CI/CD pipeline uses **two self-hosted runners** with specific job assignments based on technical requirements.

## Runner Configuration

### HART-DESKTOP (Windows)
**Location:** `D:\GitHub\actions-runner`  
**Service:** `actions.runner.AHartTN-Hartonomous-Sandbox.HART-DESKTOP`  
**Labels:** `self-hosted`, `windows`, `sql-server`

**Capabilities:**
- Windows Server 2025
- .NET Framework 4.8.1 (required for CLR assemblies)
- MSBuild from Visual Studio 2022/Insiders
- SQL Server 2022 Developer Edition (localhost)
- SqlPackage for DACPAC deployment
- Azure CLI with Arc agent
- PowerShell 7.5

**Assigned Jobs:**
1. **build-dacpac** - Builds .sqlproj with CLR assemblies (requires .NET Framework + MSBuild)
2. **deploy-database** - Deploys DACPAC, CLR assemblies, sets up SQL Server (requires local SQL Server)
3. **scaffold-entities** - Generates EF Core entities from database (requires SQL Server access)

### hart-server (Linux)
**Location:** `/var/workload/GitHub/actions-runner`  
**Service:** `actions.runner.AHartTN-Hartonomous-Sandbox.hart-server`  
**Service User:** `github-runner` (system account)  
**Labels:** `self-hosted`, `linux`, `hart-server`

**Capabilities:**
- Ubuntu Linux
- .NET 8 SDK / .NET 10 SDK
- Cross-platform .NET builds
- Docker support
- Faster builds (dedicated workload disk)

**Assigned Jobs:**
1. **build-and-test** - Builds and tests .NET 10 solution (cross-platform)
2. **build-applications** - Publishes API and worker applications (cross-platform)

## Job Dependencies & Flow

```
build-dacpac (Windows)
    ↓
deploy-database (Windows)
    ↓
scaffold-entities (Windows)
    ↓
build-and-test (Linux)
    ↓
build-applications (Linux)
```

## Technical Constraints

### Why Windows for Database Jobs?
1. **CLR Assemblies** - Hartonomous.Database.sqlproj contains SQLCLR objects targeting .NET Framework 4.8.1
2. **MSBuild Requirement** - .NET Framework projects require MSBuild from Visual Studio (not cross-platform)
3. **SQL Server Access** - Direct localhost connection to SQL Server for deployment and scaffolding
4. **Azure Arc Integration** - HART-DESKTOP is Arc-enabled for Azure AD authentication

### Why Linux for .NET 10 Jobs?
1. **.NET 10 is Cross-Platform** - Builds work on Linux, macOS, and Windows
2. **Performance** - Linux builds are typically faster
3. **Cost Efficiency** - Dedicated workload disk on `/var/workload`
4. **Resource Isolation** - Separates heavy .NET builds from SQL Server workload

## Workflow Configuration

Jobs specify runner requirements using label arrays:

```yaml
# Windows runner for database operations
runs-on: [self-hosted, windows, sql-server]

# Linux runner for .NET builds
runs-on: [self-hosted, linux]

# Any available self-hosted runner (fallback)
runs-on: self-hosted
```

## Service Management

### Windows (HART-DESKTOP)
```powershell
# Check status
Get-Service -Name "actions.runner.AHartTN-Hartonomous-Sandbox.HART-DESKTOP"

# Restart
Restart-Service -Name "actions.runner.AHartTN-Hartonomous-Sandbox.HART-DESKTOP"

# View logs
Get-Content "D:\GitHub\actions-runner\_diag\Runner_*.log" -Tail 50
```

### Linux (hart-server)
```bash
# Check status
sudo systemctl status actions.runner.AHartTN-Hartonomous-Sandbox.hart-server

# Restart
sudo systemctl restart actions.runner.AHartTN-Hartonomous-Sandbox.hart-server

# View logs
sudo journalctl -u actions.runner.AHartTN-Hartonomous-Sandbox.hart-server -f
```

## Expected Build Behavior

### ✅ Should Succeed
- **build-dacpac** - DACPAC builds with .NET Framework CLR assemblies
- **deploy-database** - DACPAC deployment, CLR assemblies, TRUSTWORTHY setting
- **scaffold-entities** - EF Core entity generation from database schema

### ⚠️ Expected to Fail Initially
- **build-and-test** - May fail due to missing scaffolded entities or test issues
- **build-applications** - May fail if dependencies aren't properly restored

### 🔄 Improvement Path
1. Fix .NET 10 dependency issues
2. Ensure scaffolded entities are properly committed
3. Update test configurations for CI environment
4. Verify all NuGet packages restore correctly on Linux

## Cost Savings

Compared to Azure DevOps Pipelines:
- **Azure DevOps:** $15/month per parallel job = $360/year for 2 agents
- **GitHub Actions Self-Hosted:** $0/year (unlimited parallel jobs)
- **Savings:** $360/year

## Security Notes

- Both runners use service accounts (not personal accounts)
- Windows runner: `NT AUTHORITY\NETWORK SERVICE`
- Linux runner: `github-runner` system user
- Runners are **repository-scoped** (not organization-wide)
- OIDC authentication for Azure deployments (no stored credentials)
