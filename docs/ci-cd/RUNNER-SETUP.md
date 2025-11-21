# ?? Self-Hosted Runner Setup Guide

**Purpose**: Ensure your self-hosted GitHub Actions and Azure DevOps runners meet all prerequisites for Hartonomous CI/CD pipelines.

---

## ?? Overview

Hartonomous uses **self-hosted runners** for CI/CD:
- **HART-DESKTOP** (Windows) - Database builds and deployments
- **hart-server** (Linux) - .NET builds and tests

This guide ensures both runners have all required software installed.

---

## ??? HART-DESKTOP (Windows Runner)

### **Required Software**

| Software | Version | Purpose | Download |
|----------|---------|---------|----------|
| **Windows Server** | 2022+ | Host OS | Pre-installed |
| **.NET SDK** | 10.0+ | Build .NET projects | https://dot.net/download |
| **SQL Server** | 2022+ | Database | https://www.microsoft.com/sql-server |
| **MSBuild** | VS 2022 | Build DACPAC | https://visualstudio.microsoft.com |
| **SqlPackage** | Latest | Deploy DACPAC | Auto-installed |
| **PowerShell** | 7.4+ | Scripting | https://github.com/PowerShell/PowerShell |
| **Git** | 2.40+ | Source control | https://git-scm.com |
| **Azure CLI** | Latest | Azure operations | https://aka.ms/installazurecliwindows |

### **Setup Script**

Save as `scripts/setup/setup-windows-runner.ps1`:

```powershell
#Requires -Version 7.0
<#
.SYNOPSIS
    Setup Windows self-hosted runner for Hartonomous CI/CD
.DESCRIPTION
    Installs and verifies all prerequisites for HART-DESKTOP runner
#>

$ErrorActionPreference = 'Stop'

Write-Host "`n=== Hartonomous Windows Runner Setup ===" -ForegroundColor Cyan
Write-Host "Target: HART-DESKTOP (Windows)" -ForegroundColor Gray
Write-Host ""

$checks = @()

# Check 1: .NET SDK
Write-Host "[1/8] Checking .NET SDK..." -NoNewline
try {
    $dotnetVersion = dotnet --version
    $majorVersion = [int]($dotnetVersion.Split('.')[0])
    
    if ($majorVersion -ge 10) {
        Write-Host " ? $dotnetVersion" -ForegroundColor Green
        $checks += @{ Name = ".NET SDK"; Status = "?"; Version = $dotnetVersion }
    } else {
        Write-Host " ? Version $dotnetVersion is too old (need 10.0+)" -ForegroundColor Red
        Write-Host "      Install: https://dot.net/download/dotnet/10.0" -ForegroundColor Yellow
        $checks += @{ Name = ".NET SDK"; Status = "?"; Version = $dotnetVersion }
    }
} catch {
    Write-Host " ? Not installed" -ForegroundColor Red
    Write-Host "      Install: https://dot.net/download/dotnet/10.0" -ForegroundColor Yellow
    $checks += @{ Name = ".NET SDK"; Status = "?"; Version = "Not found" }
}

# Check 2: SQL Server
Write-Host "[2/8] Checking SQL Server..." -NoNewline
try {
    $sqlVersion = sqlcmd -S localhost -E -Q "SELECT @@VERSION" -h -1 2>&1 | Select-Object -First 1
    if ($LASTEXITCODE -eq 0) {
        Write-Host " ? Installed" -ForegroundColor Green
        $checks += @{ Name = "SQL Server"; Status = "?"; Version = "Installed" }
    } else {
        throw "Connection failed"
    }
} catch {
    Write-Host " ? Not installed or not running" -ForegroundColor Red
    Write-Host "      Install: https://www.microsoft.com/sql-server" -ForegroundColor Yellow
    $checks += @{ Name = "SQL Server"; Status = "?"; Version = "Not found" }
}

# Check 3: MSBuild (Visual Studio)
Write-Host "[3/8] Checking MSBuild..." -NoNewline
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (Test-Path $vswhere) {
    $msbuildPath = & $vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1
    if ($msbuildPath) {
        Write-Host " ? Found" -ForegroundColor Green
        $checks += @{ Name = "MSBuild"; Status = "?"; Version = "VS 2022" }
    } else {
        Write-Host " ? Not found" -ForegroundColor Red
        Write-Host "      Install Visual Studio 2022 with MSBuild component" -ForegroundColor Yellow
        $checks += @{ Name = "MSBuild"; Status = "?"; Version = "Not found" }
    }
} else {
    Write-Host " ? Visual Studio not installed" -ForegroundColor Red
    Write-Host "      Install: https://visualstudio.microsoft.com/downloads/" -ForegroundColor Yellow
    $checks += @{ Name = "MSBuild"; Status = "?"; Version = "Not found" }
}

# Check 4: PowerShell 7
Write-Host "[4/8] Checking PowerShell..." -NoNewline
$psVersion = $PSVersionTable.PSVersion.ToString()
if ($PSVersionTable.PSVersion.Major -ge 7) {
    Write-Host " ? $psVersion" -ForegroundColor Green
    $checks += @{ Name = "PowerShell"; Status = "?"; Version = $psVersion }
} else {
    Write-Host " ? Version $psVersion (need 7.0+)" -ForegroundColor Red
    Write-Host "      Install: https://github.com/PowerShell/PowerShell/releases" -ForegroundColor Yellow
    $checks += @{ Name = "PowerShell"; Status = "?"; Version = $psVersion }
}

# Check 5: Git
Write-Host "[5/8] Checking Git..." -NoNewline
try {
    $gitVersion = git --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host " ? $gitVersion" -ForegroundColor Green
        $checks += @{ Name = "Git"; Status = "?"; Version = $gitVersion }
    } else {
        throw "Not found"
    }
} catch {
    Write-Host " ? Not installed" -ForegroundColor Red
    Write-Host "      Install: https://git-scm.com/downloads" -ForegroundColor Yellow
    $checks += @{ Name = "Git"; Status = "?"; Version = "Not found" }
}

# Check 6: Azure CLI
Write-Host "[6/8] Checking Azure CLI..." -NoNewline
try {
    $azVersion = az --version 2>&1 | Select-String "azure-cli" | Select-Object -First 1
    if ($azVersion) {
        Write-Host " ? Installed" -ForegroundColor Green
        $checks += @{ Name = "Azure CLI"; Status = "?"; Version = "Installed" }
    } else {
        throw "Not found"
    }
} catch {
    Write-Host " ? Not installed" -ForegroundColor Red
    Write-Host "      Install: https://aka.ms/installazurecliwindows" -ForegroundColor Yellow
    $checks += @{ Name = "Azure CLI"; Status = "?"; Version = "Not found" }
}

# Check 7: GitHub CLI
Write-Host "[7/8] Checking GitHub CLI..." -NoNewline
try {
    $ghVersion = gh --version 2>&1 | Select-String "gh version" | Select-Object -First 1
    if ($ghVersion) {
        Write-Host " ? $ghVersion" -ForegroundColor Green
        $checks += @{ Name = "GitHub CLI"; Status = "?"; Version = $ghVersion.ToString() }
    } else {
        throw "Not found"
    }
} catch {
    Write-Host " ?? Optional (recommended for testing)" -ForegroundColor Yellow
    Write-Host "      Install: winget install GitHub.cli" -ForegroundColor Yellow
    $checks += @{ Name = "GitHub CLI"; Status = "??"; Version = "Not found" }
}

# Check 8: GitHub Actions Runner
Write-Host "[8/8] Checking GitHub Actions Runner..." -NoNewline
$runnerService = Get-Service -Name "actions.runner.*" -ErrorAction SilentlyContinue
if ($runnerService) {
    if ($runnerService.Status -eq 'Running') {
        Write-Host " ? Running" -ForegroundColor Green
        $checks += @{ Name = "GitHub Runner"; Status = "?"; Version = "Running" }
    } else {
        Write-Host " ?? Not running" -ForegroundColor Yellow
        Write-Host "      Start with: Start-Service '$($runnerService.Name)'" -ForegroundColor Yellow
        $checks += @{ Name = "GitHub Runner"; Status = "??"; Version = "Stopped" }
    }
} else {
    Write-Host " ? Not installed" -ForegroundColor Red
    Write-Host "      Install: See docs/ci-cd/runner-setup.md" -ForegroundColor Yellow
    $checks += @{ Name = "GitHub Runner"; Status = "?"; Version = "Not found" }
}

# Summary
Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
$checks | Format-Table -Property Name, Status, Version -AutoSize

$failCount = ($checks | Where-Object { $_.Status -eq "?" }).Count
$warnCount = ($checks | Where-Object { $_.Status -eq "??" }).Count

if ($failCount -eq 0 -and $warnCount -eq 0) {
    Write-Host ""
    Write-Host "? ALL CHECKS PASSED - Runner is ready!" -ForegroundColor Green
    exit 0
} elseif ($failCount -eq 0) {
    Write-Host ""
    Write-Host "?? WARNINGS FOUND - Runner will work but some features may be limited" -ForegroundColor Yellow
    exit 0
} else {
    Write-Host ""
    Write-Host "? CRITICAL ISSUES FOUND - Please install missing prerequisites" -ForegroundColor Red
    exit 1
}
```

### **Run the Setup**

```powershell
# On HART-DESKTOP, run:
cd D:\Repositories\Hartonomous
.\scripts\setup\setup-windows-runner.ps1
```

**Expected Output:**
```
=== Hartonomous Windows Runner Setup ===
Target: HART-DESKTOP (Windows)

[1/8] Checking .NET SDK... ? 10.0.1
[2/8] Checking SQL Server... ? Installed
[3/8] Checking MSBuild... ? Found
[4/8] Checking PowerShell... ? 7.4.0
[5/8] Checking Git... ? git version 2.43.0
[6/8] Checking Azure CLI... ? Installed
[7/8] Checking GitHub CLI... ? gh version 2.40.0
[8/8] Checking GitHub Actions Runner... ? Running

=== Summary ===
Name            Status Version
----            ------ -------
.NET SDK        ?      10.0.1
SQL Server      ?      Installed
MSBuild         ?      VS 2022
PowerShell      ?      7.4.0
Git             ?      git version 2.43.0
Azure CLI       ?      Installed
GitHub CLI      ?      gh version 2.40.0
GitHub Runner   ?      Running

? ALL CHECKS PASSED - Runner is ready!
```

---

## ?? hart-server (Linux Runner)

### **Required Software**

| Software | Version | Purpose | Download |
|----------|---------|---------|----------|
| **Ubuntu** | 22.04+ | Host OS | Pre-installed |
| **.NET SDK** | 10.0+ | Build .NET projects | https://dot.net/download |
| **PowerShell** | 7.4+ | Scripting | https://aka.ms/powershell |
| **Git** | 2.40+ | Source control | `apt install git` |
| **Docker** | 24.0+ | Test containers | `apt install docker.io` |

### **Setup Script**

Save as `scripts/setup/setup-linux-runner.sh`:

```bash
#!/bin/bash
set -e

echo ""
echo "=== Hartonomous Linux Runner Setup ==="
echo "Target: hart-server (Linux)"
echo ""

declare -a checks=()
fail_count=0
warn_count=0

# Check 1: .NET SDK
echo -n "[1/5] Checking .NET SDK..."
if command -v dotnet &> /dev/null; then
    dotnet_version=$(dotnet --version)
    major_version=$(echo "$dotnet_version" | cut -d. -f1)
    
    if [ "$major_version" -ge 10 ]; then
        echo " ? $dotnet_version"
        checks+=(".NET SDK|?|$dotnet_version")
    else
        echo " ? Version $dotnet_version is too old (need 10.0+)"
        echo "      Install: wget https://dot.net/v1/dotnet-install.sh && bash dotnet-install.sh --channel 10.0"
        checks+=(".NET SDK|?|$dotnet_version")
        ((fail_count++))
    fi
else
    echo " ? Not installed"
    echo "      Install: wget https://dot.net/v1/dotnet-install.sh && bash dotnet-install.sh --channel 10.0"
    checks+=(".NET SDK|?|Not found")
    ((fail_count++))
fi

# Check 2: PowerShell
echo -n "[2/5] Checking PowerShell..."
if command -v pwsh &> /dev/null; then
    pwsh_version=$(pwsh --version | grep -oP '\d+\.\d+\.\d+')
    echo " ? $pwsh_version"
    checks+=("PowerShell|?|$pwsh_version")
else
    echo " ? Not installed"
    echo "      Install: https://aka.ms/powershell-release?tag=stable"
    checks+=("PowerShell|?|Not found")
    ((fail_count++))
fi

# Check 3: Git
echo -n "[3/5] Checking Git..."
if command -v git &> /dev/null; then
    git_version=$(git --version | cut -d' ' -f3)
    echo " ? $git_version"
    checks+=("Git|?|$git_version")
else
    echo " ? Not installed"
    echo "      Install: sudo apt install git"
    checks+=("Git|?|Not found")
    ((fail_count++))
fi

# Check 4: Docker
echo -n "[4/5] Checking Docker..."
if command -v docker &> /dev/null; then
    docker_version=$(docker --version | cut -d' ' -f3 | tr -d ',')
    echo " ? $docker_version"
    checks+=("Docker|?|$docker_version")
else
    echo " ?? Not installed (optional, for test containers)"
    echo "      Install: sudo apt install docker.io"
    checks+=("Docker|??|Not found")
    ((warn_count++))
fi

# Check 5: GitHub Actions Runner
echo -n "[5/5] Checking GitHub Actions Runner..."
if systemctl is-active --quiet actions.runner.* 2>/dev/null; then
    echo " ? Running"
    checks+=("GitHub Runner|?|Running")
else
    echo " ? Not running or not installed"
    echo "      Install: See docs/ci-cd/runner-setup.md"
    checks+=("GitHub Runner|?|Not found")
    ((fail_count++))
fi

# Summary
echo ""
echo "=== Summary ==="
printf "%-20s %-10s %-20s\n" "Name" "Status" "Version"
printf "%-20s %-10s %-20s\n" "----" "------" "-------"
for check in "${checks[@]}"; do
    IFS='|' read -r name status version <<< "$check"
    printf "%-20s %-10s %-20s\n" "$name" "$status" "$version"
done

echo ""
if [ $fail_count -eq 0 ] && [ $warn_count -eq 0 ]; then
    echo "? ALL CHECKS PASSED - Runner is ready!"
    exit 0
elif [ $fail_count -eq 0 ]; then
    echo "?? WARNINGS FOUND - Runner will work but some features may be limited"
    exit 0
else
    echo "? CRITICAL ISSUES FOUND - Please install missing prerequisites"
    exit 1
fi
```

### **Run the Setup**

```bash
# On hart-server, run:
cd /var/workload/Hartonomous  # Or wherever you cloned the repo
chmod +x scripts/setup/setup-linux-runner.sh
./scripts/setup/setup-linux-runner.sh
```

**Expected Output:**
```
=== Hartonomous Linux Runner Setup ===
Target: hart-server (Linux)

[1/5] Checking .NET SDK... ? 10.0.1
[2/5] Checking PowerShell... ? 7.4.0
[3/5] Checking Git... ? 2.43.0
[4/5] Checking Docker... ? 24.0.7
[5/5] Checking GitHub Actions Runner... ? Running

=== Summary ===
Name                 Status     Version
----                 ------     -------
.NET SDK             ?          10.0.1
PowerShell           ?          7.4.0
Git                  ?          2.43.0
Docker               ?          24.0.7
GitHub Runner        ?          Running

? ALL CHECKS PASSED - Runner is ready!
```

---

## ?? Installation Commands

### **Windows (HART-DESKTOP)**

```powershell
# Install .NET 10 SDK
winget install Microsoft.DotNet.SDK.10

# Install PowerShell 7
winget install Microsoft.PowerShell

# Install Git
winget install Git.Git

# Install Azure CLI
winget install Microsoft.AzureCLI

# Install GitHub CLI (optional)
winget install GitHub.cli

# Install Visual Studio 2022 (for MSBuild)
winget install Microsoft.VisualStudio.2022.Community
# Or just Build Tools:
winget install Microsoft.VisualStudio.2022.BuildTools
```

### **Linux (hart-server)**

```bash
# Install .NET 10 SDK
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 10.0
export PATH="$HOME/.dotnet:$PATH"
echo 'export PATH="$HOME/.dotnet:$PATH"' >> ~/.bashrc

# Install PowerShell 7
wget https://github.com/PowerShell/PowerShell/releases/download/v7.4.0/powershell_7.4.0-1.deb_amd64.deb
sudo dpkg -i powershell_7.4.0-1.deb_amd64.deb
sudo apt-get install -f

# Install Git
sudo apt update
sudo apt install git

# Install Docker (for test containers)
sudo apt install docker.io
sudo usermod -aG docker $USER
```

---

## ? Verification

After running setup scripts on both runners, verify pipelines work:

```powershell
# Trigger GitHub Actions workflow
gh workflow run "CI/CD Pipeline" --ref main -f environment=development

# Watch it run
gh run watch
```

**Expected**: All 5 stages pass ?

---

## ?? Related Documentation

- **[GitHub Actions Runner Setup](https://docs.github.com/en/actions/hosting-your-own-runners)**
- **[Azure DevOps Agent Setup](https://learn.microsoft.com/en-us/azure/devops/pipelines/agents/linux-agent)**
- **[.NET Installation Guide](https://learn.microsoft.com/en-us/dotnet/core/install/linux)**

---

**Last Updated**: 2025-11-21  
**Status**: ? Production Ready
