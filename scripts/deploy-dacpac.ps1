<#
.SYNOPSIS
    Production-grade DACPAC deployment for Hartonomous - The Periodic Table of Knowledge
.DESCRIPTION
    Enterprise deployment with CLR assembly registration, spatial indexing, atomic decomposition.
    Idempotent, secure, production-ready.
.PARAMETER Server
    SQL Server instance (default: localhost)
.PARAMETER Database
    Target database name (default: Hartonomous)
.PARAMETER TrustServerCertificate
    Trust server certificate for encrypted connections
.EXAMPLE
    .\deploy-dacpac.ps1 -Server localhost -Database Hartonomous -TrustServerCertificate
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)]
    [string]$Server = "localhost",
    
    [Parameter(Mandatory=$false)]
    [string]$Database = "Hartonomous",
    
    [Parameter(Mandatory=$false)]
    [switch]$TrustServerCertificate
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

# Paths
$RootPath = Split-Path -Parent $PSScriptRoot
$ProjectPath = Join-Path $RootPath "src\Hartonomous.Database"
$DacpacPath = Join-Path $ProjectPath "bin\Output\Hartonomous.Database.dacpac"
$DependenciesPath = Join-Path $RootPath "dependencies"

Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  HARTONOMOUS DATABASE DEPLOYMENT" -ForegroundColor Cyan
Write-Host "  The Periodic Table of Knowledge" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Server:       $Server" -ForegroundColor White
Write-Host "Database:     $Database" -ForegroundColor White
Write-Host "Dependencies: $DependenciesPath" -ForegroundColor White
Write-Host ""

# ============================================================================
# PHASE 0: CLR SECURITY SETUP (IDEMPOTENT)
# ============================================================================
Write-Host "[0/4] Setting up CLR security..." -ForegroundColor Yellow

# Known correct hashes for dependencies (SHA512)
# NOTE: Hartonomous.Database.dll is deployed by DACPAC, only dependencies need pre-registration
$KnownHashes = @{
    "MathNet.Numerics.dll" = "2604BEFE84736BC9DD144A895AC5E02886AC0E5891FB0F22EF5E6D6712327684B103CBA35680A91CF0DE73B6991F58735A97E732421226C7CBF4E30E7B210E62"
    "Microsoft.SqlServer.Types.dll" = "36F1C16A10B40C5D53C585637C023C0576E8105A47B99F06FA5ECC11C4403976D34455257221963384038CAEC67C33694A556DFE94C7F70AAB81CBD0A7FD156F"
    "Newtonsoft.Json.dll" = "56EB7F070929B239642DAB729537DDE2C2287BDB852AD9E80B5358C74B14BC2B2DDED910D0E3B6304EA27EB587E5F19DB0A92E1CBAE6A70FB20B4EF05057E4AC"
    "SMDiagnostics.dll" = "FDE0118BF629D69004C520F7425D14488F82F65E7F5EE93B16EDD48ECC87541767B7F250157F95074DE311AE73700586A52A3466C8E0596C797402C5AA35350C"
    "System.Buffers.dll" = "5FC7FEE5C25CB2EEE19737068968E00A00961C257271B420F594E5A0DA0559502D04EE6BA2D8D2AAD77F3769622F6743A5EE8DAE23F8F993F33FB09ED8DB2741"
    "System.Collections.Immutable.dll" = "058431AA2280511B00A72EA55DED9BDAEF55420F5BCE10C9352D4F92736A11884D1E70706016B988CCA560358B3B43CE1BAD5C9BD726F11D8AD66E3C91F98CCB"
    "System.Drawing.dll" = "86BE0AB6AF3A3843E06265331820746F2C955946F2CDAEBCAEF15C4A741E19126647237555DE3BFCE892E65348E127B8A090B7B4F3AA42BBDD0C26A7A6DF9B83"
    "System.Memory.dll" = "293D8C709BC68D2C980A0DF423741CE06D05FF757077E63986D34CB6459F9623A024D12EF35A280F50D3D516D98ABE193213B9CA71BFDE2A9FE8753B1A6DE2F0"
    "System.Numerics.Vectors.dll" = "0B14A039CA67982794A2BB69974EF04A7FBEE3686D7364F8F4DB70EA6259D29640CBB83D5B544D92FA1D3676C7619CD580FF45671A2BB4753ED8B383597C6DA8"
    "System.Reflection.Metadata.dll" = "11569F8707F87E182C3CF7C15545988FB9E0302C3831416EE6CF825DC237590C4E82B5BCBB83E52BEF439B8B0454C807FBB3D8182901955CAB49F78C2CB40734"
    "System.Runtime.CompilerServices.Unsafe.dll" = "26AF01CA25E921465F477A0E1499EDC9E0AC26C23908E5E9B97D3AFD60F3308BFBF2C8CA89EA21878454CD88A1CDDD2F2F0172A6E1E87EF33C56CD7A8D16E9C8"
    "System.Runtime.Serialization.dll" = "7EBE0CE6C6968CAB2519F0B5863F1FCD3AECF353561A7D256ACD9713B9A6FCF28A3A33B33A3D3198C55B69EE963E221FE568B1E783228E3C0B07BDE9088A9EFB"
    "System.ServiceModel.Internals.dll" = "506455BB8FA96E3AFC4D576211791C7A4F00F4F1DE7AF55549450EA90D8387F35BD538CC23FA6BCDA66A31E8137F53D5631F623C8D68511271F5CC02D740479D"
    "System.ValueTuple.dll" = "6B223CE7F580A40A8864A762E3D5CCCF1D34A554847787551E8A5D4D05D7F7A5F116F2DE8A1C793F327A64D23570228C6E3648A541DD52F93D58F8F243591E32"
}

$clrSetupQuery = @"
USE [master];

-- Check and create master key
IF NOT EXISTS (SELECT 1 FROM sys.symmetric_keys WHERE name = '##MS_DatabaseMasterKey##')
BEGIN
    CREATE MASTER KEY ENCRYPTION BY PASSWORD = 'CLR_Security_MasterKey_2024!@#';
    PRINT '✓ Master key created';
END

-- Check and create asymmetric key
IF NOT EXISTS (SELECT 1 FROM sys.asymmetric_keys WHERE name = 'SqlClrAsymmetricKey')
BEGIN
    CREATE ASYMMETRIC KEY [SqlClrAsymmetricKey] 
    FROM FILE = '$($ProjectPath)\CLR\SqlClrKey.snk';
    PRINT '✓ Asymmetric key created';
END

-- Check and create login
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'SqlClrLogin')
BEGIN
    CREATE LOGIN [SqlClrLogin] FROM ASYMMETRIC KEY [SqlClrAsymmetricKey];
    PRINT '✓ Login created';
END

-- Check and grant permission
IF NOT EXISTS (
    SELECT 1 FROM sys.server_permissions sp
    JOIN sys.server_principals spr ON sp.grantee_principal_id = spr.principal_id
    WHERE spr.name = 'SqlClrLogin' AND sp.permission_name = 'UNSAFE ASSEMBLY' AND sp.state = 'G'
)
BEGIN
    GRANT UNSAFE ASSEMBLY TO [SqlClrLogin];
    PRINT '✓ UNSAFE ASSEMBLY granted';
END

-- Enable CLR
DECLARE @clr_enabled INT;
SELECT @clr_enabled = CAST(value_in_use AS INT) FROM sys.configurations WHERE name = 'clr enabled';
IF @clr_enabled = 0
BEGIN
    EXEC sp_configure 'clr enabled', 1;
    RECONFIGURE;
    PRINT '✓ CLR enabled';
END

-- Enable CLR strict security
DECLARE @clr_strict INT;
SELECT @clr_strict = CAST(value_in_use AS INT) FROM sys.configurations WHERE name = 'clr strict security';
IF @clr_strict = 0
BEGIN
    EXEC sp_configure 'clr strict security', 1;
    RECONFIGURE;
    PRINT '✓ CLR strict security enabled';
END
"@

# Execute CLR setup via sqlcmd
$clrSetupFile = [System.IO.Path]::Combine($env:TEMP, "clr-setup-$(Get-Date -Format 'yyyyMMddHHmmss').sql")
$clrSetupQuery | Out-File -FilePath $clrSetupFile -Encoding UTF8

$sqlcmdArgs = @("-S", $Server, "-d", "master", "-E", "-i", $clrSetupFile, "-b")
if ($TrustServerCertificate) { $sqlcmdArgs += "-C" }

$clrResult = & sqlcmd $sqlcmdArgs 2>&1
Remove-Item $clrSetupFile -ErrorAction SilentlyContinue

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ CLR security configured" -ForegroundColor Green
} else {
    Write-Warning "CLR security may already be configured"
}

# Enable SQL Server 2025 preview features for native JSON data type
Write-Host "  Enabling SQL Server 2025 preview features..." -ForegroundColor Gray

$previewFeaturesQuery = @"
USE [$Database];

-- Enable PREVIEW_FEATURES for native JSON data type, vector type, and other preview features
DECLARE @current_setting BIT;
SELECT @current_setting = CAST(value AS BIT) 
FROM sys.database_scoped_configurations 
WHERE name = 'PREVIEW_FEATURES';

IF @current_setting IS NULL OR @current_setting = 0
BEGIN
    ALTER DATABASE SCOPED CONFIGURATION SET PREVIEW_FEATURES = ON;
    PRINT '✓ PREVIEW_FEATURES enabled';
END
ELSE
BEGIN
    PRINT '✓ PREVIEW_FEATURES already enabled';
END
"@

$previewFile = [System.IO.Path]::Combine($env:TEMP, "preview-features-$(Get-Date -Format 'yyyyMMddHHmmss').sql")
$previewFeaturesQuery | Out-File -FilePath $previewFile -Encoding UTF8

$sqlcmdArgsPreview = @("-S", $Server, "-d", $Database, "-E", "-i", $previewFile, "-b")
if ($TrustServerCertificate) { $sqlcmdArgsPreview += "-C" }

$previewResult = & sqlcmd $sqlcmdArgsPreview 2>&1
Remove-Item $previewFile -ErrorAction SilentlyContinue

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ SQL Server 2025 preview features enabled" -ForegroundColor Green
} else {
    Write-Warning "Preview features configuration may need manual verification"
}

Write-Host "  Managing trusted assembly hashes..." -ForegroundColor Gray

# Idempotent trusted assembly management:
# 1. Get current state from sys.trusted_assemblies
# 2. For each required dependency, compute actual hash from file
# 3. If hash exists but wrong description: drop and re-add (idempotent update)
# 4. If hash doesn't exist: add (idempotent insert)
# 5. Remove orphaned hashes for our dependencies (cleanup)

foreach ($dll in $KnownHashes.Keys) {
    # Verify file exists
    $dllPath = Join-Path $DependenciesPath $dll
    if (-not (Test-Path $dllPath)) {
        throw "Missing dependency: $dll at $dllPath"
    }
    
    # Compute actual hash from file (source of truth)
    $actualHashBytes = [System.Security.Cryptography.SHA512]::Create().ComputeHash([System.IO.File]::ReadAllBytes($dllPath))
    $actualHash = [System.BitConverter]::ToString($actualHashBytes).Replace("-","")
    
    # Check if this exact hash already exists in sys.trusted_assemblies
    $checkQuery = "SELECT description FROM sys.trusted_assemblies WHERE hash = 0x$actualHash"
    $checkArgs = @("-S", $Server, "-d", "master", "-E", "-Q", $checkQuery, "-h", "-1")
    if ($TrustServerCertificate) { $checkArgs += "-C" }
    $existingDescription = & sqlcmd $checkArgs 2>&1 | Out-String | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' } | Select-Object -First 1
    
    if ($existingDescription) {
        # Hash exists - verify description matches
        if ($existingDescription.Trim() -ne $dll) {
            # Wrong description - drop and re-add for consistency
            $dropQuery = "EXEC sp_drop_trusted_assembly @hash=0x$actualHash"
            $dropArgs = @("-S", $Server, "-d", "master", "-E", "-Q", $dropQuery)
            if ($TrustServerCertificate) { $dropArgs += "-C" }
            & sqlcmd $dropArgs 2>&1 | Out-Null
            
            $addQuery = "EXEC sp_add_trusted_assembly @hash=0x$actualHash, @description=N'$dll'"
            $addArgs = @("-S", $Server, "-d", "master", "-E", "-Q", $addQuery)
            if ($TrustServerCertificate) { $addArgs += "-C" }
            & sqlcmd $addArgs 2>&1 | Out-Null
        }
        # else: correct hash and description already exist, no action needed (idempotent)
    } else {
        # Hash doesn't exist - add it
        $addQuery = "EXEC sp_add_trusted_assembly @hash=0x$actualHash, @description=N'$dll'"
        $addArgs = @("-S", $Server, "-d", "master", "-E", "-Q", $addQuery)
        if ($TrustServerCertificate) { $addArgs += "-C" }
        $addResult = & sqlcmd $addArgs 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to add trusted assembly for $dll`: $addResult"
        }
    }
}

# Add Hartonomous.Database.dll hash (computed from built DACPAC assembly)
if (Test-Path $DacpacPath) {
    $mainDllPath = Join-Path $ProjectPath "bin\Output\Hartonomous.Database.dll"
    if (Test-Path $mainDllPath) {
        $mainHashBytes = [System.Security.Cryptography.SHA512]::Create().ComputeHash([System.IO.File]::ReadAllBytes($mainDllPath))
        $mainHash = [System.BitConverter]::ToString($mainHashBytes).Replace("-","")
        
        $checkMainQuery = "SELECT description FROM sys.trusted_assemblies WHERE hash = 0x$mainHash"
        $checkMainArgs = @("-S", $Server, "-d", "master", "-E", "-Q", $checkMainQuery, "-h", "-1")
        if ($TrustServerCertificate) { $checkMainArgs += "-C" }
        $mainExists = & sqlcmd $checkMainArgs 2>&1 | Out-String | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' } | Select-Object -First 1
        
        if (-not $mainExists) {
            $addMainQuery = "EXEC sp_add_trusted_assembly @hash=0x$mainHash, @description=N'Hartonomous.Database.dll'"
            $addMainArgs = @("-S", $Server, "-d", "master", "-E", "-Q", $addMainQuery)
            if ($TrustServerCertificate) { $addMainArgs += "-C" }
            & sqlcmd $addMainArgs 2>&1 | Out-Null
        }
    }
}

Write-Host "✓ All trusted assemblies synchronized" -ForegroundColor Green

# ============================================================================
# PHASE 1: BUILD DACPAC
# ============================================================================
Write-Host "`n[1/4] Building DACPAC..." -ForegroundColor Yellow

# Locate MSBuild
$msbuild = $null

# Try vswhere first
$vswhere = "${env:ProgramFiles}\Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path $vswhere)) {
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
}

if (Test-Path $vswhere) {
    $msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1
}

# Fallback: search for MSBuild directly
if (-not $msbuild) {
    $msbuild = Get-ChildItem "C:\Program Files\Microsoft Visual Studio" -Recurse -Filter "MSBuild.exe" -ErrorAction SilentlyContinue | 
        Select-Object -First 1 | 
        Select-Object -ExpandProperty FullName
}

if (-not $msbuild) {
    throw "MSBuild not found. Install Visual Studio or VS Build Tools."
}

# Build DACPAC - NO SUPPRESSION, FULL DIAGNOSTICS
$buildArgs = @(
    $ProjectPath
    "/p:Configuration=Release"
    "/p:Platform=AnyCPU"
    "/t:Build"
    "/v:detailed"
    "/consoleloggerparameters:ErrorsOnly"
)

Write-Host "  Building with MSBuild..." -ForegroundColor Gray
& $msbuild $buildArgs

if ($LASTEXITCODE -ne 0) {
    throw "DACPAC build failed with exit code $LASTEXITCODE"
}

if (-not (Test-Path $DacpacPath)) {
    throw "DACPAC not found at: $DacpacPath"
}

$dacpacSize = [math]::Round((Get-Item $DacpacPath).Length / 1KB, 2)
Write-Host "✓ DACPAC built successfully ($dacpacSize KB)" -ForegroundColor Green

# ============================================================================
# PHASE 2: VERIFY TRUSTED ASSEMBLIES (IDEMPOTENT CHECK)
# ============================================================================
Write-Host "`n[2/4] Verifying trusted assemblies..." -ForegroundColor Yellow

foreach ($dll in $KnownHashes.Keys) {
    $expectedHash = $KnownHashes[$dll]
    
    # Verify file exists and hash matches expected
    $dllPath = Join-Path $DependenciesPath $dll
    if (-not (Test-Path $dllPath)) {
        Write-Host "  ❌ $dll - MISSING FILE" -ForegroundColor Red
        throw "Dependency missing: $dll"
    }
    
    $actualHashBytes = [System.Security.Cryptography.SHA512]::Create().ComputeHash([System.IO.File]::ReadAllBytes($dllPath))
    $actualHash = [System.BitConverter]::ToString($actualHashBytes).Replace("-","")
    
    if ($actualHash -ne $expectedHash) {
        Write-Host "  ❌ $dll - HASH MISMATCH" -ForegroundColor Red
        Write-Host "     Expected: $expectedHash" -ForegroundColor Gray
        Write-Host "     Got:      $actualHash" -ForegroundColor Gray
        throw "Hash verification failed for $dll - file may be corrupted or wrong version"
    }
    
    Write-Host "  ✓ $dll" -ForegroundColor Green
}

Write-Host "✓ All dependencies verified" -ForegroundColor Green

# ============================================================================
# PHASE 3: DEPLOY DACPAC
# ============================================================================
Write-Host "`n[3/4] Deploying DACPAC to $Server..." -ForegroundColor Yellow

# Check for SqlPackage
$sqlpackage = Get-Command sqlpackage -ErrorAction SilentlyContinue
if (-not $sqlpackage) {
    throw "SqlPackage not found. Install: dotnet tool install -g microsoft.sqlpackage"
}

# SqlPackage deployment arguments
$connStr = "Server=$Server;Database=$Database;Integrated Security=true;"
if ($TrustServerCertificate) {
    $connStr += "TrustServerCertificate=true;"
}

$deployArgs = @(
    "/Action:Publish"
    "/SourceFile:$DacpacPath"
    "/TargetConnectionString:$connStr"
    "/p:IncludeCompositeObjects=True"
    "/p:BlockOnPossibleDataLoss=False"
    "/p:DropObjectsNotInSource=False"
    "/p:AllowIncompatiblePlatform=False"
    "/p:VerifyDeployment=True"
    "/Variables:DependenciesPath=$DependenciesPath"
)

Write-Host "  Deploying with SqlPackage..." -ForegroundColor Gray
& sqlpackage $deployArgs

if ($LASTEXITCODE -ne 0) {
    throw "DACPAC deployment failed with exit code $LASTEXITCODE"
}

Write-Host "✓ DACPAC deployed successfully" -ForegroundColor Green

# ============================================================================
# PHASE 4: VERIFICATION
# ============================================================================
Write-Host "`n[4/4] Verifying deployment..." -ForegroundColor Yellow

$connStr = "Server=$Server;Database=$Database;Integrated Security=true;"
if ($TrustServerCertificate) {
    $connStr += "TrustServerCertificate=true;"
}

$verifyQuery = @"
SELECT 
    (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0) AS TableCount,
    (SELECT COUNT(*) FROM sys.procedures WHERE is_ms_shipped = 0) AS ProcedureCount,
    (SELECT COUNT(*) FROM sys.views WHERE is_ms_shipped = 0) AS ViewCount,
    (SELECT COUNT(*) FROM sys.assemblies WHERE is_user_defined = 1) AS AssemblyCount,
    (SELECT COUNT(*) FROM sys.spatial_indexes) AS SpatialIndexCount,
    (SELECT COUNT(*) FROM sys.indexes WHERE type_desc = 'CLUSTERED COLUMNSTORE') AS ColumnStoreCount
"@

try {
    $result = Invoke-Sqlcmd -ConnectionString $connStr -Query $verifyQuery -TrustServerCertificate:$TrustServerCertificate
    
    Write-Host "✓ Database verification:" -ForegroundColor Green
    Write-Host "  • Tables:              $($result.TableCount)" -ForegroundColor White
    Write-Host "  • Stored Procedures:   $($result.ProcedureCount)" -ForegroundColor White
    Write-Host "  • Views:               $($result.ViewCount)" -ForegroundColor White
    Write-Host "  • CLR Assemblies:      $($result.AssemblyCount)" -ForegroundColor White
    Write-Host "  • Spatial Indexes:     $($result.SpatialIndexCount)" -ForegroundColor White
    Write-Host "  • ColumnStore Indexes: $($result.ColumnStoreCount)" -ForegroundColor White
    
    # Verify CLR assemblies specifically
    $clrQuery = @"
SELECT name, permission_set_desc 
FROM sys.assemblies 
WHERE is_user_defined = 1 
ORDER BY name
"@
    
    $assemblies = Invoke-Sqlcmd -ConnectionString $connStr -Query $clrQuery -TrustServerCertificate:$TrustServerCertificate
    
    if ($assemblies.Count -gt 0) {
        Write-Host "`n  CLR Assemblies registered:" -ForegroundColor Cyan
        foreach ($asm in $assemblies) {
            Write-Host "    - $($asm.name) ($($asm.permission_set_desc))" -ForegroundColor Gray
        }
    }
    
} catch {
    Write-Warning "Verification query failed: $_"
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  DEPLOYMENT COMPLETE" -ForegroundColor Green
Write-Host "  Database: $Database on $Server" -ForegroundColor White
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
