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

# NuGet packages
$nugetPackages = @(
    @{Name="System.Runtime.CompilerServices.Unsafe"; Path="system.runtime.compilerservices.unsafe\4.5.3\lib\net461\System.Runtime.CompilerServices.Unsafe.dll"},
    @{Name="System.Buffers"; Path="system.buffers\4.5.1\lib\net461\System.Buffers.dll"},
    @{Name="System.Memory"; Path="system.memory\4.5.5\lib\net461\System.Memory.dll"},
    @{Name="System.ValueTuple"; Path="system.valuetuple\4.5.0\lib\net47\System.ValueTuple.dll"},
    @{Name="System.Threading.Tasks.Extensions"; Path="system.threading.tasks.extensions\4.5.4\lib\net461\System.Threading.Tasks.Extensions.dll"},
    @{Name="Microsoft.Bcl.AsyncInterfaces"; Path="microsoft.bcl.asyncinterfaces\8.0.0\lib\net462\Microsoft.Bcl.AsyncInterfaces.dll"},
    @{Name="System.Text.Encodings.Web"; Path="system.text.encodings.web\8.0.0\lib\net462\System.Text.Encodings.Web.dll"},
    @{Name="System.Numerics.Vectors"; Path="system.numerics.vectors\4.5.0\lib\net46\System.Numerics.Vectors.dll"},
    @{Name="System.Text.Json"; Path="system.text.json\8.0.5\lib\net462\System.Text.Json.dll"},
    @{Name="MathNet.Numerics"; Path="mathnet.numerics\5.0.0\lib\net48\MathNet.Numerics.dll"}
)

# .NET Framework GAC assemblies
$gacAssemblies = @(
    "System.Xml.Linq.dll",
    "System.Runtime.Serialization.dll"
)

$frameworkDir = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319"

Write-Host "Copying NuGet packages..." -ForegroundColor Cyan
foreach ($pkg in $nugetPackages) {
    $source = Join-Path $NuGetRoot $pkg.Path
    $dest = Join-Path $TargetDir "$($pkg.Name).dll"
    
    if (Test-Path $source) {
        Copy-Item $source $dest -Force
        $size = (Get-Item $dest).Length / 1KB
        Write-Host "  ✓ $($pkg.Name).dll ($([math]::Round($size,1)) KB)" -ForegroundColor Green
    } else {
        Write-Host "  ✗ NOT FOUND: $source" -ForegroundColor Red
    }
}

Write-Host "`nCopying .NET Framework GAC assemblies..." -ForegroundColor Cyan
foreach ($gac in $gacAssemblies) {
    $source = Join-Path $frameworkDir $gac
    $dest = Join-Path $TargetDir $gac
    
    if (Test-Path $source) {
        Copy-Item $source $dest -Force
        $size = (Get-Item $dest).Length / 1KB
        Write-Host "  ✓ $gac ($([math]::Round($size,1)) KB)" -ForegroundColor Green
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
