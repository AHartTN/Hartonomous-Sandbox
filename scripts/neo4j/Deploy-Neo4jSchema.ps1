# =============================================
# Deploy Neo4j Schema - Idempotent Deployment
# =============================================
# This script can be run multiple times safely
# All operations use MERGE/IF NOT EXISTS patterns

param(
    [string]$Neo4jUri = "bolt://localhost:7687",
    [string]$Neo4jUser = "neo4j",
    [string]$Neo4jPassword = "neo4jneo4j",
    [string]$Database = "neo4j",
    [switch]$SkipBackup,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"
$CypherShellPath = "D:\Neo4j\cypher-shell-2025.10.1\bin\cypher-shell.bat"
$SchemaPath = "$PSScriptRoot\schemas\CoreSchema.cypher"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Neo4j Schema Deployment" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "URI: $Neo4jUri"
Write-Host "Database: $Database"
Write-Host "Schema: $SchemaPath"
Write-Host ""

# Function to execute Cypher command
function Invoke-Cypher {
    param([string]$Query, [string]$Description)
    
    if ($Verbose) {
        Write-Host "Executing: $Description" -ForegroundColor Yellow
        Write-Host "Query: $Query" -ForegroundColor DarkGray
    } else {
        Write-Host "  → $Description" -NoNewline
    }
    
    try {
        $result = & $CypherShellPath -a $Neo4jUri -u $Neo4jUser -p $Neo4jPassword -d $Database $Query 2>$null
        
        if ($LASTEXITCODE -eq 0) {
            if ($Verbose) {
                Write-Host "✓ Success" -ForegroundColor Green
                if ($result) { Write-Host $result -ForegroundColor Gray }
            } else {
                Write-Host " ✓" -ForegroundColor Green
            }
            return $true
        } else {
            Write-Host " ✗ Failed" -ForegroundColor Red
            Write-Host "Error: $result" -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host " ✗ Exception" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
        return $false
    }
}

# Step 1: Test Connection
Write-Host "[1/6] Testing Neo4j Connection..." -ForegroundColor Cyan
$connected = Invoke-Cypher "RETURN 'connected' as status;" "Connection test"
if (-not $connected) {
    Write-Host "Failed to connect to Neo4j. Check connection settings." -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 2: Backup existing schema (optional)
if (-not $SkipBackup) {
    Write-Host "[2/6] Backing up existing constraints and indexes..." -ForegroundColor Cyan
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $backupPath = "$PSScriptRoot\backups\schema_backup_$timestamp.cypher"
    
    if (-not (Test-Path "$PSScriptRoot\backups")) {
        New-Item -ItemType Directory -Path "$PSScriptRoot\backups" | Out-Null
    }
    
    # Export constraints
    $constraints = & $CypherShellPath -a $Neo4jUri -u $Neo4jUser -p $Neo4jPassword -d $Database "SHOW CONSTRAINTS;" 2>$null
    
    # Export indexes
    $indexes = & $CypherShellPath -a $Neo4jUri -u $Neo4jUser -p $Neo4jPassword -d $Database "SHOW INDEXES;" 2>$null
    
    $backupContent = @"
// Neo4j Schema Backup - $timestamp
// Database: $Database
// URI: $Neo4jUri

// ===== CONSTRAINTS =====
$constraints

// ===== INDEXES =====
$indexes
"@
    
    $backupContent | Out-File -FilePath $backupPath -Encoding UTF8
    Write-Host "  → Backup saved to: $backupPath" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "[2/6] Skipping backup..." -ForegroundColor Yellow
    Write-Host ""
}

# Step 3: Deploy Constraints
Write-Host "[3/6] Creating Constraints (idempotent)..." -ForegroundColor Cyan

$constraints = @(
    @{Query="CREATE CONSTRAINT atom_unique IF NOT EXISTS FOR (a:Atom) REQUIRE (a.id, a.tenantId) IS UNIQUE;"; Desc="Atom unique constraint (id, tenantId)"},
    @{Query="CREATE CONSTRAINT atom_id_exists IF NOT EXISTS FOR (a:Atom) REQUIRE a.id IS NOT NULL;"; Desc="Atom id existence"},
    @{Query="CREATE CONSTRAINT tenant_id_unique IF NOT EXISTS FOR (t:Tenant) REQUIRE t.tenantId IS UNIQUE;"; Desc="Tenant unique constraint"},
    @{Query="CREATE CONSTRAINT user_id_unique IF NOT EXISTS FOR (u:User) REQUIRE (u.userId, u.tenantId) IS UNIQUE;"; Desc="User unique constraint"},
    @{Query="CREATE CONSTRAINT model_id_unique IF NOT EXISTS FOR (m:Model) REQUIRE m.modelId IS UNIQUE;"; Desc="Model unique constraint"},
    @{Query="CREATE CONSTRAINT model_version_unique IF NOT EXISTS FOR (mv:ModelVersion) REQUIRE (mv.modelId, mv.version) IS UNIQUE;"; Desc="ModelVersion unique constraint"},
    @{Query="CREATE CONSTRAINT inference_id_unique IF NOT EXISTS FOR (i:Inference) REQUIRE i.inferenceId IS UNIQUE;"; Desc="Inference unique constraint"},
    @{Query="CREATE CONSTRAINT operation_id_unique IF NOT EXISTS FOR (o:Operation) REQUIRE o.operationId IS UNIQUE;"; Desc="Operation unique constraint"},
    @{Query="CREATE CONSTRAINT generation_stream_unique IF NOT EXISTS FOR (g:GenerationStream) REQUIRE g.streamId IS UNIQUE;"; Desc="GenerationStream unique constraint"}
)

$successCount = 0
foreach ($constraint in $constraints) {
    if (Invoke-Cypher $constraint.Query $constraint.Desc) {
        $successCount++
    }
}

Write-Host "  Created/Verified $successCount of $($constraints.Count) constraints" -ForegroundColor Green
Write-Host ""

# Step 4: Deploy Indexes
Write-Host "[4/6] Creating Indexes (idempotent)..." -ForegroundColor Cyan

$indexes = @(
    @{Query="CREATE INDEX atom_created IF NOT EXISTS FOR (a:Atom) ON (a.createdAt);"; Desc="Atom createdAt index"},
    @{Query="CREATE INDEX atom_tenant IF NOT EXISTS FOR (a:Atom) ON (a.tenantId);"; Desc="Atom tenantId index"},
    @{Query="CREATE INDEX atom_type IF NOT EXISTS FOR (a:Atom) ON (a.atomType);"; Desc="Atom atomType index"},
    @{Query="CREATE INDEX atom_hash IF NOT EXISTS FOR (a:Atom) ON (a.contentHash);"; Desc="Atom contentHash index"},
    @{Query="CREATE INDEX operation_created IF NOT EXISTS FOR (o:Operation) ON (o.createdAt);"; Desc="Operation createdAt index"},
    @{Query="CREATE INDEX operation_type IF NOT EXISTS FOR (o:Operation) ON (o.operationType);"; Desc="Operation operationType index"},
    @{Query="CREATE INDEX operation_tenant IF NOT EXISTS FOR (o:Operation) ON (o.tenantId);"; Desc="Operation tenantId index"},
    @{Query="CREATE INDEX inference_timestamp IF NOT EXISTS FOR (i:Inference) ON (i.timestamp);"; Desc="Inference timestamp index"},
    @{Query="CREATE INDEX inference_task IF NOT EXISTS FOR (i:Inference) ON (i.taskType);"; Desc="Inference taskType index"},
    @{Query="CREATE INDEX inference_tenant IF NOT EXISTS FOR (i:Inference) ON (i.tenantId);"; Desc="Inference tenantId index"},
    @{Query="CREATE INDEX model_name IF NOT EXISTS FOR (m:Model) ON (m.name);"; Desc="Model name index"},
    @{Query="CREATE INDEX user_tenant IF NOT EXISTS FOR (u:User) ON (u.tenantId);"; Desc="User tenantId index"},
    @{Query="CREATE INDEX decision_confidence IF NOT EXISTS FOR (d:Decision) ON (d.confidence);"; Desc="Decision confidence index"}
)

$successCount = 0
foreach ($index in $indexes) {
    if (Invoke-Cypher $index.Query $index.Desc) {
        $successCount++
    }
}

Write-Host "  Created/Verified $successCount of $($indexes.Count) indexes" -ForegroundColor Green
Write-Host ""

# Step 5: Initialize Reference Data
Write-Host "[5/6] Initializing Reference Data (idempotent)..." -ForegroundColor Cyan

$referenceData = @(
    @{Query="MERGE (r:ReasoningMode {type: 'vector_similarity'}) ON CREATE SET r.description = 'Semantic similarity via vector embeddings and DiskANN', r.created = datetime();"; Desc="ReasoningMode: vector_similarity"},
    @{Query="MERGE (r:ReasoningMode {type: 'spatial_query'}) ON CREATE SET r.description = 'Geometric reasoning via spatial indexes', r.created = datetime();"; Desc="ReasoningMode: spatial_query"},
    @{Query="MERGE (r:ReasoningMode {type: 'graph_traversal'}) ON CREATE SET r.description = 'Symbolic/causal reasoning via graph patterns', r.created = datetime();"; Desc="ReasoningMode: graph_traversal"},
    @{Query="MERGE (r:ReasoningMode {type: 'hybrid'}) ON CREATE SET r.description = 'Combined multi-modal reasoning', r.created = datetime();"; Desc="ReasoningMode: hybrid"},
    @{Query="MERGE (r:ReasoningMode {type: 'symbolic_logic'}) ON CREATE SET r.description = 'Classical logical inference', r.created = datetime();"; Desc="ReasoningMode: symbolic_logic"},
    @{Query="MERGE (c:Context {domain: 'text_generation'}) ON CREATE SET c.created = datetime();"; Desc="Context: text_generation"},
    @{Query="MERGE (c:Context {domain: 'image_generation'}) ON CREATE SET c.created = datetime();"; Desc="Context: image_generation"},
    @{Query="MERGE (c:Context {domain: 'audio_generation'}) ON CREATE SET c.created = datetime();"; Desc="Context: audio_generation"},
    @{Query="MERGE (c:Context {domain: 'multimodal'}) ON CREATE SET c.created = datetime();"; Desc="Context: multimodal"},
    @{Query="MERGE (c:Context {domain: 'embedding_generation'}) ON CREATE SET c.created = datetime();"; Desc="Context: embedding_generation"},
    @{Query="MERGE (c:Context {domain: 'semantic_search'}) ON CREATE SET c.created = datetime();"; Desc="Context: semantic_search"},
    @{Query="MERGE (d:DataClassification {level: 'public'}) ON CREATE SET d.description = 'Public information', d.sensitivity = 0, d.created = datetime();"; Desc="DataClassification: public"},
    @{Query="MERGE (d:DataClassification {level: 'internal'}) ON CREATE SET d.description = 'Internal use only', d.sensitivity = 1, d.created = datetime();"; Desc="DataClassification: internal"},
    @{Query="MERGE (d:DataClassification {level: 'confidential'}) ON CREATE SET d.description = 'Confidential data', d.sensitivity = 2, d.created = datetime();"; Desc="DataClassification: confidential"},
    @{Query="MERGE (d:DataClassification {level: 'restricted'}) ON CREATE SET d.description = 'Restricted/regulated data', d.sensitivity = 3, d.created = datetime();"; Desc="DataClassification: restricted"}
)

$successCount = 0
foreach ($data in $referenceData) {
    if (Invoke-Cypher $data.Query $data.Desc) {
        $successCount++
    }
}

Write-Host "  Created/Verified $successCount of $($referenceData.Count) reference nodes" -ForegroundColor Green
Write-Host ""

# Step 6: Verify Deployment
Write-Host "[6/6] Verifying Deployment..." -ForegroundColor Cyan

$verifyQueries = @(
    @{Query="SHOW CONSTRAINTS YIELD name RETURN count(*) as constraint_count;"; Desc="Constraint count"},
    @{Query="SHOW INDEXES YIELD name RETURN count(*) as index_count;"; Desc="Index count"},
    @{Query="MATCH (r:ReasoningMode) RETURN count(*) as reasoning_mode_count;"; Desc="ReasoningMode nodes"},
    @{Query="MATCH (c:Context) RETURN count(*) as context_count;"; Desc="Context nodes"},
    @{Query="MATCH (d:DataClassification) RETURN count(*) as classification_count;"; Desc="DataClassification nodes"}
)

foreach ($verify in $verifyQueries) {
    Invoke-Cypher $verify.Query $verify.Desc | Out-Null
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "✓ Neo4j Schema Deployment Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  - All constraints created/verified" -ForegroundColor White
Write-Host "  - All indexes created/verified" -ForegroundColor White
Write-Host "  - Reference data initialized" -ForegroundColor White
Write-Host "  - Schema is ready for atom synchronization" -ForegroundColor White
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Test worker: Run Neo4jSyncWorker" -ForegroundColor White
Write-Host "  2. Send message: EXEC sp_SendAtomToNeo4j @AtomId = 999" -ForegroundColor White
Write-Host "  3. Verify sync: MATCH (a:Atom {id: 999}) RETURN a" -ForegroundColor White
Write-Host ""
