#Requires -Version 7.0
param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectPath,
    
    [Parameter(Mandatory=$true)]
    [string]$OutputDir,
    
    [Parameter(Mandatory=$false)]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

# Find MSBuild
$msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
  -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe `
  -prerelease | Select-Object -First 1

if (-not $msbuild) {
  $msbuild = "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
}

if (-not (Test-Path $msbuild)) {
  Write-Error "MSBuild not found. Agent needs Visual Studio or MSBuild installed."
  exit 1
}

Write-Host "Using MSBuild: $msbuild"

# Build DACPAC
& $msbuild $ProjectPath `
  /p:Configuration=$Configuration `
  /p:OutDir="$OutputDir" `
  /v:minimal

if ($LASTEXITCODE -ne 0) {
  Write-Error "MSBuild failed with exit code $LASTEXITCODE"
  exit $LASTEXITCODE
}

Write-Host "✓ DACPAC built successfully"
