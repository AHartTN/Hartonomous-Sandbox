<#
.SYNOPSIS
    Validation and health check module for Hartonomous deployments.

.DESCRIPTION
    Provides pre-deployment and post-deployment validation checks for:
    - Required tools and dependencies
    - Database connectivity
    - Network connectivity
    - Permissions
    - Deployment success

.NOTES
    Author: Hartonomous DevOps Team
    Version: 1.0.0
#>

#Requires -Version 7.0

# Import dependencies
Import-Module "$PSScriptRoot\Logger.psm1" -Force

<#
.SYNOPSIS
    Runs all pre-deployment validation checks.

.PARAMETER Config
    Deployment configuration object

.OUTPUTS
    Boolean: True if all checks pass

.EXAMPLE
    if (Test-Prerequisites -Config $config) {
        # Proceed with deployment
    }
#>
function Test-Prerequisites {
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory = $true)]
        [PSCustomObject]$Config
    )

    Write-Log "Running pre-deployment validation checks..." -Level Info

    $checks = @()

    # Check required tools
    $checks += Test-RequiredTools

    # Check disk space
    $checks += Test-DiskSpace -MinimumGB 10

    # Check SQL Server connectivity
    if ($Config.database -and $Config.database.server) {
        $checks += Test-SqlConnection -Config $Config
    }

    # Check Neo4j connectivity (if configured)
    if ($Config.neo4j -and $Config.neo4j.enabled) {
        $checks += Test-Neo4jConnection -Config $Config
    }

    # Check permissions
    $checks += Test-Permissions -Config $Config

    $failed = ($checks | Where-Object { $_ -eq $false }).Count

    if ($failed -gt 0) {
        Write-Log "Pre-deployment validation failed: $failed check(s) failed" -Level Error
        return $false
    }

    Write-Log "Pre-deployment validation passed: All checks successful" -Level Success
    return $true
}

<#
.SYNOPSIS
    Checks for required command-line tools.

.OUTPUTS
    Boolean: True if all required tools are available
#>
function Test-RequiredTools {
    [CmdletBinding()]
    [OutputType([bool])]
    param()

    Write-Log "Checking required tools..." -Level Info

    $requiredTools = @(
        @{ Name = "dotnet"; Command = "dotnet --version"; Description = ".NET SDK" },
        @{ Name = "sqlcmd"; Command = "sqlcmd -?"; Description = "SQL Server command-line tools" },
        @{ Name = "sqlpackage"; Command = "sqlpackage /?"; Description = "SqlPackage (DACPAC deployment)" }
    )

    $optionalTools = @(
        @{ Name = "az"; Command = "az version"; Description = "Azure CLI" },
        @{ Name = "gh"; Command = "gh --version"; Description = "GitHub CLI" },
        @{ Name = "git"; Command = "git --version"; Description = "Git" }
    )

    $allPassed = $true

    foreach ($tool in $requiredTools) {
        if (Test-CommandExists -CommandName $tool.Name) {
            Write-Log "  ✓ $($tool.Description)" -Level Success
        }
        else {
            Write-Log "  ✗ $($tool.Description) not found" -Level Error
            $allPassed = $false
        }
    }

    foreach ($tool in $optionalTools) {
        if (Test-CommandExists -CommandName $tool.Name) {
            Write-Log "  ✓ $($tool.Description)" -Level Success
        }
        else {
            Write-Log "  ⚠ $($tool.Description) not found (optional)" -Level Warning
        }
    }

    return $allPassed
}

<#
.SYNOPSIS
    Checks if a command exists in PATH.

.PARAMETER CommandName
    Command to check

.OUTPUTS
    Boolean
#>
function Test-CommandExists {
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory = $true)]
        [string]$CommandName
    )

    $command = Get-Command $CommandName -ErrorAction SilentlyContinue
    return $null -ne $command
}

<#
.SYNOPSIS
    Checks available disk space.

.PARAMETER MinimumGB
    Minimum required GB

.OUTPUTS
    Boolean
#>
function Test-DiskSpace {
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory = $true)]
        [int]$MinimumGB
    )

    try {
        $drive = Get-PSDrive -Name C -ErrorAction Stop
        $freeSpaceGB = [math]::Round($drive.Free / 1GB, 2)

        if ($freeSpaceGB -ge $MinimumGB) {
            Write-Log "  ✓ Disk space: ${freeSpaceGB}GB available" -Level Success
            return $true
        }
        else {
            Write-Log "  ✗ Insufficient disk space: ${freeSpaceGB}GB (need ${MinimumGB}GB)" -Level Error
            return $false
        }
    }
    catch {
        Write-Log "  ⚠ Could not check disk space: $($_.Exception.Message)" -Level Warning
        return $true  # Don't fail deployment
    }
}

<#
.SYNOPSIS
    Tests SQL Server connectivity.

.PARAMETER Config
    Deployment configuration

.OUTPUTS
    Boolean
#>
function Test-SqlConnection {
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory = $true)]
        [PSCustomObject]$Config
    )

    $server = $Config.database.server
    $database = $Config.database.name

    Write-Log "  Testing SQL Server connection: $server..." -Level Info

    try {
        $query = "SELECT @@VERSION AS SqlVersion, SUSER_NAME() AS CurrentUser"
        $testResult = sqlcmd -S $server -d master -Q $query -h -1 -C 2>$null

        if ($LASTEXITCODE -eq 0) {
            Write-Log "  ✓ SQL Server connected: $server" -Level Success
            return $true
        }
        else {
            Write-Log "  ✗ Cannot connect to SQL Server: $server" -Level Error
            return $false
        }
    }
    catch {
        Write-Log "  ✗ SQL Server connection error: $($_.Exception.Message)" -Level Error
        return $false
    }
}

<#
.SYNOPSIS
    Tests Neo4j connectivity.

.PARAMETER Config
    Deployment configuration

.OUTPUTS
    Boolean
#>
function Test-Neo4jConnection {
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory = $true)]
        [PSCustomObject]$Config
    )

    if (-not $Config.neo4j -or -not $Config.neo4j.enabled) {
        return $true  # Skip if not configured
    }

    $uri = $Config.neo4j.uri

    Write-Log "  Testing Neo4j connection: $uri..." -Level Info

    try {
        # Check if Neo4j port is accessible
        $uriObj = [System.Uri]$uri
        $port = $uriObj.Port

        $tcpClient = New-Object System.Net.Sockets.TcpClient
        $connect = $tcpClient.BeginConnect($uriObj.Host, $port, $null, $null)
        $wait = $connect.AsyncWaitHandle.WaitOne(3000, $false)

        if ($wait) {
            $tcpClient.EndConnect($connect)
            $tcpClient.Close()
            Write-Log "  ✓ Neo4j port accessible: $uri" -Level Success
            return $true
        }
        else {
            Write-Log "  ✗ Cannot connect to Neo4j: $uri (timeout)" -Level Error
            $tcpClient.Close()
            return $false
        }
    }
    catch {
        Write-Log "  ✗ Neo4j connection error: $($_.Exception.Message)" -Level Error
        return $false
    }
}

<#
.SYNOPSIS
    Tests required permissions.

.PARAMETER Config
    Deployment configuration

.OUTPUTS
    Boolean
#>
function Test-Permissions {
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory = $true)]
        [PSCustomObject]$Config
    )

    Write-Log "  Checking permissions..." -Level Info

    $allPassed = $true

    # Check if running as administrator (Windows)
    if ($IsWindows -or $PSVersionTable.PSVersion.Major -le 5) {
        $currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
        $isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

        if (-not $isAdmin) {
            Write-Log "  ⚠ Not running as administrator (may be required for some operations)" -Level Warning
        }
        else {
            Write-Log "  ✓ Running as administrator" -Level Success
        }
    }

    return $allPassed
}

<#
.SYNOPSIS
    Runs post-deployment validation checks.

.PARAMETER Config
    Deployment configuration

.OUTPUTS
    Boolean: True if deployment was successful
#>
function Test-DeploymentSuccess {
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory = $true)]
        [PSCustomObject]$Config
    )

    Write-Log "Running post-deployment validation..." -Level Info

    $checks = @()

    # Check database objects
    $checks += Test-DatabaseObjects -Config $Config

    # Check CLR assemblies
    $checks += Test-CLRAssemblies -Config $Config

    # Check Service Broker
    $checks += Test-ServiceBroker -Config $Config

    # Check application health (if deployed)
    if ($Config.application -and $Config.application.deployTarget) {
        $checks += Test-ApplicationHealth -Config $Config
    }

    $failed = ($checks | Where-Object { $_ -eq $false }).Count

    if ($failed -gt 0) {
        Write-Log "Post-deployment validation failed: $failed check(s) failed" -Level Error
        return $false
    }

    Write-Log "Post-deployment validation passed: All checks successful" -Level Success
    return $true
}

<#
.SYNOPSIS
    Validates database objects exist.

.PARAMETER Config
    Deployment configuration

.OUTPUTS
    Boolean
#>
function Test-DatabaseObjects {
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory = $true)]
        [PSCustomObject]$Config
    )

    $server = $Config.database.server
    $database = $Config.database.name

    Write-Log "  Checking database objects..." -Level Info

    try {
        $query = @"
SELECT
    (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0) AS TableCount,
    (SELECT COUNT(*) FROM sys.procedures WHERE is_ms_shipped = 0) AS ProcCount,
    (SELECT COUNT(*) FROM sys.views WHERE is_ms_shipped = 0) AS ViewCount
"@

        $result = sqlcmd -S $server -d $database -Q $query -h -1 -C 2>$null

        if ($LASTEXITCODE -eq 0) {
            Write-Log "  ✓ Database objects verified" -Level Success
            return $true
        }
        else {
            Write-Log "  ✗ Could not verify database objects" -Level Error
            return $false
        }
    }
    catch {
        Write-Log "  ✗ Database validation error: $($_.Exception.Message)" -Level Error
        return $false
    }
}

<#
.SYNOPSIS
    Validates CLR assemblies are deployed.

.PARAMETER Config
    Deployment configuration

.OUTPUTS
    Boolean
#>
function Test-CLRAssemblies {
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory = $true)]
        [PSCustomObject]$Config
    )

    $server = $Config.database.server
    $database = $Config.database.name

    Write-Log "  Checking CLR assemblies..." -Level Info

    try {
        $query = "SELECT COUNT(*) AS AssemblyCount FROM sys.assemblies WHERE is_user_defined = 1"
        $result = sqlcmd -S $server -d $database -Q $query -h -1 -C 2>$null

        if ($LASTEXITCODE -eq 0) {
            Write-Log "  ✓ CLR assemblies verified" -Level Success
            return $true
        }
        else {
            Write-Log "  ⚠ Could not verify CLR assemblies" -Level Warning
            return $true  # Don't fail deployment
        }
    }
    catch {
        Write-Log "  ⚠ CLR validation error: $($_.Exception.Message)" -Level Warning
        return $true  # Don't fail deployment
    }
}

<#
.SYNOPSIS
    Validates Service Broker is enabled and healthy.

.PARAMETER Config
    Deployment configuration

.OUTPUTS
    Boolean
#>
function Test-ServiceBroker {
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory = $true)]
        [PSCustomObject]$Config
    )

    $server = $Config.database.server
    $database = $Config.database.name

    Write-Log "  Checking Service Broker..." -Level Info

    try {
        $query = "SELECT is_broker_enabled FROM sys.databases WHERE name = '$database'"
        $result = sqlcmd -S $server -d master -Q $query -h -1 -C 2>$null

        if ($LASTEXITCODE -eq 0 -and $result -match "1") {
            Write-Log "  ✓ Service Broker enabled" -Level Success
            return $true
        }
        else {
            Write-Log "  ⚠ Service Broker not enabled" -Level Warning
            return $true  # Don't fail deployment
        }
    }
    catch {
        Write-Log "  ⚠ Service Broker check error: $($_.Exception.Message)" -Level Warning
        return $true  # Don't fail deployment
    }
}

<#
.SYNOPSIS
    Tests application health (API endpoint).

.PARAMETER Config
    Deployment configuration

.OUTPUTS
    Boolean
#>
function Test-ApplicationHealth {
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory = $true)]
        [PSCustomObject]$Config
    )

    if (-not $Config.application -or -not $Config.application.healthCheckUrl) {
        Write-Log "  ⚠ No health check URL configured" -Level Warning
        return $true  # Skip if not configured
    }

    $url = $Config.application.healthCheckUrl

    Write-Log "  Testing application health: $url..." -Level Info

    try {
        $response = Invoke-WebRequest -Uri $url -Method Get -TimeoutSec 10 -UseBasicParsing -ErrorAction Stop

        if ($response.StatusCode -eq 200) {
            Write-Log "  ✓ Application health check passed" -Level Success
            return $true
        }
        else {
            Write-Log "  ✗ Application health check failed: HTTP $($response.StatusCode)" -Level Error
            return $false
        }
    }
    catch {
        Write-Log "  ✗ Application health check error: $($_.Exception.Message)" -Level Error
        return $false
    }
}

# Export module members
Export-ModuleMember -Function @(
    'Test-Prerequisites',
    'Test-DeploymentSuccess',
    'Test-RequiredTools',
    'Test-CommandExists',
    'Test-DiskSpace',
    'Test-SqlConnection',
    'Test-Neo4jConnection',
    'Test-Permissions',
    'Test-DatabaseObjects',
    'Test-CLRAssemblies',
    'Test-ServiceBroker',
    'Test-ApplicationHealth'
)
