[CmdletBinding()]
param([string]$Server = "localhost", [string]$Database = "Hartonomous")
$ErrorActionPreference = "Stop"

function Invoke-Sql {
    param([string]$Query)
    Invoke-Sqlcmd -ServerInstance $Server -Database $Database -Query $Query -TrustServerCertificate -ErrorAction Stop
}

Write-Host "=== HARTONOMOUS KERNEL VALIDATION ===" -ForegroundColor Cyan

# 1. PHYSICS CHECK (AVX2)
try {
    $res = Invoke-Sql "SELECT dbo.clr_VectorDotProduct(0x0000803F00000040, 0x0000803F00000040) as V"
    if ($res.V -eq 5.0) { Write-Host "[PASS] Physics Engine (AVX2)" -ForegroundColor Green }
    else { Write-Host "[FAIL] Physics Engine Math Error" -ForegroundColor Red }
} catch { Write-Host "[FAIL] CLR Not Loaded: $_" -ForegroundColor Red }

# 2. NERVOUS SYSTEM CHECK (Queues)
try {
    $q = Invoke-Sql "SELECT count(*) as C FROM sys.service_queues WHERE is_receive_enabled = 1 AND name IN ('IngestionQueue','Neo4jSyncQueue')"
    if ($q.C -ge 2) { Write-Host "[PASS] Nervous System (Queues Active)" -ForegroundColor Green }
    else { Write-Host "[FAIL] Queues Disabled or Missing" -ForegroundColor Red }
} catch { Write-Host "[FAIL] Broker Error: $_" -ForegroundColor Red }

# 3. COGNITION CHECK (OODA)
try {
    Invoke-Sql "EXEC dbo.sp_Analyze"
    $h = Invoke-Sql "SELECT TOP 1 ImprovementId FROM dbo.AutonomousImprovementHistory ORDER BY StartedAt DESC"
    if ($h) { Write-Host "[PASS] Cognition (OODA Loop Active)" -ForegroundColor Green }
    else { Write-Host "[FAIL] OODA Loop produced no history" -ForegroundColor Red }
} catch { Write-Host "[FAIL] Analysis Error: $_" -ForegroundColor Red }
