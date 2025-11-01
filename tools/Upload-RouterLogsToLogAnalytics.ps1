[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$WorkspaceId = $env:ROUTER_LOGS_WORKSPACE_ID,

    [Parameter(Mandatory = $false)]
    [string]$SharedKey = $env:ROUTER_LOGS_SHARED_KEY,

    [Parameter(Mandatory = $false)]
    [string]$ArchivePath = "\\192.168.1.1\routerlogs\archive",

    [Parameter(Mandatory = $false)]
    [string]$ProcessedPath = "\\192.168.1.1\routerlogs\processed",

    [Parameter(Mandatory = $false)]
    [string]$RouterName = "hart-router",

    [Parameter(Mandatory = $false)]
    [string]$LogType = "RouterSyslog",

    [Parameter(Mandatory = $false)]
    [ValidateRange(1, 5000)]
    [int]$LinesPerBatch = 500,

    [Parameter(Mandatory = $false)]
    [ValidateRange(1048576, 31457280)]
    [int]$MaxBatchBytes = 26214400,

    [Parameter(Mandatory = $false)]
    [ValidateRange(0, 10)]
    [int]$MaxRetryCount = 3,

    [Parameter(Mandatory = $false)]
    [ValidateRange(1, 300)]
    [int]$InitialRetryDelaySeconds = 2,

    [switch]$WhatIf
)

if ([string]::IsNullOrWhiteSpace($WorkspaceId)) {
    throw "WorkspaceId is required. Provide it via -WorkspaceId or set ROUTER_LOGS_WORKSPACE_ID."
}

if ([string]::IsNullOrWhiteSpace($SharedKey)) {
    throw "SharedKey is required. Provide it via -SharedKey or set ROUTER_LOGS_SHARED_KEY."
}

if (-not (Test-Path -LiteralPath $ArchivePath)) {
    throw "ArchivePath '$ArchivePath' is not accessible. Ensure the SMB share is mounted."
}

if (-not (Test-Path -LiteralPath $ProcessedPath)) {
    if ($WhatIf) {
        Write-Verbose "Would create processed directory '$ProcessedPath'."
    }
    else {
        New-Item -Path $ProcessedPath -ItemType Directory -Force | Out-Null
    }
}

$ingestionUri = "https://$WorkspaceId.ods.opinsights.azure.com/api/logs?api-version=2016-04-01"
$contentType = "application/json"
$resource = "/api/logs"

function New-LogAnalyticsSignature {
    param(
        [string]$CustomerId,
        [string]$SharedKey,
        [string]$Date,
        [int]$ContentLength,
        [string]$Method,
        [string]$ContentType,
        [string]$Resource
    )

    $stringToHash = "$Method`n$ContentLength`n$ContentType`nx-ms-date:$Date`n$Resource"
    $bytesToHash = [Text.Encoding]::UTF8.GetBytes($stringToHash)
    $keyBytes = [Convert]::FromBase64String($SharedKey)
    $hmacSha256 = [System.Security.Cryptography.HMACSHA256]::new($keyBytes)
    $hashedBytes = $hmacSha256.ComputeHash($bytesToHash)
    $encodedHash = [Convert]::ToBase64String($hashedBytes)
    return ('SharedKey {0}:{1}' -f $CustomerId, $encodedHash)
}

function Convert-LineToRecord {
    param(
        [string]$Line,
        [string]$Router,
        [string]$FileName
    )

    $timestamp = [DateTimeOffset]::UtcNow
    $pattern = '^(?<dow>\w{3})\s+(?<mon>\w{3})\s+(?<day>\d{1,2})\s+(?<time>\d{2}:\d{2}:\d{2})\s+(?<year>\d{4})\s+'
    if ($Line -match $pattern) {
        $month = $Matches.mon
        $day = $Matches.day
        $time = $Matches.time
        $year = $Matches.year
        $format = 'MMM d yyyy HH:mm:ss'
        $dateString = "{0} {1} {2} {3}" -f $month, $day, $year, $time
        try {
            $parsed = [DateTimeOffset]::ParseExact(
                $dateString,
                $format,
                [System.Globalization.CultureInfo]::InvariantCulture,
                [System.Globalization.DateTimeStyles]::AssumeLocal
            ).ToUniversalTime()
            if ($parsed) {
                $timestamp = $parsed
            }
        }
        catch {
            # ignore parse errors, keep UTC now
        }
    }

    [pscustomobject]@{
        Router      = $Router
        FileName    = $FileName
        Message     = $Line
        LoggedAtUtc = $timestamp.ToString('o')
    }
}

function Send-LogBatch {
    param(
        [pscustomobject[]]$Batch
    )

    if (-not $Batch -or $Batch.Count -eq 0) {
        return
    }

    $body = $Batch | ConvertTo-Json -Depth 5
    $contentLength = [Text.Encoding]::UTF8.GetByteCount($body)

    if ($contentLength -gt $MaxBatchBytes) {
        if ($Batch.Count -le 1) {
            throw "Single record payload exceeds MaxBatchBytes ($MaxBatchBytes bytes)."
        }

        $splitIndex = [Math]::Ceiling($Batch.Count / 2)
        $firstHalf = $Batch[0..($splitIndex - 1)]
        $secondHalf = $Batch[$splitIndex..($Batch.Count - 1)]

        Send-LogBatch -Batch $firstHalf
        Send-LogBatch -Batch $secondHalf
        return
    }

    $date = [DateTimeOffset]::UtcNow.ToString('r')
    $signature = New-LogAnalyticsSignature -CustomerId $WorkspaceId -SharedKey $SharedKey -Date $date -ContentLength $contentLength -Method 'POST' -ContentType $contentType -Resource $resource

    $headers = @{
        'Authorization'        = $signature
        'Log-Type'             = $LogType
        'x-ms-date'            = $date
        'time-generated-field' = 'LoggedAtUtc'
    }

    if ($WhatIf) {
        Write-Verbose "Would send batch of $($Batch.Count) records ($contentLength bytes)."
        return
    }

    $attempt = 0
    $delay = $InitialRetryDelaySeconds

    while ($true) {
        try {
            $response = Invoke-WebRequest -Uri $ingestionUri -Method Post -Body $body -ContentType $contentType -Headers $headers -UseBasicParsing -ErrorAction Stop
            if ($response.StatusCode -ne 200 -and $response.StatusCode -ne 202) {
                throw "Failed to ingest logs: $($response.StatusCode) $($response.StatusDescription)"
            }
            break
        }
        catch {
            if ($attempt -ge $MaxRetryCount) {
                throw "Failed to ingest batch after $($attempt + 1) attempts: $_"
            }

            Start-Sleep -Seconds $delay
            $attempt++
            $delay = [Math]::Min($delay * 2, 300)
        }
    }
}

$files = Get-ChildItem -Path $ArchivePath -Filter '*.log' -File | Sort-Object -Property LastWriteTime
$processedResults = New-Object System.Collections.Generic.List[object]
$failedFiles = New-Object System.Collections.Generic.List[object]

foreach ($file in $files) {
    Write-Verbose "Processing $($file.FullName)"

    try {
        $lines = Get-Content -Path $file.FullName

        if (-not $lines) {
            if ($WhatIf) {
                Write-Verbose "Would archive empty file $($file.Name)."
            }
            else {
                Move-Item -LiteralPath $file.FullName -Destination (Join-Path $ProcessedPath $file.Name) -Force
            }
            continue
        }

        $batch = New-Object System.Collections.Generic.List[object]
        $lineCount = 0
        foreach ($line in $lines) {
            $record = Convert-LineToRecord -Line $line -Router $RouterName -FileName $file.Name
            $batch.Add($record)
            $lineCount++

            if ($batch.Count -ge $LinesPerBatch) {
                Send-LogBatch -Batch $batch.ToArray()
                $batch.Clear()
            }
        }

        if ($batch.Count -gt 0) {
            Send-LogBatch -Batch $batch.ToArray()
            $batch.Clear()
        }

        if ($WhatIf) {
            Write-Verbose "Would move $($file.Name) to processed folder."
        }
        else {
            $destination = Join-Path $ProcessedPath $file.Name
            Move-Item -LiteralPath $file.FullName -Destination $destination -Force
            $processedResults.Add([pscustomobject]@{
                FileName     = $file.Name
                Source       = $file.FullName
                Destination  = $destination
                Lines        = $lineCount
                Status       = 'Success'
            })
        }
    }
    catch {
        Write-Warning "Failed to process $($file.FullName): $_"
        $failedFiles.Add([pscustomobject]@{
            FileName = $file.Name
            Path     = $file.FullName
            Error    = $_.ToString()
        })
    }
}

if ($failedFiles.Count -gt 0) {
    $errors = $failedFiles | ForEach-Object { "{0}: {1}" -f $_.Path, $_.Error }
    throw "One or more files failed to upload:`n$($errors -join [Environment]::NewLine)"
}

return $processedResults
