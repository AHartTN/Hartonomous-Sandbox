# ?? PHASE 1 EXECUTION PLAN - FILE SEPARATION

**Objective**: One type per file (Single Responsibility Principle)  
**Scope**: All C# files with multiple type definitions  
**Status**: ?? READY TO EXECUTE

---

## ?? FILES IDENTIFIED FOR SEPARATION

### **Source Code (32 files)**

#### **Atomizers (10 files)**
1. `AudioStreamAtomizer.cs` - 2 types
2. `BaseAtomizer.cs` - 2 types
3. `DatabaseAtomizer.cs` - 3 types
4. `EnhancedImageAtomizer.cs` - **12 types** ?? PRIORITY
5. `GitRepositoryAtomizer.cs` - 3 types
6. `RoslynAtomizer.cs` - 2 types
7. `TelemetryAtomizer.cs` - 4 types
8. `TelemetryStreamAtomizer.cs` - 4 types
9. `TreeSitterAtomizer.cs` - 2 types
10. `VideoStreamAtomizer.cs` - 2 types

#### **Services (16 files)**
11. `FileTypeDetector.cs` - 2 types
12. `FFmpegHelper.cs` - 2 types
13. `MediaAnalysisModels.cs` - 3 types
14. `MediaExtractionService.cs` - 3 types
15. `Neo4jProvenanceQueryService.cs` - 3 types
16. `StreamingIngestionService.cs` - 5 types
17. `TelemetryStreamProcessor.cs` - 4 types
18. `SqlConversationService.cs` - 2 types
19. `ContentTypeStrategies.cs` - 4 types
20. `Neo4jProvenanceService.cs` - **6 types** ?? PRIORITY
21. `AudioMetadataExtractor.cs` - 2 types
22. `CompressionAnalyzer.cs` - 2 types
23. `HartonomousObjectDetectionService.cs` - 3 types
24. `ImageMetadataExtractor.cs` - 2 types
25. `MediaMetadata.cs` - 2 types
26. `VideoMetadataExtractor.cs` - 2 types

#### **Shared/Contracts (3 files)**
27. `AtomDetailDTO.cs` - 2 types
28. `ErrorCodes.cs` - **7 types** ?? PRIORITY
29. `OperationResult.cs` - 2 types

#### **Workers (3 files)**
30. `Worker.cs` (CesConsumer) - 2 types
31. `EmbeddingGeneratorWorker.cs` - 3 types
32. `Neo4jSyncWorker.cs` - 3 types

---

### **Test Code (12 files)**

1. `DatabaseTestBase.cs` - 2 types
2. `SqlServerTestFixture.cs` - 2 types
3. `SpIngestAtomsTests.cs` - 2 types
4. `PlaywrightSetupTests.cs` - **6 types** ?? PRIORITY
5. `AzureEnabledWebApplicationFactory.cs` - 2 types
6. `IntegrationTestBase.cs` - 2 types
7. `ServiceBaseTests.cs` - 2 types
8. `UnitTestBase.cs` - 2 types
9. `BaseAtomizerTests.cs` - 2 types
10. `CodeFileAtomizerTests.cs` - **7 types** ?? PRIORITY
11. `MarkdownAtomizerTests.cs` - 3 types
12. `TreeSitterAtomizerTests.cs` - 4 types

---

## ?? EXECUTION STRATEGY

### **Priority Tiers**

#### **Tier 1: High Complexity (12+ types)**
- ? `EnhancedImageAtomizer.cs` (12 types)

#### **Tier 2: Medium Complexity (6-7 types)**
- ? `ErrorCodes.cs` (7 types)
- ? `CodeFileAtomizerTests.cs` (7 types)
- ? `PlaywrightSetupTests.cs` (6 types)
- ? `Neo4jProvenanceService.cs` (6 types)

#### **Tier 3: Low Complexity (2-5 types)**
- All remaining files (37 files)

---

## ?? SEPARATION RULES

### **General Rules**
1. **One primary type per file**
2. **Nested classes stay with parent** (e.g., `MyClass.Builder`)
3. **File name = Primary type name**
4. **Namespace = Directory structure**
5. **Place in appropriate subfolder if needed**

### **Specific Patterns**

#### **Pattern 1: Class + Interface**
```csharp
// Before: MyService.cs
public interface IMyService { }
public class MyService : IMyService { }

// After:
// IMyService.cs
public interface IMyService { }

// MyService.cs
public class MyService : IMyService { }
```

#### **Pattern 2: Multiple Classes**
```csharp
// Before: Services.cs
public class ServiceA { }
public class ServiceB { }

// After:
// ServiceA.cs
public class ServiceA { }

// ServiceB.cs
public class ServiceB { }
```

#### **Pattern 3: Enums + Classes**
```csharp
// Before: ErrorCodes.cs
public enum ErrorCategory { }
public static class ErrorCodes { }

// After:
// ErrorCategory.cs
public enum ErrorCategory { }

// ErrorCodes.cs
public static class ErrorCodes { }
```

#### **Pattern 4: Helper Classes**
```csharp
// Before: MyService.cs
public class MyService { }
public class MyServiceHelper { }

// After:
// MyService.cs
public class MyService { }

// Helpers/MyServiceHelper.cs (new subfolder)
public class MyServiceHelper { }
```

---

## ? VALIDATION CHECKLIST

After each file separation:
- [ ] Original file removed or updated
- [ ] New files created in correct location
- [ ] Namespaces updated correctly
- [ ] Using directives added
- [ ] File compiles without errors
- [ ] Tests still pass (if applicable)
- [ ] Git commit created

---

## ?? EXECUTION ORDER

### **Phase 1A: Tier 1 (Highest Priority)**
1. EnhancedImageAtomizer.cs (12 types) ? 12 files

### **Phase 1B: Tier 2 (Medium Priority)**
2. ErrorCodes.cs (7 types) ? 7 files
3. CodeFileAtomizerTests.cs (7 types) ? 7 files
4. PlaywrightSetupTests.cs (6 types) ? 6 files
5. Neo4jProvenanceService.cs (6 types) ? 6 files

### **Phase 1C: Tier 3 (Remaining)**
6-44. All remaining files (2-5 types each) ? ~90 files

---

## ?? ESTIMATED IMPACT

**Files to Process**: 44 total  
**New Files Created**: ~140 estimated  
**Files Modified**: 44  
**Net Increase**: ~96 files  

**Benefits**:
- ? Cleaner organization
- ? Easier navigation
- ? Better testability
- ? Single Responsibility Principle
- ? Reduced merge conflicts

---

## ?? SUCCESS CRITERIA

Phase 1 Complete When:
- ? All 44 files separated
- ? Solution builds without errors
- ? All tests pass
- ? Namespaces correct
- ? Files in appropriate directories
- ? Git history preserved

---

**Status**: ?? READY TO BEGIN  
**Estimated Time**: 2-3 hours  
**Checkpoint Saved**: ? Commit created

---

*Phase 1 execution plan ready. Awaiting go-ahead to begin separation.*
