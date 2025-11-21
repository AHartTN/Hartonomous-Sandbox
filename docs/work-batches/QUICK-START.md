# Quick Start: Deploy Work Batch 3

**Time Required:** ~30 minutes  
**Prerequisites:** Hartonomous Phase 2 deployed, API running

---

## Step 1: Deploy SQL Changes (2 minutes)

Open PowerShell in repository root:

```powershell
# Deploy updated sp_Learn (RLHF integration)
sqlcmd -S localhost -d Hartonomous -E -i "src\Hartonomous.Database\Procedures\dbo.sp_Learn.sql"

# Deploy new sp_ClusterConcepts (unsupervised learning)
sqlcmd -S localhost -d Hartonomous -E -i "src\Hartonomous.Database\Procedures\dbo.sp_ClusterConcepts.sql"
```

**Verify:**
```sql
-- Check procedures exist
SELECT name FROM sys.procedures WHERE name IN ('sp_Learn', 'sp_ClusterConcepts')
```

---

## Step 2: Verify API is Running (1 minute)

```powershell
# Test API health endpoint
curl http://localhost:5000/health
```

**Expected:** HTTP 200 OK

If API is not running:
```powershell
cd src\Hartonomous.Api
dotnet run --configuration Release
```

---

## Step 3: Run Self-Ingestion (15-20 minutes)

```powershell
cd scripts\operations

# Option A: Dry run first (recommended)
.\Seed-HartonomousRepo.ps1 -DryRun

# Option B: Full ingestion
.\Seed-HartonomousRepo.ps1 -ApiBaseUrl "http://localhost:5000" -TenantId 1 -BatchSize 5
```

**Progress:**
- You'll see real-time progress: `? file.cs (2,340 bytes, 15 atoms)`
- Errors are highlighted in red
- Final summary shows: Success count, total size, throughput

**Coffee Break:** This takes 15-20 minutes for ~1,500 files.

---

## Step 4: Validate RLHF Cycle (5 minutes)

```powershell
# Full validation test
.\Test-RLHFCycle.ps1 -SkipSeeding
```

**This will:**
1. ? Test API connectivity
2. ? Generate inference: "What is the IngestionService?"
3. ? Submit feedback (5/5 rating)
4. ? Verify weight adjustments
5. ? Trigger OODA loop

**Expected Output:**
```
? Feedback recorded in database
? AtomRelation weights updated (17 relations)
? Learning metrics recorded
? OODA loop triggered

The 2ms warning is RESOLVED.
```

---

## Step 5: Explore Self-Awareness (5 minutes)

Open your API client (Postman, curl, etc.):

### Query 1: What is the IngestionService?
```http
POST http://localhost:5000/api/v1/reasoning/infer
Content-Type: application/json

{
  "query": "What is the IngestionService and how does it work?",
  "tenantId": 1,
  "maxResults": 10
}
```

**Expected:** Detailed response about `IngestionService.cs`, atomizers, and `sp_IngestAtoms`.

### Query 2: What is the OODA loop?
```http
POST http://localhost:5000/api/v1/reasoning/infer
Content-Type: application/json

{
  "query": "Explain the OODA loop in Hartonomous",
  "tenantId": 1,
  "maxResults": 10
}
```

**Expected:** Explanation of Analyze ? Hypothesize ? Act ? Learn cycle.

### Query 3: How does feedback work?
```http
POST http://localhost:5000/api/v1/reasoning/infer
Content-Type: application/json

{
  "query": "How does user feedback improve the system?",
  "tenantId": 1,
  "maxResults": 10
}
```

**Expected:** Description of RLHF, weight adjustments, and `sp_ProcessFeedback`.

---

## Step 6: Monitor Learning (Ongoing)

Open SQL Server Management Studio or Azure Data Studio:

### Check Feedback Processing
```sql
-- View recent feedback
SELECT TOP 10
    f.FeedbackId,
    f.Rating,
    f.Comments,
    f.FeedbackTimestamp,
    ir.RequestQuery
FROM dbo.InferenceFeedback f
INNER JOIN dbo.InferenceRequests ir ON f.InferenceRequestId = ir.InferenceRequestId
ORDER BY f.FeedbackTimestamp DESC;
```

### Check Weight Adjustments
```sql
-- View recently updated relation weights
SELECT TOP 20
    ar.AtomRelationId,
    ar.Weight,
    ar.UpdatedAt,
    a1.CanonicalText AS SourceAtom,
    a2.CanonicalText AS TargetAtom
FROM dbo.AtomRelation ar
INNER JOIN dbo.Atom a1 ON ar.SourceAtomId = a1.AtomId
INNER JOIN dbo.Atom a2 ON ar.TargetAtomId = a2.AtomId
WHERE ar.UpdatedAt >= DATEADD(HOUR, -1, SYSUTCDATETIME())
ORDER BY ar.UpdatedAt DESC;
```

### Check Learning Metrics
```sql
-- View learning curve
SELECT 
    MetricType,
    MetricValue,
    MeasuredAt
FROM dbo.LearningMetrics
WHERE MeasuredAt >= DATEADD(DAY, -1, SYSUTCDATETIME())
ORDER BY MeasuredAt DESC;
```

### Check Proposed Concepts
```sql
-- View unsupervised discoveries
SELECT 
    ConceptName,
    ConceptDescription,
    AtomCount,
    Density,
    DominantModality,
    Status,
    ProposedAt
FROM dbo.ProposedConcepts
ORDER BY ProposedAt DESC;
```

---

## Step 7: Trigger Concept Discovery (Manual)

If you want to manually trigger unsupervised learning:

```sql
-- Run concept discovery
EXEC dbo.sp_ClusterConcepts 
    @TenantId = 1,
    @MinClusterSize = 5,
    @MaxClusters = 10,
    @DensityThreshold = 0.3;

-- View results
SELECT * FROM dbo.ProposedConcepts WHERE Status = 'Pending' ORDER BY Density DESC;
```

**Note:** This happens automatically via the OODA loop, but you can trigger it manually for testing.

---

## Troubleshooting

### Issue: Seeding fails with "API timeout"
**Solution:** Increase batch size delay:
```powershell
.\Seed-HartonomousRepo.ps1 -BatchSize 3  # Slower, more reliable
```

### Issue: "No atomizer found for file type"
**Solution:** This is expected for unsupported file types (images, binaries). The script skips them automatically.

### Issue: sp_Learn doesn't trigger feedback processing
**Solution:** Ensure feedback threshold is met:
```sql
-- Check pending feedback count
SELECT COUNT(*) FROM dbo.InferenceFeedback 
WHERE FeedbackTimestamp >= DATEADD(HOUR, -1, SYSUTCDATETIME());
```
Threshold is 5 items. If below, wait for more feedback or manually call:
```sql
EXEC dbo.sp_ProcessFeedback @InferenceId = <your_id>, @Rating = 5, @Comments = 'Test', @UserId = 'admin';
```

### Issue: Test-RLHFCycle.ps1 fails
**Solution:** Verify prerequisites:
1. API is running: `curl http://localhost:5000/health`
2. SQL Server is accessible: `sqlcmd -S localhost -Q "SELECT 1"`
3. Database exists: `sqlcmd -S localhost -Q "SELECT DB_ID('Hartonomous')"`

---

## What Success Looks Like

After completing these steps, you should see:

1. **Self-Awareness:**
   - System answers questions about its own code
   - Queries return relevant source file atoms

2. **Learning:**
   - Feedback is recorded in `InferenceFeedback` table
   - Weights in `AtomRelation` are updated
   - Metrics in `LearningMetrics` show convergence

3. **Discovery:**
   - New concepts proposed in `ProposedConcepts`
   - Orphan atoms being clustered

4. **Autonomy:**
   - OODA loop runs automatically
   - Feedback triggers weight adjustments
   - No manual intervention needed

---

## Next Steps

1. **Production Deployment:**
   - Deploy to staging environment
   - Run validation tests
   - Monitor for 24 hours
   - Deploy to production

2. **Optimization:**
   - Review `ProposedConcepts` and approve valid ones
   - Adjust learning rate if weights diverge
   - Tune clustering parameters for better discovery

3. **Scaling:**
   - Increase batch size for faster ingestion
   - Add more atomizers for new file types
   - Implement multi-tenant seeding

---

## Completion Checklist

- [ ] SQL procedures deployed (`sp_Learn`, `sp_ClusterConcepts`)
- [ ] API health check passed
- [ ] Self-ingestion completed (~1,500 files)
- [ ] RLHF cycle validated
- [ ] Self-awareness queries successful
- [ ] Feedback processing verified
- [ ] Learning metrics visible
- [ ] Concept discovery tested

**When all checkboxes are complete, Work Batch 3 is deployed. ??**

---

**Questions?**  
Refer to: `docs\work-batches\work-batch-3-completion.md`
