param(
    [string]$ServerName = "localhost",
    [string]$DatabaseName = "Hartonomous",
    [string]$SqlUser,
    [string]$SqlPassword
)

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
    $arguments.AddRange(@("-S", $ServerName, "-d", $Database, "-C", "-b"))

    if ($useSqlAuth) {
        $arguments.AddRange(@("-U", $SqlUser, "-P", $SqlPassword))
    }
    else {
        $arguments.Add("-E")
    }

    if ($Query) {
        $arguments.AddRange(@("-Q", $Query))
    }

    if ($InputFile) {
        $arguments.AddRange(@("-i", $InputFile))
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

$connectionString = if ($useSqlAuth) {
    "Server=$ServerName;Database=$DatabaseName;User ID=$SqlUser;Password=$SqlPassword;TrustServerCertificate=True;MultipleActiveResultSets=True"
} else {
    "Server=$ServerName;Database=$DatabaseName;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
}

$previousConnectionString = $env:ConnectionStrings__HartonomousDb
$env:ConnectionStrings__HartonomousDb = $connectionString

try {
    Write-Host ""
    Write-Host "Step 3: Applying EF Core migrations..." -ForegroundColor Yellow
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
    Write-Host "Step 4: Deploying tables..." -ForegroundColor Yellow

    $sqlTableDir = Join-Path $repoRoot "sql\tables"
    if (Test-Path $sqlTableDir) {
        $tableFiles = Get-ChildItem -Path $sqlTableDir -Filter "*.sql" | Sort-Object Name
        foreach ($file in $tableFiles) {
            Write-Host "  Ensuring $($file.Name)..." -NoNewline
            $tableResult = Invoke-SqlFile -FilePath $file.FullName -Database $DatabaseName
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
    Write-Host "Step 5: Deploying stored procedures..." -ForegroundColor Yellow

    $sqlProcDir = Join-Path $repoRoot "sql\procedures"
    $procFiles = Get-ChildItem -Path $sqlProcDir -Filter "*.sql" | Sort-Object Name

    $deployed = 0
    $failed = 0

    foreach ($file in $procFiles) {
        Write-Host "  Deploying $($file.Name)..." -NoNewline

        $sqlContent = Get-Content $file.FullName -Raw
        $sqlContent = $sqlContent -replace '\bCREATE\s+PROCEDURE\b', 'CREATE OR ALTER PROCEDURE'
        $sqlContent = $sqlContent -replace '\bCREATE\s+PROC\b', 'CREATE OR ALTER PROC'

        $tempFile = [System.IO.Path]::GetTempFileName() + ".sql"
        $sqlContent | Out-File -FilePath $tempFile -Encoding UTF8

        $procResult = Invoke-SqlFile -FilePath $tempFile -Database $DatabaseName

        if ($procResult.ExitCode -eq 0) {
            Write-Host " OK" -ForegroundColor Green
            $deployed++
        }
        else {
            Write-Host " FAILED" -ForegroundColor Red
            $procResult.Output | ForEach-Object { Write-Host "    $_" }
            $failed++
        }

        Remove-Item $tempFile -ErrorAction SilentlyContinue
    }

    Write-Host ""
    Write-Host "  Summary: $deployed deployed, $failed failed"
    if ($failed -gt 0) {
        Write-Host "  ERROR: One or more stored procedures failed." -ForegroundColor Red
        exit 1
    }

    Write-Host ""
    Write-Host "Step 6: Deploying SQL CLR assembly..." -ForegroundColor Yellow

    Write-Host "  Enabling CLR integration..." -NoNewline
    $enableClrScript = "EXEC sp_configure 'show advanced options', 1; RECONFIGURE; EXEC sp_configure 'clr enabled', 1; RECONFIGURE;"
    $clrEnableResult = Invoke-SqlQuery -Query $enableClrScript -Database "master"
    if ($clrEnableResult.ExitCode -ne 0) {
        Write-Host " FAILED" -ForegroundColor Red
        $clrEnableResult.Output | ForEach-Object { Write-Host "    $_" }
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
IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'SqlClrFunctions')
    DROP ASSEMBLY SqlClrFunctions;
CREATE ASSEMBLY SqlClrFunctions FROM 0x$assemblyHex WITH PERMISSION_SET = SAFE;
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
