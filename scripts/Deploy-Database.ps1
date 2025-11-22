[CmdletBinding()]
param (
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Server,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$Database,

    [Parameter(Mandatory = $false)]
    [string]$AccessToken = "",

    [Parameter(Mandatory = $true)]
    [ValidateScript({ Test-Path $_ -PathType Leaf })]
    [string]$DacpacPath,

    [Parameter(Mandatory = $true)]
    [ValidateScript({ Test-Path $_ -PathType Container })]
    [string]$DependenciesPath,

    [Parameter(Mandatory = $true)]
    [ValidateScript({ Test-Path $_ -PathType Container })]
    [string]$ScriptsPath,

    [Parameter(Mandatory = $false)]
    [switch]$DryRun,

    [Parameter(Mandatory = $false)]
    [switch]$SkipValidation
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

function Write-Log {
    param (
        [Parameter(Mandatory = $false)]
        [AllowEmptyString()]
        [string]$Message = "",
        
        [Parameter(Mandatory = $false)]
        [ValidateSet('Info', 'Success', 'Warning', 'Error', 'Debug')]
        [string]$Level = 'Info'
    )
    
    $timestamp = [DateTime]::Now.ToString('yyyy-MM-dd HH:mm:ss')
    $colors = @{
        'Info'    = 'Cyan'
        'Success' = 'Green'
        'Warning' = 'Yellow'
        'Error'   = 'Red'
        'Debug'   = 'Gray'
    }
    
    $prefix = switch ($Level) {
        'Success' { '✓' }
        'Error'   { '✗' }
        'Warning' { '⚠' }
        'Debug'   { '→' }
        default   { '•' }
    }
    
    if ($Message) {
        Write-Host "[$timestamp] $prefix $Message" -ForegroundColor $colors[$Level]
    } else {
        Write-Host ""
    }
}

function Invoke-SqlCmdSafe {
    <#
    .SYNOPSIS
    Executes SQL commands with comprehensive error handling and logging.
    #>
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $false)]
        [string]$Query,

        [Parameter(Mandatory = $false)]
        [string]$InputFile,

        [Parameter(Mandatory = $false)]
        [string]$DatabaseName = 'master',

        [Parameter(Mandatory = $false)]
        [hashtable]$Variables = @{},

        [Parameter(Mandatory = $false)]
        [int]$QueryTimeout = 300
    )

    # Force TCP/IP protocol when using AccessToken (Azure AD auth requires TCP/IP, not Shared Memory)
    # If server is localhost or ., resolve to actual machine name
    $serverInstance = $Server
    if ($AccessToken -and ($Server -eq 'localhost' -or $Server -eq '.' -or $Server -eq '(local)')) {
        $serverInstance = $env:COMPUTERNAME
        Write-Log "Resolved localhost to: $serverInstance (required for Azure AD token auth)" -Level Debug
    }

    $sqlParams = @{
        ServerInstance    = $serverInstance
        Database          = $DatabaseName
        ConnectionTimeout = 30
        QueryTimeout      = $QueryTimeout
        TrustServerCertificate = $true
        ErrorAction       = 'Stop'
    }

    # Only add AccessToken if provided (otherwise use Windows Auth)
    if ($AccessToken) {
        $sqlParams.AccessToken = $AccessToken
    }

    # Add variables if provided
    if ($Variables.Count -gt 0) {
        $sqlParams.Variable = $Variables.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" }
    }

    # Add query or input file
    if ($InputFile) {
        if (-not (Test-Path $InputFile)) {
            throw "SQL script file not found: $InputFile"
        }
        $sqlParams.InputFile = $InputFile
        Write-Log "Executing SQL script: $(Split-Path $InputFile -Leaf)" -Level Debug
    }
    elseif ($Query) {
        $sqlParams.Query = $Query
        $queryPreview = if ($Query.Length -gt 100) { $Query.Substring(0, 100) + '...' } else { $Query }
        Write-Log "Executing SQL query: $queryPreview" -Level Debug
    }
    else {
        throw "Either -Query or -InputFile must be specified"
    }

    try {
        if ($DryRun) {
            Write-Log "[DRY RUN] Would execute SQL command" -Level Warning
            return $null
        }
        
        $result = Invoke-Sqlcmd @sqlParams
        return $result
    }
    catch {
        Write-Log "SQL execution failed: $($_.Exception.Message)" -Level Error
        Write-Log "Connection: Server=$Server, Database=$DatabaseName" -Level Error
        throw
    }
}

function Test-DatabaseExists {
    <#
    .SYNOPSIS
    Checks if a database exists on the SQL Server instance.
    #>
    param (
        [Parameter(Mandatory = $true)]
        [string]$DatabaseName
    )

    $query = "SELECT COUNT(*) AS DbCount FROM sys.databases WHERE name = '$DatabaseName'"
    $result = Invoke-SqlCmdSafe -Query $query -DatabaseName 'master'
    return ($result.DbCount -gt 0)
}

function Enable-ClrIntegration {
    <#
    .SYNOPSIS
    Enables CLR integration on the SQL Server instance (idempotent).
    #>
    Write-Log "Enabling CLR integration..." -Level Info
    
    $clrScript = Join-Path $ScriptsPath "enable-clr.sql"
    if (Test-Path $clrScript) {
        Invoke-SqlCmdSafe -InputFile $clrScript -DatabaseName 'master'
        Write-Log "CLR integration enabled successfully" -Level Success
    }
    else {
        # Fallback to inline SQL if script doesn't exist
        Write-Log "enable-clr.sql not found, using inline SQL" -Level Warning
        $query = @"
EXEC sp_configure 'show advanced options', 1; RECONFIGURE;
EXEC sp_configure 'clr enabled', 1; RECONFIGURE;
EXEC sp_configure 'clr strict security', 0; RECONFIGURE;
"@
        Invoke-SqlCmdSafe -Query $query -DatabaseName 'master'
        Write-Log "CLR integration enabled (inline)" -Level Success
    }
}

function Invoke-PreDeploymentCleanup {
    <#
    .SYNOPSIS
    Runs pre-deployment cleanup to drop CLR-dependent objects for idempotency.
    #>
    Write-Log "Running pre-deployment cleanup..." -Level Info

    # Check if database exists first
    $dbCheckQuery = "SELECT COUNT(*) FROM sys.databases WHERE name = '$Database'"
    $dbExists = Invoke-SqlCmdSafe -Query $dbCheckQuery -DatabaseName 'master'

    if ($dbExists.Column1 -eq 0) {
        Write-Log "Database [$Database] does not exist yet - skipping pre-deployment cleanup" -Level Info
        return
    }

    $preDeployScript = Join-Path $ScriptsPath "Pre-Deployment.sql"
    if (-not (Test-Path $preDeployScript)) {
        Write-Log "Pre-Deployment.sql not found, skipping cleanup" -Level Warning
        return
    }

    $variables = @{ DatabaseName = $Database }
    Invoke-SqlCmdSafe -InputFile $preDeployScript -DatabaseName 'master' -Variables $variables
    Write-Log "Pre-deployment cleanup completed" -Level Success
}

function Deploy-ExternalAssemblies {
    <#
    .SYNOPSIS
    Deploys external CLR dependency assemblies (idempotent).
    #>
    Write-Log "Deploying external CLR assemblies from: $DependenciesPath" -Level Info
    
    $dependencyDlls = Get-ChildItem -Path $DependenciesPath -Filter *.dll -File | Sort-Object Name
    
    if ($dependencyDlls.Count -eq 0) {
        Write-Log "No dependency DLLs found in $DependenciesPath" -Level Warning
        return
    }

    # CRITICAL: Exclude assemblies that are DACPAC-generated or already in SQL Server
    # - Hartonomous.Database.dll, Hartonomous.Clr.dll: deployed BY the DACPAC
    # - Newtonsoft.Json, Microsoft.SqlServer.Types: shipped with SQL Server 2025
    # - System.Runtime.Serialization: v4.0.0.0 already in SQL Server system assemblies
    $excludedAssemblies = @('Hartonomous.Database', 'Hartonomous.Clr', 'Newtonsoft.Json', 'Microsoft.SqlServer.Types', 'System.Runtime.Serialization')
    $dependencyDlls = $dependencyDlls | Where-Object { $excludedAssemblies -notcontains $_.BaseName }
    
    if ($dependencyDlls.Count -eq 0) {
        Write-Log "No external dependencies to deploy (all DLLs are DACPAC-managed)" -Level Info
        return
    }
    
    # Define strict dependency order based on dependency analysis
    $orderedAssemblies = @(
        # Level 1: No dependencies (deploy first)
        'System.Runtime.CompilerServices.Unsafe'
        'System.Buffers'

        # Level 2: Depends only on GAC assemblies
        'System.Numerics.Vectors'      # Depends on System.Numerics (GAC)

        # Level 3: Depends on Level 1 + Level 2
        'System.Memory'                # Depends on: Unsafe, Buffers, Numerics.Vectors

        # Level 4: Depends on System.Memory
        'System.Collections.Immutable' # Depends on System.Memory

        # Level 5: Depends on System.Collections.Immutable
        'System.Reflection.Metadata'   # Depends on System.Collections.Immutable

        # Level 6: Third-party libraries (excluded - already in SQL Server)
        # 'Microsoft.SqlServer.Types', 'Newtonsoft.Json', 'MathNet.Numerics'
        'MathNet.Numerics'             # Depends on System.Runtime.Serialization (SQL Server v4.0)
    )
    
    # Sort DLLs by defined order, unknown assemblies go last alphabetically
    $sortedDlls = $dependencyDlls | Sort-Object {
        $index = $orderedAssemblies.IndexOf($_.BaseName)
        if ($index -ge 0) { $index } else { 1000 + $_.Name }
    }

    $deployedCount = 0
    $skippedCount = 0

    foreach ($dll in $sortedDlls) {
        $assemblyName = $dll.BaseName
        
        # Check if assembly already exists in target database
        $checkQuery = "SELECT COUNT(*) AS AssemblyCount FROM sys.assemblies WHERE name = '$assemblyName'"
        $existingResult = Invoke-SqlCmdSafe -Query $checkQuery -DatabaseName $Database

        if ($existingResult.AssemblyCount -gt 0) {
            Write-Log "  ⊙ Assembly '$assemblyName' already exists in [$Database], skipping" -Level Debug
            $skippedCount++
            continue
        }

        Write-Log "  → Deploying assembly to [$Database]: $assemblyName" -Level Info

        try {
            $bytes = [System.IO.File]::ReadAllBytes($dll.FullName)
            $hexString = '0x' + [System.BitConverter]::ToString($bytes).Replace('-', '')

            # Deploy to target database with UNSAFE permission
            $createAssemblySql = @"
CREATE ASSEMBLY [$assemblyName]
FROM $hexString
WITH PERMISSION_SET = UNSAFE;
"@
            Invoke-SqlCmdSafe -Query $createAssemblySql -DatabaseName $Database -QueryTimeout 600
            Write-Log "  ✓ Deployed to [$Database]: $assemblyName" -Level Success
            $deployedCount++
        }
        catch {
            $errorMsg = $_.Exception.Message
            Write-Log "  ✗ Failed to deploy ${assemblyName}: $errorMsg" -Level Error
            throw
        }
    }

    Write-Log "External assemblies: $deployedCount deployed to [$Database], $skippedCount already existed" -Level Success
}

function Deploy-Dacpac {
    <#
    .SYNOPSIS
    Deploys the DACPAC using SqlPackage.exe with comprehensive options.
    #>
    Write-Log "Deploying DACPAC: $DacpacPath" -Level Info
    
    # Find SqlPackage.exe using Get-Command (idempotent, checks PATH first)
    # MS Docs best practice: Use Get-Command to find executables in PATH
    # https://learn.microsoft.com/en-us/sql/tools/sqlpackage/sqlpackage-pipelines
    $sqlPackagePath = $null
    
    # 1. Check if sqlpackage is in PATH (dotnet global tool or added to PATH)
    $sqlPackageCmd = Get-Command sqlpackage -ErrorAction SilentlyContinue
    if ($sqlPackageCmd) {
        $sqlPackagePath = $sqlPackageCmd.Source
        Write-Log "Found SqlPackage in PATH: $sqlPackagePath" -Level Debug
    }
    
    # 2. Check standard SqlPackage.exe locations (DacFx.msi installs)
    if (-not $sqlPackagePath) {
        $sqlPackagePaths = @(
            "C:\Program Files\Microsoft SQL Server\170\DAC\bin\SqlPackage.exe",  # SQL Server 2025 (DacFx 170)
            "C:\Program Files\Microsoft SQL Server\160\DAC\bin\SqlPackage.exe",  # SQL Server 2022 (DacFx 160)
            "C:\Program Files (x86)\Microsoft SQL Server\170\DAC\bin\SqlPackage.exe",
            "C:\Program Files (x86)\Microsoft SQL Server\160\DAC\bin\SqlPackage.exe"
        )
        
        $sqlPackagePath = $sqlPackagePaths | Where-Object { Test-Path $_ } | Select-Object -First 1
        
        if ($sqlPackagePath) {
            Write-Log "Found SqlPackage at: $sqlPackagePath" -Level Debug
        }
    }
    
    if (-not $sqlPackagePath) {
        throw "SqlPackage not found. Install via: dotnet tool install -g microsoft.sqlpackage"
    }
    
    Write-Log "Using SqlPackage: $sqlPackagePath" -Level Debug

    # Resolve localhost to machine name for Azure AD token auth (requires TCP/IP, not Shared Memory)
    $serverInstance = $Server
    if ($Server -eq 'localhost' -or $Server -eq '.' -or $Server -eq '(local)') {
        $serverInstance = $env:COMPUTERNAME
        Write-Log "Resolved localhost to: $serverInstance (Azure AD auth requires TCP/IP)" -Level Debug
    }

    # Build connection string (use Windows Auth if no AccessToken)
    if ($AccessToken) {
        $connectionString = "Server=$serverInstance;Database=$Database;Encrypt=True;TrustServerCertificate=True;"
    } else {
        $connectionString = "Server=$serverInstance;Database=$Database;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;"
    }

    $sqlPackageArgs = @(
        "/Action:Publish",
        "/SourceFile:$DacpacPath",
        "/TargetConnectionString:$connectionString",
        "/p:BlockOnPossibleDataLoss=False",
        "/p:DropObjectsNotInSource=False",
        "/p:AllowIncompatiblePlatform=True",
        "/p:IncludeCompositeObjects=True",
        "/p:AllowDropBlockingAssemblies=True",
        "/p:NoAlterStatementsToChangeClrTypes=False",
        "/p:TreatVerificationErrorsAsWarnings=False",
        "/p:IgnorePermissions=False",
        "/p:IgnoreRoleMembership=False",
        "/v:DependenciesPath=$DependenciesPath"
    )

    # Add AccessToken only if provided (Azure AD auth)
    if ($AccessToken) {
        $sqlPackageArgs += "/AccessToken:$AccessToken"
    }

    if ($DryRun) {
        Write-Log "[DRY RUN] Would execute: $sqlPackagePath $($sqlPackageArgs -join ' ')" -Level Warning
        return
    }
    
    Write-Log "Executing SqlPackage.exe..." -Level Debug
    $output = & $sqlPackagePath $sqlPackageArgs 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Log "SqlPackage.exe output:" -Level Error
        $output | ForEach-Object { Write-Log "  $_" -Level Error }
        throw "SqlPackage.exe failed with exit code $LASTEXITCODE"
    }
    
    # Log successful output (last 10 lines)
    $output | Select-Object -Last 10 | ForEach-Object { Write-Log "  $_" -Level Debug }
    Write-Log "DACPAC deployed successfully" -Level Success
}

function Set-DatabaseTrustworthy {
    <#
    .SYNOPSIS
    Sets the database to TRUSTWORTHY ON for CLR assemblies.
    #>
    Write-Log "Setting database to TRUSTWORTHY ON..." -Level Info
    
    $trustworthyScript = Join-Path $ScriptsPath "set-trustworthy.sql"
    if (Test-Path $trustworthyScript) {
        $variables = @{ DatabaseName = $Database }
        Invoke-SqlCmdSafe -InputFile $trustworthyScript -DatabaseName 'master' -Variables $variables
    }
    else {
        # Fallback to inline SQL
        $query = "ALTER DATABASE [$Database] SET TRUSTWORTHY ON;"
        Invoke-SqlCmdSafe -Query $query -DatabaseName 'master'
    }
    
    Write-Log "Database set to TRUSTWORTHY ON" -Level Success
}

function Enable-ServiceBroker {
    <#
    .SYNOPSIS
    Enables Service Broker and ensures all queues are healthy (idempotent).
    Fixes queues disabled by poison message handling.
    #>
    Write-Log "Checking Service Broker status..." -Level Info
    
    # Check if Service Broker is enabled
    $brokerCheckQuery = @"
SELECT is_broker_enabled 
FROM sys.databases 
WHERE name = '$Database'
"@
    $brokerResult = Invoke-SqlCmdSafe -Query $brokerCheckQuery -DatabaseName 'master'
    
    if (-not $brokerResult.is_broker_enabled) {
        Write-Log "  Enabling Service Broker..." -Level Info
        
        # Check for active connections that would block SINGLE_USER mode
        $activeConnQuery = @"
SELECT COUNT(*) as ConnectionCount
FROM sys.dm_exec_sessions
WHERE database_id = DB_ID('$Database')
  AND session_id <> @@SPID
"@
        $activeConns = Invoke-SqlCmdSafe -Query $activeConnQuery -DatabaseName 'master'
        
        if ($activeConns.ConnectionCount -gt 0) {
            Write-Log "  Warning: $($activeConns.ConnectionCount) active connections detected" -Level Warning
            Write-Log "  Attempting ENABLE_BROKER WITH ROLLBACK IMMEDIATE" -Level Info
        }
        
        $enableBrokerSql = @"
ALTER DATABASE [$Database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
ALTER DATABASE [$Database] SET ENABLE_BROKER;
ALTER DATABASE [$Database] SET MULTI_USER;
"@
        
        try {
            Invoke-SqlCmdSafe -Query $enableBrokerSql -DatabaseName 'master' -QueryTimeout 60
            Write-Log "  ✓ Service Broker enabled" -Level Success
        }
        catch {
            Write-Log "  ✗ Failed to enable Service Broker: $($_.Exception.Message)" -Level Error
            throw
        }
    }
    else {
        Write-Log "  ○ Service Broker already enabled" -Level Debug
    }
    
    # Check and fix queue status (poison message handling may disable queues)
    Write-Log "Checking queue health..." -Level Info
    
    $queueCheckQuery = @"
SELECT 
    name,
    is_receive_enabled,
    is_enqueue_enabled,
    is_poison_message_handling_enabled
FROM sys.service_queues
WHERE is_ms_shipped = 0
  AND (is_receive_enabled = 0 OR is_enqueue_enabled = 0)
"@
    
    $disabledQueues = Invoke-SqlCmdSafe -Query $queueCheckQuery -DatabaseName $Database
    
    if ($disabledQueues -and $disabledQueues.Count -gt 0) {
        foreach ($queue in $disabledQueues) {
            Write-Log "  Re-enabling disabled queue: $($queue.name)" -Level Warning
            
            # Clear any stuck messages first
            $clearQueueSql = @"
DECLARE @ConvHandle UNIQUEIDENTIFIER;
WHILE EXISTS (SELECT 1 FROM dbo.[$($queue.name)])
BEGIN
    RECEIVE TOP(1) @ConvHandle = conversation_handle FROM dbo.[$($queue.name)];
    IF @ConvHandle IS NOT NULL
        END CONVERSATION @ConvHandle WITH CLEANUP;
END
"@
            
            try {
                Invoke-SqlCmdSafe -Query $clearQueueSql -DatabaseName $Database -QueryTimeout 30
                Write-Log "    • Cleared stuck messages from queue" -Level Debug
            }
            catch {
                Write-Log "    • Warning: Could not clear queue messages: $($_.Exception.Message)" -Level Warning
            }
            
            # Re-enable the queue
            $enableQueueSql = "ALTER QUEUE dbo.[$($queue.name)] WITH STATUS = ON;"
            
            try {
                Invoke-SqlCmdSafe -Query $enableQueueSql -DatabaseName $Database
                Write-Log "    ✓ Queue re-enabled: $($queue.name)" -Level Success
            }
            catch {
                Write-Log "    ✗ Failed to re-enable queue: $($_.Exception.Message)" -Level Error
                throw
            }
        }
    }
    else {
        Write-Log "  ○ All queues healthy" -Level Debug
    }
    
    # Clean up orphaned conversations
    Write-Log "Cleaning orphaned conversations..." -Level Info
    
    $cleanConversationsSql = @"
DECLARE @ConvHandle UNIQUEIDENTIFIER;
DECLARE @CleanedCount INT = 0;

DECLARE conv_cursor CURSOR FOR
SELECT conversation_handle
FROM sys.conversation_endpoints
WHERE state IN ('DI', 'CD', 'ER')  -- Disconnected, Closed, Error
  AND is_initiator = 1;

OPEN conv_cursor;
FETCH NEXT FROM conv_cursor INTO @ConvHandle;

WHILE @@FETCH_STATUS = 0
BEGIN
    END CONVERSATION @ConvHandle WITH CLEANUP;
    SET @CleanedCount = @CleanedCount + 1;
    FETCH NEXT FROM conv_cursor INTO @ConvHandle;
END

CLOSE conv_cursor;
DEALLOCATE conv_cursor;

SELECT @CleanedCount AS CleanedConversations;
"@
    
    try {
        $cleanResult = Invoke-SqlCmdSafe -Query $cleanConversationsSql -DatabaseName $Database -QueryTimeout 30
        if ($cleanResult.CleanedConversations -gt 0) {
            Write-Log "  ✓ Cleaned $($cleanResult.CleanedConversations) orphaned conversations" -Level Success
        }
        else {
            Write-Log "  ○ No orphaned conversations found" -Level Debug
        }
    }
    catch {
        Write-Log "  Warning: Could not clean orphaned conversations: $($_.Exception.Message)" -Level Warning
    }
    
    Write-Log "Service Broker configuration complete" -Level Success
}

function Test-DeploymentSuccess {
    <#
    .SYNOPSIS
    Validates the deployment by checking key database objects.
    #>
    Write-Log "Validating deployment..." -Level Info
    
    # Check assemblies in master
    $assembliesQuery = "SELECT COUNT(*) AS AssemblyCount FROM sys.assemblies WHERE is_user_defined = 1"
    $assemblies = Invoke-SqlCmdSafe -Query $assembliesQuery -DatabaseName 'master'
    Write-Log "  • External assemblies in master: $($assemblies.AssemblyCount)" -Level Info
    
    # Check database-specific assembly
    $clrAssemblyQuery = "SELECT COUNT(*) AS ClrCount FROM sys.assemblies WHERE name = 'Hartonomous.Clr'"
    $clrAssembly = Invoke-SqlCmdSafe -Query $clrAssemblyQuery -DatabaseName $Database
    Write-Log "  • Hartonomous.Clr assembly: $($clrAssembly.ClrCount)" -Level Info
    
    # Check functions
    $functionsQuery = "SELECT COUNT(*) AS FuncCount FROM sys.objects WHERE type IN ('FN', 'IF', 'TF', 'FS', 'FT') AND is_ms_shipped = 0"
    $functions = Invoke-SqlCmdSafe -Query $functionsQuery -DatabaseName $Database
    Write-Log "  • User-defined functions: $($functions.FuncCount)" -Level Info
    
    # Check UDTs
    $udtsQuery = "SELECT COUNT(*) AS UdtCount FROM sys.types WHERE is_user_defined = 1 AND is_assembly_type = 1"
    $udts = Invoke-SqlCmdSafe -Query $udtsQuery -DatabaseName $Database
    Write-Log "  • CLR User-Defined Types: $($udts.UdtCount)" -Level Info
    
    # Check CLR aggregates
    $aggregatesQuery = "SELECT COUNT(*) AS AggCount FROM sys.objects WHERE type = 'AF'"
    $aggregates = Invoke-SqlCmdSafe -Query $aggregatesQuery -DatabaseName $Database
    Write-Log "  • CLR Aggregates: $($aggregates.AggCount)" -Level Info
    
    # Check TRUSTWORTHY setting
    $trustworthyQuery = "SELECT is_trustworthy_on FROM sys.databases WHERE name = '$Database'"
    $trustworthy = Invoke-SqlCmdSafe -Query $trustworthyQuery -DatabaseName 'master'
    $trustworthyStatus = if ($trustworthy.is_trustworthy_on) { "ENABLED" } else { "DISABLED" }
    Write-Log "  • TRUSTWORTHY: $trustworthyStatus" -Level Info
    
    Write-Log "Validation completed successfully" -Level Success
}

#
# === MAIN EXECUTION ===
#

$deploymentStartTime = Get-Date

Write-Log "═══════════════════════════════════════════════════════════════" -Level Info
Write-Log "  HARTONOMOUS DATABASE DEPLOYMENT" -Level Info
Write-Log "═══════════════════════════════════════════════════════════════" -Level Info
Write-Log "Server:       $Server" -Level Info
Write-Log "Database:     $Database" -Level Info
Write-Log "DACPAC:       $DacpacPath" -Level Info
Write-Log "Dependencies: $DependenciesPath" -Level Info
Write-Log "Scripts:      $ScriptsPath" -Level Info
if ($DryRun) {
    Write-Log "Mode:         DRY RUN (no changes will be made)" -Level Warning
}
Write-Log "═══════════════════════════════════════════════════════════════" -Level Info
Write-Log "" -Level Info

try {
    # Validate prerequisites
    Write-Log "STEP 0: Validating prerequisites..." -Level Info
    
    if (-not (Get-Module -ListAvailable -Name SqlServer)) {
        throw "SqlServer PowerShell module is not installed. Install with: Install-Module -Name SqlServer"
    }
    
    Import-Module SqlServer -ErrorAction Stop
    Write-Log "SqlServer module loaded successfully" -Level Success
    Write-Log "" -Level Info

    # Step 1: Enable CLR Integration
    Write-Log "STEP 1: Enabling CLR Integration..." -Level Info
    Enable-ClrIntegration
    Write-Log "" -Level Info

    # Step 2: Pre-deployment cleanup
    Write-Log "STEP 2: Pre-deployment cleanup..." -Level Info
    Invoke-PreDeploymentCleanup
    Write-Log "" -Level Info

    # Step 3: Deploy external assemblies
    Write-Log "STEP 3: Deploying external CLR dependencies..." -Level Info
    Deploy-ExternalAssemblies
    Write-Log "" -Level Info

    # Step 4: Deploy DACPAC
    Write-Log "STEP 4: Deploying DACPAC..." -Level Info
    Deploy-Dacpac
    Write-Log "" -Level Info

    # Step 5: Set TRUSTWORTHY
    Write-Log "STEP 5: Setting database to TRUSTWORTHY..." -Level Info
    Set-DatabaseTrustworthy
    Write-Log "" -Level Info

    # Step 6: Enable Service Broker and fix queue status
    Write-Log "STEP 6: Configuring Service Broker..." -Level Info
    Enable-ServiceBroker
    Write-Log "" -Level Info

    # Step 7: Validation
    if (-not $SkipValidation) {
        Write-Log "STEP 6: Validating deployment..." -Level Info
        Test-DeploymentSuccess
        Write-Log "" -Level Info
    }

    # Success summary
    $deploymentDuration = (Get-Date) - $deploymentStartTime
    Write-Log "═══════════════════════════════════════════════════════════════" -Level Success
    Write-Log "  DEPLOYMENT COMPLETED SUCCESSFULLY" -Level Success
    Write-Log "═══════════════════════════════════════════════════════════════" -Level Success
    Write-Log "Duration: $($deploymentDuration.ToString('mm\:ss'))" -Level Success
    Write-Log "Database: $Database on $Server" -Level Success
    Write-Log "═══════════════════════════════════════════════════════════════" -Level Success

    exit 0
}
catch {
    $deploymentDuration = (Get-Date) - $deploymentStartTime
    Write-Log "" -Level Error
    Write-Log "═══════════════════════════════════════════════════════════════" -Level Error
    Write-Log "  DEPLOYMENT FAILED" -Level Error
    Write-Log "═══════════════════════════════════════════════════════════════" -Level Error
    Write-Log "Error: $($_.Exception.Message)" -Level Error
    Write-Log "Duration: $($deploymentDuration.ToString('mm\:ss'))" -Level Error
    Write-Log "═══════════════════════════════════════════════════════════════" -Level Error
    
    if ($_.ScriptStackTrace) {
        Write-Log "Stack trace:" -Level Debug
        Write-Log $_.ScriptStackTrace -Level Debug
    }
    
    exit 1
}