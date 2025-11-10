#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deploys UNSAFE CLR assembly to Arc-enabled SQL Server with proper dependency management.

.DESCRIPTION
    Idempotent CLR deployment for Azure Arc SQL Server (on-prem UNSAFE mode only).
    - Enables CLR integration if disabled
    - Handles 'clr strict security' per MS Docs (sys.sp_add_trusted_assembly)
    - Computes SHA-512 hash of assembly DLL for trusted assembly registration
    - Drops dependent objects in correct order (functions → aggregates → types → assembly)
    - Creates assembly with PERMISSION_SET = UNSAFE for ILGPU/cuBLAS support
    - Verifies assembly deployment via sys.assemblies
    - Returns JSON with deployment status for Azure DevOps pipeline

.PARAMETER ServerName
    SQL Server instance name

.PARAMETER DatabaseName
    Target database name (default: Hartonomous)

.PARAMETER AssemblyPath
    Full path to SqlClrFunctions.dll compiled assembly

.PARAMETER SqlUser
    SQL authentication username (optional)

.PARAMETER SqlPassword
    SQL authentication password as SecureString

.EXAMPLE
    .\04-clr-assembly.ps1 -ServerName "hart-server" -DatabaseName "Hartonomous" -AssemblyPath "/path/to/SqlClrFunctions.dll"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$ServerName,
    
    [Parameter(Mandatory=$false)]
    [string]$DatabaseName = "Hartonomous",
    
    [Parameter(Mandatory)]
    [string]$AssemblyPath,
    
    [Parameter(Mandatory=$false)]
    [string]$SqlUser,
    
    [Parameter(Mandatory=$false)]
    [SecureString]$SqlPassword,
    
    [Parameter(Mandatory=$false)]
    [string[]]$DependencyPaths
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$result = @{
    Step = "ClrAssemblyDeployment"
    Success = $false
    ClrEnabled = $false
    ClrStrictSecurity = $false
    AssemblyPath = $AssemblyPath
    AssemblyHash = ""
    AssemblyExists = $false
    AssemblyCreated = $false
    DependenciesDropped = @()
    TrustedAssemblyRegistered = $false
    Errors = @()
    Warnings = @()
    Timestamp = (Get-Date -Format "o")
}

function Invoke-SqlCommand {
    param(
        [string]$Query,
        [string]$Database = "master",
        [switch]$IgnoreErrors
    )
    
    $sqlArgs = @("-S", $ServerName, "-d", $Database, "-C", "-b", "-Q", $Query, "-h", "-1")
    
    if ($SqlUser) {
        $plainPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto(
            [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SqlPassword)
        )
        $sqlArgs += @("-U", $SqlUser, "-P", $plainPassword)
    } else {
        $sqlArgs += "-E"
    }
    
    $output = & sqlcmd @sqlArgs 2>&1
    if (-not $IgnoreErrors -and $LASTEXITCODE -ne 0) {
        throw "SQL command failed: $output"
    }
    return ($output | Out-String).Trim()
}

try {
    Write-Host "=== CLR Assembly Deployment Script ===" -ForegroundColor Cyan
    Write-Host "Server: $ServerName" -ForegroundColor Gray
    Write-Host "Database: $DatabaseName" -ForegroundColor Gray
    Write-Host "Assembly: $AssemblyPath" -ForegroundColor Gray
    
    # Verify assembly file exists
    Write-Host "`nVerifying assembly file..." -NoNewline
    if (-not (Test-Path $AssemblyPath)) {
        throw "Assembly file not found: $AssemblyPath"
    }
    $assemblyInfo = Get-Item $AssemblyPath
    Write-Host " Found ($([math]::Round($assemblyInfo.Length / 1MB, 2)) MB)" -ForegroundColor Green
    
    # Compute SHA-512 hash for trusted assembly registration (MS Docs requirement for clr strict security)
    Write-Host "Computing assembly hash..." -NoNewline
    $hashBytes = (Get-FileHash -Path $AssemblyPath -Algorithm SHA512).Hash
    $result.AssemblyHash = $hashBytes
    Write-Host " SHA-512: $($hashBytes.Substring(0, 16))..." -ForegroundColor Green
    
    # Check CLR enabled setting
    Write-Host "Checking CLR integration..." -NoNewline
    $clrEnabledQuery = "SELECT CAST(value AS INT) FROM sys.configurations WHERE name = 'clr enabled'"
    $clrEnabled = [int](Invoke-SqlCommand -Query $clrEnabledQuery)
    $result.ClrEnabled = ($clrEnabled -eq 1)
    
    if (-not $result.ClrEnabled) {
        Write-Host " Disabled, enabling..." -NoNewline
        $enableClrSql = @"
EXEC sp_configure 'clr enabled', 1;
RECONFIGURE;
"@
        Invoke-SqlCommand -Query $enableClrSql
        $result.ClrEnabled = $true
        Write-Host " Enabled" -ForegroundColor Green
    }
    else {
        Write-Host " Enabled" -ForegroundColor Green
    }
    
    # Check CLR strict security setting (SQL Server 2017+)
    Write-Host "Checking CLR strict security..." -NoNewline
    $clrStrictQuery = "SELECT CAST(value AS INT) FROM sys.configurations WHERE name = 'clr strict security'"
    $clrStrict = [int](Invoke-SqlCommand -Query $clrStrictQuery)
    $result.ClrStrictSecurity = ($clrStrict -eq 1)
    
    if ($result.ClrStrictSecurity) {
        Write-Host " Enabled (requires trusted assembly)" -ForegroundColor Yellow
        $result.Warnings += "CLR strict security enabled - assembly must be registered via sys.sp_add_trusted_assembly"
    }
    else {
        Write-Host " Disabled" -ForegroundColor Green
        $result.Warnings += "CLR strict security is disabled. MS Docs recommends enabling for production environments."
    }
    
    # Read assembly bytes
    Write-Host "Reading assembly bytes..." -NoNewline
    $assemblyBytes = [System.IO.File]::ReadAllBytes($AssemblyPath)
    $hexString = "0x" + [System.BitConverter]::ToString($assemblyBytes).Replace("-", "")
    Write-Host " Done ($($assemblyBytes.Length) bytes)" -ForegroundColor Green
    
    # Check if assembly already exists
    Write-Host "Checking existing assembly..." -NoNewline
    $assemblyCheckQuery = @"
SELECT 
    a.name,
    a.clr_name,
    a.permission_set_desc,
    CONVERT(VARCHAR(MAX), HASHBYTES('SHA2_512', a.content), 2) AS content_hash
FROM sys.assemblies a
WHERE a.name = 'SqlClrFunctions'
FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
"@
    $existingAssemblyJson = Invoke-SqlCommand -Query $assemblyCheckQuery -Database $DatabaseName -IgnoreErrors
    
    if ($existingAssemblyJson -and $existingAssemblyJson -ne "null") {
        $existingAssembly = $existingAssemblyJson | ConvertFrom-Json
        $result.AssemblyExists = $true
        Write-Host " Exists" -ForegroundColor Yellow
        Write-Host "  Name: $($existingAssembly.name)" -ForegroundColor Gray
        Write-Host "  Permission Set: $($existingAssembly.permission_set_desc)" -ForegroundColor Gray
        Write-Host "  Hash: $($existingAssembly.content_hash.Substring(2, 16))..." -ForegroundColor Gray
        
        # Compare hashes to determine if assembly needs updating
        $existingHash = $existingAssembly.content_hash.TrimStart('0x')
        if ($existingHash -eq $hashBytes) {
            Write-Host "✓ Assembly is up-to-date (hash match)" -ForegroundColor Green
            $result.Success = $true
            # Skip redeployment
            return
        }
        else {
            Write-Host "! Assembly hash mismatch - redeployment required" -ForegroundColor Yellow
            
            # Drop dependent objects in reverse dependency order
            Write-Host "Dropping dependent CLR objects..." -NoNewline
            
            # 1. Drop CLR functions
            $dropFunctionsQuery = @"
DECLARE @sql NVARCHAR(MAX) = '';
SELECT @sql += 'DROP FUNCTION ' + QUOTENAME(SCHEMA_NAME(o.schema_id)) + '.' + QUOTENAME(o.name) + ';' + CHAR(13)
FROM sys.objects o
INNER JOIN sys.assembly_modules am ON o.object_id = am.object_id
INNER JOIN sys.assemblies a ON am.assembly_id = a.assembly_id
WHERE a.name = 'SqlClrFunctions' AND o.type IN ('FN', 'FS', 'FT', 'IF', 'TF');
EXEC sp_executesql @sql;
"@
            Invoke-SqlCommand -Query $dropFunctionsQuery -Database $DatabaseName -IgnoreErrors
            $result.DependenciesDropped += "Functions"
            
            # 2. Drop CLR aggregates
            $dropAggregatesQuery = @"
DECLARE @sql NVARCHAR(MAX) = '';
SELECT @sql += 'DROP AGGREGATE ' + QUOTENAME(SCHEMA_NAME(o.schema_id)) + '.' + QUOTENAME(o.name) + ';' + CHAR(13)
FROM sys.objects o
INNER JOIN sys.assembly_modules am ON o.object_id = am.object_id
INNER JOIN sys.assemblies a ON am.assembly_id = a.assembly_id
WHERE a.name = 'SqlClrFunctions' AND o.type = 'AF';
EXEC sp_executesql @sql;
"@
            Invoke-SqlCommand -Query $dropAggregatesQuery -Database $DatabaseName -IgnoreErrors
            $result.DependenciesDropped += "Aggregates"
            
            # 3. Drop CLR types
            $dropTypesQuery = @"
DECLARE @sql NVARCHAR(MAX) = '';
SELECT @sql += 'DROP TYPE ' + QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name) + ';' + CHAR(13)
FROM sys.types
WHERE is_assembly_type = 1 AND ASSEMBLYPROPERTY(assembly_class, 'AssemblyName') = 'SqlClrFunctions';
EXEC sp_executesql @sql;
"@
            Invoke-SqlCommand -Query $dropTypesQuery -Database $DatabaseName -IgnoreErrors
            $result.DependenciesDropped += "Types"
            
            Write-Host " Done" -ForegroundColor Green
            
            # 4. Drop assembly
            Write-Host "Dropping assembly..." -NoNewline
            $dropAssemblySql = "DROP ASSEMBLY [SqlClrFunctions];"
            Invoke-SqlCommand -Query $dropAssemblySql -Database $DatabaseName
            Write-Host " Done" -ForegroundColor Green
        }
    }
    else {
        Write-Host " Not found" -ForegroundColor Yellow
    }
    
    # Register as trusted assembly if clr strict security is enabled
    if ($result.ClrStrictSecurity) {
        Write-Host "Registering trusted assembly..." -NoNewline
        
        # Check if already registered in master
        $checkTrustedQuery = @"
SELECT COUNT(*) 
FROM sys.trusted_assemblies 
WHERE hash = $hexString
"@
        $trustedCount = [int](Invoke-SqlCommand -Query $checkTrustedQuery -Database "master")
        
        if ($trustedCount -eq 0) {
            # Register in sys.trusted_assemblies (master database)
            $trustAssemblySql = @"
EXEC sys.sp_add_trusted_assembly 
    @hash = $hexString,
    @description = N'Hartonomous CLR Functions with ILGPU/cuBLAS support for autonomous systems';
"@
            Invoke-SqlCommand -Query $trustAssemblySql -Database "master"
            $result.TrustedAssemblyRegistered = $true
            Write-Host " Registered" -ForegroundColor Green
        }
        else {
            Write-Host " Already registered" -ForegroundColor Yellow
        }
    }
    
    # Create assembly with UNSAFE permission set (required for ILGPU/cuBLAS)
    Write-Host "Creating CLR assembly..." -NoNewline
    
    # Note: Assembly bytes embedded directly in T-SQL (size limit ~2GB theoretical, practical limit much lower)
    # For very large assemblies, consider using SQLCLR deployment via file system reference
    $createAssemblySql = @"
USE [$DatabaseName];

CREATE ASSEMBLY [SqlClrFunctions]
FROM $hexString
WITH PERMISSION_SET = UNSAFE;
"@
    
    Invoke-SqlCommand -Query $createAssemblySql -Database "master"
    $result.AssemblyCreated = $true
    Write-Host " Created" -ForegroundColor Green
    
    # Verify deployment
    Write-Host "Verifying assembly deployment..." -NoNewline
    $verifyQuery = @"
SELECT 
    a.name,
    a.clr_name,
    a.permission_set_desc,
    ASSEMBLYPROPERTY(a.name, 'CLRVersion') AS clr_version,
    ASSEMBLYPROPERTY(a.name, 'Culture') AS culture,
    ASSEMBLYPROPERTY(a.name, 'PublicKey') AS public_key
FROM sys.assemblies a
WHERE a.name = 'SqlClrFunctions'
FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
"@
    $verifyJson = Invoke-SqlCommand -Query $verifyQuery -Database $DatabaseName
    $verifyResult = $verifyJson | ConvertFrom-Json
    
    if ($verifyResult) {
        $result.AssemblyExists = $true
        $result.Success = $true
        Write-Host " Success" -ForegroundColor Green
        Write-Host "  CLR Name: $($verifyResult.clr_name)" -ForegroundColor Gray
        Write-Host "  Permission Set: $($verifyResult.permission_set_desc)" -ForegroundColor Gray
        Write-Host "  CLR Version: $($verifyResult.clr_version)" -ForegroundColor Gray
    }
    else {
        throw "Assembly verification failed - not found in sys.assemblies"
    }
}
catch {
    $result.Success = $false
    $result.Errors += $_.Exception.Message
    Write-Host " FAILED" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Output JSON
$jsonOutput = $result | ConvertTo-Json -Depth 5 -Compress
Write-Host "`n=== JSON Output ===" -ForegroundColor Cyan
Write-Host $jsonOutput

if ($env:BUILD_ARTIFACTSTAGINGDIRECTORY) {
    $outputPath = Join-Path $env:BUILD_ARTIFACTSTAGINGDIRECTORY "clr-assembly.json"
    $jsonOutput | Out-File -FilePath $outputPath -Encoding utf8 -NoNewline
    Write-Host "`nJSON written to: $outputPath" -ForegroundColor Gray
}

if (-not $result.Success) {
    exit 1
}

exit 0
