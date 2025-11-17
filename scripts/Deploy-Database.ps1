[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [string]$Server,

    [Parameter(Mandatory = $true)]
    [string]$Database,

    [Parameter(Mandatory = $true)]
    [string]$AccessToken,

    [Parameter(Mandatory = $true)]
    [string]$DacpacPath,

    [Parameter(Mandatory = $true)]
    [string]$DependenciesPath,

    [Parameter(Mandatory = $true)]
    [string]$ScriptsPath
)

function Write-Log {
    param (
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host "[$([DateTime]::Now.ToString('yyyy-MM-dd HH:mm:ss'))] $Message" -ForegroundColor $Color
}

function Invoke-SqlCmdWithLogging {
    param (
        [string]$Query,
        [string]$InputFile
    )
    
    $params = @{
        ServerInstance = $Server
        Database = $Database
        AccessToken = $AccessToken
        ConnectionTimeout = 30
        QueryTimeout = 0
        Verbose = $true
    }

    if ($PSBoundParameters.ContainsKey('InputFile')) {
        $params.InputFile = $InputFile
    } else {
        $params.Query = $Query
    }

    try {
        Invoke-SqlCmd @params
    }
    catch {
        Write-Error "SQL command failed: $_"
        exit 1
    }
}

#
# --- Main Execution ---
#

try {
    # Step 1: Enable CLR on the server
    Write-Log "Step 1: Enabling CLR Integration on server '$Server'..."
    $enableClrSql = "sp_configure 'show advanced options', 1; RECONFIGURE; sp_configure 'clr enabled', 1; RECONFIGURE;"
    Invoke-SqlCmd -ServerInstance $Server -Database "master" -Query $enableClrSql -AccessToken $AccessToken -TrustServerCertificate
    Write-Log "CLR Integration enabled successfully." -ForegroundColor Green

    # Step 2: Run pre-deployment cleanup script
    Write-Log "Step 2: Running pre-deployment cleanup script..."
    $preDeployScriptPath = Join-Path $ScriptsPath "Pre-Deployment.sql"
    Invoke-SqlCmdWithLogging -InputFile $preDeployScriptPath
    Write-Log "Pre-deployment cleanup script executed successfully." -ForegroundColor Green

    # Step 3: Deploy external dependency assemblies
    Write-Log "Step 3: Deploying external dependencies from '$DependenciesPath'..."
    $dependencyDlls = Get-ChildItem -Path $DependenciesPath -Filter *.dll | Sort-Object Name
    foreach ($dll in $dependencyDlls) {
        $assemblyName = $dll.BaseName
        Write-Log "  - Deploying assembly: $assemblyName"
        $bytes = [System.IO.File]::ReadAllBytes($dll.FullName)
        $hexString = '0x' + [System.BitConverter]::ToString($bytes).Replace('-', '')
        
        $createAssemblySql = @"
IF NOT EXISTS (SELECT * FROM sys.assemblies WHERE name = '$assemblyName')
BEGIN
    CREATE ASSEMBLY [$assemblyName] FROM $hexString WITH PERMISSION_SET = UNSAFE;
END
"@
        Invoke-SqlCmdWithLogging -Query $createAssemblySql
        Write-Log "    ✓ Deployed $assemblyName"
    }
    Write-Log "External dependencies deployed successfully." -ForegroundColor Green

    # Step 4: Deploy the DACPAC
    Write-Log "Step 4: Deploying DACPAC from '$DacpacPath'..."
    $sqlPackagePath = "C:\Program Files\Microsoft SQL Server\160\DAC\bin\SqlPackage.exe"
    $sqlPackageArgs = @(
        "/Action:Publish",
        "/SourceFile:`"$DacpacPath`"",
        "/TargetConnectionString:`"Server=$Server;Database=$Database;Authentication=Active Directory
        Password`"",
        "/p:BlockOnPossibleDataLoss=False",
        "/p:DropObjectsNotInSource=true",
        "/p:TreatVerificationErrorsAsWarnings=false",
        "/AccessToken:`"$AccessToken`""
    )
    & $sqlPackagePath $sqlPackageArgs
    if ($LASTEXITCODE -ne 0) {
        throw "SqlPackage.exe failed with exit code $LASTEXITCODE"
    }
    Write-Log "DACPAC deployed successfully." -ForegroundColor Green

    # Step 5: Set database to trustworthy
    Write-Log "Step 5: Setting database '$Database' to TRUSTWORTHY ON..."
    $setTrustworthySql = "ALTER DATABASE [$Database] SET TRUSTWORTHY ON;"
    Invoke-SqlCmdWithLogging -Query $setTrustworthySql
    Write-Log "Database set to TRUSTWORTHY." -ForegroundColor Green

    Write-Log "Database deployment completed successfully." -ForegroundColor Green
}
catch {
    Write-Error "An error occurred during the deployment process: $_"
    exit 1
}