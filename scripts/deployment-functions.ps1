function Test-Prerequisites {
    if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
        throw "ERROR: sqlcmd is not installed or not on the PATH."
    }

    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw "ERROR: dotnet CLI is not installed or not on the PATH."
    }
}

function New-ConnectionString {
    param(
        [string]$ServerName,
        [string]$DatabaseName,
        [string]$SqlUser,
        [string]$SqlPassword
    )

    $useSqlAuth = -not [string]::IsNullOrWhiteSpace($SqlUser)
    if ($useSqlAuth -and [string]::IsNullOrWhiteSpace($SqlPassword)) {
        throw "ERROR: SqlPassword must be supplied when SqlUser is provided."
    }

    if ($useSqlAuth) {
        "Server=$ServerName;Database=$DatabaseName;User ID=$SqlUser;Password=$SqlPassword;TrustServerCertificate=True;MultipleActiveResultSets=True"
    } else {
        "Server=$ServerName;Database=$DatabaseName;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
    }
}

function Get-SqlcmdArgs {
    param(
        [string]$ServerName,
        [string]$DatabaseName,
        [string]$SqlUser,
        [string]$SqlPassword,
        [string]$Database = "master",
        [string]$Query,
        [string]$InputFile
    )

    $arguments = [System.Collections.Generic.List[string]]::new()
    $arguments.AddRange([string[]]@("-S", $ServerName, "-d", $Database, "-C", "-b"))

    $useSqlAuth = -not [string]::IsNullOrWhiteSpace($SqlUser)
    if ($useSqlAuth) {
        $arguments.AddRange([string[]]@("-U", $SqlUser, "-P", $SqlPassword))
    } else {
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
        [string]$ServerName,
        [string]$DatabaseName,
        [string]$SqlUser,
        [string]$SqlPassword,
        [string]$Query,
        [string]$Database = "master"
    )

    $args = Get-SqlcmdArgs -ServerName $ServerName -DatabaseName $DatabaseName -SqlUser $SqlUser -SqlPassword $SqlPassword -Database $Database -Query $Query
    $output = & sqlcmd @args 2>&1
    return @{ ExitCode = $LASTEXITCODE; Output = $output }
}

function Invoke-SqlFile {
    param(
        [string]$ServerName,
        [string]$DatabaseName,
        [string]$SqlUser,
        [string]$SqlPassword,
        [string]$FilePath,
        [string]$Database
    )

    $args = Get-SqlcmdArgs -ServerName $ServerName -DatabaseName $DatabaseName -SqlUser $SqlUser -SqlPassword $SqlPassword -Database $Database -InputFile $FilePath
    $output = & sqlcmd @args 2>&1
    return @{ ExitCode = $LASTEXITCODE; Output = $output }
}

function Get-SqlScalar {
    param(
        [string]$ServerName,
        [string]$DatabaseName,
        [string]$SqlUser,
        [string]$SqlPassword,
        [string]$Query,
        [string]$Database = "master"
    )

    $result = Invoke-SqlQuery -ServerName $ServerName -DatabaseName $DatabaseName -SqlUser $SqlUser -SqlPassword $SqlPassword -Query $Query -Database $Database
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

function Test-SqlConnection {
    param(
        [string]$ConnectionString
    )

    $result = Invoke-SqlQuery -ServerName $ServerName -DatabaseName $DatabaseName -SqlUser $SqlUser -SqlPassword $SqlPassword -Query "SELECT @@VERSION" -Database "master"
    if ($result.ExitCode -ne 0) {
        throw "Cannot connect to SQL Server $ServerName"
    }
}

function New-DatabaseIfNotExists {
    param(
        [string]$ServerName,
        [string]$DatabaseName,
        [string]$SqlUser,
        [string]$SqlPassword
    )

    $escapedDbName = $DatabaseName.Replace("]", "]]")
    $createDbQuery = "IF DB_ID(N'$escapedDbName') IS NULL BEGIN EXEC(N'CREATE DATABASE [$escapedDbName]'); END;"
    $result = Invoke-SqlQuery -ServerName $ServerName -DatabaseName $DatabaseName -SqlUser $SqlUser -SqlPassword $SqlPassword -Query $createDbQuery -Database "master"
    if ($result.ExitCode -ne 0) {
        throw "Failed to ensure database [$DatabaseName] exists"
    }
}

function Initialize-Filestream {
    param(
        [string]$ServerName,
        [string]$DatabaseName,
        [string]$SqlUser,
        [string]$SqlPassword,
        [string]$FilestreamPath
    )

    $targetPath = [System.IO.Path]::GetFullPath($FilestreamPath)

    # Check if FILESTREAM is already configured
    $existingFilePath = Get-SqlScalar -ServerName $ServerName -DatabaseName $DatabaseName -SqlUser $SqlUser -SqlPassword $SqlPassword -Query "SELECT TOP (1) physical_name FROM [$DatabaseName].sys.database_files WHERE type = 2 AND name = N'HartonomousFileStream_File';"

    if (-not [string]::IsNullOrWhiteSpace($existingFilePath)) {
        $existingFilePath = $existingFilePath.Trim()
        if ([string]::Compare($existingFilePath, $targetPath, $true) -ne 0) {
            throw "HartonomousFileStream_File already configured at '$existingFilePath'. Drop or relocate the FILESTREAM file manually before changing to '$targetPath'."
        }
        return
    }

    # Create directory if needed
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
                throw "Unable to create directory '$parentDir' for FILESTREAM container."
            }
        }
    }

    # Remove existing directory if it exists
    if (Test-Path -LiteralPath $targetPath) {
        try {
            Remove-Item -LiteralPath $targetPath -Recurse -Force
        }
        catch {
            if (Test-Path -LiteralPath $targetPath) {
                throw "Remove or rename existing directory '$targetPath' before configuring FILESTREAM."
            }
        }
    }

    $escapedFilePath = $targetPath.TrimEnd('\\').Replace("'", "''")

    $filestreamSql = @"
DECLARE @fgExists bit;
SELECT @fgExists = CASE WHEN EXISTS (SELECT 1 FROM [$DatabaseName].sys.filegroups WHERE type = 'FD' AND name = N'HartonomousFileStream') THEN 1 ELSE 0 END;
IF @fgExists = 0
BEGIN
    ALTER DATABASE [$DatabaseName] ADD FILEGROUP HartonomousFileStream CONTAINS FILESTREAM;
END;

IF NOT EXISTS (
    SELECT 1 FROM [$DatabaseName].sys.database_files
    WHERE type = 2 AND name = N'HartonomousFileStream_File'
)
BEGIN
    ALTER DATABASE [$DatabaseName] ADD FILE (NAME = N'HartonomousFileStream_File', FILENAME = N'$escapedFilePath') TO FILEGROUP HartonomousFileStream;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.database_filestream_options
    WHERE database_id = DB_ID(N'$DatabaseName')
      AND directory_name = N'HartonomousFS'
)
BEGIN
    ALTER DATABASE [$DatabaseName]
        SET FILESTREAM ( NON_TRANSACTED_ACCESS = FULL, DIRECTORY_NAME = N'HartonomousFS');
END;
"@

    $result = Invoke-SqlQuery -ServerName $ServerName -DatabaseName $DatabaseName -SqlUser $SqlUser -SqlPassword $SqlPassword -Query $filestreamSql -Database "master"
    if ($result.ExitCode -ne 0) {
        throw ($result.Output -join [Environment]::NewLine)
    }
}

function Deploy-ClrAssemblies {
    param(
        [string]$ConnectionString,
        [string]$RepoRoot,
        [string]$PermissionSet = 'SAFE',
        [switch]$ForceRebuild
    )

    # Enable CLR integration
    $enableClrScript = "EXEC sp_configure 'show advanced options', 1; RECONFIGURE; EXEC sp_configure 'clr enabled', 1; RECONFIGURE;"
    $result = Invoke-SqlQuery -ServerName $ServerName -DatabaseName $DatabaseName -SqlUser $SqlUser -SqlPassword $SqlPassword -Query $enableClrScript -Database "master"
    if ($result.ExitCode -ne 0) {
        throw "Failed to enable CLR integration"
    }

    # Disable CLR strict security
    $disableStrictQuery = "EXEC sp_configure 'clr strict security', 0; RECONFIGURE;"
    $result = Invoke-SqlQuery -ServerName $ServerName -DatabaseName $DatabaseName -SqlUser $SqlUser -SqlPassword $SqlPassword -Query $disableStrictQuery -Database "master"
    if ($result.ExitCode -ne 0) {
        throw "Failed to disable CLR strict security"
    }

    # Build CLR assembly
    $clrProjectPath = Join-Path $RepoRoot "src\SqlClr\SqlClrFunctions.csproj"
    Push-Location $RepoRoot
    try {
        dotnet build $clrProjectPath -c Release -v minimal 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "CLR assembly build failed"
        }
    }
    finally {
        Pop-Location
    }

    $assemblyPath = Join-Path $RepoRoot "src\SqlClr\bin\Release\SqlClrFunctions.dll"
    if (-not (Test-Path $assemblyPath)) {
        throw "Assembly not found at $assemblyPath"
    }

    # Convert assembly to hex
    $assemblyBytes = [System.IO.File]::ReadAllBytes($assemblyPath)
    $hexBuilder = New-Object System.Text.StringBuilder($assemblyBytes.Length * 2)
    foreach ($b in $assemblyBytes) {
        [void]$hexBuilder.AppendFormat("{0:X2}", $b)
    }
    $assemblyHex = $hexBuilder.ToString()

    # Deploy assembly
    $clrScript = @"
USE [$DatabaseName];
DECLARE @assemblyBits VARBINARY(MAX) = 0x$assemblyHex;

IF EXISTS (SELECT 1 FROM sys.assemblies WHERE name = 'SqlClrFunctions')
BEGIN
    ALTER ASSEMBLY SqlClrFunctions FROM @assemblyBits WITH UNCHECKED DATA;
END
ELSE
BEGIN
    CREATE ASSEMBLY SqlClrFunctions FROM @assemblyBits WITH PERMISSION_SET = $PermissionSet;
END;
"@

    $tempFile = [System.IO.Path]::GetTempFileName() + ".sql"
    $clrScript | Out-File -FilePath $tempFile -Encoding UTF8

    try {
        $result = Invoke-SqlFile -ServerName $ServerName -DatabaseName $DatabaseName -SqlUser $SqlUser -SqlPassword $SqlPassword -FilePath $tempFile -Database $DatabaseName
        if ($result.ExitCode -ne 0) {
            throw "CLR assembly deployment failed"
        }
    }
    finally {
        Remove-Item $tempFile -ErrorAction SilentlyContinue
    }
}

function Update-EfMigrations {
    param(
        [string]$RepoRoot,
        [string]$ConnectionString
    )

    $dataProjectPath = Join-Path $RepoRoot "src\Hartonomous.Data"
    if (-not (Test-Path $dataProjectPath)) {
        throw "Data project not found at $dataProjectPath"
    }

    Push-Location $RepoRoot
    try {
        $efArgs = @("ef", "database", "update", "--project", $dataProjectPath, "--connection", $ConnectionString)
        dotnet @efArgs 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "EF Core migration failed"
        }
    }
    finally {
        Pop-Location
    }
}

function Deploy-SqlScripts {
    param(
        [string]$ConnectionString,
        [string]$RepoRoot
    )

    # Deploy types
    $typesDir = Join-Path $RepoRoot "sql\types"
    if (Test-Path $typesDir) {
        $typeFiles = Get-ChildItem -Path $typesDir -Filter "*.sql" -Recurse | Sort-Object FullName
        foreach ($file in $typeFiles) {
            $result = Invoke-IdempotentSqlFile -ServerName $ServerName -DatabaseName $DatabaseName -SqlUser $SqlUser -SqlPassword $SqlPassword -FilePath $file.FullName
            if ($result.ExitCode -ne 0) {
                throw "Failed to deploy type script: $($file.Name)"
            }
        }
    }

    # Deploy tables
    $tablesDir = Join-Path $RepoRoot "sql\tables"
    if (Test-Path $tablesDir) {
        $tableFiles = Get-ChildItem -Path $tablesDir -Filter "*.sql" | Sort-Object Name
        foreach ($file in $tableFiles) {
            $result = Invoke-IdempotentSqlFile -ServerName $ServerName -DatabaseName $DatabaseName -SqlUser $SqlUser -SqlPassword $SqlPassword -FilePath $file.FullName
            if ($result.ExitCode -ne 0) {
                throw "Failed to deploy table script: $($file.Name)"
            }
        }
    }

    # Deploy procedures and functions
    $proceduresDir = Join-Path $RepoRoot "sql\procedures"
    if (Test-Path $proceduresDir) {
        $procFiles = Get-ChildItem -Path $proceduresDir -Filter "*.sql" | Sort-Object Name
        foreach ($file in $procFiles) {
            $result = Invoke-IdempotentSqlFile -ServerName $ServerName -DatabaseName $DatabaseName -SqlUser $SqlUser -SqlPassword $SqlPassword -FilePath $file.FullName
            if ($result.ExitCode -ne 0) {
                throw "Failed to deploy procedure/function script: $($file.Name)"
            }
        }
    }

    # Deploy autonomous CLR functions
    $autonomousScript = Join-Path $RepoRoot "scripts\deploy-autonomous-clr-functions.sql"
    if (Test-Path $autonomousScript) {
        $result = Invoke-IdempotentSqlFile -ServerName $ServerName -DatabaseName $DatabaseName -SqlUser $SqlUser -SqlPassword $SqlPassword -FilePath $autonomousScript
        if ($result.ExitCode -ne 0) {
            throw "Failed to deploy autonomous CLR functions"
        }
    }
}

function Invoke-VerificationScripts {
    param(
        [string]$ConnectionString,
        [string]$RepoRoot
    )

    $verificationDir = Join-Path $RepoRoot "sql\verification"
    if (Test-Path $verificationDir) {
        $verificationFiles = Get-ChildItem -Path $verificationDir -Filter "*.sql" | Sort-Object Name
        foreach ($file in $verificationFiles) {
            $result = Invoke-IdempotentSqlFile -ServerName $ServerName -DatabaseName $DatabaseName -SqlUser $SqlUser -SqlPassword $SqlPassword -FilePath $file.FullName
            if ($result.ExitCode -ne 0) {
                throw "Verification failed: $($file.Name)"
            }
        }
    }
}

function Invoke-IdempotentSqlFile {
    param(
        [string]$ServerName,
        [string]$DatabaseName,
        [string]$SqlUser,
        [string]$SqlPassword,
        [string]$FilePath
    )

    if (-not (Test-Path $FilePath)) {
        return @{ ExitCode = 0; Output = @("File not found: $FilePath") }
    }

    $content = Get-Content -Path $FilePath -Raw
    $normalized = ConvertTo-IdempotentSqlContent -SqlContent $content -DatabaseName $DatabaseName

    $tempFile = [System.IO.Path]::GetTempFileName() + '.sql'
    $normalized | Out-File -FilePath $tempFile -Encoding UTF8

    try {
        return Invoke-SqlFile -ServerName $ServerName -DatabaseName $DatabaseName -SqlUser $SqlUser -SqlPassword $SqlPassword -FilePath $tempFile -Database $DatabaseName
    }
    finally {
        Remove-Item $tempFile -ErrorAction SilentlyContinue
    }
}

function ConvertTo-IdempotentSqlContent {
    param(
        [string]$SqlContent,
        [string]$DatabaseName
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