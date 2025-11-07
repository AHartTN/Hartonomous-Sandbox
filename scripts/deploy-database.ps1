param(param(

    [Parameter(Mandatory = $true)]    [Parameter(Mandatory = $true)]

    [string]$ServerName,    [string]$ServerName,



    [Parameter(Mandatory = $true)]    [Parameter(Mandatory = $true)]

    [string]$DatabaseName,    [string]$DatabaseName,



    [string]$SqlUser,    [string]$SqlUser,

    [string]$SqlPassword,    [string]$SqlPassword,

    [string]$FilestreamPath = "$PSScriptRoot\..\data\filestream",    [string]$FilestreamPath = "$PSScriptRoot\..\data\filestream",

    [switch]$SkipClrDeployment,    [switch]$SkipClrDeployment,

    [switch]$SkipEfMigrations,    [switch]$SkipEfMigrations,

    [switch]$SkipSqlScripts,    [switch]$SkipSqlScripts,

    [switch]$SkipVerification,    [switch]$SkipVerification,

    [switch]$ForceRebuildClr,    [switch]$ForceRebuildClr,

    [ValidateSet('SAFE', 'EXTERNAL_ACCESS', 'UNSAFE')]    [ValidateSet('SAFE', 'EXTERNAL_ACCESS', 'UNSAFE')]

    [string]$ClrPermissionSet = 'SAFE'    [string]$ClrPermissionSet = 'SAFE'

))



$ErrorActionPreference = "Stop"$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

$repoRoot = Split-Path -Parent $scriptDir$repoRoot = Split-Path -Parent $scriptDir



# Import deployment functions# Import deployment functions

. "$scriptDir\deployment-functions.ps1". "$scriptDir\deployment-functions.ps1"



# Validate prerequisites# Validate prerequisites

Test-PrerequisitesTest-Prerequisites



# Build connection string# Build connection string

$connectionString = New-ConnectionString -ServerName $ServerName -DatabaseName $DatabaseName -SqlUser $SqlUser -SqlPassword $SqlPassword$connectionString = New-ConnectionString -ServerName $ServerName -DatabaseName $DatabaseName -SqlUser $SqlUser -SqlPassword $SqlPassword



Write-Host ""Write-Host ""

Write-Host "========================================" -ForegroundColor CyanWrite-Host "========================================" -ForegroundColor Cyan

Write-Host "  Hartonomous Database Deployment"Write-Host "  Hartonomous Database Deployment"

Write-Host "========================================" -ForegroundColor CyanWrite-Host "========================================" -ForegroundColor Cyan

Write-Host ""Write-Host ""

Write-Host "Configuration:" -ForegroundColor YellowWrite-Host "Configuration:" -ForegroundColor Yellow

Write-Host "  Server: $ServerName" -ForegroundColor GrayWrite-Host "  Server: $ServerName" -ForegroundColor Gray

Write-Host "  Database: $DatabaseName" -ForegroundColor GrayWrite-Host "  Database: $DatabaseName" -ForegroundColor Gray

Write-Host "  FILESTREAM Path: $FilestreamPath" -ForegroundColor GrayWrite-Host "  FILESTREAM Path: $FilestreamPath" -ForegroundColor Gray

Write-Host "  CLR Permission Set: $ClrPermissionSet" -ForegroundColor GrayWrite-Host "  CLR Permission Set: $ClrPermissionSet" -ForegroundColor Gray

Write-Host "  Skip CLR: $($SkipClrDeployment.IsPresent)" -ForegroundColor GrayWrite-Host "  Skip CLR: $($SkipClrDeployment.IsPresent)" -ForegroundColor Gray

Write-Host "  Skip EF Migrations: $($SkipEfMigrations.IsPresent)" -ForegroundColor GrayWrite-Host "  Skip EF Migrations: $($SkipEfMigrations.IsPresent)" -ForegroundColor Gray

Write-Host "  Skip SQL Scripts: $($SkipSqlScripts.IsPresent)" -ForegroundColor GrayWrite-Host "  Skip SQL Scripts: $($SkipSqlScripts.IsPresent)" -ForegroundColor Gray

Write-Host ""Write-Host ""



try {try {

    # Step 1: Test SQL Server connection    # Step 1: Test SQL Server connection

    Write-Host "Step 1: Testing SQL Server connection..." -ForegroundColor Yellow    Write-Host "Step 1: Testing SQL Server connection..." -ForegroundColor Yellow

    Test-SqlConnection -ConnectionString $connectionString    Test-SqlConnection -ConnectionString $connectionString

    Write-Host "  SUCCESS: Connected to SQL Server" -ForegroundColor Green    Write-Host "  SUCCESS: Connected to SQL Server" -ForegroundColor Green



    # Step 2: Ensure database exists    # Step 2: Ensure database exists

    Write-Host ""    Write-Host ""

    Write-Host "Step 2: Ensuring database exists..." -ForegroundColor Yellow    Write-Host "Step 2: Ensuring database exists..." -ForegroundColor Yellow

    New-DatabaseIfNotExists -ServerName $ServerName -DatabaseName $DatabaseName -SqlUser $SqlUser -SqlPassword $SqlPassword    New-DatabaseIfNotExists -ServerName $ServerName -DatabaseName $DatabaseName -SqlUser $SqlUser -SqlPassword $SqlPassword

    Write-Host "  SUCCESS: Database ready" -ForegroundColor Green    Write-Host "  SUCCESS: Database ready" -ForegroundColor Green



    # Step 3: Configure FILESTREAM    # Step 3: Configure FILESTREAM

    Write-Host ""    Write-Host ""

    Write-Host "Step 3: Configuring FILESTREAM..." -ForegroundColor Yellow    Write-Host "Step 3: Configuring FILESTREAM..." -ForegroundColor Yellow

    Initialize-Filestream -ServerName $ServerName -DatabaseName $DatabaseName -FilestreamPath $FilestreamPath -SqlUser $SqlUser -SqlPassword $SqlPassword    Initialize-Filestream -ServerName $ServerName -DatabaseName $DatabaseName -FilestreamPath $FilestreamPath -SqlUser $SqlUser -SqlPassword $SqlPassword

    Write-Host "  SUCCESS: FILESTREAM configured" -ForegroundColor Green    Write-Host "  SUCCESS: FILESTREAM configured" -ForegroundColor Green



    # Step 4: Deploy CLR assemblies    # Step 4: Deploy CLR assemblies

    if (-not $SkipClrDeployment) {    if (-not $SkipClrDeployment) {

        Write-Host ""        Write-Host ""

        Write-Host "Step 4: Deploying CLR assemblies..." -ForegroundColor Yellow        Write-Host "Step 4: Deploying CLR assemblies..." -ForegroundColor Yellow

        Deploy-ClrAssemblies -ConnectionString $connectionString -RepoRoot $repoRoot -PermissionSet $ClrPermissionSet -ForceRebuild:$ForceRebuildClr        Deploy-ClrAssemblies -ConnectionString $connectionString -RepoRoot $repoRoot -PermissionSet $ClrPermissionSet -ForceRebuild:$ForceRebuildClr

        Write-Host "  SUCCESS: CLR assemblies deployed" -ForegroundColor Green        Write-Host "  SUCCESS: CLR assemblies deployed" -ForegroundColor Green

    } else {    } else {

        Write-Host ""        Write-Host ""

        Write-Host "Step 4: Skipping CLR deployment" -ForegroundColor Yellow        Write-Host "Step 4: Skipping CLR deployment" -ForegroundColor Yellow

    }    }



    # Step 5: Apply EF Core migrations    # Step 5: Apply EF Core migrations

    if (-not $SkipEfMigrations) {    if (-not $SkipEfMigrations) {

        Write-Host ""        Write-Host ""

        Write-Host "Step 5: Applying EF Core migrations..." -ForegroundColor Yellow        Write-Host "Step 5: Applying EF Core migrations..." -ForegroundColor Yellow

        Update-EfMigrations -RepoRoot $repoRoot -ConnectionString $connectionString        Update-EfMigrations -RepoRoot $repoRoot -ConnectionString $connectionString

        Write-Host "  SUCCESS: EF migrations applied" -ForegroundColor Green        Write-Host "  SUCCESS: EF migrations applied" -ForegroundColor Green

    } else {    } else {

        Write-Host ""        Write-Host ""

        Write-Host "Step 5: Skipping EF migrations" -ForegroundColor Yellow        Write-Host "Step 5: Skipping EF migrations" -ForegroundColor Yellow

    }    }



    # Step 6: Deploy SQL scripts    # Step 6: Deploy SQL scripts

    if (-not $SkipSqlScripts) {    if (-not $SkipSqlScripts) {

        Write-Host ""        Write-Host ""

        Write-Host "Step 6: Deploying SQL scripts..." -ForegroundColor Yellow        Write-Host "Step 6: Deploying SQL scripts..." -ForegroundColor Yellow

        Deploy-SqlScripts -ConnectionString $connectionString -RepoRoot $repoRoot        Deploy-SqlScripts -ConnectionString $connectionString -RepoRoot $repoRoot

        Write-Host "  SUCCESS: SQL scripts deployed" -ForegroundColor Green        Write-Host "  SUCCESS: SQL scripts deployed" -ForegroundColor Green

    } else {    } else {

        Write-Host ""        Write-Host ""

        Write-Host "Step 6: Skipping SQL scripts" -ForegroundColor Yellow        Write-Host "Step 6: Skipping SQL scripts" -ForegroundColor Yellow

    }    }



    # Step 7: Run verification    # Step 7: Run verification

    if (-not $SkipVerification) {    if (-not $SkipVerification) {

        Write-Host ""        Write-Host ""

        Write-Host "Step 7: Running verification..." -ForegroundColor Yellow        Write-Host "Step 7: Running verification..." -ForegroundColor Yellow

        Invoke-VerificationScripts -ConnectionString $connectionString -RepoRoot $repoRoot        Invoke-VerificationScripts -ConnectionString $connectionString -RepoRoot $repoRoot

        Write-Host "  SUCCESS: Verification completed" -ForegroundColor Green        Write-Host "  SUCCESS: Verification completed" -ForegroundColor Green

    } else {    } else {

        Write-Host ""        Write-Host ""

        Write-Host "Step 7: Skipping verification" -ForegroundColor Yellow        Write-Host "Step 7: Skipping verification" -ForegroundColor Yellow

    }    }



    Write-Host ""    Write-Host ""

    Write-Host "========================================" -ForegroundColor Green    Write-Host "========================================" -ForegroundColor Green

    Write-Host "  Deployment Complete!"    Write-Host "  Deployment Complete!"

    Write-Host "========================================" -ForegroundColor Green    Write-Host "========================================" -ForegroundColor Green

    Write-Host ""    Write-Host ""

    Write-Host "Database is ready for use." -ForegroundColor Green    Write-Host "Database is ready for use." -ForegroundColor Green

    Write-Host "Connection String: $connectionString" -ForegroundColor Gray    Write-Host "Connection String: $connectionString" -ForegroundColor Gray



} catch {} catch {

    Write-Host ""    Write-Host ""

    Write-Host "========================================" -ForegroundColor Red    Write-Host "========================================" -ForegroundColor Red

    Write-Host "  Deployment Failed!"    Write-Host "  Deployment Failed!"

    Write-Host "========================================" -ForegroundColor Red    Write-Host "========================================" -ForegroundColor Red

    Write-Host ""    Write-Host ""

    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red

    Write-Host ""    Write-Host ""

    throw    throw

}}



$ErrorActionPreference = "Stop"$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

$repoRoot = Split-Path -Parent $scriptDir$repoRoot = Split-Path -Parent $scriptDir



# Import deployment functions# Import deployment functions

. "$scriptDir\deployment-functions.ps1". "$scriptDir\deployment-functions.ps1"



# Validate prerequisites# Validate prerequisites

Test-PrerequisitesTest-Prerequisites



# Build connection string# Build connection string

$connectionString = New-ConnectionString -ServerName $ServerName -DatabaseName $DatabaseName -SqlUser $SqlUser -SqlPassword $SqlPassword$connectionString = New-ConnectionString -ServerName $ServerName -DatabaseName $DatabaseName -SqlUser $SqlUser -SqlPassword $SqlPassword



Write-Host ""Write-Host ""

Write-Host "========================================" -ForegroundColor CyanWrite-Host "========================================" -ForegroundColor Cyan

Write-Host "  Hartonomous Database Deployment"Write-Host "  Hartonomous Database Deployment"

Write-Host "========================================" -ForegroundColor CyanWrite-Host "========================================" -ForegroundColor Cyan

Write-Host ""Write-Host ""

Write-Host "Configuration:" -ForegroundColor YellowWrite-Host "Configuration:" -ForegroundColor Yellow

Write-Host "  Server: $ServerName" -ForegroundColor GrayWrite-Host "  Server: $ServerName" -ForegroundColor Gray

Write-Host "  Database: $DatabaseName" -ForegroundColor GrayWrite-Host "  Database: $DatabaseName" -ForegroundColor Gray

Write-Host "  FILESTREAM Path: $FilestreamPath" -ForegroundColor GrayWrite-Host "  FILESTREAM Path: $FilestreamPath" -ForegroundColor Gray

Write-Host "  CLR Permission Set: $ClrPermissionSet" -ForegroundColor GrayWrite-Host "  CLR Permission Set: $ClrPermissionSet" -ForegroundColor Gray

Write-Host "  Skip CLR: $($SkipClrDeployment.IsPresent)" -ForegroundColor GrayWrite-Host "  Skip CLR: $($SkipClrDeployment.IsPresent)" -ForegroundColor Gray

Write-Host "  Skip EF Migrations: $($SkipEfMigrations.IsPresent)" -ForegroundColor GrayWrite-Host "  Skip EF Migrations: $($SkipEfMigrations.IsPresent)" -ForegroundColor Gray

Write-Host "  Skip SQL Scripts: $($SkipSqlScripts.IsPresent)" -ForegroundColor GrayWrite-Host "  Skip SQL Scripts: $($SkipSqlScripts.IsPresent)" -ForegroundColor Gray

Write-Host ""Write-Host ""



try {try {

    # Step 1: Test SQL Server connection    # Step 1: Test SQL Server connection

    Write-Host "Step 1: Testing SQL Server connection..." -ForegroundColor Yellow    Write-Host "Step 1: Testing SQL Server connection..." -ForegroundColor Yellow

    Test-SqlConnection -ConnectionString $connectionString    Test-SqlConnection -ConnectionString $connectionString

    Write-Host "  SUCCESS: Connected to SQL Server" -ForegroundColor Green    Write-Host "  SUCCESS: Connected to SQL Server" -ForegroundColor Green



    # Step 2: Ensure database exists    # Step 2: Ensure database exists

    Write-Host ""    Write-Host ""

    Write-Host "Step 2: Ensuring database exists..." -ForegroundColor Yellow    Write-Host "Step 2: Ensuring database exists..." -ForegroundColor Yellow

    New-DatabaseIfNotExists -ServerName $ServerName -DatabaseName $DatabaseName -SqlUser $SqlUser -SqlPassword $SqlPassword    New-DatabaseIfNotExists -ServerName $ServerName -DatabaseName $DatabaseName -SqlUser $SqlUser -SqlPassword $SqlPassword

    Write-Host "  SUCCESS: Database ready" -ForegroundColor Green    Write-Host "  SUCCESS: Database ready" -ForegroundColor Green



    # Step 3: Configure FILESTREAM    # Step 3: Configure FILESTREAM

    Write-Host ""    Write-Host ""

    Write-Host "Step 3: Configuring FILESTREAM..." -ForegroundColor Yellow    Write-Host "Step 3: Configuring FILESTREAM..." -ForegroundColor Yellow

    Initialize-Filestream -ServerName $ServerName -DatabaseName $DatabaseName -FilestreamPath $FilestreamPath -SqlUser $SqlUser -SqlPassword $SqlPassword    Initialize-Filestream -ServerName $ServerName -DatabaseName $DatabaseName -FilestreamPath $FilestreamPath -SqlUser $SqlUser -SqlPassword $SqlPassword

    Write-Host "  SUCCESS: FILESTREAM configured" -ForegroundColor Green    Write-Host "  SUCCESS: FILESTREAM configured" -ForegroundColor Green



    # Step 4: Deploy CLR assemblies    # Step 4: Deploy CLR assemblies

    if (-not $SkipClrDeployment) {    if (-not $SkipClrDeployment) {

        Write-Host ""        Write-Host ""

        Write-Host "Step 4: Deploying CLR assemblies..." -ForegroundColor Yellow        Write-Host "Step 4: Deploying CLR assemblies..." -ForegroundColor Yellow

        Deploy-ClrAssemblies -ConnectionString $connectionString -RepoRoot $repoRoot -PermissionSet $ClrPermissionSet -ForceRebuild:$ForceRebuildClr        Deploy-ClrAssemblies -ConnectionString $connectionString -RepoRoot $repoRoot -PermissionSet $ClrPermissionSet -ForceRebuild:$ForceRebuildClr

        Write-Host "  SUCCESS: CLR assemblies deployed" -ForegroundColor Green        Write-Host "  SUCCESS: CLR assemblies deployed" -ForegroundColor Green

    } else {    } else {

        Write-Host ""        Write-Host ""

        Write-Host "Step 4: Skipping CLR deployment" -ForegroundColor Yellow        Write-Host "Step 4: Skipping CLR deployment" -ForegroundColor Yellow

    }    }



    # Step 5: Apply EF Core migrations    # Step 5: Apply EF Core migrations

    if (-not $SkipEfMigrations) {    if (-not $SkipEfMigrations) {

        Write-Host ""        Write-Host ""

        Write-Host "Step 5: Applying EF Core migrations..." -ForegroundColor Yellow        Write-Host "Step 5: Applying EF Core migrations..." -ForegroundColor Yellow

        Update-EfMigrations -RepoRoot $repoRoot -ConnectionString $connectionString        Update-EfMigrations -RepoRoot $repoRoot -ConnectionString $connectionString

        Write-Host "  SUCCESS: EF migrations applied" -ForegroundColor Green        Write-Host "  SUCCESS: EF migrations applied" -ForegroundColor Green

    } else {    } else {

        Write-Host ""        Write-Host ""

        Write-Host "Step 5: Skipping EF migrations" -ForegroundColor Yellow        Write-Host "Step 5: Skipping EF migrations" -ForegroundColor Yellow

    }    }



    # Step 6: Deploy SQL scripts    # Step 6: Deploy SQL scripts

    if (-not $SkipSqlScripts) {    if (-not $SkipSqlScripts) {

        Write-Host ""        Write-Host ""

        Write-Host "Step 6: Deploying SQL scripts..." -ForegroundColor Yellow        Write-Host "Step 6: Deploying SQL scripts..." -ForegroundColor Yellow

        Deploy-SqlScripts -ConnectionString $connectionString -RepoRoot $repoRoot        Deploy-SqlScripts -ConnectionString $connectionString -RepoRoot $repoRoot

        Write-Host "  SUCCESS: SQL scripts deployed" -ForegroundColor Green        Write-Host "  SUCCESS: SQL scripts deployed" -ForegroundColor Green

    } else {    } else {

        Write-Host ""        Write-Host ""

        Write-Host "Step 6: Skipping SQL scripts" -ForegroundColor Yellow        Write-Host "Step 6: Skipping SQL scripts" -ForegroundColor Yellow

    }    }



    # Step 7: Run verification    # Step 7: Run verification

    if (-not $SkipVerification) {    if (-not $SkipVerification) {

        Write-Host ""        Write-Host ""

        Write-Host "Step 7: Running verification..." -ForegroundColor Yellow        Write-Host "Step 7: Running verification..." -ForegroundColor Yellow

        Invoke-VerificationScripts -ConnectionString $connectionString -RepoRoot $repoRoot        Invoke-VerificationScripts -ConnectionString $connectionString -RepoRoot $repoRoot

        Write-Host "  SUCCESS: Verification completed" -ForegroundColor Green        Write-Host "  SUCCESS: Verification completed" -ForegroundColor Green

    } else {    } else {

        Write-Host ""        Write-Host ""

        Write-Host "Step 7: Skipping verification" -ForegroundColor Yellow        Write-Host "Step 7: Skipping verification" -ForegroundColor Yellow

    }    }



    Write-Host ""    Write-Host ""

    Write-Host "========================================" -ForegroundColor Green    Write-Host "========================================" -ForegroundColor Green

    Write-Host "  Deployment Complete!"    Write-Host "  Deployment Complete!"

    Write-Host "========================================" -ForegroundColor Green    Write-Host "========================================" -ForegroundColor Green

    Write-Host ""    Write-Host ""

    Write-Host "Database is ready for use." -ForegroundColor Green    Write-Host "Database is ready for use." -ForegroundColor Green

    Write-Host "Connection String: $connectionString" -ForegroundColor Gray    Write-Host "Connection String: $connectionString" -ForegroundColor Gray



} catch {} catch {

    Write-Host ""    Write-Host ""

    Write-Host "========================================" -ForegroundColor Red    Write-Host "========================================" -ForegroundColor Red

    Write-Host "  Deployment Failed!"    Write-Host "  Deployment Failed!"

    Write-Host "========================================" -ForegroundColor Red    Write-Host "========================================" -ForegroundColor Red

    Write-Host ""    Write-Host ""

    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red

    Write-Host ""    Write-Host ""

    throw    throw

}}                                                                                                                                                                                                                                                                                                

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: sqlcmd is not installed or not on the PATH." -ForegroundColor Red
    exit 1
}

$useSqlAuth = -not [string]::IsNullOrWhiteSpace($SqlUser)
if ($useSqlAuth -and [string]::IsNullOrWhiteSpace($SqlPassword)) {
    Write-Host "ERROR: SqlPassword must be supplied when SqlUser is provided." -ForegroundColor Red
    exit 1
}

function Get-SqlcmdArgs {
    param(
        [string]$Database = "master",
        [string]$Query,
        [string]$InputFile
    )

    $arguments = [System.Collections.Generic.List[string]]::new()
    $arguments.AddRange([string[]]@("-S", $ServerName, "-d", $Database, "-C", "-b"))

    if ($useSqlAuth) {
        $arguments.AddRange([string[]]@("-U", $SqlUser, "-P", $SqlPassword))
    }
    else {
        $arguments.Add("-E")
    }

    if ($Query) {
        $arguments.AddRange([string[]]@("-Q", $Query))
    }

    if ($InputFile) {
        $arguments.AddRange([string[]]@("-i", $InputFile))
    }

    return $arguments.ToArray()
}

function Invoke-SqlQuery {
    param(
        [string]$Query,
        [string]$Database = "master"
    )

    $args = Get-SqlcmdArgs -Database $Database -Query $Query
    $output = & sqlcmd @args 2>&1
    return @{ ExitCode = $LASTEXITCODE; Output = $output }
}

function Invoke-SqlFile {
    param(
        [string]$FilePath,
        [string]$Database
    )

    $args = Get-SqlcmdArgs -Database $Database -InputFile $FilePath
    $output = & sqlcmd @args 2>&1
    return @{ ExitCode = $LASTEXITCODE; Output = $output }
}

function Get-SqlScalar {
    param(
        [string]$Query,
        [string]$Database = "master"
    )

    $result = Invoke-SqlQuery -Query $Query -Database $Database
    if ($result.ExitCode -ne 0) {
        throw "SQL query failed: $Query`n$($result.Output -join [Environment]::NewLine)"
    }

    foreach ($line in $result.Output) {
        $trimmed = $line.Trim()
        if ([string]::IsNullOrWhiteSpace($trimmed)) { continue }
        if ($trimmed -match '^\(.*rows affected\)$') { continue }
        if ($trimmed -match '^[-]+$') { continue }
        if ($trimmed -match '^Msg ') { continue }
        if ($trimmed -match '^[A-Za-z0-9_]+$') { continue }
        return $trimmed
    }

    return $null
}

function ConvertTo-IdempotentSqlContent {
    param(
        [string]$SqlContent
    )

    $escapedDb = $DatabaseName.Replace(']', ']]')

    $SqlContent = [System.Text.RegularExpressions.Regex]::Replace(
        $SqlContent,
        '(?im)\bUSE\s+\[?Hartonomous\]?\s*;',
        { param($match) "USE [$escapedDb];" }
    )

    $SqlContent = [System.Text.RegularExpressions.Regex]::Replace(
        $SqlContent,
        '(?im)(\b(?:ALTER|CREATE)\s+DATABASE\s+)(\[?)Hartonomous(\]?)',
        { param($match) $match.Groups[1].Value + '[' + $escapedDb + ']' }
    )

    $SqlContent = [System.Text.RegularExpressions.Regex]::Replace(
        $SqlContent,
        '(?im)\bCREATE\s+(?!OR\s+ALTER)(PROCEDURE|PROC|FUNCTION|VIEW|TRIGGER)\b',
        { param($match) 'CREATE OR ALTER ' + $match.Groups[1].Value }
    )

    return $SqlContent
}

function Invoke-IdempotentSqlFile {
    param(
        [string]$FilePath
    )

    if (-not (Test-Path $FilePath)) {
        return @{ ExitCode = 0; Output = @("File not found: $FilePath") }
    }

    $content = Get-Content -Path $FilePath -Raw
    $normalized = ConvertTo-IdempotentSqlContent -SqlContent $content

    $tempFile = [System.IO.Path]::GetTempFileName() + '.sql'
    $normalized | Out-File -FilePath $tempFile -Encoding UTF8

    try {
        return Invoke-SqlFile -FilePath $tempFile -Database $DatabaseName
    }
    finally {
        Remove-Item $tempFile -ErrorAction SilentlyContinue
    }
}

Write-Host ""
Write-Host "========================================"
Write-Host "  Hartonomous Database Deployment"
Write-Host "========================================"
Write-Host ""
Write-Host "Server: $ServerName"
Write-Host "Database: $DatabaseName"
if ($useSqlAuth) {
    Write-Host "Auth Mode: SQL Authentication"
}
else {
    Write-Host "Auth Mode: Integrated Security"
}
Write-Host ""

Write-Host "Step 1: Testing SQL Server connection..." -ForegroundColor Yellow
$connectResult = Invoke-SqlQuery -Query "SELECT @@VERSION" -Database "master"
if ($connectResult.ExitCode -ne 0) {
    Write-Host "  ERROR: Cannot connect to $ServerName" -ForegroundColor Red
    $connectResult.Output | ForEach-Object { Write-Host "    $_" }
    exit 1
}
Write-Host "  SUCCESS: Connected to SQL Server" -ForegroundColor Green

$escapedDbName = $DatabaseName.Replace("]", "]]" )

Write-Host ""
Write-Host "Step 2: Ensuring database exists..." -ForegroundColor Yellow
$createDbQuery = "IF DB_ID(N'$escapedDbName') IS NULL BEGIN EXEC(N'CREATE DATABASE [$escapedDbName]'); END;"
$createResult = Invoke-SqlQuery -Query $createDbQuery -Database "master"
if ($createResult.ExitCode -ne 0) {
    Write-Host "  ERROR: Failed to ensure database [$DatabaseName] exists" -ForegroundColor Red
    $createResult.Output | ForEach-Object { Write-Host "    $_" }
    exit 1
}
Write-Host "  SUCCESS: Database ready" -ForegroundColor Green

Write-Host ""
Write-Host "Step 2.5: Ensuring FILESTREAM filegroup..." -ForegroundColor Yellow
try {
    if (![string]::IsNullOrWhiteSpace($FilestreamPath)) {
        $targetPath = $FilestreamPath
    }
    else {
        $targetPath = "D:\\Hartonomous\\HartonomousFileStream"
    }
    $targetPath = [System.IO.Path]::GetFullPath($targetPath)

    $existingFilePath = Get-SqlScalar -Query "SELECT TOP (1) physical_name FROM [$escapedDbName].sys.database_files WHERE type = 2 AND name = N'HartonomousFileStream_File';"
    if (-not [string]::IsNullOrWhiteSpace($existingFilePath)) {
        $existingFilePath = $existingFilePath.Trim()
        if (-not [string]::IsNullOrWhiteSpace($FilestreamPath) -and ([string]::Compare($existingFilePath, $targetPath, $true) -ne 0)) {
            throw "HartonomousFileStream_File already configured at '$existingFilePath'. Drop or relocate the FILESTREAM file manually before changing to '$targetPath'."
        }
        $targetPath = $existingFilePath
    }
    else {
        $parentDir = Split-Path -Parent $targetPath
        if ([string]::IsNullOrWhiteSpace($parentDir)) {
            $parentDir = $targetPath
        }

        if (-not (Test-Path -LiteralPath $parentDir)) {
            try {
                New-Item -ItemType Directory -Path $parentDir -Force | Out-Null
            }
            catch {
                if (-not (Test-Path -LiteralPath $parentDir)) {
                    throw "Unable to create directory '$parentDir' for FILESTREAM container. Run in an elevated session or create the folder manually and grant the SQL Server service account access."
                }
            }
        }

        if (Test-Path -LiteralPath $targetPath) {
            try {
                Remove-Item -LiteralPath $targetPath -Recurse -Force
            }
            catch {
                if (Test-Path -LiteralPath $targetPath) {
                    throw "Remove or rename existing directory '$targetPath' before configuring FILESTREAM, or specify a different -FilestreamPath."
                }
            }
        }
    }

    $escapedFilePath = $targetPath.TrimEnd('\\').Replace("'", "''")

    $filestreamSql = @"
DECLARE @fgExists bit;
SELECT @fgExists = CASE WHEN EXISTS (SELECT 1 FROM [$escapedDbName].sys.filegroups WHERE type = 'FD' AND name = N'HartonomousFileStream') THEN 1 ELSE 0 END;
IF @fgExists = 0
BEGIN
    ALTER DATABASE [$escapedDbName] ADD FILEGROUP HartonomousFileStream CONTAINS FILESTREAM;
END;

IF NOT EXISTS (
    SELECT 1 FROM [$escapedDbName].sys.database_files
    WHERE type = 2 AND name = N'HartonomousFileStream_File'
)
BEGIN
    ALTER DATABASE [$escapedDbName] ADD FILE (NAME = N'HartonomousFileStream_File', FILENAME = N'$escapedFilePath') TO FILEGROUP HartonomousFileStream;
END
ELSE
BEGIN
    DECLARE @existingPath nvarchar(4000) = (SELECT physical_name FROM [$escapedDbName].sys.database_files WHERE type = 2 AND name = N'HartonomousFileStream_File');
    IF LOWER(@existingPath) <> LOWER(N'$escapedFilePath')
    BEGIN
        THROW 51001, 'HartonomousFileStream_File already configured with different path. Update manually if relocation is required.', 1;
    END;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.database_filestream_options
    WHERE database_id = DB_ID(N'$escapedDbName')
      AND directory_name = N'HartonomousFS'
)
BEGIN
    ALTER DATABASE [$escapedDbName]
        SET FILESTREAM ( NON_TRANSACTED_ACCESS = FULL, DIRECTORY_NAME = N'HartonomousFS');
END;
"@

    $filestreamResult = Invoke-SqlQuery -Query $filestreamSql -Database "master"
    if ($filestreamResult.ExitCode -ne 0) {
        throw ($filestreamResult.Output -join [Environment]::NewLine)
    }

    Write-Host "  SUCCESS: FILESTREAM filegroup ready (Path: $targetPath)" -ForegroundColor Green
}
catch {
    Write-Host "  ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$connectionString = if ($useSqlAuth) {
    "Server=$ServerName;Database=$DatabaseName;User ID=$SqlUser;Password=$SqlPassword;TrustServerCertificate=True;MultipleActiveResultSets=True"
} else {
    "Server=$ServerName;Database=$DatabaseName;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
}

$previousConnectionString = $env:ConnectionStrings__HartonomousDb
$env:ConnectionStrings__HartonomousDb = $connectionString

try {
    Write-Host ""
    Write-Host "Step 3: Deploying SQL CLR assets..." -ForegroundColor Yellow

    Write-Host "  Enabling CLR integration..." -NoNewline
    $enableClrScript = "EXEC sp_configure 'show advanced options', 1; RECONFIGURE; EXEC sp_configure 'clr enabled', 1; RECONFIGURE;"
    $clrEnableResult = Invoke-SqlQuery -Query $enableClrScript -Database "master"
    if ($clrEnableResult.ExitCode -ne 0) {
        Write-Host " FAILED" -ForegroundColor Red
        $clrEnableResult.Output | ForEach-Object { Write-Host "    $_" }
        exit 1
    }
    Write-Host " OK" -ForegroundColor Green

    Write-Host "  Disabling CLR strict security..." -NoNewline
    $disableStrictQuery = "EXEC sp_configure 'clr strict security', 0; RECONFIGURE;"
    $disableStrictResult = Invoke-SqlQuery -Query $disableStrictQuery -Database "master"
    if ($disableStrictResult.ExitCode -ne 0) {
        Write-Host " FAILED" -ForegroundColor Red
        $disableStrictResult.Output | ForEach-Object { Write-Host "    $_" }
        exit 1
    }
    Write-Host " OK" -ForegroundColor Green

    Write-Host "  Dropping CLR-bound modules..." -NoNewline
    $dropClrBindingsScript = @"
DECLARE @assemblyName sysname = N'SqlClrFunctions';
DECLARE @assemblyId INT = (SELECT assembly_id FROM sys.assemblies WHERE name = @assemblyName);

IF @assemblyId IS NULL
BEGIN
    PRINT 'Assembly not present; nothing to drop.';
END
ELSE
BEGIN
    DECLARE @drops TABLE
    (
        RowId INT IDENTITY(1,1),
        SortOrder INT NOT NULL,
        Summary NVARCHAR(200) NOT NULL,
        Command NVARCHAR(MAX) NOT NULL
    );

    INSERT INTO @drops (SortOrder, Summary, Command)
    SELECT
        CASE
            WHEN o.type IN ('TR', 'TA') THEN 3
            WHEN o.type IN ('PC', 'P') THEN 2
            ELSE 1
        END AS SortOrder,
        o.type_desc + ' ' + QUOTENAME(s.name) + '.' + QUOTENAME(o.name) AS Summary,
        dropInfo.Command
    FROM sys.assembly_modules am
    INNER JOIN sys.objects o ON am.object_id = o.object_id
    INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
    CROSS APPLY (
        SELECT
            CASE o.type
                WHEN 'AF' THEN 'DROP AGGREGATE ' + QUOTENAME(s.name) + '.' + QUOTENAME(o.name) + ';'
                WHEN 'FS' THEN 'DROP FUNCTION ' + QUOTENAME(s.name) + '.' + QUOTENAME(o.name) + ';'
                WHEN 'FT' THEN 'DROP FUNCTION ' + QUOTENAME(s.name) + '.' + QUOTENAME(o.name) + ';'
                WHEN 'TF' THEN 'DROP FUNCTION ' + QUOTENAME(s.name) + '.' + QUOTENAME(o.name) + ';'
                WHEN 'IF' THEN 'DROP FUNCTION ' + QUOTENAME(s.name) + '.' + QUOTENAME(o.name) + ';'
                WHEN 'PC' THEN 'DROP PROCEDURE ' + QUOTENAME(s.name) + '.' + QUOTENAME(o.name) + ';'
                WHEN 'P' THEN 'DROP PROCEDURE ' + QUOTENAME(s.name) + '.' + QUOTENAME(o.name) + ';'
                WHEN 'TR' THEN 'DROP TRIGGER ' + QUOTENAME(s.name) + '.' + QUOTENAME(o.name) + ';'
                WHEN 'TA' THEN 'DROP TRIGGER ' + QUOTENAME(s.name) + '.' + QUOTENAME(o.name) + ';'
                ELSE NULL
            END AS Command
    ) AS dropInfo
    WHERE am.assembly_id = @assemblyId
      AND o.is_ms_shipped = 0
      AND dropInfo.Command IS NOT NULL;

    IF EXISTS (SELECT 1 FROM @drops)
    BEGIN
        DECLARE drop_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT Command, Summary
            FROM @drops
            ORDER BY SortOrder, RowId;

        DECLARE @command NVARCHAR(MAX);
        DECLARE @summary NVARCHAR(200);

        OPEN drop_cursor;
        FETCH NEXT FROM drop_cursor INTO @command, @summary;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            PRINT 'Dropping ' + @summary;
            EXEC (@command);
            FETCH NEXT FROM drop_cursor INTO @command, @summary;
        END

        CLOSE drop_cursor;
        DEALLOCATE drop_cursor;
    END
    ELSE
    BEGIN
        PRINT 'No dependent modules found.';
    END
END;
"@
    $dropClrBindingsResult = Invoke-SqlQuery -Query $dropClrBindingsScript -Database $DatabaseName
    if ($dropClrBindingsResult.ExitCode -ne 0) {
        Write-Host " FAILED" -ForegroundColor Red
        $dropClrBindingsResult.Output | ForEach-Object { Write-Host "    $_" }
        exit 1
    }
    Write-Host " OK" -ForegroundColor Green
    $dropClrBindingsResult.Output | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object {
        Write-Host "    $_"
    }

    Write-Host "  Dropping computed columns referencing CLR assembly (if any)..." -NoNewline
    $dropComputedColumnsScript = @"
IF COL_LENGTH('provenance.GenerationStreams', 'PayloadSizeBytes') IS NOT NULL
BEGIN
    PRINT 'Dropping provenance.GenerationStreams.PayloadSizeBytes';
    ALTER TABLE provenance.GenerationStreams DROP COLUMN PayloadSizeBytes;
END;
"@
    $dropComputedResult = Invoke-SqlQuery -Query $dropComputedColumnsScript -Database $DatabaseName
    if ($dropComputedResult.ExitCode -ne 0) {
        Write-Host " FAILED" -ForegroundColor Red
        $dropComputedResult.Output | ForEach-Object { Write-Host "    $_" }
        exit 1
    }
    Write-Host " OK" -ForegroundColor Green
    $dropComputedResult.Output | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object {
        Write-Host "    $_"
    }

    Write-Host "  Dropping provenance.AtomicStream type (if unused)..." -NoNewline
    $dropTypeQuery = @"
DECLARE @typeId INT = TYPE_ID(N'provenance.AtomicStream');
IF @typeId IS NULL
BEGIN
    PRINT 'Type does not exist.';
END
ELSE IF EXISTS (SELECT 1 FROM sys.columns WHERE user_type_id = @typeId)
BEGIN
    PRINT 'Type referenced by existing columns; skipping drop.';
END
ELSE
BEGIN
    DROP TYPE provenance.AtomicStream;
    PRINT 'Type dropped.';
END;
"@
    $dropTypeResult = Invoke-SqlQuery -Query $dropTypeQuery -Database $DatabaseName
    if ($dropTypeResult.ExitCode -ne 0) {
        Write-Host " FAILED" -ForegroundColor Red
        $dropTypeResult.Output | ForEach-Object { Write-Host "    $_" }
        exit 1
    }
    Write-Host " OK" -ForegroundColor Green

    Write-Host "  Building SqlClrFunctions.dll..." -NoNewline
    $clrProjectPath = Join-Path $repoRoot "src\SqlClr\SqlClrFunctions.csproj"
    Push-Location $repoRoot
    dotnet build $clrProjectPath -c Release -v minimal 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host " FAILED" -ForegroundColor Red
        Pop-Location
        exit 1
    }
    Pop-Location
    Write-Host " OK" -ForegroundColor Green

    $assemblyPath = Join-Path $repoRoot "src\SqlClr\bin\Release\SqlClrFunctions.dll"
    if (-not (Test-Path $assemblyPath)) {
        Write-Host "  ERROR: Assembly not found at $assemblyPath" -ForegroundColor Red
        exit 1
    }

    $assemblyBytes = [System.IO.File]::ReadAllBytes($assemblyPath)
    $hexBuilder = New-Object System.Text.StringBuilder($assemblyBytes.Length * 2)
    foreach ($b in $assemblyBytes) {
        [void]$hexBuilder.AppendFormat("{0:X2}", $b)
    }
    $assemblyHex = $hexBuilder.ToString()

    Write-Host "  Deploying CLR assembly..." -NoNewline
    $clrScript = @"
USE [$DatabaseName];
DECLARE @assemblyBits VARBINARY(MAX) = 0x$assemblyHex;

IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'SqlClrFunctions')
BEGIN
    DECLARE @alterHadUnchecked BIT = 0;

    BEGIN TRY
        ALTER ASSEMBLY SqlClrFunctions FROM @assemblyBits WITH UNCHECKED DATA;
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() = 6288
        BEGIN
            SET @alterHadUnchecked = 1;
            PRINT 'ALTER ASSEMBLY completed with unchecked assembly data marks. Running DBCC to clear flags.';
        END
        ELSE IF ERROR_NUMBER() = 6285
        BEGIN
            PRINT 'ALTER ASSEMBLY skipped: deployed bits already match current assembly (MVID unchanged).';
        END
        ELSE
        BEGIN
            THROW;
        END
    END CATCH;

    IF @alterHadUnchecked = 0
    BEGIN
        PRINT 'ALTER ASSEMBLY completed without unchecked data flags.';
    END
END
ELSE
BEGIN
    CREATE ASSEMBLY SqlClrFunctions FROM @assemblyBits WITH PERMISSION_SET = SAFE;
END;

DECLARE @unchecked TABLE
(
    SchemaName sysname NOT NULL,
    ObjectName sysname NOT NULL,
    ObjectType NVARCHAR(60) NOT NULL
);

INSERT INTO @unchecked (SchemaName, ObjectName, ObjectType)
SELECT s.name, o.name, o.type_desc
FROM sys.objects o
INNER JOIN sys.schemas s ON o.schema_id = s.schema_id
LEFT JOIN sys.tables t ON o.object_id = t.object_id
LEFT JOIN sys.views v ON o.object_id = v.object_id
WHERE o.type IN ('U', 'V')
    AND (
                (o.type = 'U' AND t.has_unchecked_assembly_data = 1)
                OR
                (o.type = 'V' AND v.has_unchecked_assembly_data = 1)
            );

DECLARE @rowcount INT = (SELECT COUNT(1) FROM @unchecked);

IF @rowcount > 0
BEGIN
    DECLARE @schemaName sysname;
    DECLARE @objectName sysname;
    DECLARE @command NVARCHAR(MAX);

    WHILE EXISTS (SELECT 1 FROM @unchecked)
    BEGIN
        SELECT TOP (1)
               @schemaName = SchemaName,
               @objectName = ObjectName
        FROM @unchecked
        ORDER BY SchemaName, ObjectName;

        SET @command = N'DBCC CHECKTABLE (' + N'''' + QUOTENAME(@schemaName) + N'.' + QUOTENAME(@objectName) + N'''' + N') WITH ALL_ERRORMSGS;';
        PRINT 'Running ' + @command;
        EXEC (@command);

        DELETE FROM @unchecked WHERE SchemaName = @schemaName AND ObjectName = @objectName;
    END
END
ELSE
BEGIN
    PRINT 'No tables or views require DBCC CHECKTABLE after ALTER ASSEMBLY.';
END;
"@
    $tempFile = [System.IO.Path]::GetTempFileName() + ".sql"
    $clrScript | Out-File -FilePath $tempFile -Encoding UTF8
    $clrResult = Invoke-SqlFile -FilePath $tempFile -Database $DatabaseName
    Remove-Item $tempFile -ErrorAction SilentlyContinue
    if ($clrResult.ExitCode -ne 0) {
        Write-Host " FAILED" -ForegroundColor Red
        $clrResult.Output | ForEach-Object { Write-Host "    $_" }
        exit 1
    }
    Write-Host " OK" -ForegroundColor Green

    $atomicStreamTypeScript = Join-Path $repoRoot "sql\types\provenance.AtomicStream.sql"
    if (Test-Path $atomicStreamTypeScript) {
        Write-Host "  Binding provenance.AtomicStream type..." -NoNewline
        $typeResult = Invoke-IdempotentSqlFile -FilePath $atomicStreamTypeScript
        if ($typeResult.ExitCode -ne 0) {
            Write-Host " FAILED" -ForegroundColor Red
            $typeResult.Output | ForEach-Object { Write-Host "    $_" }
            exit 1
        }
        Write-Host " OK" -ForegroundColor Green
    }
    else {
        Write-Host "  WARNING: sql/types/provenance.AtomicStream.sql not found, type not refreshed." -ForegroundColor Yellow
    }

    $componentStreamTypeScript = Join-Path $repoRoot "sql\types\provenance.ComponentStream.sql"
    if (Test-Path $componentStreamTypeScript) {
        Write-Host "  Binding provenance.ComponentStream type..." -NoNewline
        $componentTypeResult = Invoke-IdempotentSqlFile -FilePath $componentStreamTypeScript
        if ($componentTypeResult.ExitCode -ne 0) {
            Write-Host " FAILED" -ForegroundColor Red
            $componentTypeResult.Output | ForEach-Object { Write-Host "    $_" }
            exit 1
        }
        Write-Host " OK" -ForegroundColor Green
    }
    else {
        Write-Host "  WARNING: sql/types/provenance.ComponentStream.sql not found, type not refreshed." -ForegroundColor Yellow
    }

    Write-Host ""
    Write-Host "Step 4: Applying EF Core migrations..." -ForegroundColor Yellow
    $dataProjectPath = Join-Path $repoRoot "src\Hartonomous.Data"

    if (-not (Test-Path $dataProjectPath)) {
        Write-Host "  ERROR: Data project not found" -ForegroundColor Red
        exit 1
    }

    Push-Location $repoRoot
    try {
        $efArgs = @("ef", "database", "update", "--project", $dataProjectPath, "--connection", $connectionString)
        dotnet @efArgs
        if ($LASTEXITCODE -ne 0) {
            throw "Migration failed"
        }
        Write-Host "  SUCCESS: Migrations applied" -ForegroundColor Green
    }
    catch {
        Write-Host "  ERROR: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
    finally {
        Pop-Location
    }

    Write-Host ""
    Write-Host "Step 5: Deploying tables..." -ForegroundColor Yellow

    $sqlTableDir = Join-Path $repoRoot "sql\tables"
    if (Test-Path $sqlTableDir) {
        $tableFiles = Get-ChildItem -Path $sqlTableDir -Filter "*.sql" | Sort-Object Name
        foreach ($file in $tableFiles) {
            Write-Host "  Ensuring $($file.Name)..." -NoNewline
            $tableResult = Invoke-IdempotentSqlFile -FilePath $file.FullName
            if ($tableResult.ExitCode -eq 0) {
                Write-Host " OK" -ForegroundColor Green
            }
            else {
                Write-Host " FAILED" -ForegroundColor Red
                $tableResult.Output | ForEach-Object { Write-Host "    $_" }
                exit 1
            }
        }
    }
    else {
        Write-Host "  WARNING: sql/tables directory not found, skipping." -ForegroundColor Yellow
    }

    Write-Host ""
    Write-Host "Step 6: Deploying stored procedures and functions..." -ForegroundColor Yellow

    $sqlProcDir = Join-Path $repoRoot "sql\procedures"
    $procFiles = Get-ChildItem -Path $sqlProcDir -Filter "*.sql" | Sort-Object Name

    $deployed = 0
    $failed = 0

    foreach ($file in $procFiles) {
        Write-Host "  Deploying $($file.Name)..." -NoNewline

        $procResult = Invoke-IdempotentSqlFile -FilePath $file.FullName

        if ($procResult.ExitCode -eq 0) {
            Write-Host " OK" -ForegroundColor Green
            $deployed++
        }
        else {
            Write-Host " FAILED" -ForegroundColor Red
            $procResult.Output | ForEach-Object { Write-Host "    $_" }
            $failed++
        }

    }

    Write-Host ""
    Write-Host "  Summary: $deployed deployed, $failed failed"
    if ($failed -gt 0) {
        Write-Host "  ERROR: One or more stored procedures failed." -ForegroundColor Red
        exit 1
    }

    Write-Host ""
    Write-Host "Step 7: Applying configuration scripts..." -ForegroundColor Yellow
    $configurationScripts = @(
        "EnableQueryStore.sql",
        "Setup_FILESTREAM.sql",
        "Optimize_ColumnstoreCompression.sql",
        "Temporal_Tables_Evaluation.sql",
        "Predict_Integration.sql"
    )

    foreach ($scriptName in $configurationScripts) {
        $scriptPath = Join-Path $repoRoot "sql\$scriptName"
        if (-not (Test-Path $scriptPath)) {
            Write-Host "  Skipping $scriptName (not found)" -ForegroundColor Yellow
            continue
        }

        Write-Host "  Running $scriptName..." -NoNewline
        $configResult = Invoke-IdempotentSqlFile -FilePath $scriptPath
        if ($configResult.ExitCode -eq 0) {
            Write-Host " OK" -ForegroundColor Green
        }
        else {
            Write-Host " FAILED" -ForegroundColor Red
            $configResult.Output | ForEach-Object { Write-Host "    $_" }
            exit 1
        }
    }

    Write-Host ""
    Write-Host "Step 8: Seeding reference data..." -ForegroundColor Yellow
    $seedScripts = @(
        "Ingest_Models.sql"
    )

    foreach ($seed in $seedScripts) {
        $seedPath = Join-Path $repoRoot "sql\$seed"
        if (-not (Test-Path $seedPath)) {
            Write-Host "  Skipping $seed (not found)" -ForegroundColor Yellow
            continue
        }

        Write-Host "  Running $seed..." -NoNewline
        $seedResult = Invoke-IdempotentSqlFile -FilePath $seedPath
        if ($seedResult.ExitCode -eq 0) {
            Write-Host " OK" -ForegroundColor Green
        }
        else {
            Write-Host " FAILED" -ForegroundColor Red
            $seedResult.Output | ForEach-Object { Write-Host "    $_" }
            exit 1
        }
    }

    Write-Host ""
    Write-Host "Step 9: Running verification scripts..." -ForegroundColor Yellow
    $verificationDir = Join-Path $repoRoot "sql\verification"
    if (Test-Path $verificationDir) {
        $verificationFiles = Get-ChildItem -Path $verificationDir -Filter "*.sql" | Sort-Object Name
        foreach ($verification in $verificationFiles) {
            Write-Host "  Executing $($verification.Name)..." -NoNewline
            $verificationResult = Invoke-IdempotentSqlFile -FilePath $verification.FullName
            if ($verificationResult.ExitCode -eq 0) {
                Write-Host " OK" -ForegroundColor Green
            }
            else {
                Write-Host " FAILED" -ForegroundColor Red
                $verificationResult.Output | ForEach-Object { Write-Host "    $_" }
                exit 1
            }
        }
    }
    else {
        Write-Host "  WARNING: sql/verification directory not found, skipping." -ForegroundColor Yellow
    }

}
finally {
    if ($null -eq $previousConnectionString) {
        Remove-Item Env:ConnectionStrings__HartonomousDb -ErrorAction SilentlyContinue
    }
    else {
        $env:ConnectionStrings__HartonomousDb = $previousConnectionString
    }
}

Write-Host ""
Write-Host "========================================"
Write-Host "  Deployment Complete"
Write-Host "========================================"
