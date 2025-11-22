# ?? **DEDUPLICATION IMPLEMENTATION PLAN**

**Status**: Ready for Implementation  
**Timeline**: 3-4 weeks  
**Risk Level**: Medium (requires careful testing)  
**Impact**: ~4,000-5,000 LOC reduction

---

## ?? **PHASE 1: LOW-HANGING FRUIT** (Week 1)

### **Task 1.1: Create HashUtilities Class**

**Priority**: HIGH  
**Risk**: LOW  
**Effort**: 2 hours  
**Files**: 1 new, 10+ updates

**Steps**:
1. Create `src\Hartonomous.Core\Utilities\HashUtilities.cs` (see audit doc for full code)
2. Add unit tests in `tests\Hartonomous.Core.Tests\Utilities\HashUtilitiesTests.cs`
3. Update files (replace inline SHA256 with HashUtilities calls):
   - `BaseAtomizer.cs`
   - `CodeFileAtomizer.cs`
   - `TreeSitterAtomizer.cs`
   - `RoslynAtomizer.cs`
   - `CorrelationIdMiddleware.cs`
   - `GenerationFunctions.cs`
   - 4+ other atomizers
4. Run all tests
5. Commit: `refactor: centralize SHA256 hashing in HashUtilities`

**Acceptance Criteria**:
- ? All atomizers use `HashUtilities.ComputeSHA256()`
- ? Deterministic GUID uses `HashUtilities.ComputeDeterministicGuid()`
- ? All unit tests pass
- ? No direct `SHA256.Create()` or `SHA256.HashData()` calls in application code

---

### **Task 1.2: Create Azure Configuration Extensions**

**Priority**: HIGH  
**Risk**: LOW  
**Effort**: 1 hour  
**Files**: 1 new, 5 updates

**Steps**:
1. Create `src\Hartonomous.Core\Configuration\AzureConfigurationExtensions.cs`
2. Update `Program.cs` files (5 files):
   - `src\Hartonomous.Admin\Program.cs`
   - `src\Hartonomous.Api\Program.cs`
   - `src\Hartonomous.Workers.CesConsumer\Program.cs`
   - `src\Hartonomous.Workers.EmbeddingGenerator\Program.cs`
   - `src\Hartonomous.Workers.Neo4jSync\Program.cs`
3. Replace 30-40 lines in each Program.cs with single line: `builder.AddAzureConfiguration();`
4. Test each application starts correctly
5. Commit: `refactor: centralize Azure configuration in extension method`

**Acceptance Criteria**:
- ? All 5 applications load Azure App Configuration correctly
- ? All 5 applications load Azure Key Vault correctly
- ? All 5 applications load Application Insights correctly
- ? No inline configuration code in Program.cs files

---

### **Task 1.3: Verify Guard Clause Usage**

**Priority**: MEDIUM  
**Risk**: NONE (documentation only)  
**Effort**: 2 hours  
**Files**: Documentation

**Steps**:
1. Review `Guard.cs` implementation
2. Search for inline validation patterns that should use Guard
3. Document preferred patterns in `docs\code-standards.md`
4. Create task list for Phase 2 to migrate inline validations

**Acceptance Criteria**:
- ? Document created showing Guard usage examples
- ? List of files with inline validation created
- ? Migration priority order defined

---

## ?? **PHASE 2: SQL FACTORY & MEDIA UTILS** (Week 2)

### **Task 2.1: Create SQL Connection Factory**

**Priority**: HIGH  
**Risk**: MEDIUM (affects 20+ services)  
**Effort**: 1 day  
**Files**: 2 new, 20+ updates

**Steps**:

1. **Create Interface** (`src\Hartonomous.Infrastructure\Data\ISqlConnectionFactory.cs`):
   ```csharp
   public interface ISqlConnectionFactory
   {
       Task<SqlConnection> CreateConnectionAsync(CancellationToken ct = default);
       SqlConnection CreateConnection();
   }
   ```

2. **Create Implementation** (`src\Hartonomous.Infrastructure\Data\SqlConnectionFactory.cs`):
   - See audit doc for full implementation
   - Handles both password-based and managed identity auth
   - Centralizes TokenRequestContext logic

3. **Register in DI** (`src\Hartonomous.Infrastructure\Configurations\DataAccessRegistration.cs`):
   ```csharp
   services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
   ```

4. **Update Services** (20 files - do in batches of 5):
   
   **Batch 1** (Core Services):
   - `SqlAtomizationService.cs`
   - `SqlGenerationService.cs`
   - `SqlSemanticService.cs`
   - `SqlReasoningService.cs`
   - `SqlSearchService.cs`
   
   **Batch 2** (Discovery/Concept):
   - `SqlDiscoveryService.cs`
   - `SqlConceptService.cs`
   - `SqlCognitiveService.cs`
   - `SqlOodaService.cs`
   - `SqlInferenceService.cs`
   
   **Batch 3** (Provenance/Conversation):
   - `SqlProvenanceWriteService.cs`
   - `SqlConversationService.cs`
   - `SqlModelManagementService.cs`
   - `SqlSpatialSearchService.cs`
   - `SqlStreamProcessingService.cs`
   
   **Batch 4** (Billing/Background):
   - `SqlBillingService.cs`
   - `BackgroundJobService.cs`
   - `SqlBackgroundJobService.cs`
   - 2+ other services

5. **For Each Service**:
   - Remove: `private readonly string _connectionString;`
   - Remove: `private readonly TokenCredential _credential;`
   - Remove: Constructor initialization of above fields
   - Remove: `private async Task SetupConnectionAsync(...)` method
   - Add: `private readonly ISqlConnectionFactory _connectionFactory;`
   - Add: Constructor parameter `ISqlConnectionFactory connectionFactory`
   - Replace: `await using var connection = new SqlConnection(_connectionString); await SetupConnectionAsync(...)`
   - With: `await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);`

6. **Test Each Batch**:
   - Run integration tests for affected services
   - Verify SQL connections work with both auth methods
   - Check managed identity authentication
   - Verify no connection leaks

7. **Commit Strategy** (4 commits):
   - `refactor: add SQL connection factory interface and implementation`
   - `refactor: migrate core services to use SQL connection factory (batch 1)`
   - `refactor: migrate discovery services to use SQL connection factory (batch 2)`
   - `refactor: migrate remaining services to use SQL connection factory (batches 3-4)`

**Acceptance Criteria**:
- ? All 20+ services use `ISqlConnectionFactory`
- ? No services have `SetupConnectionAsync` method
- ? All integration tests pass
- ? No SQL connection leaks detected
- ? Both password and managed identity auth work

---

### **Task 2.2: Enhance Binary Reader Helper**

**Priority**: MEDIUM  
**Risk**: LOW  
**Effort**: 4 hours  
**Files**: 1 update, 2 file updates

**Steps**:
1. Enhance `src\Hartonomous.Infrastructure\Services\Vision\BinaryReaderHelper.cs`
   - Add all methods from audit doc
   - Add XML documentation
2. Update `AudioMetadataExtractor.cs` to use BinaryReaderHelper
3. Update `VideoMetadataExtractor.cs` to use BinaryReaderHelper
4. Add unit tests for binary reading utilities
5. Test with various audio/video formats
6. Commit: `refactor: consolidate binary reading utilities for media extraction`

**Acceptance Criteria**:
- ? No duplicate binary reading code in extractors
- ? All audio/video metadata extraction works correctly
- ? Unit tests for all binary reading methods

---

## ?? **PHASE 3: ATOMIZER CONSOLIDATION** (Weeks 3-4)

### **Task 3.1: Enhance BaseAtomizer**

**Priority**: HIGH  
**Risk**: HIGH (affects all 22 atomizers)  
**Effort**: 2 days  
**Files**: 1 update (BaseAtomizer.cs)

**Steps**:
1. Add helper methods to `BaseAtomizer.cs` (see audit doc):
   - `CreateStandardFileMetadataAtom()`
   - `AddComposition()`
   - Enhanced `AtomizeAsync()` template method
2. Add unit tests for new helper methods
3. Create example migration for 1 atomizer
4. Document migration pattern
5. Commit: `refactor: enhance BaseAtomizer with helper methods`

**Acceptance Criteria**:
- ? BaseAtomizer has all helper methods
- ? Helper methods have unit tests
- ? Example migration documented
- ? All existing atomizers still work (no breaking changes yet)

---

### **Task 3.2: Migrate Atomizers** (Batched)

**Priority**: HIGH  
**Risk**: MEDIUM  
**Effort**: 1 week  
**Files**: 22 atomizer files

**Migration Strategy**:

**Batch 1** (Simple Atomizers - 5 files):
- `TextAtomizer.cs`
- `ImageAtomizer.cs`
- `AudioFileAtomizer.cs`
- `VideoFileAtomizer.cs`
- `DocumentAtomizer.cs`

**Batch 2** (Code Atomizers - 3 files):
- `CodeFileAtomizer.cs`
- `RoslynAtomizer.cs`
- `TreeSitterAtomizer.cs`

**Batch 3** (Model Atomizers - 4 files):
- `ModelFileAtomizer.cs`
- `HuggingFaceModelAtomizer.cs`
- `OllamaModelAtomizer.cs`
- `DatabaseAtomizer.cs`

**Batch 4** (Stream Atomizers - 3 files):
- `AudioStreamAtomizer.cs`
- `VideoStreamAtomizer.cs`
- `TelemetryStreamAtomizer.cs`

**Batch 5** (Archive/Git Atomizers - 3 files):
- `ArchiveAtomizer.cs`
- `GitRepositoryAtomizer.cs`
- `WebFetchAtomizer.cs`

**Batch 6** (Special Atomizers - 4 files):
- `EnhancedImageAtomizer.cs`
- `TelemetryAtomizer.cs`
- Others discovered during implementation

**For Each Atomizer**:

1. **Before Migration Checklist**:
   - [ ] Review atomizer implementation
   - [ ] Identify file metadata creation code
   - [ ] Identify composition creation code
   - [ ] Identify SHA256 hashing code
   - [ ] Note any special cases

2. **Migration Steps**:
   - Inherit from `BaseAtomizer<TInput>` if not already
   - Move atomization logic to `AtomizeCoreAsync()` override
   - Replace file metadata creation with `CreateStandardFileMetadataAtom()`
   - Replace composition creation with `AddComposition()`
   - Replace SHA256 hashing with `HashUtilities.ComputeSHA256()`
   - Remove duplicate error handling (now in base class)
   - Remove duplicate stopwatch/logging (now in base class)

3. **Testing**:
   - Run unit tests for atomizer
   - Test with various file types
   - Verify atom count matches before/after
   - Verify compositions are created correctly
   - Check AtomizationResult metadata

4. **Commit** (per batch):
   - `refactor: migrate [batch name] atomizers to use BaseAtomizer helpers`

**Acceptance Criteria Per Atomizer**:
- ? Uses `CreateStandardFileMetadataAtom()`
- ? Uses `AddComposition()`
- ? Uses `HashUtilities.ComputeSHA256()`
- ? Overrides `AtomizeCoreAsync()` only
- ? All tests pass
- ? Atom count unchanged
- ? Compositions created correctly

---

## ?? **TESTING STRATEGY**

### **Phase 1 Testing**:
- Unit tests for `HashUtilities`
- Integration tests for configuration loading
- Manual testing of all 5 applications

### **Phase 2 Testing**:
- **SQL Factory**:
  - Unit tests for `SqlConnectionFactory`
  - Integration tests for all 20+ services
  - Connection leak detection (SQL Profiler)
  - Managed identity auth testing (Azure environment)
- **Binary Reader**:
  - Unit tests for all binary reading methods
  - Integration tests with real audio/video files

### **Phase 3 Testing**:
- **Per Atomizer**:
  - Unit tests for atomizer
  - Integration tests with various file types
  - Atom count verification (before/after match)
  - Composition verification
  - Performance benchmarking

**Regression Testing** (after each phase):
- Run full test suite
- Perform end-to-end ingestion tests
- Verify no breaking changes to public APIs
- Check memory usage unchanged
- Verify logging still works

---

## ?? **ROLLBACK PLAN**

### **If Phase 1 Fails**:
- Revert commits
- Fix issues
- Re-run tests
- Low risk - isolated changes

### **If Phase 2 Fails**:
- **SQL Factory**: Can revert per-service in batches
- **Binary Reader**: Can revert easily (few files)
- Medium risk - keep batches small

### **If Phase 3 Fails**:
- **BaseAtomizer**: Can revert easily (single file)
- **Atomizer Migration**: Can revert per-batch
- Highest risk - test thoroughly per batch

---

## ? **COMPLETION CHECKLIST**

### **Phase 1 Complete When**:
- [ ] `HashUtilities.cs` created and tested
- [ ] 10+ files using `HashUtilities`
- [ ] `AzureConfigurationExtensions.cs` created
- [ ] 5 Program.cs files updated
- [ ] Guard clause documentation created
- [ ] All tests passing
- [ ] Code review approved
- [ ] Merged to main

### **Phase 2 Complete When**:
- [ ] `ISqlConnectionFactory` created
- [ ] `SqlConnectionFactory` implemented
- [ ] 20+ services migrated
- [ ] All integration tests passing
- [ ] No SQL connection leaks
- [ ] `BinaryReaderHelper` enhanced
- [ ] Audio/Video extractors updated
- [ ] Code review approved
- [ ] Merged to main

### **Phase 3 Complete When**:
- [ ] `BaseAtomizer` enhanced
- [ ] All 22 atomizers migrated
- [ ] All atomizer tests passing
- [ ] Atom counts verified
- [ ] Compositions verified
- [ ] Performance benchmarks show no regression
- [ ] Code review approved
- [ ] Merged to main

---

## ?? **SUCCESS METRICS**

### **Quantitative**:
- **LOC Reduction**: Target 4,000-5,000 lines removed
- **Test Coverage**: Maintain or improve (currently ~65%)
- **Performance**: No regression (benchmark all atomizers)
- **Build Time**: Should not increase
- **Memory Usage**: Should not increase

### **Qualitative**:
- **Maintainability**: Single source of truth for each pattern
- **Consistency**: All atomizers follow same pattern
- **Testability**: Easier to test isolated utilities
- **Documentation**: Clear patterns documented
- **Onboarding**: Easier for new developers

---

## ?? **TIMELINE**

| Week | Phase | Tasks | Owner | Status |
|------|-------|-------|-------|--------|
| Week 1 | Phase 1 | Tasks 1.1-1.3 | TBD | ? Pending |
| Week 2 | Phase 2 | Tasks 2.1-2.2 | TBD | ? Pending |
| Week 3 | Phase 3 | Task 3.1 + Batches 1-3 | TBD | ? Pending |
| Week 4 | Phase 3 | Batches 4-6 + Testing | TBD | ? Pending |

**Total Duration**: 3-4 weeks  
**Parallel Work**: Some tasks can be done in parallel (e.g., different batches)

---

**END OF IMPLEMENTATION PLAN**

*Ready for team review and approval.*
