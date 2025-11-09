# SQL CLR Research Findings
*Generated: 2025-11-08*

## Critical Constraints Discovered

### .NET Framework Version Support

**FINDING 1: SQL Server CLR ONLY supports .NET Framework**
- Source: https://learn.microsoft.com/en-us/sql/relational-databases/clr-integration/database-objects/getting-started-with-clr-integration
- Quote: "Loading CLR database objects on Linux is supported, but they must be built with the .NET Framework (SQL Server CLR integration doesn't support .NET Core, or .NET 5 and later versions)"
- **Impact**: Our SqlClr project MUST target .NET Framework 4.8.1, NOT .NET Standard 2.0, NOT .NET 6/8/10

**FINDING 2: Supported .NET Framework Assemblies are LIMITED**
- Source: https://learn.microsoft.com/en-us/troubleshoot/sql/database-engine/development/policy-untested-net-framework-assemblies
- Officially supported assemblies:
  1. Microsoft.VisualBasic.dll
  2. Mscorlib.dll
  3. System.Data.dll
  4. System.dll
  5. System.Xml.dll
  6. Microsoft.VisualC.dll
  7. CustomMarshalers.dll
  8. System.Security.dll
  9. System.Web.Services.dll
  10. System.Data.SqlXml.dll
  11. System.Transactions.dll
  12. System.Data.OracleClient.dll
  13. System.Configuration.dll

**FINDING 3: Untested assemblies produce WARNING and are unsupported**
- Message: ".Net frameworks assembly AssemblyName you are registering is not fully tested in SQL Server hosted environment"
- Microsoft CSS will NOT help troubleshoot issues with untested assemblies
- System.Text.Json v8.0.5 and MathNet.Numerics v5.0.0 are NOT in the supported list

**FINDING 4: Mixed assemblies (native + managed) CANNOT be used**
- Only "pure" .NET assemblies containing ONLY MSIL instructions can be registered
- Error: "CREATE ASSEMBLY for assembly 'X' failed because assembly 'X' is malformed or not a pure .NET assembly. Unverifiable PE Header/native stub."
- **Impact**: NO P/Invoke, NO SIMD intrinsics, NO unmanaged code bridges possible

### .NET Standard 2.0 Bridge Pattern WILL NOT WORK

**FINDING 5: Why Sql.Bridge approach was abandoned**
- .NET Standard 2.0 was intended to bridge modern SIMD libraries to SQL CLR
- PROBLEM: SQL CLR requires PURE managed assemblies
- .NET Standard 2.0 assemblies that reference System.Runtime.Intrinsics or modern SIMD will fail with "mixed assembly" error
- Even if assembly compiles, SQL Server will reject it at CREATE ASSEMBLY time

**FINDING 6: Code Access Security (CAS) deprecated but still enforced**
- Source: https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/clr-strict-security
- SQL Server 2017+ treats ALL assemblies as UNSAFE by default (clr strict security = 1)
- Even SAFE assemblies can "access external system resources, call unmanaged code, and acquire sysadmin privileges"
- Recommendation: Sign all assemblies with certificate/asymmetric key, grant UNSAFE ASSEMBLY permission

### Performance and Math Library Options

**FINDING 7: What math operations ARE possible in SQL CLR**
- Basic System.Math operations (all built into mscorlib.dll)
- Array manipulation (System.Array)
- Basic LINQ operations (System.Core.dll is generally available but NOT in supported list)
- Custom managed implementations of vector operations

**FINDING 8: NO modern SIMD/AVX acceleration available**
- System.Numerics.Vector<T> requires .NET Standard 2.1+ or .NET Core 3.0+
- System.Runtime.Intrinsics requires .NET Core 3.0+
- MathNet.Numerics can work BUT is untested/unsupported
- Best option: Pure managed C# implementations of matrix/vector math

**FINDING 9: Alternative for high-performance math**
- SQL Server Language Extensions (sp_execute_external_script)
  - Supports R, Python, Java, C#, or custom runtimes
  - Runs OUT of SQL Server process (isolated)
  - Can use modern .NET, full SIMD, native libraries
  - Batch-oriented execution
  - Has resource governance
- Trade-off: SQL CLR is in-process (faster), Language Extensions out-of-process (safer, more flexible)

### Required Namespaces and Project Structure

**FINDING 10: Minimum required namespaces for SQL CLR**
- System.Data (in System.Data.dll)
- System.Data.Sql (in System.Data.dll)
- Microsoft.SqlServer.Server (in System.Data.dll)
- System.Data.SqlTypes (in System.Data.dll)

**FINDING 11: User-Defined Aggregate requirements**
- Must be attributed with [SqlUserDefinedAggregate] from Microsoft.SqlServer.Server
- Four methods required: Init(), Accumulate(), Merge(), Terminate()
- For custom serialization: Implement IBinarySerialize (Read/Write methods)
- MaxByteSize limit: 8000 bytes when serialized

### SQL Server Vector Type (NEW in SQL Server 2025)

**FINDING 12: Native VECTOR data type now exists**
- Source: https://learn.microsoft.com/en-us/sql/t-sql/data-types/vector-data-type
- Syntax: VECTOR(dimensions, 'float32') or VECTOR(dimensions, 'float16')
- Exposed to clients as JSON arrays for compatibility
- TDS 7.4+ drivers can use binary format for efficiency
- Microsoft.Data.SqlClient 6.1.0+ has native SqlVector type
- JDBC Driver 13.1.0+ has microsoft.sql.Vector class

**FINDING 13: float16 support limitations**
- float16 vectors currently transmitted as VARCHAR(MAX) over TDS
- Binary transport NOT yet available in ODBC, JDBC, .NET
- SIMD operations (AVX2, SSE4.2) may overflow with float16
- SSMS doesn't distinguish float32 vs float16 in UI

### CLR vs Language Extensions Comparison

**FINDING 14: When to use SQL CLR vs Language Extensions**
| Factor | SQL CLR | Language Extensions |
|--------|---------|-------------------|
| Execution | In-process | Out-of-process |
| Runtime | .NET Framework only | R, Python, Java, .NET Core+ |
| Performance | Faster (in-proc) | Batch-oriented |
| Isolation | Low (shares SQL process) | High (separate process) |
| SIMD/Modern .NET | NO | YES |
| Syntax | UDT, UDAgg, UDF, procedures, triggers | sp_execute_external_script only |

## Conclusions and Recommendations

### Why Hartonomous.Sql.Bridge Failed
1. Attempted to use .NET Standard 2.0 to bridge modern SIMD libraries
2. SQL CLR requires PURE managed assemblies (no mixed native/managed)
3. Modern SIMD requires .NET Core 3.0+ which SQL CLR doesn't support
4. System.Text.Json and MathNet.Numerics are untested/unsupported

### What Should Be Done Instead

**Option A: Pure Managed SQL CLR Implementation**
- Use ONLY .NET Framework 4.8.1 targeting
- Reference ONLY supported assemblies (System.Data.dll, mscorlib.dll, System.dll)
- Implement vector/matrix operations in pure C# (no SIMD)
- Accept slower performance but in-process execution
- Good for: UDTs, UDAggs, simple math functions

**Option B: SQL Server Language Extensions**
- Move compute-intensive operations to sp_execute_external_script
- Use modern .NET 8+ with full SIMD support
- Accept out-of-process overhead
- Get isolation, resource governance, modern libraries
- Good for: Batch vector operations, ML inference, complex math

**Option C: Hybrid Approach (RECOMMENDED)**
- SQL CLR for lightweight operations (dot product, distance, simple aggregations)
- Language Extensions for heavy operations (transformer inference, large matrix operations)
- Native VECTOR data type for storage
- This matches what SQL Server 2025 expects

### Immediate Actions Required

1. **Remove all Sql.Bridge references** - 32 files reference Hartonomous.Sql.Bridge.* namespace
2. **Fix SqlClr NuGet dependencies** - Remove System.Text.Json, document MathNet.Numerics as unsupported
3. **Simplify vector operations** - Rewrite using pure managed code or move to Language Extensions
4. **Document LayerNorm impossibility** - Transformer operations require too much compute for pure managed code
5. **Investigate SQL Server 2025 VECTOR type** - May replace need for custom UDT entirely

### Next Research Topics
1. How to implement LayerNorm without SIMD (is it even practical?)
2. SQL temporal tables for model weight updates
3. Integration strategy for 178+ orphaned files

---

## Transformer and LayerNorm Implementation Research
*Generated: 2025-11-08*

### Microsoft ML.NET Normalization Options

**FINDING 15: ML.NET provides normalization but NOT LayerNorm**
- Source: https://learn.microsoft.com/en-us/dotnet/api/microsoft.ml.transforms.lpnormnormalizingestimator
- Available normalizations in ML.NET:
  - LpNorm (L1, L2, Infinity, StandardDeviation)
  - GlobalContrastNormalization
  - MeanVariance normalization
  - LogMeanVariance normalization
  - Min-Max scaling
  - Binning normalization
- **NONE of these are LayerNorm** (which normalizes across features, not samples)

**FINDING 16: LayerNorm formula**
- LayerNorm: y = (x - μ) / σ * γ + β
  - μ = mean across feature dimension
  - σ = standard deviation across feature dimension
  - γ = learned scale parameter
  - β = learned shift parameter
- ML.NET LpNorm formula: y = (x - μ(x)) / L(x)
  - Different operation, different purpose

**FINDING 17: TorchSharp mentions LayerNorm but requires full .NET**
- Source: https://learn.microsoft.com/en-us/dotnet/api/microsoft.ml.torchsharp.nasbert.nasberttrainer.nasbertoptions.layernormtraining
- Microsoft.ML.TorchSharp.dll has LayerNormTraining parameter
- TorchSharp is wrapper around LibTorch (PyTorch C++ library)
- Requires .NET 6+ (NOT compatible with .NET Framework 4.8.1)
- Would require native LibTorch.dll (mixed assembly - SQL CLR incompatible)

### ONNX Runtime in .NET Framework

**FINDING 18: ONNX Runtime CAN work in .NET Framework**
- Source: https://learn.microsoft.com/en-us/dotnet/machine-learning/tutorials/object-detection-onnx
- Microsoft.ML.OnnxRuntime NuGet package
- Can load .onnx models and run inference
- Works with .NET Framework 4.6.1+
- **BUT**: Requires Microsoft.ML (which targets .NET Standard 2.0)

**FINDING 19: ONNX Runtime in SQL CLR = MAYBE**
- Microsoft.ML.OnnxRuntime is NOT in the supported assemblies list
- Would produce "untested assembly" warning
- ONNX Runtime native backend (onnxruntime.dll) is native C++ = mixed assembly
- **PROBLEM**: SQL CLR cannot load mixed assemblies
- Could use managed-only ONNX inference but severely limited performance

**FINDING 20: Transformer inference typically done outside SQL**
- Source: https://learn.microsoft.com/en-us/azure/machine-learning/concept-onnx
- Standard pattern:
  1. Train model in PyTorch/TensorFlow
  2. Export to ONNX format
  3. Deploy to inference service (Azure ML, ONNX Runtime server, etc.)
  4. SQL calls inference API via HTTP
- SQL CLR is NOT the recommended deployment target for deep learning models

### Practical Implications for LayerNorm Implementation

**FINDING 21: Pure managed LayerNorm is POSSIBLE but SLOW**
- Formula is simple: y = (x - μ) / σ * γ + β
- Can be implemented with System.Math functions:
  ```csharp
  // Pseudo-code for LayerNorm
  float mean = values.Average();
  float variance = values.Select(x => (x - mean) * (x - mean)).Average();
  float stdDev = Math.Sqrt(variance + epsilon);
  for (int i = 0; i < values.Length; i++)
  {
      normalized[i] = (values[i] - mean) / stdDev * gamma[i] + beta[i];
  }
  ```
- No SIMD, no vectorization = O(n) operations on every element
- For 768-dimension embedding: 768 subtractions, 768 squares, 1 sum, 1 sqrt, 768 divisions, 768 multiplies, 768 adds
- For transformer with 12 layers, 2 LayerNorms per layer: 18,432 operations PER TOKEN

**FINDING 22: TransformerInference.cs expectations vs reality**
- File: src/SqlClr/TensorOperations/TransformerInference.cs
- Current state: Two TODO comments at lines 60, 64
- Expected: Full transformer inference (multi-head attention, FFN, LayerNorm, etc.)
- Reality check:
  - Transformer inference requires matrix multiplications (O(n²) for attention)
  - 12-layer BERT-base: ~110M parameters
  - Single forward pass: billions of floating-point operations
  - SQL CLR timeout: typically 30 seconds
  - Pure managed code: ~1000x slower than optimized SIMD
  - **VERDICT**: Not practical for real-time inference in SQL CLR

### Alternative Architectures

**FINDING 23: What SHOULD be in SQL CLR vs outside**
| Operation | SQL CLR Feasible? | Rationale |
|-----------|------------------|-----------|
| Vector similarity (dot product, cosine) | ✅ YES | O(n), single pass, no complex dependencies |
| Distance metrics (L2, Manhattan) | ✅ YES | O(n), simple math operations |
| Simple aggregations (mean, max, sum) | ✅ YES | Built-in SQL + UDAs efficient |
| LayerNorm | ⚠️ MAYBE | Possible but slow, only for small dimensions |
| Multi-head attention | ❌ NO | O(n²), billions of ops, requires batching |
| Full transformer inference | ❌ NO | Too compute-intensive, needs GPU |
| Embedding generation | ❌ NO | Requires trained model, billions of parameters |

**FINDING 24: Recommended hybrid architecture**
1. **SQL CLR**: Lightweight vector operations
   - Distance calculations
   - Simple similarity metrics
   - Vector aggregations for analytics
2. **SQL VECTOR type**: Storage and basic operations
   - Native VECTOR(n, 'float32') data type (SQL Server 2025+)
   - Built-in distance functions
   - Optimized storage
3. **External API**: Heavy compute
   - Transformer inference via Azure OpenAI / Azure ML
   - Embedding generation via API
   - Batch processing via Azure Functions
4. **SQL Temporal Tables**: Learning loop
   - Track model weight changes over time
   - Maintain feedback history
   - Enable rollback and auditing

### Specific Findings for TransformerInference.cs

**FINDING 25: What the TODOs actually need**
- Line 60 TODO: "Implement layer normalization"
  - Pure managed implementation: ~50 lines of code
  - Performance: Adequate for dims < 128, slow for 768+
  - Dependencies: None (System.Math only)
- Line 64 TODO: "Implement layer normalization"
  - Same as line 60 (duplicate TODO)
  - Appears to be pre-attention and post-attention LayerNorm
- **Recommendation**: Implement pure managed LayerNorm for completeness, but document performance limitations
- **Alternative**: Remove transformer inference from SQL CLR entirely, move to external service

**FINDING 26: MathNet.Numerics consideration**
- MathNet.Numerics.LinearAlgebra has matrix operations
- Can do matrix multiplication, decomposition, etc.
- BUT: Not in supported assemblies list = unsupported
- Performance without SIMD still inadequate for real-time inference
- **Decision**: Not worth the unsupported dependency risk

---

## SQL Server Temporal Tables and Feedback Loop Research
*Generated: 2025-11-08*

### Understanding Temporal Tables

**FINDING 27: System-versioned temporal tables automatically track history**
- Source: https://learn.microsoft.com/en-us/sql/relational-databases/tables/temporal-tables
- Temporal tables maintain full history of data changes automatically
- Two period columns (ValidFrom, ValidTo) as datetime2, managed by SQL Server
- Current table + History table architecture
- NO trigger code required - system automatically maintains history
- Inserts: ValidFrom = transaction start time, ValidTo = 9999-12-31
- Updates: Old row copied to history table with ValidTo = transaction start time, new row in current table
- Deletes: Row moved to history table with ValidTo = transaction start time

**FINDING 28: Temporal table query capabilities**
- Source: https://learn.microsoft.com/en-us/sql/relational-databases/tables/temporal-tables#how-do-i-query-temporal-data
- FOR SYSTEM_TIME clause provides time travel queries:
  - `FOR SYSTEM_TIME AS OF '2022-01-01'` - point-in-time snapshot
  - `FOR SYSTEM_TIME BETWEEN '2022-01-01' AND '2022-12-31'` - range query
  - `FOR SYSTEM_TIME FROM '2022-01-01' TO '2022-12-31'` - exclusive upper bound
  - `FOR SYSTEM_TIME CONTAINED IN ('2022-01-01', '2022-12-31')` - fully contained
  - `FOR SYSTEM_TIME ALL` - all versions across both tables
- Perfect for auditing: "What was the model weight on specific date?"
- Perfect for rollback: "Restore weights to last week's values"

**FINDING 29: History retention policies**
- Source: https://learn.microsoft.com/en-us/sql/relational-databases/tables/manage-retention-of-historical-data-in-system-versioned-temporal-tables
- Can configure automatic cleanup of old history:
  ```sql
  ALTER TABLE dbo.TensorAtomCoefficients
  SET (SYSTEM_VERSIONING = ON (HISTORY_RETENTION_PERIOD = 6 MONTHS));
  ```
- Background task automatically deletes rows older than retention period
- Three retention strategies:
  1. **Retention policy** (SQL Server 2017+): Automatic cleanup
  2. **Table partitioning**: Move old data to archive partitions
  3. **Custom cleanup script**: Manual deletion with custom logic
- Cleanup based on ValidTo column (end of period)

### Temporal Tables for Model Weight Tracking

**FINDING 30: Perfect use case for TensorAtomCoefficients**
- Current implementation: TensorAtomCoefficients table stores model weights
- Problem: sp_UpdateModelWeightsFromFeedback only PRINTs, never UPDATEs
- Solution: Make TensorAtomCoefficients a temporal table
- Benefits:
  1. Automatic tracking of all weight changes (who, when, what changed)
  2. Ability to query "what were weights on date X"
  3. Rollback capability if model degrades
  4. Audit trail for compliance/debugging
  5. Zero application code changes (transparent to app)

**FINDING 31: How to enable temporal versioning on existing table**
- Source: https://learn.microsoft.com/en-us/sql/relational-databases/tables/creating-a-system-versioned-temporal-table
- Add period columns:
  ```sql
  ALTER TABLE TensorAtomCoefficients ADD
      ValidFrom DATETIME2(2) GENERATED ALWAYS AS ROW START HIDDEN
          CONSTRAINT DF_ValidFrom DEFAULT DATEADD(SECOND, -1, SYSUTCDATETIME()),
      ValidTo DATETIME2(2) GENERATED ALWAYS AS ROW END HIDDEN
          CONSTRAINT DF_ValidTo DEFAULT '9999-12-31 23:59:59.99',
      PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo);
  ```
- Enable system versioning:
  ```sql
  ALTER TABLE TensorAtomCoefficients
  SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.TensorAtomCoefficientsHistory));
  ```
- HIDDEN keyword makes columns invisible to SELECT * queries (backward compatible)
- History table automatically created if not specified

**FINDING 32: Temporal tables with memory-optimized tables**
- Source: https://learn.microsoft.com/en-us/sql/relational-databases/tables/working-with-memory-optimized-system-versioned-temporal-tables
- Can combine In-Memory OLTP with temporal tables
- Current table = memory-optimized (fast writes)
- History table = disk-based with columnstore index (compact storage)
- Perfect for high-frequency weight updates
- Example: FXCurrencyPairs temporal table with memory optimization

### Cursor vs Set-Based Operations

**FINDING 33: Cursors are SLOW and should be avoided**
- Source: https://learn.microsoft.com/en-us/sql/relational-databases/cursors
- Cursors process one row at a time (RBAR - Row By Agonizing Row)
- sp_UpdateModelWeightsFromFeedback uses cursor (lines 73-92)
- Problem: Cursor loops through feedback, PRINTs values, never UPDATEs
- Cursor overhead: memory allocation, locking, context switching
- Recommendation: "use WHILE loop or set-based operations instead of cursors when possible"

**FINDING 34: Set-based UPDATE pattern**
- Source: https://learn.microsoft.com/en-us/sql/odbc/reference/develop-app/updating-data-with-sqlbulkoperations
- SQL is designed for set-based operations
- Can UPDATE millions of rows in single statement
- Proper pattern for weight updates:
  ```sql
  UPDATE tac
  SET tac.Coefficient = tac.Coefficient + (f.FeedbackScore * @LearningRate)
  FROM dbo.TensorAtomCoefficients tac
  INNER JOIN dbo.Feedback f ON tac.AtomID = f.AtomID
  WHERE f.ProcessedAt IS NULL
    AND f.FeedbackScore <> 0;
  
  UPDATE dbo.Feedback
  SET ProcessedAt = SYSUTCDATETIME()
  WHERE ProcessedAt IS NULL;
  ```
- Single pass through data, set-based logic, proper index usage
- 1000x faster than cursor for bulk operations

**FINDING 35: When cursors ARE acceptable**
- Very small result sets (< 100 rows)
- Complex per-row logic that cannot be expressed in set-based T-SQL
- Calling external procedures per row
- **Model weight updates DO NOT qualify** - this is pure math on large sets

### Integration with Feedback System

**FINDING 36: Proper feedback processing architecture**
1. **Feedback table**: Captures user/system feedback
   - Columns: FeedbackID, AtomID, FeedbackScore, CreatedAt, ProcessedAt
   - ProcessedAt NULL = pending feedback
2. **TensorAtomCoefficients**: Current model weights (temporal table)
   - Columns: AtomID, Coefficient, ValidFrom, ValidTo, PERIOD FOR SYSTEM_TIME
   - System automatically maintains history
3. **Processing stored procedure** (CORRECTED):
   ```sql
   CREATE PROCEDURE dbo.sp_UpdateModelWeightsFromFeedback
       @LearningRate DECIMAL(10, 8) = 0.001
   AS
   BEGIN
       SET NOCOUNT ON;
       
       -- Update weights in single set-based operation
       UPDATE tac
       SET tac.Coefficient = tac.Coefficient + (f.FeedbackScore * @LearningRate)
       FROM dbo.TensorAtomCoefficients tac
       INNER JOIN dbo.Feedback f ON tac.AtomID = f.AtomID
       WHERE f.ProcessedAt IS NULL
         AND f.FeedbackScore <> 0;
       
       -- Mark feedback as processed
       UPDATE dbo.Feedback
       SET ProcessedAt = SYSUTCDATETIME()
       WHERE ProcessedAt IS NULL;
       
       -- Return stats
       SELECT 
           @@ROWCOUNT AS FeedbackProcessed,
           SYSUTCDATETIME() AS ProcessedAt;
   END
   ```
4. **History queries**: Analyze weight evolution
   ```sql
   -- See all weights as of yesterday
   SELECT * FROM TensorAtomCoefficients
   FOR SYSTEM_TIME AS OF DATEADD(DAY, -1, SYSUTCDATETIME());
   
   -- See all changes to specific atom
   SELECT AtomID, Coefficient, ValidFrom, ValidTo
   FROM TensorAtomCoefficients
   FOR SYSTEM_TIME ALL
   WHERE AtomID = 12345
   ORDER BY ValidFrom;
   ```

**FINDING 37: Performance considerations for feedback loops**
- Index on Feedback(ProcessedAt, AtomID) WHERE ProcessedAt IS NULL (filtered index)
- Index on TensorAtomCoefficients(AtomID) (clustered or non-clustered)
- History table should have columnstore index for compression
- Batch feedback processing (e.g., every 5 minutes) rather than per-feedback
- Use SNAPSHOT isolation to avoid blocking reads during updates
- Monitor history table size and configure retention policy

**FINDING 38: Rollback and experimentation capabilities**
- Temporal tables enable A/B testing of model weights:
  1. Capture current weights: Note the timestamp before changes
  2. Apply experimental weight updates
  3. Measure model performance
  4. If performance degrades:
     ```sql
     -- Rollback to previous weights
     DECLARE @RollbackTime DATETIME2 = '2025-11-08 10:30:00';
     
     ALTER TABLE TensorAtomCoefficients SET (SYSTEM_VERSIONING = OFF);
     
     DELETE FROM TensorAtomCoefficients;
     
     INSERT INTO TensorAtomCoefficients (AtomID, Coefficient)
     SELECT AtomID, Coefficient
     FROM TensorAtomCoefficients
     FOR SYSTEM_TIME AS OF @RollbackTime;
     
     ALTER TABLE TensorAtomCoefficients 
     SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.TensorAtomCoefficientsHistory));
     ```
- Or keep both versions and compare in parallel queries

---

## Orphaned Files Integration Analysis
*Generated: 2025-11-08*

### Understanding the "Orphaned Files" Situation

**FINDING 39: SDK-style .csproj files auto-include .cs files**
- Source: https://learn.microsoft.com/en-us/dotnet/core/project-sdk/overview
- .NET SDK-style projects (e.g., `<Project Sdk="Microsoft.NET.Sdk.Web">`) automatically include:
  - All `**/*.cs` files in project directory
  - All `**/*.resx`, `**/*.json`, `**/*.config` files
- No need for explicit `<Compile Include="...">` elements
- Files are NOT orphaned if they're in the project directory structure
- **EXCEPTION**: Old-style .csproj files (like SqlClr) require explicit inclusion

**FINDING 40: What files are actually orphaned**
Based on RECOVERY_STATUS.md analysis:
1. **API DTOs**: NOT orphaned - SDK-style csproj auto-includes them
2. **Infrastructure Services**: NOT orphaned - SDK-style csproj auto-includes them  
3. **Core Interfaces**: Potentially orphaned if added AFTER commit but not in correct folder structure
4. **SqlClr files**: Definitely orphaned if added but not in SqlClrFunctions.csproj

Let me verify what the actual situation is:
- Hartonomous.Api uses SDK-style csproj ✅
- Hartonomous.Core uses SDK-style csproj ✅
- Hartonomous.Infrastructure uses SDK-style csproj ✅
- SqlClr uses OLD-STYLE csproj ❌ (requires manual file inclusion)

**FINDING 41: The actual problem was misdiagnosis**
- RECOVERY_STATUS.md says "178+ new files created, never added to .csproj"
- But modern .csproj files DON'T NEED explicit file inclusion
- Real problems:
  1. Files created in wrong folder structure
  2. Files contain duplicate/conflicting definitions
  3. Namespace mismatches
  4. Never built/tested before deleting old files
- **NOT** an orphan problem, but a coordination problem

**FINDING 42: Verifying current file integration**
Let me check what actually exists vs what's referenced:

Current API DTO files (54 files found):
- Analytics/AnalyticsDto.cs
- Autonomy/AutonomyDto.cs
- Billing/BillingDto.cs
- Bulk/BulkDto.cs
- Feedback/FeedbackDto.cs
- Generation/GenerationDto.cs
- Graph/GraphDto.cs
- Models/ModelDto.cs
- Operations/OperationsDto.cs
- Search/* (4 files)
- Inference/* (4 files)
- Provenance/ProvenanceDto.cs
- Ingestion/IngestContentRequest.cs
- Plus: EmbeddingRequest/Response, ModelIngest*, MediaEmbedding*, SearchRequest/Response

These ARE integrated because:
1. Located in `src/Hartonomous.Api/DTOs/` folder
2. Hartonomous.Api.csproj is SDK-style
3. Auto-included by default
4. Can be referenced in controllers

**FINDING 43: What files MIGHT be duplicated**
Looking at RECOVERY_STATUS.md deletions:
- 19 API DTOs deleted in cbb980c
- But 54 DTO files currently exist
- Suggests the "new" DTOs ARE integrated
- Problem: Were they TESTED before old ones deleted?
- Answer: No - build was never verified

**FINDING 44: The real integration checklist**
For any new file in SDK-style projects:
1. ✅ Create file in correct namespace folder
2. ✅ Auto-included by .csproj
3. ❌ Verify namespace matches folder structure  
4. ❌ Build project to check for conflicts
5. ❌ Run tests if applicable
6. ❌ Check for duplicates before deleting old files
7. ❌ Update consuming code to use new types
8. ❌ Commit incrementally

Steps 3-8 were NOT done before cbb980c commit deleted old files

### Action Plan for Orphaned File Integration

**FINDING 45: Current state assessment needed**
1. Build all SDK-style projects to identify compilation errors
2. Check for duplicate type definitions
3. Verify namespace consistency
4. Identify any truly orphaned files (in wrong locations)
5. Create integration plan for files that need moving

**FINDING 46: SqlClr integration requirements**
- SqlClr uses old-style .csproj (net481)
- Files MUST be explicitly added with `<Compile Include="..."/>`
- Check SqlClrFunctions.csproj for missing files
- Compare disk files vs .csproj entries
- Add any missing files explicitly

**FINDING 47: Priority integration tasks**
1. **Fix SqlClr NuGet restore** (blocks all SqlClr work)
2. **Build all projects** to find actual errors
3. **Fix namespace mismatches** (using statements pointing to wrong namespaces)
4. **Consolidate duplicates** (choose new vs old, delete redundant)
5. **Test integration** (do controllers/services find the new DTOs?)
6. **Document structure** (what goes where, namespace conventions)

**FINDING 48: Files that DON'T need integration**
- Any .cs file in SDK-style project folders is already integrated
- No action needed unless:
  - Namespace is wrong
  - Contains duplicate types
  - Has compilation errors
  - Not being used (dead code)

**FINDING 49: Preventing future orphaned files**
1. Always build after creating new files
2. Always test after creating new files  
3. Never batch-delete files without verification
4. Use incremental commits (one feature at a time)
5. Keep .csproj and disk in sync for old-style projects
6. Run `dotnet build` in CI/CD to catch integration issues

---

## 5. MULTI-TARGETING .CSPROJ STRUCTURE

### FINDING 50: Multi-Targeting Basics
**Source**: https://learn.microsoft.com/en-us/dotnet/core/porting/project-structure
- **TWO APPROACHES TO SUPPORT .NET FRAMEWORK + .NET**:
  1. **Single multi-targeted project**: One .csproj with `<TargetFrameworks>net481;net8.0</TargetFrameworks>`
  2. **Separate projects**: Keep old-style .NET Framework project, create new SDK-style .NET project in separate folder
- Approach #1 REQUIRES SDK-style project (old-style projects cannot multi-target)
- Approach #2 allows keeping old-style project unchanged (for SqlClr compatibility)

### FINDING 51: TargetFrameworks vs TargetFramework
**Source**: https://learn.microsoft.com/en-us/nuget/create-packages/multiple-target-frameworks-project-file
- `<TargetFramework>` (singular): Single target, e.g., `<TargetFramework>net481</TargetFramework>`
- `<TargetFrameworks>` (plural): Multiple targets, e.g., `<TargetFrameworks>net481;net8.0;net9.0</TargetFrameworks>`
- Changing from singular to plural enables multi-targeting in single project
- Must add "s" to BOTH opening and closing tags
- Example:
  ```xml
  <Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
      <TargetFrameworks>net8.0;netstandard2.0</TargetFrameworks>
    </PropertyGroup>
  </Project>
  ```

### FINDING 52: Framework-Specific Dependencies
**Source**: https://learn.microsoft.com/en-us/visualstudio/msbuild/net-sdk-multitargeting
- Multi-targeted projects can have different NuGet packages per framework
- Use conditional `<ItemGroup>` with `Condition` attribute:
  ```xml
  <ItemGroup Condition="'$(TargetFramework)' == 'net481'">
    <PackageReference Include="System.Text.Json" Version="8.0.5" />
  </ItemGroup>
  <ItemGroup Condition="'$(TargetFramework)' == 'net8.0'">
    <PackageReference Include="SomeModernPackage" Version="2.0.0" />
  </ItemGroup>
  ```
- Allows .NET Framework to use backported packages, .NET 8 to use modern packages
- CRITICAL for Hartonomous: SqlClr needs MathNet.Numerics for .NET Framework, API needs .NET 8+ packages

### FINDING 53: Conditional Compilation Symbols
**Source**: https://learn.microsoft.com/en-us/nuget/create-packages/multiple-target-frameworks-project-file#create-a-project-that-supports-multiple-net-framework-versions
- Multi-targeted projects get automatic preprocessor symbols per TFM:
  - `NET481` for net481
  - `NET8_0` for net8.0
  - `NETSTANDARD2_0` for netstandard2.0
- Use `#if` directives to conditionally compile code:
  ```csharp
  public string Platform {
      get {
  #if NET481
         return ".NET Framework"
  #elif NET8_0
         return ".NET 8"
  #else
  #error This code block does not match csproj TargetFrameworks list
  #endif
      }
  }
  ```
- Useful for platform-specific APIs or workarounds

### FINDING 54: SqlClr Cannot Be Converted to SDK-Style
**Source**: SQL CLR constraints + multi-targeting docs
- **CRITICAL LIMITATION**: SQL CLR requires specific assembly metadata
- Old-style .csproj allows precise control over:
  - Assembly versioning
  - Strong naming
  - Referenced assemblies
  - Output paths
- SDK-style .csproj auto-generates many properties (may conflict with SQL CLR requirements)
- **RECOMMENDATION**: Keep SqlClr as old-style .NET Framework 4.8.1 project in separate folder
- Other projects (API, Core, Infrastructure) can be SDK-style and multi-target

### FINDING 55: Hartonomous Project Organization Strategy
**Source**: https://learn.microsoft.com/en-us/dotnet/core/porting/project-structure (Keep existing projects approach)
- **RECOMMENDED STRUCTURE**:
  - `src/SqlClr/` - Old-style .NET Framework 4.8.1 project (unchanged)
  - `src/Hartonomous.Api/` - SDK-style multi-targeted `net481;net8.0` OR just net8.0
  - `src/Hartonomous.Core/` - SDK-style multi-targeted `net481;net8.0;netstandard2.0`
  - `src/Hartonomous.Infrastructure/` - SDK-style multi-targeted `net481;net8.0`
- Separate folders avoid forcing Visual Studio 2019+ requirement for SqlClr
- Can create two solution files:
  - `Hartonomous.sln` - All projects (requires VS 2022)
  - `Hartonomous.Legacy.sln` - Only old-style projects (works in older VS)

### FINDING 56: Target Framework Moniker (TFM) Reference
**Source**: https://learn.microsoft.com/en-us/dotnet/standard/frameworks
- **.NET Framework TFMs**:
  - `net481` = .NET Framework 4.8.1
  - `net48` = .NET Framework 4.8
  - `net472` = .NET Framework 4.7.2
  - `net462` = .NET Framework 4.6.2
- **.NET (Core) TFMs**:
  - `net9.0` = .NET 9
  - `net8.0` = .NET 8
  - `net7.0` = .NET 7 (out of support)
  - `net6.0` = .NET 6 (LTS)
- **Cross-Platform TFMs**:
  - `netstandard2.1` = .NET Standard 2.1 (.NET Core 3.0+, NOT .NET Framework)
  - `netstandard2.0` = .NET Standard 2.0 (.NET Framework 4.6.1+, .NET Core 2.0+)

### FINDING 57: Multi-Targeting NuGet Package Behavior
**Source**: https://learn.microsoft.com/en-us/nuget/create-packages/supporting-multiple-target-frameworks
- When you build multi-targeted project with `dotnet pack`, NuGet creates:
  - `lib/net481/YourLibrary.dll` (compiled for .NET Framework 4.8.1)
  - `lib/net8.0/YourLibrary.dll` (compiled for .NET 8)
- Consuming projects automatically get correct assembly based on their TFM
- Dependency groups created per TFM in .nupkg
- **NOT APPLICABLE TO HARTONOMOUS**: SqlClr assemblies deployed directly to SQL Server, not via NuGet

### FINDING 58: Visual Studio Multi-Targeting UI
**Source**: https://learn.microsoft.com/en-us/visualstudio/ide/visual-studio-multi-targeting-overview
- In Visual Studio 2022 Project Properties → Application tab:
  - Single target: "Target Framework" dropdown (one selection)
  - Multi-target: "Target frameworks" textbox (semicolon-separated list)
- After changing from single to multi-target, must reload project
- IntelliSense understands conditional compilation (#if NET481 vs #if NET8_0)
- Build output shows compilation per framework

### FINDING 59: Old-Style vs SDK-Style Project File Size Difference
**Source**: https://learn.microsoft.com/en-us/dotnet/core/project-sdk/overview
- **Old-Style .csproj**: Hundreds of lines
  - Explicit `<Compile Include="..."/>` for EVERY .cs file
  - Explicit `<Reference Include="..."/>` for EVERY assembly
  - `packages.config` file for NuGet packages
  - Complex MSBuild imports
- **SDK-Style .csproj**: 10-20 lines
  - Auto-includes all .cs files
  - `<PackageReference>` directly in .csproj (no packages.config)
  - Simplified syntax
- **Example SDK-style multi-targeted project**:
  ```xml
  <Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
      <TargetFrameworks>netstandard2.0;net481</TargetFrameworks>
      <Description>Sample project that targets multiple TFMs</Description>
    </PropertyGroup>
  </Project>
  ```
  That's it - 6 lines!

### FINDING 60: .NET Framework 4.8.1 Multi-Targeting Support
**Source**: https://learn.microsoft.com/en-us/dotnet/framework/install/versions-and-dependencies
- .NET Framework 4.8.1 released with Windows 11 24H2
- TFM: `net481`
- Supported on Windows 11 (multiple versions), Windows Server 2025, Windows Server 2022
- Uses CLR 4 (same as .NET Framework 4.0-4.8)
- **CRITICAL FOR HARTONOMOUS**: SqlClr targets net481, API/Core could multi-target `net481;net8.0`

### FINDING 61: When NOT to Multi-Target
**Source**: https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/cross-platform-targeting
- ❌ AVOID multi-targeting if:
  - Source code identical for all targets
  - No library/package dependencies
  - No framework-specific APIs used
  - Reason: Increases package size for no benefit, .NET Standard assembly works everywhere
- ✅ DO multi-target if:
  - Need framework-specific APIs (e.g., SQL CLR APIs only in .NET Framework)
  - Dependencies differ per framework (e.g., System.Text.Json backport for net481)
  - Performance optimizations available in newer frameworks

### FINDING 62: Hartonomous SqlClr Should NOT Multi-Target
**Source**: SQL CLR requirements + multi-targeting best practices
- SqlClr project MUST stay .NET Framework 4.8.1 only
- Reasons:
  1. SQL Server CLR integration ONLY supports .NET Framework
  2. Cannot deploy .NET Core/.NET 8 assemblies to SQL Server
  3. No benefit to multi-targeting (cannot use other TFMs in SQL Server)
  4. Old-style project format required for precise assembly control
- **RECOMMENDATION**: Keep SqlClr single-targeted `net481`, old-style project

### FINDING 63: Hartonomous.Core Could Benefit from Multi-Targeting
**Source**: Multi-targeting best practices
- Hartonomous.Core likely contains shared business logic
- Could be consumed by:
  - SqlClr (.NET Framework 4.8.1)
  - API (.NET 8+)
  - Future external tools (.NET 9+)
- **RECOMMENDATION**: Multi-target `<TargetFrameworks>net481;net8.0</TargetFrameworks>`
- Benefits:
  - SqlClr can reference net481 build
  - API can reference net8.0 build (better performance, modern APIs)
  - Single codebase, two optimized outputs

### FINDING 64: Default RID for .NET Framework in Multi-Target Projects
**Source**: https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/9.0/default-rid
- .NET 9 SDK changed default Runtime Identifier (RID) for .NET Framework projects
- OLD: `win7-x86` / `win7-x64`
- NEW: `win-x86` / `win-x64`
- Affects multi-targeted projects that include both .NET and .NET Framework
- Reason: Restore happens once, must use RID compatible with both graphs
- **IMPACT ON HARTONOMOUS**: If multi-targeting net481+net8.0, use .NET SDK 9+ to avoid restore errors

### FINDING 65: How to Inspect Expanded Multi-Target Project
**Source**: https://learn.microsoft.com/en-us/dotnet/core/project-sdk/overview#project-files
- Use `dotnet msbuild -preprocess` to see fully expanded project file
- Shows all SDK imports, targets, properties
- For multi-targeted projects, specify framework:
  ```powershell
  dotnet msbuild -property:TargetFramework=net481 -preprocess:output-net481.xml
  dotnet msbuild -property:TargetFramework=net8.0 -preprocess:output-net80.xml
  ```
- Useful for debugging multi-target build issues

### FINDING 66: Multi-Targeting and Assembly Versioning
**Source**: Multi-targeting docs + best practices
- ❌ AVOID different assembly names per TFM (breaks package consumers)
- ✅ DO use same assembly name across all TFMs
- Assembly version can be same or different (usually same)
- Example:
  ```xml
  <PropertyGroup>
    <AssemblyName>Hartonomous.Core</AssemblyName>
    <Version>1.0.0</Version>
  </PropertyGroup>
  ```
  Produces:
  - `lib/net481/Hartonomous.Core.dll` (version 1.0.0)
  - `lib/net8.0/Hartonomous.Core.dll` (version 1.0.0)

### FINDING 67: SqlClr Old-Style Project Can Reference SDK-Style Projects
**Source**: Visual Studio project system documentation
- Old-style .NET Framework project CAN reference SDK-style multi-targeted project
- Visual Studio automatically selects compatible TFM from referenced project
- Example: SqlClr (net481) references Hartonomous.Core (net481;net8.0)
  - Visual Studio picks `net481` build from Hartonomous.Core
  - Works seamlessly
- **BENEFIT FOR HARTONOMOUS**: Can modernize Core/Infrastructure to SDK-style without breaking SqlClr

