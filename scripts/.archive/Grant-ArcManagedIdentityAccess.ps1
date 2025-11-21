<#
.SYNOPSIS
    Grants NETWORK SERVICE read access to Azure Arc Managed Identity tokens (idempotent).

.DESCRIPTION
    This script ensures that the NT AUTHORITY\NETWORK SERVICE account has read permissions
    to the Azure Arc Connected Machine Agent's token directory. This is required for
    applications and services running as NETWORK SERVICE to authenticate using the 
    machine's System-Assigned Managed Identity.
    
    The script is idempotent - safe to run multiple times without adverse effects.

.PARAMETER TokenPath
    The path to the Azure Arc token directory. Defaults to the standard location.

.PARAMETER ServiceAccount
    The Windows service account to grant permissions to. Defaults to "NETWORK SERVICE".

.EXAMPLE
    .\Grant-ArcManagedIdentityAccess.ps1
    
.EXAMPLE
    .\Grant-ArcManagedIdentityAccess.ps1 -ServiceAccount "NT AUTHORITY\LOCAL SERVICE"

.NOTES
    - This script must be run with Administrator privileges
    - The Azure Connected Machine Agent must be installed and running
    - The machine must be Arc-enabled (connected to Azure)
    
    Author: Hartonomous Deployment Team
    Version: 1.0
    Date: 2025-11-20
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$TokenPath = "C:\ProgramData\AzureConnectedMachineAgent\Tokens",
    
    [Parameter(Mandatory = $false)]
    [string]$ServiceAccount = "NT AUTHORITY\NETWORK SERVICE"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

function Test-IsAdministrator {
    $currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    return $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Test-ArcAgentInstalled {
    $service = Get-Service -Name "himds" -ErrorAction SilentlyContinue
    return $null -ne $service
}

function Test-ArcAgentRunning {
    $service = Get-Service -Name "himds" -ErrorAction SilentlyContinue
    return ($null -ne $service) -and ($service.Status -eq "Running")
}

# ===== Validation =====

Write-ColorOutput "`n🔐 Azure Arc Managed Identity Access Configuration" -Color Cyan
Write-ColorOutput "=" * 60 -Color Cyan

# Check Administrator privileges
if (-not (Test-IsAdministrator)) {
    Write-ColorOutput "❌ ERROR: This script must be run as Administrator" -Color Red
    Write-ColorOutput "   Please re-run this script from an elevated PowerShell prompt." -Color Yellow
    exit 1
}

Write-ColorOutput "✅ Running with Administrator privileges" -Color Green

# Check if Azure Arc agent is installed
if (-not (Test-ArcAgentInstalled)) {
    Write-ColorOutput "❌ ERROR: Azure Connected Machine Agent (himds) is not installed" -Color Red
    Write-ColorOutput "   This machine must be Arc-enabled before running this script." -Color Yellow
    Write-ColorOutput "   Install guide: https://learn.microsoft.com/en-us/azure/azure-arc/servers/onboard-portal" -Color Yellow
    exit 1
}

Write-ColorOutput "✅ Azure Arc agent is installed" -Color Green

# Check if Azure Arc agent is running
if (-not (Test-ArcAgentRunning)) {
    Write-ColorOutput "⚠️  WARNING: Azure Arc agent (himds) is not running" -Color Yellow
    Write-ColorOutput "   Attempting to start the service..." -Color Yellow
    
    try {
        Start-Service -Name "himds" -ErrorAction Stop
        Start-Sleep -Seconds 2
        
        if (Test-ArcAgentRunning) {
            Write-ColorOutput "✅ Azure Arc agent started successfully" -Color Green
        } else {
            Write-ColorOutput "❌ ERROR: Failed to start Azure Arc agent" -Color Red
            exit 1
        }
    } catch {
        Write-ColorOutput "❌ ERROR: Failed to start Azure Arc agent: $_" -Color Red
        exit 1
    }
} else {
    Write-ColorOutput "✅ Azure Arc agent is running" -Color Green
}

# Check if token directory exists
if (-not (Test-Path $TokenPath)) {
    Write-ColorOutput "⚠️  WARNING: Token directory does not exist: $TokenPath" -Color Yellow
    Write-ColorOutput "   This may indicate the Arc agent hasn't fully initialized." -Color Yellow
    Write-ColorOutput "   Creating directory..." -Color Yellow
    
    try {
        New-Item -Path $TokenPath -ItemType Directory -Force | Out-Null
        Write-ColorOutput "✅ Token directory created" -Color Green
    } catch {
        Write-ColorOutput "❌ ERROR: Failed to create token directory: $_" -Color Red
        exit 1
    }
}

Write-ColorOutput "✅ Token directory exists: $TokenPath" -Color Green

# ===== Grant Permissions (Idempotent) =====

Write-ColorOutput "`n🔑 Granting permissions to: $ServiceAccount" -Color Cyan

try {
    # Grant Read access with inheritance:
    # (OI) - Object Inherit: Apply to files created in sub-directories
    # (CI) - Container Inherit: Apply to sub-directories created in this directory
    # R - Read Access
    $icaclsArgs = @(
        $TokenPath,
        "/grant",
        "${ServiceAccount}:(OI)(CI)R"
    )
    
    Write-ColorOutput "   Executing: icacls $($icaclsArgs -join ' ')" -Color Gray
    
    $result = & icacls @icaclsArgs 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-ColorOutput "✅ Permissions granted successfully (idempotent)" -Color Green
        Write-ColorOutput "   Account: $ServiceAccount" -Color Gray
        Write-ColorOutput "   Path: $TokenPath" -Color Gray
        Write-ColorOutput "   Access: Read (OI)(CI)" -Color Gray
    } else {
        Write-ColorOutput "❌ ERROR: icacls failed with exit code $LASTEXITCODE" -Color Red
        Write-ColorOutput "   Output: $result" -Color Yellow
        exit 1
    }
} catch {
    Write-ColorOutput "❌ ERROR: Failed to grant permissions: $_" -Color Red
    exit 1
}

# ===== Verify Permissions =====

Write-ColorOutput "`n🔍 Verifying permissions..." -Color Cyan

try {
    $aclOutput = icacls $TokenPath 2>&1
    
    if ($aclOutput -match [regex]::Escape($ServiceAccount)) {
        Write-ColorOutput "✅ Permissions verified - $ServiceAccount has access" -Color Green
    } else {
        Write-ColorOutput "⚠️  WARNING: Could not verify permissions in icacls output" -Color Yellow
        Write-ColorOutput "   This may be a false negative - permissions might be inherited." -Color Gray
    }
    
    # Show current ACL (for logging/debugging)
    Write-ColorOutput "`n📋 Current ACL for $TokenPath" -Color Cyan
    Write-ColorOutput "-" * 60 -Color Gray
    $aclOutput | ForEach-Object { Write-Host $_ -ForegroundColor Gray }
} catch {
    Write-ColorOutput "⚠️  WARNING: Could not verify permissions: $_" -Color Yellow
    Write-ColorOutput "   Permissions were likely applied successfully." -Color Gray
}

# ===== Summary =====

Write-ColorOutput "`n" + ("=" * 60) -Color Cyan
Write-ColorOutput "✅ Configuration complete!" -Color Green
Write-ColorOutput "`nApplications and services running as $ServiceAccount" -Color White
Write-ColorOutput "can now authenticate using this machine's Arc Managed Identity." -Color White
Write-ColorOutput "`n💡 This script is idempotent - safe to run repeatedly." -Color Cyan
Write-ColorOutput ("=" * 60) -Color Cyan

exit 0
