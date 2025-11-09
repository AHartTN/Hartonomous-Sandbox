#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Complete dependency mapping of ALL assemblies in bin\Release
#>

$binPath = "d:\Repositories\Hartonomous\src\SqlClr\bin\Release"
$ildasm = "C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8.1 Tools\ildasm.exe"

$assemblies = Get-ChildItem -Path $binPath -Filter "*.dll" | Where-Object { $_.Name -ne "Microsoft.SqlServer.Types.dll" }

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "COMPLETE DEPENDENCY MAP" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

foreach ($asm in $assemblies) {
    Write-Host "`n[$($asm.Name)]" -ForegroundColor Yellow
    
    # Get assembly version
    $versionOutput = & $ildasm $asm.FullName /text /nobar | Select-String -Pattern "^\.assembly.*$($asm.BaseName)" -Context 0,10
    $version = ($versionOutput | Select-String -Pattern "\.ver \d").ToString().Trim()
    Write-Host "  Version: $version" -ForegroundColor Gray
    
    # Get external assembly references
    $refs = & $ildasm $asm.FullName /text /nobar | Select-String -Pattern "^\.assembly extern" -Context 0,4
    
    if ($refs) {
        Write-Host "  External Dependencies:" -ForegroundColor Cyan
        
        $currentRef = $null
        foreach ($line in $refs) {
            $lineText = $line.ToString().Trim()
            
            if ($lineText -match '^\.assembly extern (.+)$') {
                $currentRef = $matches[1]
            }
            elseif ($lineText -match '\.ver (\d+):(\d+):(\d+):(\d+)') {
                $refVersion = "$($matches[1]).$($matches[2]).$($matches[3]).$($matches[4])"
                if ($currentRef -ne "mscorlib" -and $currentRef -ne "System" -and $currentRef -ne "System.Core" -and $currentRef -ne "System.Data" -and $currentRef -ne "System.Xml" -and $currentRef -ne "System.Numerics") {
                    Write-Host "    → $currentRef (v$refVersion)" -ForegroundColor White
                }
                $currentRef = $null
            }
        }
    }
}

Write-Host "`n========================================`n" -ForegroundColor Cyan
