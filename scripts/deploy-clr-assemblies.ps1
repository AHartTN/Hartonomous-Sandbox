#Requires -Version 7.0
<#
.SYNOPSIS
    Deploys external CLR assembly dependencies to SQL Server in correct tier order.

.DESCRIPTION
    Deploys the 16 external CLR DLL dependencies required by Hartonomous.Clr
    in 6 tiers to respect dependency order. Based on official Microsoft Docs
    guidance for SQL CLR assembly deployment.
    
    IMPORTANT: This script does NOT deploy Hartonomous.Clr.dll itself!
               That assembly is embedded in the DACPAC as hex binary and is
               deployed automatically by SqlPackage when publishing the DACPAC.
               
    This script only deploys the external dependency DLLs that have
    <Private>False</Private> in the .sqlproj file (compile-time references).

.PARAMETER Server
    SQL Server instance name (e.g., localhost or server.database.windows.net)

.PARAMETER Database
    Target database name

.PARAMETER UseAzureAD
    Use Azure AD authentication instead of SQL authentication

.PARAMETER AccessToken
    Azure AD access token (required if UseAzureAD is true)

.PARAMETER Username
    SQL Server username for SQL authentication (ignored if UseAzureAD)

.PARAMETER Password
    SQL Server password for SQL authentication (ignored if UseAzureAD)

.PARAMETER DependenciesPath
    Path to directory containing the 16 external DLL files

.EXAMPLE
    # Azure AD authentication
    .\deploy-clr-assemblies.ps1 -Server yourserver.database.windows.net -Database Hartonomous -UseAzureAD -AccessToken $token -DependenciesPath ".\dependencies"
    
.EXAMPLE
    # SQL authentication (legacy)
    .\deploy-clr-assemblies.ps1 -Server localhost -Database Hartonomous -Username sa -Password "password" -DependenciesPath ".\dependencies"
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$Server,

    [Parameter()]
    [string]$Database,

    [Parameter()]
    [switch]$UseAzureAD,

    [Parameter()]
    [string]$AccessToken,

    [Parameter()]
    [string]$Username,

    [Parameter()]
    [string]$Password,

    [Parameter()]
    [string]$DependenciesPath
)

$ErrorActionPreference = 'Stop'

# Load local dev config
$scriptRoot = $PSScriptRoot
$localConfig = & (Join-Path $scriptRoot "local-dev-config.ps1")

# Use config defaults if not specified
if (-not $Server) { $Server = $localConfig.SqlServer }
if (-not $Database) { $Database = $localConfig.Database }
if (-not $DependenciesPath) { 
    $DependenciesPath = Join-Path (Split-Path $scriptRoot -Parent) $localConfig.ClrDependenciesPath 
}

# Validate authentication parameters
if ($UseAzureAD) {
    if ([string]::IsNullOrEmpty($AccessToken)) {
        throw "AccessToken is required when UseAzureAD is specified"
    }
    Write-Host "Using Azure AD authentication"
} elseif (-not [string]::IsNullOrEmpty($Username) -and -not [string]::IsNullOrEmpty($Password)) {
    Write-Host "Using SQL authentication"
} else {
    Write-Host "Using Windows integrated authentication"
}

# Tier structure based on dependency order
# IMPORTANT: Hartonomous.Clr.dll is NOT deployed by this script!
#            It's embedded in the DACPAC and deployed by SqlPackage.
#            This script only deploys the 16 external dependency DLLs.
$tiers = @(
    @{
        Name     = "Tier 1: System Core Dependencies"
        Assemblies = @(
            'System.Numerics.Vectors.dll'
            # System.ValueTuple.dll - REMOVED: Conflicts with SQL Server policy (Msg 6586)
        )
    },
    @{
        Name     = "Tier 2: System Runtime Extensions"
        Assemblies = @(
            'System.Memory.dll',
            'System.Buffers.dll',
            'System.Runtime.CompilerServices.Unsafe.dll'
        )
    },
    @{
        Name     = "Tier 3: System Collections and Reflection"
        Assemblies = @(
            'System.Collections.Immutable.dll',
            'System.Reflection.Metadata.dll'
        )
    },
    @{
        Name     = "Tier 4: System Services"
        Assemblies = @(
            'System.Runtime.Serialization.dll',
            'System.ServiceModel.Internals.dll',
            'SMDiagnostics.dll'
        )
    },
    @{
        Name     = "Tier 5: Third-party and SQL Server"
        Assemblies = @(
            'MathNet.Numerics.dll',
            'Newtonsoft.Json.dll',
            'Microsoft.SqlServer.Types.dll'
        )
    },
    @{
        Name     = "Tier 6: Application Support Libraries"
        Assemblies = @(
            'System.Drawing.dll'
            # SqlClrFunctions.dll - REMOVED: Deployed by DACPAC, has dependency issues
            # Hartonomous.Database.dll - REMOVED: Deployed by DACPAC as Hartonomous.Clr
        )
    }
)

function ConvertTo-HexString {
    param([string]$FilePath)

    $bytes = [System.IO.File]::ReadAllBytes($FilePath)
    return ($bytes | ForEach-Object { $_.ToString("X2") }) -join ''
}

function Deploy-Assembly {
    param(
        [string]$AssemblyName,
        [string]$FilePath,
        [switch]$Critical
    )

    $tempSqlFile = $null
    
    Write-Host "`n[$AssemblyName]" -ForegroundColor White

    if (-not (Test-Path $FilePath)) {
        $msg = "File not found: $FilePath"
        Write-Warning "  ✗ $msg"
        if ($Critical) {
            throw $msg
        }
        return $false
    }

    # Check if already registered
    $checkSql = "SELECT clr_name FROM sys.assemblies WHERE name = '$AssemblyName'"
    
    if ($UseAzureAD) {
        $existing = Invoke-Sqlcmd -ServerInstance $Server -Database $Database -Query $checkSql -AccessToken $AccessToken -TrustServerCertificate -ErrorAction SilentlyContinue | Select-Object -ExpandProperty clr_name -ErrorAction SilentlyContinue
    } elseif ($Username -and $Password) {
        $existing = sqlcmd -S $Server -U $Username -P $Password -d $Database -Q $checkSql -h -1 2>&1 | Select-Object -First 1
    } else {
        $existing = sqlcmd -S $Server -d $Database -E -C -Q $checkSql -h -1 2>&1 | Select-Object -First 1
    }

    $fileHash = (Get-FileHash -Path $FilePath -Algorithm SHA256).Hash
    Write-Host "  File: $FilePath"
    Write-Host "  Hash: $fileHash"
    
    if ($existing -and $existing -notmatch "^\s*$") {
        Write-Host "  Existing: $($existing.Trim())"
        Write-Host "  Updating..." -ForegroundColor Yellow
    } else {
        Write-Host "  Creating new..." -ForegroundColor Green
    }

    $hexString = ConvertTo-HexString -FilePath $FilePath

    # Idempotent deployment: Use ALTER ASSEMBLY for updates, CREATE for new
    $deploySql = @"
IF EXISTS (SELECT * FROM sys.assemblies WHERE name = '$AssemblyName')
BEGIN
    ALTER ASSEMBLY [$AssemblyName]
    FROM 0x$hexString
    WITH PERMISSION_SET = UNSAFE;
END
ELSE
BEGIN
    CREATE ASSEMBLY [$AssemblyName]
    FROM 0x$hexString
    WITH PERMISSION_SET = UNSAFE;
END
"@

    try {
        if ($UseAzureAD) {
            # Use Invoke-Sqlcmd with AccessToken for Azure AD auth (works for on-premises Arc SQL)
            Invoke-Sqlcmd -ServerInstance $Server -Database $Database -Query $deploySql -AccessToken $AccessToken -TrustServerCertificate -ErrorAction Stop
            $output = @()
        } elseif ($Username -and $Password) {
            # Write to temp file for SQL auth to avoid command-line length limits
            $tempSqlFile = [System.IO.Path]::GetTempFileName() + ".sql"
            "USE [$Database];`n$deploySql" | Out-File -FilePath $tempSqlFile -Encoding utf8
            $output = sqlcmd -S $Server -U $Username -P $Password -d $Database -i $tempSqlFile 2>&1
            Remove-Item $tempSqlFile -ErrorAction SilentlyContinue
        } else {
            # Write to temp file for Windows auth to avoid command-line length limits  
            $tempSqlFile = [System.IO.Path]::GetTempFileName() + ".sql"
            "USE [$Database];`n$deploySql" | Out-File -FilePath $tempSqlFile -Encoding utf8
            $output = sqlcmd -S $Server -d $Database -E -C -i $tempSqlFile 2>&1
            Remove-Item $tempSqlFile -ErrorAction SilentlyContinue
        }

        # Check for Level 16+ errors (not warnings)
        $errors = $output | Where-Object { $_ -match "^Msg \d+, Level (1[6-9]|2[0-5])" }

        # Verify registration
        $verifyCount = if ($UseAzureAD) {
            Invoke-Sqlcmd -ServerInstance $Server -Database $Database -Query "SELECT COUNT(*) as cnt FROM sys.assemblies WHERE name = '$AssemblyName'" -AccessToken $AccessToken -TrustServerCertificate | Select-Object -ExpandProperty cnt
        } elseif ($Username -and $Password) {
            sqlcmd -S $Server -U $Username -P $Password -d $Database -Q "SELECT COUNT(*) FROM sys.assemblies WHERE name = '$AssemblyName'" -h -1 2>&1 | Select-Object -First 1
        } else {
            sqlcmd -S $Server -d $Database -E -C -Q "SELECT COUNT(*) FROM sys.assemblies WHERE name = '$AssemblyName'" -h -1 2>&1 | Select-Object -First 1
        }

        if ($verifyCount -match "^\s*1\s*$") {
            Write-Host "  ✓ Successfully deployed and verified" -ForegroundColor Green
            return $true
        } else {
            $msg = "Assembly not found after deployment"
            if ($errors) {
                $errorMsg = ($errors | Select-Object -First 3) -join "`n    "
                $msg += ": $errorMsg"
            }
            Write-Warning "  ✗ $msg"
            if ($Critical) {
                throw $msg
            }
            return $false
        }
    }
    finally {
        if ($tempSqlFile) {
            Remove-Item $tempSqlFile -ErrorAction SilentlyContinue
        }
    }
}

# Main deployment logic
Write-Host "========================================"
Write-Host "  External CLR Assembly Deployment"
Write-Host "========================================"
Write-Host "Server: $Server"
Write-Host "Database: $Database"
Write-Host "Dependencies Path: $DependenciesPath"
Write-Host ""

if (-not (Test-Path $DependenciesPath)) {
    Write-Error "Dependencies path not found: $DependenciesPath"
    exit 1
}

$script:successes = 0
$script:warnings = @()
$script:criticalFailures = @()

foreach ($tier in $tiers) {
    Write-Host "`n$($tier.Name)" -ForegroundColor Cyan

    foreach ($dllName in $tier.Assemblies) {
        $assemblyName = [System.IO.Path]::GetFileNameWithoutExtension($dllName)
        $filePath = Join-Path $DependenciesPath $dllName
        
        # Mark critical assemblies - external dependencies required for CLR functions
        $isCritical = $assemblyName -in @(
            'System.Numerics.Vectors',  # Required for vector operations
            'System.Drawing',            # Required for image processing
            'System.Runtime.Serialization',  # Required by MathNet.Numerics
            'MathNet.Numerics'          # Required for ML/math operations
        )

        try {
            $result = Deploy-Assembly -AssemblyName $assemblyName -FilePath $filePath -Critical:$isCritical
            if ($result) {
                $script:successes++
            } else {
                $script:warnings += $assemblyName
            }
        } catch {
            $script:criticalFailures += "$assemblyName - $($_.Exception.Message)"
        }
    }
}

Write-Host "`n========================================"
Write-Host "Deployment Summary" -ForegroundColor Cyan
Write-Host "========================================"
Write-Host "✓ Successful: $($script:successes)" -ForegroundColor Green
Write-Host "⚠ Warnings: $($script:warnings.Count)" -ForegroundColor Yellow
Write-Host "✗ Critical Failures: $($script:criticalFailures.Count)" -ForegroundColor Red

if ($script:warnings.Count -gt 0) {
    Write-Host "`nWarnings (non-critical):" -ForegroundColor Yellow
    $script:warnings | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
}

if ($script:criticalFailures.Count -gt 0) {
    Write-Host "`nCritical Failures:" -ForegroundColor Red
    $script:criticalFailures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    throw "Critical assemblies failed to deploy"
}

Write-Host "`n✓ CLR assembly deployment completed successfully" -ForegroundColor Green
