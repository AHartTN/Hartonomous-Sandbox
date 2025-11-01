[CmdletBinding()]
param()

$logDir = Join-Path $env:ProgramData 'Hartonomous'
$logFile = Join-Path $logDir 'RouterLogUpload.log'

if (-not (Test-Path -LiteralPath $logDir)) {
    New-Item -Path $logDir -ItemType Directory -Force | Out-Null
}

function Write-UploadLog {
    param(
        [string]$Message,
        [string]$Level = 'INFO'
    )

    $timestamp = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
    $entry = "[$timestamp] [$Level] $Message"
    Add-Content -Path $logFile -Value $entry
}

try {
    $settingsPath = 'HKCU:\Software\Hartonomous'
    $workspaceId = (Get-ItemProperty -Path $settingsPath -Name 'RouterLogsWorkspaceId' -ErrorAction Stop).RouterLogsWorkspaceId
    $sharedKey = (Get-ItemProperty -Path $settingsPath -Name 'RouterLogsSharedKey' -ErrorAction Stop).RouterLogsSharedKey
}
catch {
    Write-UploadLog -Message "Failed to read workspace settings from registry: $_" -Level 'ERROR'
    throw
}

$env:ROUTER_LOGS_WORKSPACE_ID = $workspaceId
$env:ROUTER_LOGS_SHARED_KEY = $sharedKey

$sharePath = '\\192.168.1.1\routerlogs'
$archivePath = Join-Path $sharePath 'archive'
$processedPath = Join-Path $sharePath 'processed'

try {
    Start-Process -FilePath 'net.exe' -ArgumentList @('use', $sharePath, '/delete', '/y') -NoNewWindow -Wait -ErrorAction SilentlyContinue | Out-Null
    $netUse = Start-Process -FilePath 'net.exe' -ArgumentList @('use', $sharePath, '/persistent:no') -NoNewWindow -Wait -PassThru
    if ($netUse.ExitCode -ne 0) {
        throw "net use failed with exit code $($netUse.ExitCode)"
    }
}
catch {
    Write-UploadLog -Message ("Unable to mount SMB share {0}: {1}" -f $sharePath, $_) -Level 'ERROR'
    throw
}

try {
    if (-not (Test-Path -LiteralPath $archivePath)) {
        throw "Archive path $archivePath not found"
    }
    if (-not (Test-Path -LiteralPath $processedPath)) {
        New-Item -Path $processedPath -ItemType Directory -Force | Out-Null
    }

    $uploaderPath = Join-Path $PSScriptRoot 'Upload-RouterLogsToLogAnalytics.ps1'
    if (-not (Test-Path -LiteralPath $uploaderPath)) {
        throw "Uploader script not found at $uploaderPath"
    }

    Write-UploadLog -Message "Starting upload from $archivePath"
    $processedFiles = & $uploaderPath -ArchivePath $archivePath -ProcessedPath $processedPath -RouterName 'hart-router' -Verbose:$false

    $processedCount = if ($processedFiles) { $processedFiles.Count } else { 0 }
    Write-UploadLog -Message "Upload complete. Files just processed: $processedCount"

    if ($processedCount -gt 0) {
        foreach ($entry in $processedFiles) {
            Write-UploadLog -Message ("Moved {0} ({1} lines)" -f $entry.FileName, $entry.Lines)
        }
    }

    $retentionDays = 90
    $removed = 0
    $cutoff = (Get-Date).AddDays(-$retentionDays)
    Get-ChildItem -Path $processedPath -Filter '*.log' -File | Where-Object { $_.LastWriteTime -lt $cutoff } | ForEach-Object {
        Remove-Item -LiteralPath $_.FullName -Force
        $removed++
    }
    if ($removed -gt 0) {
        Write-UploadLog -Message "Cleaned $removed processed files older than $retentionDays days"
    }
}
catch {
    Write-UploadLog -Message ("Upload run failed: {0}" -f $_) -Level 'ERROR'
    throw
}
finally {
    try {
        Start-Process -FilePath 'net.exe' -ArgumentList @('use', $sharePath, '/delete', '/y') -NoNewWindow -Wait | Out-Null
    }
    catch {
        Write-UploadLog -Message ("Failed to unmount SMB share {0}: {1}" -f $sharePath, $_) -Level 'WARN'
    }
}
