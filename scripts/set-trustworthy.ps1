#Requires -Version 7.0
param(
    [Parameter(Mandatory=$true)]
    [string]$Server,
    
    [Parameter(Mandatory=$true)]
    [string]$Database,
    
    [Parameter(Mandatory=$false)]
    [switch]$UseAzureAD,
    
    [Parameter(Mandatory=$false)]
    [string]$AccessToken
)

$ErrorActionPreference = 'Stop'

$sql = "USE master; ALTER DATABASE [$Database] SET TRUSTWORTHY ON; PRINT 'TRUSTWORTHY enabled for $Database';"
$tempFile = [System.IO.Path]::GetTempFileName() + ".sql"

try {
  $sql | Out-File -FilePath $tempFile -Encoding utf8
  
  if ($UseAzureAD -and $AccessToken) {
      Write-Host "Using Azure AD service principal authentication"
      # sqlcmd doesn't support access tokens, use Invoke-Sqlcmd instead
      if (-not (Get-Module -ListAvailable -Name SqlServer)) {
        Install-Module -Name SqlServer -Force -AllowClobber -Scope CurrentUser
      }
      Invoke-Sqlcmd -InputFile $tempFile -ServerInstance $Server -Database "master" -AccessToken $AccessToken -TrustServerCertificate
  } else {
      Write-Host "Using Windows integrated authentication"
      sqlcmd -S $Server -d "master" -E -C -i $tempFile -b
      if ($LASTEXITCODE -ne 0) { 
        throw "Failed to set TRUSTWORTHY" 
      }
  }
  
  Write-Host "✓ TRUSTWORTHY validated"
} finally {
  Remove-Item $tempFile -ErrorAction SilentlyContinue
}
