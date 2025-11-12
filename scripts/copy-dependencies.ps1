#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Copy ALL dependencies to dependencies folder
#>

param(
    [string]$NuGetRoot = "$env:USERPROFILE\.nuget\packages",
    [string]$SourceBin = "d:\Repositories\Hartonomous\src\SqlClr\bin\Release",
    [string]$TargetDir = "d:\Repositories\Hartonomous\dependencies"
)

$ErrorActionPreference = "Stop"

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "COPYING ALL DEPENDENCIES" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# NuGet packages - ILGPU 0.8.0 + Unsafe 4.5.3 = compatible dependency tree
$nugetPackages = @(
    @{Name="ILGPU"; Path="ilgpu\0.8.0\lib\net47\Release\ILGPU.dll"},
    @{Name="ILGPU.Algorithms"; Path="ilgpu.algorithms\0.8.0\lib\net47\Release\ILGPU.Algorithms.dll"},
    @{Name="MathNet.Numerics"; Path="mathnet.numerics\5.0.0\lib\net48\MathNet.Numerics.dll"},
    @{Name="Newtonsoft.Json"; Path="newtonsoft.json\13.0.3\lib\net45\Newtonsoft.Json.dll"},
    @{Name="System.Collections.Immutable"; Path="system.collections.immutable\1.7.1\lib\net461\System.Collections.Immutable.dll"},
    @{Name="System.Reflection.Metadata"; Path="system.reflection.metadata\1.8.1\lib\net461\System.Reflection.Metadata.dll"},
    @{Name="System.Runtime.CompilerServices.Unsafe"; Path="system.runtime.compilerservices.unsafe\4.5.3\lib\net461\System.Runtime.CompilerServices.Unsafe.dll"},
    @{Name="System.Buffers"; Path="system.buffers\4.5.1\lib\net461\System.Buffers.dll"},
    @{Name="System.Memory"; Path="system.memory\4.5.4\lib\net461\System.Memory.dll"},
    @{Name="System.Numerics.Vectors"; Path="system.numerics.vectors\4.5.0\lib\net46\System.Numerics.Vectors.dll"},
    @{Name="Microsoft.SqlServer.Types"; Path="microsoft.sqlserver.types\160.1000.6\lib\net462\Microsoft.SqlServer.Types.dll"}
)

# Note: GAC assemblies (System.ServiceModel.Internals, SMDiagnostics, System.Runtime.Serialization, System.Drawing)
# are provided by .NET Framework 4.8.1 and must NOT be deployed to SQL Server to avoid MVID conflicts.
# They are removed from this copy script as of the CLR deployment fix.

$frameworkDir = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319"

Write-Host "Copying NuGet packages..." -ForegroundColor Cyan
foreach ($pkg in $nugetPackages) {
    $source = Join-Path $NuGetRoot $pkg.Path
    $dest = Join-Path $TargetDir "$($pkg.Name).dll"
    
    if (Test-Path $source) {
    New-Item -ItemType Directory -Path (Split-Path $dest) -Force | Out-Null
    Copy-Item $source $dest -Force
        $size = (Get-Item $dest).Length / 1KB
        Write-Host "  ✓ $($pkg.Name).dll ($([math]::Round($size,1)) KB)" -ForegroundColor Green
    } else {
        Write-Host "  ✗ NOT FOUND: $source" -ForegroundColor Red
    }
}

# Copy SqlClrFunctions.dll from bin\Release if it exists
if (Test-Path $SourceBin) {
    $sqlClrDll = Join-Path $SourceBin "SqlClrFunctions.dll"
    if (Test-Path $sqlClrDll) {
        $dest = Join-Path $TargetDir "SqlClrFunctions.dll"
        Copy-Item $sqlClrDll $dest -Force
        $size = (Get-Item $dest).Length / 1KB
        Write-Host "`nCopied SqlClrFunctions.dll ($([math]::Round($size,1)) KB)" -ForegroundColor Green
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "✓ ALL DEPENDENCIES COPIED!" -ForegroundColor Green
Write-Host "Location: $TargetDir" -ForegroundColor Yellow
Write-Host "========================================`n" -ForegroundColor Cyan

# List all files
Write-Host "Final dependency list:" -ForegroundColor Cyan
Get-ChildItem $TargetDir -Filter "*.dll" | Sort-Object Name | ForEach-Object {
    $size = $_.Length / 1KB
    Write-Host "  $($_.Name) - $([math]::Round($size,1)) KB" -ForegroundColor Gray
}
