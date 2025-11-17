#Requires -Version 7.0
param(
    [Parameter(Mandatory=$true)]
    [string]$Server,
    
    [Parameter(Mandatory=$true)]
    [string]$Database
)

$ErrorActionPreference = 'Stop'

$sql = "USE master; ALTER DATABASE [$Database] SET TRUSTWORTHY ON; PRINT 'TRUSTWORTHY enabled for $Database';"
$tempFile = [System.IO.Path]::GetTempFileName() + ".sql"

try {
  $sql | Out-File -FilePath $tempFile -Encoding utf8
  sqlcmd -S $Server -d "master" -E -C -i $tempFile -b
  if ($LASTEXITCODE -ne 0) { 
    throw "Failed to set TRUSTWORTHY" 
  }
  Write-Host "✓ TRUSTWORTHY validated"
} finally {
  Remove-Item $tempFile -ErrorAction SilentlyContinue
}
