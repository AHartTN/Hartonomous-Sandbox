#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deploy 3-tier SQL CLR architecture: .NET Framework → .NET Standard Bridge → Modern Libraries

.DESCRIPTION
    Deploys all assemblies in dependency order:
    1. Foundation libraries (System.*, MathNet.Numerics, etc.) - SAFE
    2. Hartonomous.Sql.Bridge.dll (.NET Standard 2.0) - SAFE
    3. SqlClrFunctions.dll (.NET Framework 4.8.1) - UNSAFE

    This preserves the revolutionary architecture:
    - SQL CLR provides lightweight entry points
    - Bridge enables .NET Standard 2.0 compatibility
    - Modern NuGet packages provide advanced features
#>

param(
    [string]$ServerName = ".",
    [string]$DatabaseName = "Hartonomous",
    [string]$SqlClrPath = "D:\Repositories\Hartonomous\src\SqlClr\bin\Release",
    [string]$BridgePath = "D:\Repositories\Hartonomous\src\Hartonomous.Sql.Bridge\bin\Release\netstandard2.0"
)

$ErrorActionPreference = "Stop"

Write-Host "`n=== 3-Tier SQL CLR Deployment ===" -ForegroundColor Cyan
Write-Host "SQL CLR (.NET 4.8.1) → Bridge (.NET Standard 2.0) → Modern Libraries`n"

function Deploy-Assembly {
    param([string]$Name, [string]$Path, [string]$PermissionSet)

    Write-Host "Deploying: $Name ($PermissionSet)" -ForegroundColor Yellow

    if (-not (Test-Path $Path)) {
        Write-Host "  SKIP: File not found" -ForegroundColor Gray
        return
    }

    $bytes = [System.IO.File]::ReadAllBytes($Path)
    $hex = "0x" + [System.BitConverter]::ToString($bytes).Replace("-", "")

    $sql = @"
USE [$DatabaseName];
IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = '$Name') DROP ASSEMBLY [$Name];
CREATE ASSEMBLY [$Name] FROM $hex WITH PERMISSION_SET = $PermissionSet;
PRINT '  ✓ Deployed: $Name';
"@

    $temp = [System.IO.Path]::GetTempFileName() + ".sql"
    try {
        $sql | Out-File -FilePath $temp -Encoding UTF8
        $output = & sqlcmd -S $ServerName -E -C -i $temp -b 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Host "  ERROR: $output" -ForegroundColor Red
            throw "Failed to deploy $Name"
        }
        Write-Host "  ✓ Success" -ForegroundColor Green
    } finally {
        Remove-Item $temp -ErrorAction SilentlyContinue
    }
}

# Tier 1: Foundation Libraries from Bridge output (SAFE - no external dependencies)
# NOTE: Skipping netstandard.dll deployment - .NET Framework 4.8.1 provides all .NET Standard 2.0 APIs
Write-Host "`n--- Tier 1: Foundation Libraries ---" -ForegroundColor Cyan
Deploy-Assembly "System.Runtime.CompilerServices.Unsafe" "$BridgePath\System.Runtime.CompilerServices.Unsafe.dll" "SAFE"
Deploy-Assembly "System.Buffers" "$BridgePath\System.Buffers.dll" "SAFE"
Deploy-Assembly "System.Memory" "$BridgePath\System.Memory.dll" "SAFE"
Deploy-Assembly "System.Numerics.Vectors" "$BridgePath\System.Numerics.Vectors.dll" "SAFE"
Deploy-Assembly "System.ValueTuple" "$BridgePath\System.ValueTuple.dll" "SAFE"
Deploy-Assembly "System.Threading.Tasks.Extensions" "$BridgePath\System.Threading.Tasks.Extensions.dll" "SAFE"

# Tier 2: Higher-level Libraries from Bridge output (SAFE - depend on Tier 1)
Write-Host "`n--- Tier 2: Advanced Libraries ---" -ForegroundColor Cyan
Deploy-Assembly "Microsoft.Bcl.AsyncInterfaces" "$BridgePath\Microsoft.Bcl.AsyncInterfaces.dll" "SAFE"
Deploy-Assembly "System.Text.Encodings.Web" "$BridgePath\System.Text.Encodings.Web.dll" "SAFE"
Deploy-Assembly "System.Text.Json" "$BridgePath\System.Text.Json.dll" "SAFE"
Deploy-Assembly "MathNet.Numerics" "$BridgePath\MathNet.Numerics.dll" "SAFE"

# Tier 3: Bridge Library (SAFE - .NET Standard 2.0 bridge)
Write-Host "`n--- Tier 3: .NET Standard Bridge ---" -ForegroundColor Cyan
Deploy-Assembly "Hartonomous.Sql.Bridge" "$BridgePath\Hartonomous.Sql.Bridge.dll" "SAFE"

# Tier 4: SQL CLR Entry Point (UNSAFE - for GPU/file system access)
Write-Host "`n--- Tier 4: SQL CLR Functions ---" -ForegroundColor Cyan
Deploy-Assembly "SqlClrFunctions" "$SqlClrPath\SqlClrFunctions.dll" "UNSAFE"

# Verification
Write-Host "`n--- Verification ---" -ForegroundColor Cyan
$verifyQuery = @"
SELECT
    name AS AssemblyName,
    permission_set_desc AS PermissionSet
FROM sys.assemblies
WHERE name IN (
    'System.Runtime.CompilerServices.Unsafe',
    'System.Buffers',
    'System.Memory',
    'System.Numerics.Vectors',
    'System.Threading.Tasks.Extensions',
    'System.ValueTuple',
    'Microsoft.Bcl.AsyncInterfaces',
    'System.Text.Encodings.Web',
    'System.Text.Json',
    'MathNet.Numerics',
    'Hartonomous.Sql.Bridge',
    'SqlClrFunctions'
)
ORDER BY
    CASE name
        WHEN 'SqlClrFunctions' THEN 4
        WHEN 'Hartonomous.Sql.Bridge' THEN 3
        WHEN 'System.Text.Json' THEN 2
        WHEN 'MathNet.Numerics' THEN 2
        ELSE 1
    END,
    name;
"@

sqlcmd -S $ServerName -E -C -d $DatabaseName -Q $verifyQuery

Write-Host "`n✓ 3-Tier Architecture Deployed!" -ForegroundColor Green
Write-Host "  SQL CLR → .NET Standard Bridge → Modern Libraries`n"
