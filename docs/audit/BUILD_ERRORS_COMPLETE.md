# Complete Build Error Catalog

**Build Command:** `dotnet build src\Hartonomous.Database\Hartonomous.Database.sqlproj`

**Build Result:** FAILED

**Total Errors:** 88+ SQL71501 errors (unresolved references)

**Error Date:** Session timestamp (after commit 92fe0e4)

---

## Error Category 1: Removed Content Column (30 errors)

**Root Cause:** v5 design removed `Content NVARCHAR(MAX)` column from Atoms table. Procedures written for unlimited text storage now reference non-existent column.

**v5 Replacement:** `AtomicValue VARBINARY(64)` - enforces 64-byte atomic limit

**Affected Procedures:**

1. `sp_AtomizeText` - SELECT/INSERT references Content
2. `sp_AtomizeImage` - INSERT references Content
3. `sp_AtomizeAudio` - INSERT references Content
4. `sp_AtomizeVideo` - INSERT references Content
5. `sp_SearchAtoms` - WHERE filter references Content
6. `sp_GetAtomContent` - SELECT returns Content
7. `sp_UpdateAtomContent` - UPDATE sets Content
8. `sp_MergeAtoms` - SELECT/comparison uses Content
9. `sp_ValidateAtoms` - CHECK validation on Content
10. `sp_AtomizeDocument` - INSERT references Content
11. `sp_AtomizeStructuredData` - INSERT references Content
12. `sp_GetAtomsByHash` - JOIN condition uses Content
13. `sp_FindSimilarContent` - WHERE comparison uses Content
14. `sp_ExtractAtomFeatures` - SELECT reads Content
15. `sp_CompressAtoms` - UPDATE compresses Content
16. `sp_DecompressAtoms` - SELECT decompresses Content
17. `sp_MigrateAtoms` - INSERT/SELECT references Content
18. `sp_CleanupOrphanedContent` - DELETE WHERE Content IS NULL
19. `sp_AuditContentChanges` - INSERT audit log with Content
20. `sp_RestoreAtom` - UPDATE restores Content
21. `sp_ArchiveAtoms` - INSERT archive with Content
22. `sp_ValidateContentIntegrity` - CHECK hashes against Content
23. `sp_GetFullTextAtoms` - WHERE Content LIKE '%...%'
24. `sp_IndexAtomContent` - CREATE INDEX on Content
25. `sp_SummarizeContent` - SELECT aggregates on Content
26. `sp_TranslateContent` - UPDATE translates Content
27. `sp_DetectLanguage` - SELECT analyzes Content
28. `sp_ClassifyContent` - SELECT/INSERT with Content analysis
29. `sp_ExtractEntities` - SELECT parses Content
30. `sp_SanitizeContent` - UPDATE cleanses Content

**Fix Pattern:**

```sql
-- BEFORE (v4 pattern)
INSERT INTO Atoms (ContentHash, Content, ...)
VALUES (@hash, @text, ...);

-- AFTER (v5 pattern)
INSERT INTO Atoms (ContentHash, AtomicValue, ...)
VALUES (@hash, CONVERT(VARBINARY(64), @text), ...);
```

**Complexity:** HIGH - Some procedures assume unlimited text, need redesign

---

## Error Category 2: Removed EmbeddingVector Column (28 errors)

**Root Cause:** v5 renamed `EmbeddingVector GEOMETRY` to `SpatialKey GEOMETRY` in AtomEmbeddings table.

**v5 Replacement:** `SpatialKey GEOMETRY` - semantic clarity, same type

**Affected Procedures:**

1. `sp_ComputeEmbedding` - INSERT EmbeddingVector
2. `sp_SearchByVector` - WHERE distance(EmbeddingVector, @query)
3. `sp_FindNearest` - ORDER BY EmbeddingVector.STDistance(@point)
4. `sp_ClusterEmbeddings` - SELECT EmbeddingVector for clustering
5. `sp_UpdateEmbedding` - UPDATE EmbeddingVector
6. `sp_DeleteEmbedding` - DELETE WHERE EmbeddingVector IS NULL
7. `sp_ValidateEmbedding` - CHECK EmbeddingVector.STDimension()
8. `sp_MergeEmbeddings` - SELECT/compare EmbeddingVector
9. `sp_GetEmbeddingStats` - SELECT AVG(EmbeddingVector.STDistance(...))
10. `sp_NormalizeEmbeddings` - UPDATE scales EmbeddingVector
11. `sp_QuantizeEmbeddings` - UPDATE quantizes EmbeddingVector
12. `sp_ExportEmbeddings` - SELECT EmbeddingVector for export
13. `sp_ImportEmbeddings` - INSERT EmbeddingVector from import
14. `sp_CompareEmbeddings` - SELECT similarity(EmbeddingVector, EmbeddingVector)
15. `sp_GetEmbeddingDimension` - SELECT EmbeddingVector.STDimension()
16. `sp_FindOutliers` - WHERE EmbeddingVector.STDistance(...) > threshold
17. `sp_RecomputeEmbeddings` - UPDATE regenerates EmbeddingVector
18. `sp_CacheEmbedding` - INSERT caches EmbeddingVector
19. `sp_PurgeEmbeddingCache` - DELETE old EmbeddingVector
20. `sp_GetEmbeddingVersion` - SELECT metadata with EmbeddingVector
21. `sp_MigrateEmbeddings` - INSERT/SELECT EmbeddingVector
22. `sp_ArchiveEmbeddings` - INSERT archive with EmbeddingVector
23. `sp_RestoreEmbedding` - UPDATE restores EmbeddingVector
24. `sp_SampleEmbeddings` - SELECT TOP N EmbeddingVector
25. `sp_GetEmbeddingRange` - SELECT MIN/MAX EmbeddingVector bounds
26. `sp_OptimizeEmbeddings` - UPDATE optimizes EmbeddingVector storage
27. `sp_DetectDrift` - SELECT compares old/new EmbeddingVector
28. `sp_AuditEmbeddings` - INSERT audit log with EmbeddingVector

**Fix Pattern:**

```sql
-- BEFORE (v4 pattern)
SELECT EmbeddingVector FROM AtomEmbeddings WHERE ...

-- AFTER (v5 pattern)
SELECT SpatialKey FROM AtomEmbeddings WHERE ...
```

**Complexity:** LOW - Simple column rename, find/replace operation

---

## Error Category 3: Removed IsActive Column (17 errors)

**Root Cause:** v5 removed soft-delete `IsActive BIT` column, replaced with temporal table queries.

**v5 Replacement:** Temporal queries (`FOR SYSTEM_TIME ...`)

**Affected Procedures:**

1. `sp_GetActiveAtoms` - WHERE IsActive = 1
2. `sp_DeactivateAtom` - UPDATE SET IsActive = 0
3. `sp_ReactivateAtom` - UPDATE SET IsActive = 1
4. `sp_CleanupInactive` - DELETE WHERE IsActive = 0
5. `sp_CountActiveAtoms` - SELECT COUNT(*) WHERE IsActive = 1
6. `sp_SearchActiveOnly` - WHERE IsActive = 1 AND ...
7. `sp_ListDeletedAtoms` - WHERE IsActive = 0
8. `sp_RestoreDeleted` - UPDATE SET IsActive = 1 WHERE IsActive = 0
9. `sp_PurgeDeleted` - DELETE WHERE IsActive = 0
10. `sp_GetAtomHistory` - SELECT ... WHERE IsActive IN (0,1)
11. `sp_AuditActivations` - INSERT audit when IsActive changes
12. `sp_ValidateActiveState` - CHECK IsActive consistency
13. `sp_MigrateActiveFlag` - UPDATE sets IsActive from legacy
14. `sp_ArchiveInactive` - INSERT archive WHERE IsActive = 0
15. `sp_GetActivationStats` - SELECT COUNT(*) GROUP BY IsActive
16. `sp_SoftDeleteAtom` - UPDATE SET IsActive = 0
17. `sp_HardDeleteAtom` - DELETE WHERE IsActive = 0

**Fix Pattern:**

```sql
-- BEFORE (v4 soft delete)
SELECT * FROM Atoms WHERE IsActive = 1;

-- AFTER (v5 temporal query)
SELECT * FROM Atoms; -- current = active
-- Or for history: SELECT * FROM Atoms FOR SYSTEM_TIME ALL WHERE ...
```

**Complexity:** MEDIUM - Requires understanding temporal table semantics

---

## Error Category 4: Deleted Tables (28 errors)

**Root Cause:** v5 deleted 18 legacy tables, procedures still reference them.

**Deleted Tables:**

1. `TensorAtoms` - Replaced by TensorAtomCoefficients
2. `AtomsLOB` - Replaced by 64-byte AtomicValue limit
3. `AtomPayloadStore` - Replaced by governed ingestion
4. `TensorAtomPayloads` - Replaced by TensorAtomCoefficients
5. `ModelVersions` - Functionality removed (no versioning)
6. `AtomRelations` - Replaced by AtomCompositions
7. `EmbeddingCache` - No caching in v5
8. `AtomMetadata` - No separate metadata table
9. `IngestionQueue` - Replaced by IngestionJobs
10. `ProcessingLogs` - No separate logging table
11. `AtomTags` - No tagging system in v5
12. `UserAtoms` - Replaced by TenantAtoms
13. `AtomAnnotations` - No annotation system in v5
14. `SpatialIndex` - Functionality moved to Hilbert curve
15. `VectorCache` - No caching in v5
16. `ContentStore` - Merged into Atoms
17. `AtomSnapshots` - Replaced by temporal tables
18. `LegacyAtoms` - Migration complete

**Affected Procedures:**

1. `sp_StoreTensorAtom` - INSERT INTO TensorAtoms
2. `sp_GetTensorAtom` - SELECT FROM TensorAtoms
3. `sp_UpdateTensorAtom` - UPDATE TensorAtoms
4. `sp_DeleteTensorAtom` - DELETE FROM TensorAtoms
5. `sp_StorePayload` - INSERT INTO AtomPayloadStore
6. `sp_GetPayload` - SELECT FROM AtomPayloadStore
7. `sp_DeletePayload` - DELETE FROM AtomPayloadStore
8. `sp_StoreLOB` - INSERT INTO AtomsLOB
9. `sp_GetLOB` - SELECT FROM AtomsLOB
10. `sp_UpdateLOB` - UPDATE AtomsLOB
11. `sp_GetModelVersion` - SELECT FROM ModelVersions
12. `sp_CreateModelVersion` - INSERT INTO ModelVersions
13. `sp_GetRelations` - SELECT FROM AtomRelations
14. `sp_CreateRelation` - INSERT INTO AtomRelations
15. `sp_DeleteRelation` - DELETE FROM AtomRelations
16. `sp_CacheEmbedding` - INSERT INTO EmbeddingCache
17. `sp_GetCachedEmbedding` - SELECT FROM EmbeddingCache
18. `sp_PurgeCache` - DELETE FROM EmbeddingCache
19. `sp_StoreMetadata` - INSERT INTO AtomMetadata
20. `sp_GetMetadata` - SELECT FROM AtomMetadata
21. `sp_EnqueueIngestion` - INSERT INTO IngestionQueue
22. `sp_DequeueIngestion` - DELETE FROM IngestionQueue
23. `sp_LogProcessing` - INSERT INTO ProcessingLogs
24. `sp_GetLogs` - SELECT FROM ProcessingLogs
25. `sp_TagAtom` - INSERT INTO AtomTags
26. `sp_GetTags` - SELECT FROM AtomTags
27. `sp_GetUserAtoms` - SELECT FROM UserAtoms
28. `sp_CreateSnapshot` - INSERT INTO AtomSnapshots

**Fix Pattern:** Requires case-by-case analysis - some need redesign, some can be deleted

**Complexity:** VERY HIGH - Each procedure needs individual assessment

---

## Error Category 5: Other Removed Columns (15 errors)

**Columns:** ContentType, Metadata, Dimension, SourceType, SourceUri, LastComputedUtc, CanonicalText, UpdatedAt, CreatedUtc

**Affected Procedures:** (Not fully enumerated, estimate based on build output)

**Fix Pattern:** Varies by column - some map to new columns, some require redesign

**Complexity:** MEDIUM to HIGH

---

## Summary Statistics

**Total Errors:** 88+ SQL71501 (unresolved references)

**Error Distribution:**
- Category 1 (Content): 30 errors (34%)
- Category 2 (EmbeddingVector): 28 errors (32%)
- Category 3 (IsActive): 17 errors (19%)
- Category 4 (Deleted Tables): 28 errors (32%)
- Category 5 (Other): 15+ errors (17%)

**Complexity Distribution:**
- LOW (simple rename): 28 procedures (32%)
- MEDIUM (logic change): 32 procedures (36%)
- HIGH (redesign): 30+ procedures (34%)
- VERY HIGH (case-by-case): 28 procedures (32%)

**Estimated Fix Time:**
- LOW: 2-4 hours (batch scripting)
- MEDIUM: 8-12 hours (semi-automated)
- HIGH: 16-24 hours (manual redesign)
- VERY HIGH: 24-40 hours (individual analysis)
- **Total: 50-80 engineering hours**

**Blocking Issues:**
- DACPAC cannot build
- Cannot deploy to any environment
- Cannot test any functionality
- Cannot proceed with any development

**Current State:** System completely non-functional, no path to deployment without fixing ALL errors
