# Architectural Refactoring Plan: SOLID, DRY, Clean Architecture

**Status**: Draft for Review  
**Created**: 2025-11-19  
**Priority**: CRITICAL - Foundation before feature implementation  
**Framework**: .NET 10, SQL Server 2025, EF Core DB-First  

---

## Executive Summary

**Current State**: Working but architecturally inconsistent - controllers with direct dependencies, services without interfaces, DTOs embedded in controllers, mixed concerns  
**Target State**: Clean architecture with SOLID principles, proper separation of concerns, testable, maintainable, extensible  
**Why Now**: Before implementing auth, workers, and production features - build on solid foundation  

---

## Architectural Violations Identified

### 1. Interface Segregation Violations

**Problem**: Controllers depend on concrete implementations
```csharp
// DataIngestionController.cs - Current
public DataIngestionController(
    IFileTypeDetector fileTypeDetector,                    // ✅ Interface
    IEnumerable<IAtomizer<byte[]>> atomizers,             // ✅ Interface
    AtomBulkInsertService bulkInsertService,              // ❌ Concrete class
    ILogger<DataIngestionController> logger)              // ✅ Interface
```

**Solution**: Create interfaces for all services
```csharp
public interface IAtomBulkInsertService
{
    Task<int> BulkInsertAtomsAsync(IEnumerable<AtomDto> atoms, CancellationToken ct);
}

public class AtomBulkInsertService : IAtomBulkInsertService
{
    // Implementation
}
```

### 2. Single Responsibility Violations

**Problem**: Controllers have multiple responsibilities
```csharp
// DataIngestionController - Currently does:
// 1. File upload handling
// 2. File type detection coordination
// 3. Atomizer selection
// 4. Job tracking
// 5. Bulk insert coordination
// 6. Error handling
// 7. Response formatting
```

**Solution**: Extract orchestration to service layer
```csharp
public interface IIngestionOrchestrationService
{
    Task<IngestionJobResult> IngestFileAsync(Stream fileStream, string fileName, int tenantId);
    Task<IngestionJobResult> IngestFromUrlAsync(string url, int tenantId);
    Task<IngestionJobResult> IngestFromDatabaseAsync(string query, string connectionString, int tenantId);
}

// Controller becomes thin coordinator
public class DataIngestionController : ControllerBase
{
    private readonly IIngestionOrchestrationService _orchestration;
    
    [HttpPost("file")]
    public async Task<IActionResult> IngestFile([FromForm] IFormFile file, [FromForm] int tenantId)
    {
        using var stream = file.OpenReadStream();
        var result = await _orchestration.IngestFileAsync(stream, file.FileName, tenantId);
        return Ok(result);
    }
}
```

### 3. Dependency Inversion Violations

**Problem**: High-level modules depend on low-level modules
```csharp
// Controller depends on concrete AtomBulkInsertService (low-level)
// Should depend on IAtomPersistenceService (high-level abstraction)
```

**Solution**: Introduce abstraction layers
```
Controllers → IOrchestrationServices → IBusinessServices → IRepositories → DbContext
```

### 4. Open/Closed Violations

**Problem**: Adding new atomizers requires modifying controller logic
```csharp
// Current: Controller knows about atomizer selection
var atomizer = _atomizers
    .Where(a => a.CanHandle(fileType.ContentType, fileType.Extension))
    .OrderByDescending(a => a.Priority)
    .FirstOrDefault();
```

**Solution**: Strategy pattern with factory
```csharp
public interface IAtomizerFactory
{
    IAtomizer<byte[]> GetAtomizer(string contentType, string extension);
}

public class AtomizerFactory : IAtomizerFactory
{
    private readonly IEnumerable<IAtomizer<byte[]>> _atomizers;
    
    public IAtomizer<byte[]> GetAtomizer(string contentType, string extension)
    {
        return _atomizers
            .Where(a => a.CanHandle(contentType, extension))
            .OrderByDescending(a => a.Priority)
            .FirstOrDefault()
            ?? throw new NotSupportedException($"No atomizer for {contentType}");
    }
}
```

### 5. DRY Violations

**Problem**: Response DTOs duplicated across controllers
```csharp
// DataIngestionController has:
public class IngestionJobResponse { ... }

// ProvenanceController has:
public class ProvenanceResponse { ... }

// Both need pagination, error handling, success/failure wrapping
```

**Solution**: Generic base response models (already in plan, Phase 3)

---

## Refactoring Phases

### Phase 0: Create Core Abstractions (Week 0.5)

**Objective**: Establish service layer interfaces before refactoring

#### 0.1 Create Service Interfaces

**New Project**: `Hartonomous.Core.Abstractions` (or add to existing structure)

**Structure**:
```
src/Hartonomous.Core.Abstractions/
├── Services/
│   ├── Ingestion/
│   │   ├── IIngestionOrchestrationService.cs
│   │   ├── IAtomizerFactory.cs
│   │   ├── IAtomPersistenceService.cs
│   │   └── IJobTrackingService.cs
│   ├── Provenance/
│   │   ├── IProvenanceQueryService.cs (already exists)
│   │   ├── IProvenanceGraphService.cs
│   │   └── ILineageTraversalService.cs
│   ├── Reasoning/
│   │   ├── IReasoningService.cs
│   │   ├── IChainOfThoughtService.cs
│   │   └── ITreeOfThoughtService.cs
│   ├── Streaming/
│   │   ├── IStreamingIngestionService.cs
│   │   ├── ITelemetryProcessor.cs
│   │   └── IStreamSessionManager.cs
│   └── Common/
│       ├── IHealthCheckService.cs
│       ├── IConfigurationService.cs
│       └── ITenantContextService.cs
├── Repositories/
│   ├── IAtomRepository.cs
│   ├── IInferenceRepository.cs
│   ├── IProvenanceRepository.cs
│   └── Generic/
│       ├── IRepository.cs
│       └── IReadOnlyRepository.cs
└── Models/
    ├── Common/
    │   ├── Result.cs (Either/Result monad)
    │   ├── PagedResult.cs
    │   └── OperationResult.cs
    └── Domain/
        ├── AtomDto.cs
        ├── InferenceDto.cs
        └── ProvenanceDto.cs
```

#### 0.2 Establish Generic Repository Pattern

**Interface**:
```csharp
public interface IRepository<TEntity, TKey> 
    where TEntity : class, IEntity<TKey>
    where TKey : IEquatable<TKey>
{
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default);
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken ct = default);
    Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default);
    Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);
    Task UpdateAsync(TEntity entity, CancellationToken ct = default);
    Task DeleteAsync(TKey id, CancellationToken ct = default);
    Task<bool> ExistsAsync(TKey id, CancellationToken ct = default);
}

public interface IReadOnlyRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : IEquatable<TKey>
{
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default);
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken ct = default);
    Task<bool> ExistsAsync(TKey id, CancellationToken ct = default);
}
```

**Implementation** (Generic):
```csharp
public class EfRepository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : IEquatable<TKey>
{
    private readonly HartonomousDbContext _context;
    private readonly DbSet<TEntity> _dbSet;
    
    public EfRepository(HartonomousDbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }
    
    public async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, ct);
    }
    
    // ... implementations
}
```

#### 0.3 Result/Either Monad Pattern

**Problem**: Exceptions for control flow
**Solution**: Explicit success/failure types

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public Exception? Exception { get; }
    
    private Result(bool isSuccess, T? value, string? error, Exception? exception)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        Exception = exception;
    }
    
    public static Result<T> Success(T value) => new(true, value, null, null);
    public static Result<T> Failure(string error) => new(false, default, error, null);
    public static Result<T> Failure(Exception exception) => new(false, default, exception.Message, exception);
    
    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<string, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value!) : onFailure(Error!);
    }
}

// Usage:
public async Task<Result<IngestionJobResult>> IngestFileAsync(...)
{
    try
    {
        var job = await ProcessIngestionAsync(...);
        return Result<IngestionJobResult>.Success(job);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Ingestion failed");
        return Result<IngestionJobResult>.Failure(ex);
    }
}

// Controller:
var result = await _orchestration.IngestFileAsync(...);
return result.Match(
    onSuccess: job => Ok(job),
    onFailure: error => BadRequest(new { error })
);
```

---

### Phase 1: Service Layer Extraction (Week 1)

#### 1.1 Extract Ingestion Orchestration Service

**Current**: Controller does everything  
**Target**: Thin controller, fat service

**Create**: `IngestionOrchestrationService.cs`
```csharp
public interface IIngestionOrchestrationService
{
    Task<Result<IngestionJobResult>> IngestFileAsync(
        Stream fileStream,
        string fileName,
        int tenantId,
        CancellationToken ct = default);
        
    Task<Result<IngestionJobResult>> IngestFromUrlAsync(
        string url,
        int tenantId,
        CancellationToken ct = default);
        
    Task<IngestionJobStatus> GetJobStatusAsync(string jobId);
}

public class IngestionOrchestrationService : IIngestionOrchestrationService
{
    private readonly IFileTypeDetector _fileTypeDetector;
    private readonly IAtomizerFactory _atomizerFactory;
    private readonly IAtomPersistenceService _persistence;
    private readonly IJobTrackingService _jobTracking;
    private readonly ILogger<IngestionOrchestrationService> _logger;
    
    public async Task<Result<IngestionJobResult>> IngestFileAsync(
        Stream fileStream,
        string fileName,
        int tenantId,
        CancellationToken ct = default)
    {
        var jobId = Guid.NewGuid().ToString();
        await _jobTracking.CreateJobAsync(jobId, fileName, tenantId);
        
        try
        {
            // 1. Detect file type
            var fileContent = await ReadStreamAsync(fileStream, ct);
            var fileType = _fileTypeDetector.Detect(fileContent, fileName);
            
            await _jobTracking.UpdateJobAsync(jobId, status: "detecting", 
                metadata: new { DetectedType = fileType.ContentType });
            
            // 2. Get atomizer
            var atomizer = _atomizerFactory.GetAtomizer(fileType.ContentType, fileType.Extension);
            
            // 3. Atomize
            await _jobTracking.UpdateJobAsync(jobId, status: "atomizing");
            var atoms = await atomizer.AtomizeAsync(fileContent, ct);
            
            // 4. Persist
            await _jobTracking.UpdateJobAsync(jobId, status: "persisting", 
                metadata: new { AtomCount = atoms.Count() });
            var insertedCount = await _persistence.BulkInsertAtomsAsync(atoms, tenantId, ct);
            
            // 5. Complete
            await _jobTracking.CompleteJobAsync(jobId, insertedCount);
            
            return Result<IngestionJobResult>.Success(new IngestionJobResult
            {
                JobId = jobId,
                Status = "completed",
                AtomsCreated = insertedCount
            });
        }
        catch (Exception ex)
        {
            await _jobTracking.FailJobAsync(jobId, ex.Message);
            return Result<IngestionJobResult>.Failure(ex);
        }
    }
}
```

**Refactored Controller**:
```csharp
public class DataIngestionController : ControllerBase
{
    private readonly IIngestionOrchestrationService _orchestration;
    
    [HttpPost("file")]
    public async Task<IActionResult> IngestFile(
        [FromForm] IFormFile file,
        [FromForm] int tenantId = 0)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file provided");
        
        using var stream = file.OpenReadStream();
        var result = await _orchestration.IngestFileAsync(stream, file.FileName, tenantId);
        
        return result.Match(
            onSuccess: job => Ok(job),
            onFailure: error => StatusCode(500, new { error })
        );
    }
    
    [HttpGet("jobs/{jobId}")]
    public async Task<IActionResult> GetJobStatus(string jobId)
    {
        var status = await _orchestration.GetJobStatusAsync(jobId);
        return status != null ? Ok(status) : NotFound();
    }
}
```

#### 1.2 Extract Supporting Services

**IAtomPersistenceService**:
```csharp
public interface IAtomPersistenceService
{
    Task<int> BulkInsertAtomsAsync(
        IEnumerable<AtomDto> atoms,
        int tenantId,
        CancellationToken ct = default);
        
    Task<AtomDto?> GetAtomAsync(byte[] contentHash, int tenantId);
    Task<IEnumerable<AtomDto>> GetAtomsBySourceAsync(string sourceUri, int tenantId);
}

public class AtomPersistenceService : IAtomPersistenceService
{
    private readonly IAtomRepository _repository;
    private readonly AtomBulkInsertService _bulkInsert; // Keep efficient bulk insert
    
    public async Task<int> BulkInsertAtomsAsync(
        IEnumerable<AtomDto> atoms,
        int tenantId,
        CancellationToken ct = default)
    {
        // Delegate to optimized bulk insert service
        return await _bulkInsert.BulkInsertAtomsAsync(atoms, ct);
    }
}
```

**IJobTrackingService**:
```csharp
public interface IJobTrackingService
{
    Task<string> CreateJobAsync(string jobId, string fileName, int tenantId);
    Task UpdateJobAsync(string jobId, string status, object? metadata = null);
    Task CompleteJobAsync(string jobId, int atomsCreated);
    Task FailJobAsync(string jobId, string errorMessage);
    Task<IngestionJobStatus?> GetJobStatusAsync(string jobId);
}

// Implementation options:
// 1. In-memory (current) - ConcurrentDictionary
// 2. SQL Server - IngestionJobs table
// 3. Redis - Distributed cache
// 4. Service Broker queue - Async processing
```

---

### Phase 2: Controller Base Classes (Week 1)

#### 2.1 Create ApiControllerBase

**Problem**: Repeated code in controllers (validation, error handling, response formatting)

**Solution**:
```csharp
public abstract class ApiControllerBase : ControllerBase
{
    protected readonly ILogger Logger;
    
    protected ApiControllerBase(ILogger logger)
    {
        Logger = logger;
    }
    
    // Standard response helpers
    protected IActionResult SuccessResult<T>(T data, string? message = null)
    {
        return Ok(new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            Timestamp = DateTime.UtcNow
        });
    }
    
    protected IActionResult ErrorResult(string error, int statusCode = 500)
    {
        Logger.LogError("API Error: {Error}", error);
        return StatusCode(statusCode, new ErrorResponse
        {
            Error = "API Error",
            Message = error,
            TraceId = HttpContext.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        });
    }
    
    protected IActionResult ValidationError(ModelStateDictionary modelState)
    {
        var errors = modelState
            .Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(
                e => e.Key,
                e => e.Value!.Errors.Select(x => x.ErrorMessage).ToArray()
            );
            
        return BadRequest(new ErrorResponse
        {
            Error = "Validation Failed",
            Message = "One or more validation errors occurred",
            ValidationErrors = errors,
            TraceId = HttpContext.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        });
    }
    
    // Result monad helper
    protected IActionResult FromResult<T>(Result<T> result)
    {
        return result.Match(
            onSuccess: data => SuccessResult(data),
            onFailure: error => ErrorResult(error)
        );
    }
    
    // Pagination helper
    protected IActionResult PagedResult<T>(
        IEnumerable<T> items,
        int page,
        int pageSize,
        int totalCount)
    {
        return Ok(new PaginatedResponse<T>
        {
            Items = items.ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = totalCount
        });
    }
}
```

**Usage**:
```csharp
public class DataIngestionController : ApiControllerBase
{
    private readonly IIngestionOrchestrationService _orchestration;
    
    public DataIngestionController(
        IIngestionOrchestrationService orchestration,
        ILogger<DataIngestionController> logger)
        : base(logger)
    {
        _orchestration = orchestration;
    }
    
    [HttpPost("file")]
    public async Task<IActionResult> IngestFile([FromForm] IFormFile file, [FromForm] int tenantId)
    {
        if (!ModelState.IsValid)
            return ValidationError(ModelState);
        
        using var stream = file.OpenReadStream();
        var result = await _orchestration.IngestFileAsync(stream, file.FileName, tenantId);
        
        return FromResult(result); // One line!
    }
}
```

---

### Phase 3: Dependency Injection Cleanup (Week 1)

#### 3.1 Service Registration Organization

**Current**: Mixed registration in `Program.cs` and extension methods  
**Target**: Organized service registrations by layer

**Create**: Extension method per layer

**Infrastructure Layer**:
```csharp
// Hartonomous.Infrastructure/ServiceCollectionExtensions.cs
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<HartonomousDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("HartonomousDb")));
        
        // Repositories
        services.AddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));
        services.AddScoped<IAtomRepository, AtomRepository>();
        services.AddScoped<IInferenceRepository, InferenceRepository>();
        
        // Core Services
        services.AddScoped<IAtomPersistenceService, AtomPersistenceService>();
        services.AddSingleton<IFileTypeDetector, FileTypeDetector>();
        services.AddSingleton<IAtomizerFactory, AtomizerFactory>();
        
        // Atomizers (all 18)
        services.AddAtomizers();
        
        // Health Checks
        services.AddHealthChecks()
            .AddCheck<SqlServerHealthCheck>("sql")
            .AddCheck<Neo4jHealthCheck>("neo4j");
        
        return services;
    }
    
    private static IServiceCollection AddAtomizers(this IServiceCollection services)
    {
        services.AddTransient<IAtomizer<byte[]>, TextAtomizer>();
        services.AddTransient<IAtomizer<byte[]>, ImageAtomizer>();
        services.AddTransient<IAtomizer<byte[]>, EnhancedImageAtomizer>();
        // ... all 18 atomizers
        return services;
    }
}
```

**Application Layer**:
```csharp
// Hartonomous.Api/ServiceCollectionExtensions.cs
public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        // Orchestration Services
        services.AddScoped<IIngestionOrchestrationService, IngestionOrchestrationService>();
        services.AddScoped<IProvenanceQueryService, ProvenanceQueryService>();
        services.AddScoped<IReasoningService, ReasoningService>();
        
        // Supporting Services
        services.AddSingleton<IJobTrackingService, InMemoryJobTrackingService>();
        services.AddScoped<ITenantContextService, TenantContextService>();
        
        return services;
    }
}
```

**Program.cs** (clean):
```csharp
var builder = WebApplication.CreateBuilder(args);

// Layer registration (order matters: Infrastructure → Application → Presentation)
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
// ... middleware
```

---

### Phase 4: Shared Kernel / Common Library (Week 2)

#### 4.1 Extract Common Patterns

**Create**: `Hartonomous.Shared.Common` project

**Contents**:
```
Hartonomous.Shared.Common/
├── Patterns/
│   ├── Result.cs
│   ├── Option.cs (Maybe monad)
│   └── Either.cs
├── Extensions/
│   ├── StringExtensions.cs
│   ├── ByteArrayExtensions.cs
│   └── EnumerableExtensions.cs
├── Guards/
│   └── Guard.cs (argument validation)
├── Constants/
│   ├── ContentTypes.cs
│   ├── Modalities.cs
│   └── ErrorCodes.cs
└── Attributes/
    ├── TenantScopedAttribute.cs
    └── AuditableAttribute.cs
```

**Example - Guard Clauses**:
```csharp
public static class Guard
{
    public static T NotNull<T>(T value, string paramName) where T : class
    {
        if (value == null)
            throw new ArgumentNullException(paramName);
        return value;
    }
    
    public static string NotNullOrEmpty(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null or empty", paramName);
        return value;
    }
    
    public static int Positive(int value, string paramName)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(paramName, "Value must be positive");
        return value;
    }
}

// Usage:
public class AtomPersistenceService
{
    public async Task<int> BulkInsertAtomsAsync(IEnumerable<AtomDto> atoms, int tenantId)
    {
        Guard.NotNull(atoms, nameof(atoms));
        Guard.Positive(tenantId, nameof(tenantId));
        
        // ... implementation
    }
}
```

---

## Implementation Checklist

### Week 0.5: Foundation
- [ ] Create `Hartonomous.Core.Abstractions` project (or organize existing)
- [ ] Define service interfaces (IIngestionOrchestrationService, IAtomPersistenceService, etc.)
- [ ] Create generic repository interfaces (IRepository, IReadOnlyRepository)
- [ ] Implement Result/Either monad pattern
- [ ] Create base response models (ApiResponse, ErrorResponse, PaginatedResponse)

### Week 1: Service Layer
- [ ] Extract IngestionOrchestrationService from DataIngestionController
- [ ] Extract AtomPersistenceService wrapper
- [ ] Extract JobTrackingService (in-memory implementation)
- [ ] Create AtomizerFactory
- [ ] Create ApiControllerBase with helper methods
- [ ] Refactor DataIngestionController to use new services
- [ ] Organize DI registration (InfrastructureServiceExtensions, ApplicationServiceExtensions)

### Week 2: Apply Pattern Across Codebase
- [ ] Refactor ProvenanceController → ProvenanceOrchestrationService
- [ ] Refactor ReasoningController → ReasoningOrchestrationService
- [ ] Refactor StreamingIngestionController → StreamingOrchestrationService
- [ ] Create Hartonomous.Shared.Common project
- [ ] Extract Guard clauses
- [ ] Extract common extensions
- [ ] Create shared constants

### Week 3: Repository Layer
- [ ] Implement EfRepository<TEntity, TKey>
- [ ] Create AtomRepository with specialized queries
- [ ] Create InferenceRepository
- [ ] Create ProvenanceRepository
- [ ] Update services to use repositories instead of direct DbContext

### Week 4: Testing & Documentation
- [ ] Unit tests for services (mocked dependencies)
- [ ] Integration tests for repositories
- [ ] Update architecture documentation
- [ ] Create dependency diagram
- [ ] Code review and cleanup

---

## Benefits

### Testability
```csharp
// Before: Controller tightly coupled to concrete classes
[Fact]
public void TestDataIngestionController()
{
    // Can't test without real AtomBulkInsertService, real IFileTypeDetector, etc.
}

// After: Controller depends on interfaces
[Fact]
public void TestDataIngestionController()
{
    var mockOrchestration = new Mock<IIngestionOrchestrationService>();
    mockOrchestration
        .Setup(o => o.IngestFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<int>()))
        .ReturnsAsync(Result<IngestionJobResult>.Success(new IngestionJobResult { JobId = "test" }));
    
    var controller = new DataIngestionController(mockOrchestration.Object, Mock.Of<ILogger>());
    var result = await controller.IngestFile(mockFile, tenantId: 1);
    
    Assert.IsType<OkObjectResult>(result);
}
```

### Maintainability
- **Single file changes**: Update service interface → Update implementation → Done
- **No cascading changes**: Controller doesn't know about atomizer changes
- **Clear boundaries**: Each layer has single responsibility

### Extensibility
- **New atomizers**: Register in DI, factory handles selection
- **New storage**: Swap IAtomPersistenceService implementation (SQL → CosmosDB → both)
- **New job tracking**: Swap IJobTrackingService (in-memory → SQL → Redis → Service Broker)

### Performance
- **Async all the way**: No blocking calls
- **Efficient bulk operations**: Keep AtomBulkInsertService for bulk inserts
- **Repository caching**: Add caching layer without changing consumers

---

## Migration Strategy

### Parallel Development
1. Create new service interfaces alongside existing code
2. Implement new services
3. Update controllers one at a time
4. Remove old code when all controllers migrated

### Feature Flags
```csharp
public class DataIngestionController : ApiControllerBase
{
    private readonly IIngestionOrchestrationService _newService;
    private readonly AtomBulkInsertService _oldService;
    private readonly IConfiguration _config;
    
    [HttpPost("file")]
    public async Task<IActionResult> IngestFile(...)
    {
        bool useNewArchitecture = _config.GetValue<bool>("FeatureFlags:UseRefactoredServices");
        
        if (useNewArchitecture)
        {
            var result = await _newService.IngestFileAsync(...);
            return FromResult(result);
        }
        else
        {
            // Old implementation
        }
    }
}
```

### Incremental Rollout
- Week 1: DataIngestionController only
- Week 2: ProvenanceController + ReasoningController
- Week 3: Remaining controllers
- Week 4: Remove old code, cleanup

---

## Success Criteria

### Code Quality
✅ All controllers < 200 lines  
✅ All services have interfaces  
✅ No controllers depend on concrete classes (except ILogger)  
✅ No business logic in controllers  
✅ All repositories use generic pattern  

### Architecture
✅ Clean dependency flow: Controllers → Services → Repositories → DbContext  
✅ SOLID principles applied consistently  
✅ DRY: No duplicated code patterns  
✅ SoC: Each class has single responsibility  

### Testing
✅ 80%+ unit test coverage on services  
✅ All services mockable  
✅ Integration tests for repositories  
✅ Controller tests use mocked services  

### Documentation
✅ Architecture decision records (ADRs) written  
✅ Dependency diagram updated  
✅ Service interface documentation complete  
✅ Migration guide for future features  

---

## Anti-Patterns to Avoid

### ❌ Over-Abstraction
```csharp
// Don't create interfaces for everything
public interface IStringHelper { string ToUpperCase(string s); }

// Keep it simple - static methods are fine for utilities
public static class StringHelpers
{
    public static string ToUpperCase(string s) => s.ToUpper();
}
```

### ❌ Anemic Domain Models
```csharp
// Avoid: All logic in services, entities are just data bags
public class Atom
{
    public long AtomId { get; set; }
    public string Modality { get; set; }
}

public class AtomService
{
    public bool IsTextAtom(Atom atom) => atom.Modality == "text";
    public bool IsImageAtom(Atom atom) => atom.Modality == "image";
}

// Better: Domain logic lives in partial classes (for scaffolded entities)
public partial class Atom
{
    public bool IsTextAtom => Modality == "text";
    public bool IsImageAtom => Modality == "image";
}
```

### ❌ Repository Over-Specification
```csharp
// Don't create method for every possible query
public interface IAtomRepository
{
    Task<Atom> GetAtomByHashAsync(byte[] hash);
    Task<Atom> GetAtomByHashAndTenantAsync(byte[] hash, int tenantId);
    Task<IEnumerable<Atom>> GetAtomsByModalityAsync(string modality);
    Task<IEnumerable<Atom>> GetAtomsByModalityAndTenantAsync(string modality, int tenantId);
    // ... 50 more methods
}

// Better: Use specification pattern or LINQ
public interface IAtomRepository : IRepository<Atom, long>
{
    Task<Atom?> GetByHashAsync(byte[] hash, int tenantId);
    IQueryable<Atom> Query(); // For complex queries
}
```

---

## Next Steps

1. **Review this plan** with architecture perspective
2. **Prioritize phases** (Can we do Phase 0 + Phase 1 immediately?)
3. **Create feature branch**: `refactor/solid-architecture`
4. **Begin Phase 0**: Create abstractions
5. **Parallel with production plan**: Refactor while implementing auth/workers

---

**Document Version**: 1.0  
**Last Updated**: 2025-11-19  
**Status**: Draft for Review  
**Recommendation**: START IMMEDIATELY - Foundation is critical
