<#
.SYNOPSIS
    Deploys CLR assemblies to SQL Server in strict dependency order.

.DESCRIPTION
    This script automates the deployment of Hartonomous CLR assemblies including:
    - 16 external dependency assemblies (System.*, MathNet.Numerics, Newtonsoft.Json, etc.)
    - 1 main Hartonomous.Clr assembly
    - 138 CLR function/procedure wrappers
    
    Assemblies are deployed in dependency-aware tiers to satisfy SQL Server's
    reference validation requirements.

.PARAMETER Server
    SQL Server instance name (e.g., "localhost" or "server\instance")

.PARAMETER Database
    Target database name (default: "Hartonomous")

.PARAMETER DependenciesPath
    Path to external dependency DLLs (default: "dependencies")

.PARAMETER MainAssemblyPath
    Path to Hartonomous.Clr.dll (default: "src\Hartonomous.Database\bin\Release\Hartonomous.Clr.dll")

.PARAMETER DropExisting
    If specified, drops existing assemblies before deployment

.PARAMETER SkipFunctionWrappers
    If specified, skips deployment of SQL function wrappers

.EXAMPLE
    .\deploy-clr-assemblies.ps1 -Server "localhost" -Database "Hartonomous"

.EXAMPLE
    .\deploy-clr-assemblies.ps1 -Server "localhost" -DropExisting -Verbose

.NOTES
    Prerequisites:
    - SQL Server 2022+ with CLR Integration enabled
    - CLR certificate deployed (run Deploy-CLRCertificate.ps1 first)
    - All assemblies must be strong-name signed
    - SqlServer PowerShell module installed
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Server,
    
    [Parameter(Mandatory = $false)]
    [string]$Database = "Hartonomous",
    
    [Parameter(Mandatory = $false)]
    [string]$DependenciesPath = "dependencies",
    
    [Parameter(Mandatory = $false)]
    [string]$MainAssemblyPath = "src\Hartonomous.Database\bin\Release\net481\Hartonomous.Clr.dll",
    
    [Parameter(Mandatory = $false)]
    [switch]$DropExisting,
    
    [Parameter(Mandatory = $false)]
    [switch]$SkipFunctionWrappers
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Import required modules
Import-Module SqlServer -ErrorAction Stop

# Define assembly deployment tiers (dependency order)
$AssemblyTiers = @{
    Tier1 = @(
        @{ Name = "System.Runtime.CompilerServices.Unsafe"; PermissionSet = "SAFE" }
        @{ Name = "System.Buffers"; PermissionSet = "SAFE" }
    )
    Tier2 = @(
        @{ Name = "System.Numerics.Vectors"; PermissionSet = "SAFE" }
    )
    Tier3 = @(
        @{ Name = "MathNet.Numerics"; PermissionSet = "UNSAFE" }
        @{ Name = "System.Memory"; PermissionSet = "SAFE" }
    )
    Tier4 = @(
        @{ Name = "Newtonsoft.Json"; PermissionSet = "UNSAFE" }
        @{ Name = "System.Collections.Immutable"; PermissionSet = "SAFE" }
    )
    Tier5 = @(
        @{ Name = "System.Runtime.Intrinsics"; PermissionSet = "SAFE" }
        @{ Name = "System.Threading.Tasks.Extensions"; PermissionSet = "SAFE" }
    )
}

function Write-Log {
    param(
        [string]$Message,
        [ValidateSet("Info", "Success", "Warning", "Error")]
        [string]$Level = "Info"
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $color = switch ($Level) {
        "Success" { "Green" }
        "Warning" { "Yellow" }
        "Error" { "Red" }
        default { "White" }
    }
    
    Write-Host "[$timestamp] [$Level] $Message" -ForegroundColor $color
}

function Test-AssemblyExists {
    param(
        [string]$Server,
        [string]$Database,
        [string]$AssemblyName
    )
    
    $query = @"
SELECT COUNT(*) AS AssemblyCount
FROM [$Database].sys.assemblies
WHERE name = '$AssemblyName' AND is_user_defined = 1;
"@
    
    try {
        $result = Invoke-Sqlcmd -ServerInstance $Server -Database $Database -Query $query -ErrorAction Stop
        return ($result.AssemblyCount -gt 0)
    }
    catch {
        Write-Log "Failed to check assembly existence: $_" -Level Error
        throw
    }
}

function Remove-ClrAssembly {
    param(
        [string]$Server,
        [string]$Database,
        [string]$AssemblyName
    )
    
    Write-Log "Dropping assembly [$AssemblyName]..." -Level Warning
    
    $query = @"
USE [$Database];
DROP ASSEMBLY IF EXISTS [$AssemblyName];
"@
    
    try {
        Invoke-Sqlcmd -ServerInstance $Server -Database $Database -Query $query -ErrorAction Stop
        Write-Log "Successfully dropped assembly [$AssemblyName]" -Level Success
    }
    catch {
        Write-Log "Failed to drop assembly [$AssemblyName]: $_" -Level Error
        throw
    }
}

function Deploy-ClrAssembly {
    param(
        [string]$Server,
        [string]$Database,
        [string]$AssemblyName,
        [string]$AssemblyPath,
        [string]$PermissionSet
    )
    
    if (-not (Test-Path $AssemblyPath)) {
        Write-Log "Assembly file not found: $AssemblyPath" -Level Error
        throw "Assembly file not found: $AssemblyPath"
    }
    
    # Check if assembly already exists
    if (Test-AssemblyExists -Server $Server -Database $Database -AssemblyName $AssemblyName) {
        if ($DropExisting) {
            Remove-ClrAssembly -Server $Server -Database $Database -AssemblyName $AssemblyName
        }
        else {
            Write-Log "Assembly [$AssemblyName] already exists. Skipping..." -Level Warning
            return
        }
    }
    
    Write-Log "Deploying assembly [$AssemblyName] with permission set [$PermissionSet]..."
    
    # Read assembly bytes
    $assemblyBytes = [System.IO.File]::ReadAllBytes($AssemblyPath)
    $assemblyHex = "0x" + [System.BitConverter]::ToString($assemblyBytes).Replace("-", "")
    
    $query = @"
USE [$Database];
CREATE ASSEMBLY [$AssemblyName]
FROM $assemblyHex
WITH PERMISSION_SET = $PermissionSet;
"@
    
    try {
        Invoke-Sqlcmd -ServerInstance $Server -Database $Database -Query $query -QueryTimeout 300 -ErrorAction Stop
        Write-Log "Successfully deployed assembly [$AssemblyName]" -Level Success
    }
    catch {
        Write-Log "Failed to deploy assembly [$AssemblyName]: $_" -Level Error
        throw
    }
}

function Deploy-FunctionWrappers {
    param(
        [string]$Server,
        [string]$Database
    )
    
    Write-Log "Deploying CLR function wrappers..."
    
    $wrapperPaths = @(
        "src\Hartonomous.Database\Functions\*.sql",
        "src\Hartonomous.Database\Aggregates\*.sql",
        "src\Hartonomous.Database\Procedures\*.sql"
    )
    
    $deployedCount = 0
    
    foreach ($pattern in $wrapperPaths) {
        $files = Get-ChildItem -Path $pattern -ErrorAction SilentlyContinue
        
        foreach ($file in $files) {
            # Only deploy CLR-related files
            if ($file.Name -match "^clr_" -or $file.Name -match "^sp_.*CLR") {
                Write-Log "Deploying wrapper: $($file.Name)"
                
                try {
                    Invoke-Sqlcmd -ServerInstance $Server -Database $Database -InputFile $file.FullName -ErrorAction Stop
                    $deployedCount++
                }
                catch {
                    Write-Log "Failed to deploy wrapper $($file.Name): $_" -Level Warning
                }
            }
        }
    }
    
    Write-Log "Deployed $deployedCount CLR function wrappers" -Level Success
}

function Verify-Deployment {
    param(
        [string]$Server,
        [string]$Database
    )
    
    Write-Log "Verifying deployment..."
    
    # Check assembly count
    $assemblyQuery = @"
SELECT COUNT(*) AS AssemblyCount
FROM [$Database].sys.assemblies
WHERE is_user_defined = 1;
"@
    
    $assemblyResult = Invoke-Sqlcmd -ServerInstance $Server -Database $Database -Query $assemblyQuery
    Write-Log "Total user-defined assemblies: $($assemblyResult.AssemblyCount)" -Level Info
    
    # Check CLR function count
    $functionQuery = @"
SELECT 
    type_desc,
    COUNT(*) AS FunctionCount
FROM [$Database].sys.objects
WHERE type IN ('FT', 'FS', 'AF', 'PC')  -- CLR scalar, TVF, aggregate, procedure
GROUP BY type_desc;
"@
    
    $functionResult = Invoke-Sqlcmd -ServerInstance $Server -Database $Database -Query $functionQuery
    
    foreach ($row in $functionResult) {
        Write-Log "$($row.type_desc): $($row.FunctionCount)" -Level Info
    }
    
    # List all deployed assemblies
    $detailQuery = @"
SELECT 
    name,
    permission_set_desc,
    create_date
FROM [$Database].sys.assemblies
WHERE is_user_defined = 1
ORDER BY create_date;
"@
    
    $details = Invoke-Sqlcmd -ServerInstance $Server -Database $Database -Query $detailQuery
    
    Write-Log "`nDeployed Assemblies:" -Level Info
    foreach ($assembly in $details) {
        Write-Log "  - $($assembly.name) [$($assembly.permission_set_desc)] (Created: $($assembly.create_date))" -Level Info
    }
}

# Main execution
try {
    Write-Log "Starting CLR assembly deployment to $Server.$Database" -Level Info
    Write-Log "Dependencies path: $DependenciesPath" -Level Info
    Write-Log "Main assembly path: $MainAssemblyPath" -Level Info
    
    # Resolve paths relative to script location
    $scriptRoot = Split-Path -Parent $PSCommandPath
    $repoRoot = Split-Path -Parent $scriptRoot
    
    $dependenciesFullPath = Join-Path $repoRoot $DependenciesPath
    $mainAssemblyFullPath = Join-Path $repoRoot $MainAssemblyPath
    
    # Verify paths exist
    if (-not (Test-Path $dependenciesFullPath)) {
        throw "Dependencies path not found: $dependenciesFullPath"
    }
    
    if (-not (Test-Path $mainAssemblyFullPath)) {
        throw "Main assembly not found: $mainAssemblyFullPath. Please run Build-WithSigning.ps1 first."
    }
    
    # Test SQL Server connectivity
    Write-Log "Testing SQL Server connectivity..."
    try {
        Invoke-Sqlcmd -ServerInstance $Server -Database "master" -Query "SELECT @@VERSION" -ErrorAction Stop | Out-Null
        Write-Log "SQL Server connectivity confirmed" -Level Success
    }
    catch {
        throw "Failed to connect to SQL Server: $_"
    }
    
    # Deploy external dependencies in tier order
    Write-Log "`n=== Phase 1: Deploying External Dependencies ===" -Level Info
    
    foreach ($tierName in ($AssemblyTiers.Keys | Sort-Object)) {
        Write-Log "`nDeploying $tierName..." -Level Info
        
        foreach ($assembly in $AssemblyTiers[$tierName]) {
            $assemblyPath = Join-Path $dependenciesFullPath "$($assembly.Name).dll"
            
            Deploy-ClrAssembly `
                -Server $Server `
                -Database $Database `
                -AssemblyName $assembly.Name `
                -AssemblyPath $assemblyPath `
                -PermissionSet $assembly.PermissionSet
        }
    }
    
    # Deploy main assembly
    Write-Log "`n=== Phase 2: Deploying Main Assembly ===" -Level Info
    
    Deploy-ClrAssembly `
        -Server $Server `
        -Database $Database `
        -AssemblyName "Hartonomous.Clr" `
        -AssemblyPath $mainAssemblyFullPath `
        -PermissionSet "UNSAFE"
    
    # Deploy function wrappers
    if (-not $SkipFunctionWrappers) {
        Write-Log "`n=== Phase 3: Deploying Function Wrappers ===" -Level Info
        Deploy-FunctionWrappers -Server $Server -Database $Database
    }
    
    # Verify deployment
    Write-Log "`n=== Phase 4: Verification ===" -Level Info
    Verify-Deployment -Server $Server -Database $Database
    
    Write-Log "`n=== Deployment Complete ===" -Level Success
    Write-Log "CLR assemblies successfully deployed to $Server.$Database" -Level Success
}
catch {
    Write-Log "`n=== Deployment Failed ===" -Level Error
    Write-Log "Error: $_" -Level Error
    Write-Log "Stack Trace: $($_.ScriptStackTrace)" -Level Error
    exit 1
}
