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
            'System.Numerics.Vectors.dll',
            'System.ValueTuple.dll'
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
            'System.Drawing.dll',
            'SqlClrFunctions.dll',
            'Hartonomous.Database.dll'
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

    # Write DROP + CREATE to temp file to avoid command-line length limits
    $tempSqlFile = [System.IO.Path]::GetTempFileName() + ".sql"
    
    @"
USE [$Database];
GO

IF EXISTS (SELECT * FROM sys.assemblies WHERE name = '$AssemblyName')
BEGIN
    PRINT 'Dropping existing assembly: $AssemblyName';
    DROP ASSEMBLY [$AssemblyName];
END
GO

CREATE ASSEMBLY [$AssemblyName]
FROM 0x$hexString
WITH PERMISSION_SET = UNSAFE;
GO

PRINT 'Assembly deployed: $AssemblyName';
GO
"@ | Out-File -FilePath $tempSqlFile -Encoding utf8

    try {
        if ($UseAzureAD) {
            sqlcmd -S $Server -d master -G -P $AccessToken -i $tempSqlFile -b
        } elseif ($Username -and $Password) {
            sqlcmd -S $Server -U $Username -P $Password -d master -i $tempSqlFile -b
        } else {
            sqlcmd -S $Server -d master -E -C -i $tempSqlFile -b
        }

        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to deploy assembly: $AssemblyName (Exit code: $LASTEXITCODE)"
            throw
        }

        Write-Host "  ✓ $AssemblyName deployed successfully"
    }
    finally {
        Remove-Item $tempSqlFile -ErrorAction SilentlyContinue
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
