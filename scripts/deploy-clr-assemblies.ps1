#Requires -Version 7.0
<#
.SYNOPSIS
    Deploys external CLR assembly dependencies to SQL Server in correct tier order.

.DESCRIPTION
    Deploys the 16 external CLR DLL dependencies required by Hartonomous.Clr
    in 6 tiers to respect dependency order. Based on official Microsoft Docs
    guidance for SQL CLR assembly deployment.

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
    [Parameter(Mandatory = $true)]
    [string]$Server,

    [Parameter(Mandatory = $true)]
    [string]$Database,

    [Parameter(Mandatory = $false)]
    [switch]$UseAzureAD,

    [Parameter(Mandatory = $false)]
    [string]$AccessToken,

    [Parameter(Mandatory = $false)]
    [string]$Username,

    [Parameter(Mandatory = $false)]
    [string]$Password,

    [Parameter(Mandatory = $true)]
    [string]$DependenciesPath
)

$ErrorActionPreference = 'Stop'

# Validate authentication parameters
if ($UseAzureAD) {
    if ([string]::IsNullOrEmpty($AccessToken)) {
        throw "AccessToken is required when UseAzureAD is specified"
    }
} else {
    if ([string]::IsNullOrEmpty($Username) -or [string]::IsNullOrEmpty($Password)) {
        throw "Username and Password are required when not using Azure AD authentication"
    }
}

# Tier structure based on dependency order
$tiers = @(
    @{
        Name     = "Tier 1: System.Numerics.Vectors"
        Assemblies = @('System.Numerics.Vectors.dll')
    },
    @{
        Name     = "Tier 2: System.Runtime.Intrinsics"
        Assemblies = @('System.Runtime.Intrinsics.dll')
    },
    @{
        Name     = "Tier 3: System.Memory + System.Buffers"
        Assemblies = @('System.Memory.dll', 'System.Buffers.dll')
    },
    @{
        Name     = "Tier 4: System.Runtime.CompilerServices.Unsafe + System.Text.Json"
        Assemblies = @('System.Runtime.CompilerServices.Unsafe.dll', 'System.Text.Json.dll')
    },
    @{
        Name     = "Tier 5: Third-party dependencies"
        Assemblies = @(
            'MathNet.Numerics.dll',
            'Microsoft.ML.OnnxRuntime.dll',
            'Microsoft.Extensions.AI.Abstractions.dll',
            'Newtonsoft.Json.dll'
        )
    },
    @{
        Name     = "Tier 6: Application assemblies"
        Assemblies = @(
            'Hartonomous.Core.dll',
            'Hartonomous.Infrastructure.dll',
            'Hartonomous.Clr.dll'
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
        [string]$FilePath
    )

    Write-Host "  Deploying: $AssemblyName..."

    if (-not (Test-Path $FilePath)) {
        Write-Error "Assembly file not found: $FilePath"
        throw
    }

    $hexString = ConvertTo-HexString -FilePath $FilePath

    # Drop existing assembly if present
    $dropSql = @"
IF EXISTS (SELECT * FROM sys.assemblies WHERE name = '$AssemblyName')
BEGIN
    PRINT 'Dropping existing assembly: $AssemblyName';
    DROP ASSEMBLY [$AssemblyName];
END
"@

    if ($UseAzureAD) {
        $dropSql | sqlcmd -S $Server -d $Database -G -P $AccessToken -b
    } else {
        $dropSql | sqlcmd -S $Server -U $Username -P $Password -d $Database -b
    }

    # Create assembly with UNSAFE permission set
    $createSql = @"
CREATE ASSEMBLY [$AssemblyName]
FROM 0x$hexString
WITH PERMISSION_SET = UNSAFE;

PRINT 'Assembly deployed: $AssemblyName';
"@

    if ($UseAzureAD) {
        $createSql | sqlcmd -S $Server -d $Database -G -P $AccessToken -b
    } else {
        $createSql | sqlcmd -S $Server -U $Username -P $Password -d $Database -b
    }

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to deploy assembly: $AssemblyName (Exit code: $LASTEXITCODE)"
        throw
    }

    Write-Host "  \u2713 $AssemblyName deployed successfully"
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

$totalAssemblies = 0
foreach ($tier in $tiers) {
    Write-Host "Processing $($tier.Name)..." -ForegroundColor Cyan

    foreach ($dllName in $tier.Assemblies) {
        $assemblyName = [System.IO.Path]::GetFileNameWithoutExtension($dllName)
        $filePath = Join-Path $DependenciesPath $dllName

        Deploy-Assembly -AssemblyName $assemblyName -FilePath $filePath
        $totalAssemblies++
    }

    Write-Host ""
}

Write-Host "========================================"
Write-Host "✓ All $totalAssemblies external CLR assemblies deployed successfully" -ForegroundColor Green
Write-Host "========================================"
