#Requires -Version 7.0
<#
.SYNOPSIS
    A unified, idempotent script to deploy the Hartonomous database, including all CLR assemblies and DACPAC.
.DESCRIPTION
    This master script orchestrates the entire database deployment process in a robust, streamlined, and idempotent manner.
    It follows the correct order of operations for complex CLR deployments.
    1. Enables CLR.
    2. Cleans up previous CLR objects to ensure idempotency.
    3. Deploys external (unsigned) CLR dependency assemblies.
    4. Deploys the main DACPAC (which includes the primary signed CLR assembly).
    5. Sets the TRUSTWORTHY flag, required for the unsigned assemblies.
    6. Validates the deployment by counting the created objects.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string]$Server,

    [Parameter(Mandatory=$true)]
    [string]$Database,

    [Parameter(Mandatory=$true)]
    [string]$AccessToken,

    [Parameter(Mandatory=$true)]
    [string]$DacpacPath,

    [Parameter(Mandatory=$true)]
    [string]$DependenciesPath
)

$ErrorActionPreference = 'Stop'
$scriptRoot = $PSScriptRoot

# Helper function for logging
function Write-Step {
    param([string]$Message)
    Write-Host "`n========================================================================"
    Write-Host "  $Message"
    Write-Host "========================================================================"
}

# ============================================================================
# STEP 1: Enable CLR
# ============================================================================
Write-Step "Step 1: Enabling CLR Integration"
$enableClrSql = "EXEC sp_configure 'show advanced options', 1; RECONFIGURE; EXEC sp_configure 'clr enabled', 1; RECONFIGURE;"
Invoke-Sqlcmd -ServerInstance $Server -Database "master" -Query $enableClrSql -AccessToken $AccessToken -TrustServerCertificate
Write-Host "✓ CLR integration enabled."

# ============================================================================
# STEP 2: Pre-DACPAC Cleanup (Idempotency)
# ============================================================================
Write-Step "Step 2: Performing Pre-DACPAC Cleanup"
$cleanupSqlPath = Join-Path $scriptRoot "Pre-DACPAC-Cleanup.sql"
Invoke-Sqlcmd -ServerInstance $Server -Database $Database -InputFile $cleanupSqlPath -AccessToken $AccessToken -TrustServerCertificate -Variable "DatabaseName=$Database"
Write-Host "✓ Pre-DACPAC cleanup complete."

# ============================================================================
# STEP 3: Deploy External (Unsigned) CLR Assemblies
# ============================================================================
Write-Step "Step 3: Deploying External CLR Assemblies"

$tiers = @(
    @{ Name = "Tier 1"; Assemblies = @('System.Numerics.Vectors.dll') },
    @{ Name = "Tier 2"; Assemblies = @('System.Memory.dll', 'System.Buffers.dll', 'System.Runtime.CompilerServices.Unsafe.dll') },
    @{ Name = "Tier 3"; Assemblies = @('System.Collections.Immutable.dll', 'System.Reflection.Metadata.dll') },
    @{ Name = "Tier 4"; Assemblies = @('System.Runtime.Serialization.dll', 'System.ServiceModel.Internals.dll', 'SMDiagnostics.dll') },
    @{ Name = "Tier 5"; Assemblies = @('MathNet.Numerics.dll', 'Newtonsoft.Json.dll', 'Microsoft.SqlServer.Types.dll') },
    @{ Name = "Tier 6"; Assemblies = @('System.Drawing.dll') }
)

function ConvertTo-HexString {
    param([string]$FilePath)
    $bytes = [System.IO.File]::ReadAllBytes($FilePath)
    return '0x' + ($bytes | ForEach-Object { $_.ToString("X2") }) -join ''
}

foreach ($tier in $tiers) {
    Write-Host "`nDeploying $($tier.Name)"
    foreach ($dllName in $tier.Assemblies) {
        $assemblyName = [System.IO.Path]::GetFileNameWithoutExtension($dllName)
        $filePath = Join-Path $DependenciesPath $dllName

        if (-not (Test-Path $filePath)) {
            throw "Dependency not found: $filePath"
        }

        Write-Host "  - Deploying assembly: $assemblyName..."
        $hexString = ConvertTo-HexString -FilePath $filePath

        $deploySql = @"
IF EXISTS (SELECT * FROM sys.assemblies WHERE name = '$assemblyName')
BEGIN
    DROP ASSEMBLY [$assemblyName];
END;

CREATE ASSEMBLY [$assemblyName]
FROM $hexString
WITH PERMISSION_SET = UNSAFE;
"@
        Invoke-Sqlcmd -ServerInstance $Server -Database $Database -Query $deploySql -AccessToken $AccessToken -TrustServerCertificate
        Write-Host "    ✓ Deployed $assemblyName"
    }
}
Write-Host "✓ External CLR assemblies deployed."

# ============================================================================
# STEP 4: Deploy DACPAC (Hartonomous.Clr + T-SQL Objects)
# ============================================================================
Write-Step "Step 4: Deploying DACPAC"
$connectionString = "Server=$Server;Database=$Database;Encrypt=True;TrustServerCertificate=True;"
Write-Host "Deploying DACPAC: $DacpacPath"
sqlpackage.exe /Action:Publish `
    /SourceFile:"$DacpacPath" `
    /TargetConnectionString:"$connectionString" `
    /AccessToken:"$AccessToken" `
    /p:BlockOnPossibleDataLoss=False `
    /p:DropObjectsNotInSource=False

if ($LASTEXITCODE -ne 0) {
    throw "DACPAC deployment failed with exit code $LASTEXITCODE"
}
Write-Host "✓ DACPAC deployed successfully."

# ============================================================================
# STEP 5: Set TRUSTWORTHY ON (Required for Unsigned Assemblies)
# ============================================================================
Write-Step "Step 5: Setting TRUSTWORTHY ON"
$trustworthySql = "ALTER DATABASE [$Database] SET TRUSTWORTHY ON;"
Invoke-Sqlcmd -ServerInstance $Server -Database "master" -Query $trustworthySql -AccessToken $AccessToken -TrustServerCertificate
Write-Host "✓ TRUSTWORTHY enabled for database '$Database'."

# ============================================================================
# STEP 6: Validate Deployment
# ============================================================================
Write-Step "Step 6: Validating Deployment"
$assemblyCount = Invoke-Sqlcmd -ServerInstance $Server -Database $Database -Query "SELECT COUNT(*) as cnt FROM sys.assemblies WHERE is_user_defined = 1" -AccessToken $AccessToken -TrustServerCertificate
$objectCount = Invoke-Sqlcmd -ServerInstance $Server -Database $Database -Query "SELECT COUNT(*) as cnt FROM sys.objects WHERE type IN ('FN', 'IF', 'TF', 'FS', 'FT', 'AF', 'PC') AND is_ms_shipped = 0" -AccessToken $AccessToken -TrustServerCertificate
$udtCount = Invoke-Sqlcmd -ServerInstance $Server -Database $Database -Query "SELECT COUNT(*) as cnt FROM sys.types WHERE is_user_defined = 1 AND is_assembly_type = 1" -AccessToken $AccessToken -TrustServerCertificate

Write-Host "  - User-Defined Assemblies: $($assemblyCount.cnt)"
Write-Host "  - User-Defined Functions/Procs/Aggregates: $($objectCount.cnt)"
Write-Host "  - CLR User-Defined Types: $($udtCount.cnt)"
Write-Host "✓ Deployment validation complete."
Write-Host "`n✓✓✓ Database deployment completed successfully! ✓✓✓"
