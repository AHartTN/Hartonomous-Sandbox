# ?? **STEP A: TESTING - READY TO EXECUTE**

**Status**: ? All implementation complete, ready for testing  
**Your Advantage**: Local Ollama models at `D:\Models` ??

---

## **?? WHAT'S READY**

### **? Implementation Complete**:
- ? BackgroundJobService (job queue management)
- ? IngestionService (automatic job creation)
- ? EmbeddingGeneratorWorker (job polling & processing)
- ? All projects build successfully (0 errors, 0 warnings)

### **? Test Files Created**:
1. `tests\Quick_Smoke_Test.sql` - Verify database setup
2. `tests\Run_Integration_Test.ps1` - Full end-to-end test
3. `src\Hartonomous.Workers.EmbeddingGenerator\appsettings.Development.json` - Configured for your Ollama setup

---

## **?? QUICK START**

### **Option 1: Manual SQL Test (5 minutes)**

1. **Run Smoke Test**:
   - Open `tests\Quick_Smoke_Test.sql` in SSMS
   - Execute against Hartonomous database
   - Verify: BackgroundJob table exists, CLR functions deployed

2. **Create Test Job Manually**:
   ```sql
   USE Hartonomous;
   
   -- Insert test atom
   INSERT INTO dbo.Atom (TenantId, Modality, Subtype, ContentHash, CanonicalText, AtomicValue, CreatedAt)
   VALUES (0, 'text', 'test', HASHBYTES('SHA2_256', 'Test embedding'), 'Test embedding', 0x54657374, GETUTCDATE());
   
   DECLARE @AtomId BIGINT = SCOPE_IDENTITY();
   
   -- Create embedding job
   INSERT INTO dbo.BackgroundJob (JobType, Payload, Status, TenantId, Priority, MaxRetries, AttemptCount, CreatedAtUtc)
   VALUES (
       'GenerateEmbedding',
       '{"AtomId": ' + CAST(@AtomId AS NVARCHAR(20)) + ', "TenantId": 0, "Modality": "text"}',
       0, 0, 5, 3, 0, GETUTCDATE()
   );
   
   SELECT @AtomId AS TestAtomId, 'Job created' AS Status;
   ```

3. **Start Worker**:
   ```powershell
   cd D:\Repositories\Hartonomous\src\Hartonomous.Workers.EmbeddingGenerator
   dotnet run
   ```

4. **Watch Logs** - Should see:
   ```
   [INFO] Found 1 pending embedding jobs
   [INFO] Generating embedding for atom: AtomId=123, Modality=text
   [INFO] Embedding created: AtomId=123, Dimension=1536, Hilbert=485729203
   ```

5. **Verify Results**:
   ```sql
   -- Check job completed
   SELECT JobId, Status, ResultData 
   FROM dbo.BackgroundJob 
   WHERE JobType = 'GenerateEmbedding' 
   ORDER BY CreatedAtUtc DESC;
   
   -- Check embedding created with spatial data
   SELECT TOP 1 
       AtomId,
       Dimension,
       SpatialKey.ToString() AS SpatialKey,
       HilbertValue,
       SpatialBucketX, SpatialBucketY, SpatialBucketZ
   FROM dbo.AtomEmbedding 
   ORDER BY CreatedAt DESC;
   ```

**? Success**: Spatial data populated (not 0,0,0), HilbertValue > 0

---

### **Option 2: Automated Integration Test (15 minutes)**

1. **Ensure Ollama is running**:
   ```powershell
   # Check if running
   curl http://localhost:11434/api/tags
   
   # If not, start it
   ollama serve
   ```

2. **Run integration test**:
   ```powershell
   cd D:\Repositories\Hartonomous
   .\tests\Run_Integration_Test.ps1
   ```

   The script will:
   - ? Verify Ollama is running
   - ? Build solution
   - ? Prompt you to run SQL smoke test
   - ? Create test file
   - ? Start worker in separate window
   - ? Guide you through manual file upload
   - ? Monitor job processing

3. **Upload test file**:
   - Use Swagger: `https://localhost:5001/swagger`
   - Endpoint: `POST /api/ingestion/file`
   - Upload: `D:\Repositories\Hartonomous\tests\test_sample.txt`

4. **Watch for**:
   - Worker logs: "Generating embedding for atom..."
   - SQL query: Job status changes to Completed
   - SQL query: AtomEmbedding record with spatial data

---

## **? SUCCESS CRITERIA**

### **Job Queue Works**:
```sql
SELECT COUNT(*) FROM dbo.BackgroundJob 
WHERE JobType = 'GenerateEmbedding' AND Status = 2; -- Completed
```
**Expected**: Count > 0

### **Embeddings Have Spatial Data**:
```sql
SELECT 
    COUNT(*) AS TotalEmbeddings,
    COUNT(CASE WHEN SpatialKey IS NOT NULL THEN 1 END) AS WithSpatial,
    COUNT(CASE WHEN HilbertValue > 0 THEN 1 END) AS WithHilbert,
    AVG(Dimension) AS AvgDimension
FROM dbo.AtomEmbedding;
```
**Expected**: 
- WithSpatial = TotalEmbeddings
- WithHilbert = TotalEmbeddings  
- AvgDimension ? 768-1998

### **Spatial Data is Real** (not placeholders):
```sql
SELECT TOP 1
    SpatialKey.STX AS X,
    SpatialKey.STY AS Y,
    SpatialKey.Z AS Z,
    HilbertValue,
    SpatialBucketX, SpatialBucketY, SpatialBucketZ
FROM dbo.AtomEmbedding
WHERE SpatialKey IS NOT NULL;
```
**Expected**: X, Y, Z != 0, HilbertValue > 0, Buckets != 0

---

## **?? TROUBLESHOOTING**

### **Worker doesn't pick up jobs**:
- Check connection string in `appsettings.Development.json`
- Verify BackgroundJob.Status = 0 (Pending)
- Check worker logs for errors

### **CLR functions not found**:
```sql
-- Verify CLR deployed
SELECT name FROM sys.assemblies WHERE name LIKE '%Embedding%';
SELECT name FROM sys.objects WHERE name IN ('fn_ComputeEmbedding', 'fn_ProjectTo3D', 'clr_ComputeHilbertValue');
```
If missing, run: `.\scripts\Deploy-CLR-Functions.ps1`

### **Ollama connection fails**:
- Verify: `curl http://localhost:11434/api/tags`
- Check port: Default is 11434
- Model available: `ollama list`

### **Job fails immediately**:
```sql
SELECT JobId, ErrorMessage, ErrorStackTrace 
FROM dbo.BackgroundJob 
WHERE Status = 3 -- Failed
ORDER BY CreatedAtUtc DESC;
```

---

## **?? EXPECTED TIMELINE**

| Task | Duration |
|------|----------|
| SQL Smoke Test | 5 minutes |
| Manual Job Test | 10 minutes |
| Full Integration Test | 15 minutes |
| **Total** | **30 minutes** |

---

## **?? WHEN DONE**

You'll have verified:
- ? File upload creates atoms
- ? Atoms trigger embedding jobs automatically
- ? Worker polls and processes jobs
- ? Real embeddings generated with CLR functions
- ? Spatial projection, Hilbert indexing, bucket computation
- ? Job status tracked and updated
- ? **100% END-TO-END INTEGRATION WORKING!**

---

**Ready to test? Start with Option 1 (Manual SQL Test) for quickest validation!** ??

---

*Note: Your local Ollama at D:\Models is perfect for testing without cloud dependencies!*
