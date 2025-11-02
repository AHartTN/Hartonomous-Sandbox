param(
    [string]$ServerName = "localhost",
    [string]$DatabaseName = "Hartonomous"
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir

Write-Host ""
Write-Host "========================================"
Write-Host "  Hartonomous Database Deployment"
Write-Host "========================================"
Write-Host ""
Write-Host "Server: $ServerName"
Write-Host "Database: $DatabaseName"
Write-Host ""

Write-Host "Step 1: Testing SQL Server connection..." -ForegroundColor Yellow
try {
    sqlcmd -S $ServerName -d master -E -C -Q "SELECT @@VERSION" -h -1 -W 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Cannot connect"
    }
    Write-Host "  SUCCESS: Connected to SQL Server" -ForegroundColor Green
}
catch {
    Write-Host "  ERROR: Cannot connect to $ServerName" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 2: Applying EF Core migrations..." -ForegroundColor Yellow
    $dataProjectPath = Join-Path $repoRoot "src\Hartonomous.Data"
    
    if (-not (Test-Path $dataProjectPath)) {
        Write-Host "  ERROR: Data project not found" -ForegroundColor Red
        exit 1
    }
    
    Push-Location $repoRoot
    try {
        dotnet ef database update --project $dataProjectPath
        if ($LASTEXITCODE -ne 0) {
            throw "Migration failed"
        }
        Write-Host "  SUCCESS: Migrations applied" -ForegroundColor Green
    }
    catch {
        Write-Host "  ERROR: $($_.Exception.Message)" -ForegroundColor Red
        Pop-Location
        exit 1
    }
    finally {
        Pop-Location
    }

Write-Host ""
Write-Host "Step 3: Deploying stored procedures..." -ForegroundColor Yellow
    
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
        
        sqlcmd -S $ServerName -d $DatabaseName -E -C -i $tempFile -b 2>&1 | Out-Null
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host " OK" -ForegroundColor Green
            $deployed++
        }
        else {
            Write-Host " FAILED" -ForegroundColor Red
            $failed++
        }
        
        Remove-Item $tempFile -ErrorAction SilentlyContinue
    }
    
    Write-Host ""
    Write-Host "  Summary: $deployed deployed, $failed failed"


    Write-Host ""
    Write-Host "Step 4: Deploying SQL CLR assembly..." -ForegroundColor Yellow
    
    Write-Host "  Enabling CLR integration..." -NoNewline
    $enableClrScript = "EXEC sp_configure 'show advanced options', 1; RECONFIGURE; EXEC sp_configure 'clr enabled', 1; RECONFIGURE;"
    sqlcmd -S $ServerName -d master -E -C -Q $enableClrScript 2>&1 | Out-Null
    Write-Host " OK" -ForegroundColor Green
    
    Write-Host "  Building SqlClrFunctions.dll..." -NoNewline
    $clrProjectPath = Join-Path $repoRoot "src\SqlClr\SqlClrFunctions.csproj"
    Push-Location $repoRoot
    dotnet build $clrProjectPath -c Release -v minimal 2>&1 | Out-Null
    Pop-Location
    Write-Host " OK" -ForegroundColor Green
    
    $assemblyPath = Join-Path $repoRoot "src\SqlClr\bin\Release\SqlClrFunctions.dll"
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
    sqlcmd -S $ServerName -d $DatabaseName -E -C -i $tempFile -b 2>&1 | Out-Null
    Remove-Item $tempFile -ErrorAction SilentlyContinue
    Write-Host " OK" -ForegroundColor Green

Write-Host ""
Write-Host "========================================"
Write-Host "  Deployment Complete"
Write-Host "========================================"
