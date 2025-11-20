# Hartonomous Application Layer - Production Implementation Plan

**Status**: Revised - Incorporating Validated Patterns  
**Created**: 2025-11-19  
**Last Updated**: 2025-11-19  
**Target**: Production-ready .NET 10 API with Azure Arc on-prem deployment  

---

## Executive Summary

**Current State**: Demo/marketing-ready API with comprehensive atomization pipeline and mock services  
**Target State**: Production-ready API with Microsoft-validated patterns, Entra ID auth, middleware architecture, Azure services integration, worker processes, and master/detail query capabilities  
**Gap**: Security, middleware pipeline, configuration management, background processing, observability, and hierarchical atom navigation

### Architectural Validation Applied

This plan incorporates findings from **ARCHITECTURAL-VALIDATION-REPORT.md**:

✅ **Adopted Patterns** (Microsoft Recommended):
- Clean Architecture with Dependency Injection
- DbContext direct usage (no repository layer)
- Exceptions + Problem Details for error handling
- Selective service interfaces (business logic only)
- Middleware for cross-cutting concerns
- Composition over inheritance (extension methods, filters)

❌ **Rejected Patterns** (Not Microsoft Recommended):
- Generic repository over EF Core
- Result<T> pattern
- Universal service interfaces
- ApiControllerBase inheritance
- Service locator pattern

---

## Architecture Context

### Deployment Model
- **Azure Arc on-prem**: Two servers with Arc agents
- **CI/CD**: GitHub Actions + Azure Pipelines (dual runners configured)
- **Authentication**: Entra ID (internal), External ID/CIAM (external users)
- **Azure Services**: App Configuration, Key Vault only (minimal Azure footprint)
- **Database**: SQL Server 2022 (Arc-enabled) with CLR assemblies
- **Graph**: Neo4j for provenance (local)

### Current Application Structure

```text
Hartonomous.Api (Web API - .NET 10)
├─ 8 Controllers (DataIngestion, Provenance, Reasoning, Streaming, etc.)
├─ Request/Response DTOs (embedded in controllers)
├─ Mock services (demo mode)
├─ No authentication
└─ No middleware pipeline (no error handling, logging, correlation)

Hartonomous.Data.Entities (EF Core DB-First)
├─ HartonomousDbContext (scaffolded from SQL Server 2025)
├─ 40+ Entity classes (TensorAtoms, AtomCompositions, Inferences, etc.)
├─ Fluent API configurations
└─ Auto-generated from database schema

Hartonomous.Infrastructure
├─ 18 Atomizers (text, image, video, code, models, Ollama, HuggingFace)
├─ Production services (SQL, Neo4j, Spatial)
├─ Health checks (SQL, Neo4j, Key Vault, App Config)
└─ Vision services (OCR, object detection, scene analysis)

Hartonomous.Admin (Blazor)
├─ Entra ID authentication ✅
├─ Azure App Config integration ✅
└─ Azure Key Vault integration ✅

Missing:
❌ Hartonomous.Workers.* (no background processing)
❌ API authentication & authorization
❌ Middleware pipeline (error handling, logging, telemetry)
❌ Application Insights integration
❌ Service layer (business logic separation)
❌ Problem Details error responses
❌ Production configuration
```

---

## Phase 0: Foundation - Middleware & Architecture (Week 1, Part 1)

### 0.1 Configure Middleware Pipeline

**Objective**: Establish production-ready middleware pipeline following Microsoft patterns

**Tasks**:

1. **Problem Details Configuration** (RFC 7807)
   ```csharp
   // Program.cs
   builder.Services.AddProblemDetails(options =>
   {
       options.CustomizeProblemDetails = ctx =>
       {
           ctx.ProblemDetails.Extensions["traceId"] = 
               Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier;
           ctx.ProblemDetails.Extensions["timestamp"] = DateTime.UtcNow;
           ctx.ProblemDetails.Extensions["environment"] = 
               builder.Environment.EnvironmentName;
       };
   });
   ```

2. **Global Exception Handler**
   ```csharp
   var app = builder.Build();
   
   if (!app.Environment.IsDevelopment())
   {
       app.UseExceptionHandler(); // Converts exceptions → Problem Details
   }
   else
   {
       app.UseDeveloperExceptionPage(); // Detailed errors in dev
   }
   
   app.UseStatusCodePages(); // 404, 401, etc. → Problem Details
   ```

3. **Application Insights Middleware**
   ```csharp
   builder.Services.AddApplicationInsightsTelemetry(options =>
   {
       options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
   });
   
   builder.Services.AddOpenTelemetry()
       .UseAzureMonitor();
   ```

4. **Correlation ID Middleware** (Custom)
   ```csharp
   // Middleware/CorrelationIdMiddleware.cs
   public class CorrelationIdMiddleware
   {
       private readonly RequestDelegate _next;
       
       public async Task InvokeAsync(HttpContext context)
       {
           var activity = Activity.Current;
           
           context.Response.OnStarting(() =>
           {
               context.Response.Headers.Add("X-Correlation-ID", 
                   activity?.RootId ?? context.TraceIdentifier);
               context.Response.Headers.Add("X-Request-ID", 
                   context.TraceIdentifier);
               return Task.CompletedTask;
           });
           
           await _next(context);
       }
   }
   
   // Registration
   app.Use<CorrelationIdMiddleware>(); // After exception handler
   ```

5. **Request Logging Middleware** (Custom)
   ```csharp
   // Middleware/RequestLoggingMiddleware.cs
   public class RequestLoggingMiddleware
   {
       private readonly RequestDelegate _next;
       private readonly ILogger<RequestLoggingMiddleware> _logger;
       
       public RequestLoggingMiddleware(RequestDelegate next, 
           ILogger<RequestLoggingMiddleware> logger)
       {
           _next = next;
           _logger = logger;
       }
       
       public async Task InvokeAsync(HttpContext context)
       {
           var sw = Stopwatch.StartNew();
           
           _logger.LogInformation("Request: {Method} {Path} TraceId: {TraceId}",
               context.Request.Method,
               context.Request.Path,
               context.TraceIdentifier);
           
           await _next(context);
           
           sw.Stop();
           _logger.LogInformation("Response: {StatusCode} Duration: {Duration}ms TraceId: {TraceId}",
               context.Response.StatusCode,
               sw.ElapsedMilliseconds,
               context.TraceIdentifier);
       }
   }
   ```

6. **Complete Middleware Pipeline Order**
   ```csharp
   var app = builder.Build();
   
   // 1. Exception handling (first - catches everything)
   if (!app.Environment.IsDevelopment())
       app.UseExceptionHandler();
   else
       app.UseDeveloperExceptionPage();
   
   // 2. Status code pages
   app.UseStatusCodePages();
   
   // 3. HTTPS redirection
   app.UseHttpsRedirection();
   
   // 4. Correlation ID (before logging)
   app.UseMiddleware<CorrelationIdMiddleware>();
   
   // 5. Request logging
   app.UseMiddleware<RequestLoggingMiddleware>();
   
   // 6. CORS (if needed)
   // app.UseCors("AllowWebApp");
   
   // 7. Authentication
   app.UseAuthentication();
   
   // 8. Authorization
   app.UseAuthorization();
   
   // 9. Routing
   app.UseRouting();
   
   // 10. Endpoints
   app.MapControllers();
   app.MapHealthChecks("/health");
   
   app.Run();
   ```

**Acceptance Criteria**:
- ✅ All exceptions converted to Problem Details (RFC 7807)
- ✅ Correlation IDs in response headers
- ✅ Request/response logging to Application Insights
- ✅ Middleware pipeline ordered correctly
- ✅ No manual try/catch in controllers (global handler)

---

### 0.2 Create Extension Methods & Utilities

**Objective**: Shared functionality via composition (not inheritance)

**Tasks**:

1. **Controller Extension Methods**
   ```csharp
   // Extensions/ControllerExtensions.cs
   public static class ControllerExtensions
   {
       public static int GetTenantId(this ControllerBase controller)
       {
           var claim = controller.User.FindFirst("tenant_id");
           if (claim == null)
               throw new UnauthorizedAccessException("Tenant ID not found in claims");
           return int.Parse(claim.Value);
       }
       
       public static string GetUserId(this ControllerBase controller)
       {
           var claim = controller.User.FindFirst(ClaimTypes.NameIdentifier);
           if (claim == null)
               throw new UnauthorizedAccessException("User ID not found in claims");
           return claim.Value;
       }
       
       public static ActionResult<PaginatedResponse<T>> Paginated<T>(
           this ControllerBase controller,
           IEnumerable<T> items,
           int total,
           int page,
           int pageSize)
       {
           return controller.Ok(new PaginatedResponse<T>
           {
               Items = items,
               Total = total,
               Page = page,
               PageSize = pageSize,
               TotalPages = (int)Math.Ceiling(total / (double)pageSize),
               HasNextPage = page < (int)Math.Ceiling(total / (double)pageSize),
               HasPreviousPage = page > 1
           });
       }
   }
   ```

2. **Custom Domain Exceptions**
   ```csharp
   // Exceptions/DomainExceptions.cs
   public class InvalidFileFormatException : Exception
   {
       public InvalidFileFormatException(string message) : base(message) { }
   }
   
   public class AtomizationFailedException : Exception
   {
       public AtomizationFailedException(string message, Exception inner) 
           : base(message, inner) { }
   }
   
   public class ProvenanceTrackingException : Exception
   {
       public ProvenanceTrackingException(string message, Exception inner)
           : base(message, inner) { }
   }
   
   public class TenantIsolationException : Exception
   {
       public TenantIsolationException(string message) : base(message) { }
   }
   ```

3. **Filters for Cross-Cutting Concerns**
   ```csharp
   // Filters/RequireTenantAttribute.cs
   [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
   public class RequireTenantAttribute : ActionFilterAttribute
   {
       public override void OnActionExecuting(ActionExecutingContext context)
       {
           var controller = context.Controller as ControllerBase;
           var tenantClaim = controller?.User.FindFirst("tenant_id");
           
           if (tenantClaim == null)
           {
               context.Result = new UnauthorizedObjectResult(
                   new ProblemDetails
                   {
                       Status = 401,
                       Title = "Tenant Required",
                       Detail = "Tenant ID not found in claims"
                   });
           }
       }
   }
   ```

**Acceptance Criteria**:
- ✅ Extension methods available on all controllers
- ✅ Custom exceptions defined for domain errors
- ✅ Filters created for common validations
- ✅ No ApiControllerBase class (composition pattern)

---

### 0.3 Configure Service Lifetimes

**Objective**: Correct DI lifetime configuration per Microsoft guidance

**Tasks**:

1. **Service Registration with Correct Lifetimes**
   ```csharp
   // Program.cs - Service Registration
   
   // ===== SINGLETON SERVICES =====
   // Stateless utilities, configuration
   builder.Services.AddSingleton<IFileTypeDetector, FileTypeDetector>();
   builder.Services.AddSingleton<IDateTime, SystemDateTime>();
   
   // HttpClient factory (manages lifetime internally)
   builder.Services.AddHttpClient();
   
   // ===== SCOPED SERVICES =====
   // DbContext (MUST be scoped)
   builder.Services.AddDbContext<HartonomousDbContext>(options =>
       options.UseSqlServer(builder.Configuration.GetConnectionString("HartonomousDb")),
       ServiceLifetime.Scoped); // Critical: Scoped lifetime
   
   // Business Services (scoped to request, share DbContext)
   builder.Services.AddScoped<IIngestionService, IngestionService>();
   builder.Services.AddScoped<IProvenanceService, Neo4jProvenanceService>();
   builder.Services.AddScoped<IReasoningService, ReasoningService>();
   
   // Vision Services (scoped because use HttpClient)
   builder.Services.AddScoped<IOcrService, HartonomousOcrService>();
   builder.Services.AddScoped<IObjectDetectionService, HartonomousObjectDetectionService>();
   builder.Services.AddScoped<ISceneAnalysisService, HartonomousSceneAnalysisService>();
   
   // Atomizers (scoped, may need DbContext)
   builder.Services.AddScoped<IAtomizer<byte[]>, PdfAtomizer>();
   builder.Services.AddScoped<IAtomizer<byte[]>, ImageAtomizer>();
   builder.Services.AddScoped<IAtomizer<byte[]>, VideoAtomizer>();
   // ... register remaining atomizers
   
   // ===== BACKGROUND SERVICES =====
   // Hosted services (singleton, use IServiceScopeFactory)
   builder.Services.AddHostedService<TelemetryStreamProcessor>();
   builder.Services.AddHostedService<CesConsumerWorker>();
   builder.Services.AddHostedService<Neo4jSyncWorker>();
   ```

2. **Enable Scope Validation in Development**
   ```csharp
   if (app.Environment.IsDevelopment())
   {
       // Validates no captive dependencies (singleton capturing scoped)
       var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
       using var scope = scopeFactory.CreateScope();
       var _ = scope.ServiceProvider.GetRequiredService<HartonomousDbContext>();
   }
   ```

**Acceptance Criteria**:
- ✅ DbContext registered as Scoped
- ✅ Business services registered as Scoped
- ✅ Utilities registered as Singleton
- ✅ Background services use IServiceScopeFactory
- ✅ Scope validation enabled in Development
- ✅ No captive dependency errors

---

## Phase 1: Security & Identity (Week 1, Part 2)

### 1.1 Entra ID Authentication for API

**Objective**: Secure all API endpoints with Microsoft Entra ID authentication

**Tasks**:

1. **Configure Authentication in Program.cs**
   ```csharp
   // Add authentication
   builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
       .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
   
   // Add authorization policies
   builder.Services.AddAuthorization(options =>
   {
       options.AddPolicy("Admin", policy => 
           policy.RequireRole("Admin"));
       options.AddPolicy("ApiUser", policy => 
           policy.RequireAuthenticatedUser());
       options.AddPolicy("DataIngestion", policy => 
           policy.RequireRole("Admin", "Analyst", "User"));
   });
   ```

2. **Update appsettings.json**
   ```json
   {
     "AzureAd": {
       "Instance": "https://login.microsoftonline.com/",
       "TenantId": "<from-keyvault>",
       "ClientId": "<from-keyvault>",
       "Audience": "api://hartonomous"
     }
   }
   ```

3. **Add [Authorize] Attributes**
   - `DataIngestionController`: `[Authorize(Policy = "DataIngestion")]`
   - `ProvenanceController`: `[Authorize(Policy = "ApiUser")]`
   - `ReasoningController`: `[Authorize(Policy = "ApiUser")]`
   - `StreamingIngestionController`: `[Authorize(Policy = "DataIngestion")]`
   - `AuditController`: `[Authorize(Policy = "Admin")]`
   - `MLOpsController`: `[Authorize(Policy = "Admin")]`

4. **Configure Scopes & Roles**
   - Use existing Azure script: `scripts/azure/01-create-infrastructure.ps1`
   - App roles: Admin, Analyst, User (already defined in script)
   - OAuth scopes: `access_as_user`

**Acceptance Criteria**:
- ✅ Unauthenticated requests return 401
- ✅ Authenticated requests with valid token succeed
- ✅ Role-based authorization enforces policies
- ✅ Swagger UI supports Azure AD login flow

---

### 1.2 External ID (CIAM) for Public Access

**Objective**: Support external users via External ID (B2C replacement)

**Tasks**:

1. **Configure External ID Tenant**
   - Create External ID tenant (separate from internal Entra ID)
   - Register API application in External ID
   - Configure user flows (sign-up, sign-in, password reset)

2. **Multi-Tenant Authentication**
   ```csharp
   builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
       .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))
       .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("ExternalId"), "ExternalUsers");
   ```

3. **Tenant Isolation**
   - Extract `TenantId` from claims
   - Pass to all service layer calls
   - Enforce row-level security in SQL

**Acceptance Criteria**:
- ✅ Internal users authenticate via Entra ID
- ✅ External users authenticate via External ID
- ✅ Tenant isolation enforced at data layer
- ✅ No cross-tenant data leakage

---

### 1.3 API Key Authentication (Service-to-Service)

**Objective**: Support service-to-service authentication for automated workflows

**Tasks**:

1. **API Key Middleware**
   ```csharp
   public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
   {
       // Validate API key from header or query string
       // Load valid keys from Key Vault
   }
   ```

2. **Configure in Program.cs**
   ```csharp
   builder.Services.AddAuthentication()
       .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>("ApiKey", options => { });
   ```

3. **Store API Keys in Key Vault**
   - One key per service/tenant
   - Rotation policy
   - Audit logging

**Acceptance Criteria**:
- ✅ API keys validate against Key Vault
- ✅ Invalid keys return 401
- ✅ API key usage logged for audit

---

## Phase 2: Configuration Management (Week 1)

### 2.1 Azure App Configuration Integration

**Objective**: Load all configuration from Azure App Configuration

**Tasks**:

1. **Add to Program.cs**
   ```csharp
   var appConfigEndpoint = builder.Configuration["AzureAppConfigurationEndpoint"];
   if (!string.IsNullOrEmpty(appConfigEndpoint))
   {
       builder.Configuration.AddAzureAppConfiguration(options =>
       {
           options.Connect(new Uri(appConfigEndpoint), new DefaultAzureCredential())
                  .Select("Hartonomous:*")
                  .UseFeatureFlags();
       });
   }
   ```

2. **Migrate Settings to App Configuration**
   - Connection strings (HartonomousDb, Neo4j)
   - Feature flags (OODA loop enabled, auto-execution)
   - Service endpoints
   - Thresholds and limits

3. **Local Development Override**
   - Keep `appsettings.Development.json` for local dev
   - Use Azure CLI login for DefaultAzureCredential

**Acceptance Criteria**:
- ✅ Production reads from App Configuration
- ✅ Development uses local appsettings
- ✅ Feature flags control OODA loop behavior
- ✅ Configuration changes don't require redeploy

---

### 2.2 Azure Key Vault Integration

**Objective**: Store all secrets in Key Vault

**Tasks**:

1. **Add to Program.cs**
   ```csharp
   var keyVaultUri = builder.Configuration["KeyVaultUri"];
   if (!string.IsNullOrEmpty(keyVaultUri))
   {
       builder.Configuration.AddAzureKeyVault(
           new Uri(keyVaultUri),
           new DefaultAzureCredential());
   }
   ```

2. **Migrate Secrets**
   - SQL Server connection strings
   - Neo4j password
   - Entra ID client secrets
   - HuggingFace API tokens
   - API keys

3. **Access Pattern**
   ```csharp
   var sqlConnectionString = builder.Configuration["SqlConnectionString"]; // from Key Vault
   ```

**Acceptance Criteria**:
- ✅ No secrets in appsettings files
- ✅ Secrets loaded from Key Vault at startup
- ✅ Managed identity authentication to Key Vault
- ✅ Local development uses Azure CLI credentials

---

### 2.3 appsettings Files Structure

**Objective**: Proper configuration hierarchy

**Files**:

1. **appsettings.json** (base, committed)
   ```json
   {
     "Logging": { "LogLevel": { "Default": "Information" } },
     "AllowedHosts": "*",
     "KeyVaultUri": "https://kv-hartonomous-prod.vault.azure.net/",
     "AzureAppConfigurationEndpoint": "https://appconfig-hartonomous-prod.azconfig.io"
   }
   ```

2. **appsettings.Development.json** (local dev, gitignored)
   ```json
   {
     "ConnectionStrings": {
       "HartonomousDb": "Server=localhost;Database=Hartonomous;Trusted_Connection=True;TrustServerCertificate=True;",
       "Neo4j": "bolt://localhost:7687"
     },
     "Neo4j": { "Password": "local-password" }
   }
   ```

3. **appsettings.Production.json** (production, committed)
   ```json
   {
     "Logging": { "LogLevel": { "Default": "Warning" } }
   }
   ```

**Acceptance Criteria**:
- ✅ No secrets in committed files
- ✅ Development works without Azure
- ✅ Production loads from Azure services

---

## Phase 2: Service Layer Extraction (Week 1, Part 3)

### 2.1 Create Service Interfaces (Selective)

**Objective**: Extract business logic from controllers into service layer following Microsoft patterns

**Pattern**: Service interfaces for business logic only, direct DbContext usage (no repository layer)

**Tasks**:

1. **Create Service Interfaces** (Only where abstraction provides value)
   ```csharp
   // Services/IIngestionService.cs
   public interface IIngestionService
   {
       Task<IngestionResult> IngestFileAsync(byte[] fileData, string fileName, int tenantId);
       Task<IngestionResult> IngestUrlAsync(string url, int tenantId);
       Task<IngestionResult> IngestDatabaseAsync(DatabaseIngestionRequest request, int tenantId);
   }
   
   // Services/IProvenanceService.cs
   public interface IProvenanceService
   {
       Task TrackIngestionAsync(string source, IEnumerable<Atom> atoms, int tenantId);
       Task<LineageResponse> GetLineageAsync(string atomHash, int maxDepth, int tenantId);
   }
   
   // Services/IReasoningService.cs
   public interface IReasoningService
   {
       Task<ChainOfThoughtResponse> ExecuteChainOfThoughtAsync(ChainOfThoughtRequest request, int tenantId);
       Task<TreeOfThoughtResponse> ExecuteTreeOfThoughtAsync(TreeOfThoughtRequest request, int tenantId);
   }
   ```

2. **Implement Services with Direct DbContext Usage**
   ```csharp
   // Services/IngestionService.cs
   public class IngestionService : IIngestionService
   {
       private readonly HartonomousDbContext _context; // ← Direct DbContext injection
       private readonly IFileTypeDetector _fileTypeDetector;
       private readonly IEnumerable<IAtomizer<byte[]>> _atomizers;
       private readonly IProvenanceService _provenanceService;
       private readonly ILogger<IngestionService> _logger;
       
       public IngestionService(
           HartonomousDbContext context, // ← No repository layer
           IFileTypeDetector fileTypeDetector,
           IEnumerable<IAtomizer<byte[]>> atomizers,
           IProvenanceService provenanceService,
           ILogger<IngestionService> logger)
       {
           _context = context;
           _fileTypeDetector = fileTypeDetector;
           _atomizers = atomizers;
           _provenanceService = provenanceService;
           _logger = logger;
       }
       
       public async Task<IngestionResult> IngestFileAsync(byte[] fileData, string fileName, int tenantId)
       {
           // Validation - throw exceptions (not Result<T>)
           if (fileData == null || fileData.Length == 0)
               throw new ArgumentException("File cannot be empty", nameof(fileData));
           
           // Detect file type
           var fileType = _fileTypeDetector.Detect(fileData);
           if (fileType == FileType.Unknown)
               throw new InvalidFileFormatException($"Unsupported file format: {fileName}");
           
           _logger.LogInformation("Ingesting file: {FileName}, Type: {FileType}, Tenant: {TenantId}", 
               fileName, fileType, tenantId);
           
           // Atomize using appropriate atomizers
           var atoms = new List<Atom>();
           foreach (var atomizer in _atomizers.Where(a => a.Supports(fileType)))
           {
               try
               {
                   var atomizedData = await atomizer.AtomizeAsync(fileData);
                   atoms.AddRange(atomizedData);
               }
               catch (Exception ex)
               {
                   throw new AtomizationFailedException($"Failed to atomize {fileName}", ex);
               }
           }
           
           // Save to database (DbContext = Repository + Unit of Work)
           _context.TensorAtoms.AddRange(atoms);
           await _context.SaveChangesAsync(); // ← Unit of Work
           
           // Track provenance (separate concern)
           await _provenanceService.TrackIngestionAsync(fileName, atoms, tenantId);
           
           _logger.LogInformation("Ingestion complete: {AtomCount} atoms created", atoms.Count);
           
           return new IngestionResult 
           { 
               Success = true, 
               ItemsProcessed = atoms.Count,
               Message = $"Successfully ingested {atoms.Count} atoms"
           };
       }
   }
   ```

3. **Refactor Controllers to Thin HTTP Layer**
   ```csharp
   // Controllers/DataIngestionController.cs
   [ApiController]
   [Route("api/[controller]")]
   [Authorize(Policy = "DataIngestion")]
   [RequireTenant]
   public class DataIngestionController : ControllerBase
   {
       private readonly IIngestionService _ingestionService; // ← Service interface
       private readonly ILogger<DataIngestionController> _logger;
       
       public DataIngestionController(
           IIngestionService ingestionService,
           ILogger<DataIngestionController> logger)
       {
           _ingestionService = ingestionService;
           _logger = logger;
       }
       
       /// <summary>
       /// Ingest a file and atomize its contents.
       /// </summary>
       /// <param name="file">File to ingest (max 1GB)</param>
       /// <returns>Ingestion job details</returns>
       /// <response code="200">Ingestion completed successfully</response>
       /// <response code="400">Invalid file or parameters</response>
       /// <response code="401">Authentication required</response>
       [HttpPost("file")]
       [ProducesResponseType(typeof(IngestionResponse), 200)]
       [ProducesResponseType(typeof(ProblemDetails), 400)]
       public async Task<ActionResult<IngestionResponse>> IngestFile(IFormFile file)
       {
           var tenantId = this.GetTenantId(); // Extension method
           var fileData = await file.GetBytesAsync();
           
           // Let exceptions bubble up to global handler (no try/catch)
           var result = await _ingestionService.IngestFileAsync(fileData, file.FileName, tenantId);
           
           return Ok(new IngestionResponse
           {
               Success = result.Success,
               ItemsProcessed = result.ItemsProcessed,
               Message = result.Message
           });
       }
   }
   ```

**Acceptance Criteria**:
- ✅ Service interfaces created for business logic
- ✅ Services inject DbContext directly (no repository layer)
- ✅ Services throw exceptions (not Result<T>)
- ✅ Controllers are thin (5-10 lines per action)
- ✅ Controllers delegate to services
- ✅ No try/catch in controllers (global handler)

---

### 2.2 Register Services with Correct Lifetimes

**Objective**: Register all services with proper DI lifetimes

**Tasks**:

```csharp
// Program.cs
builder.Services.AddScoped<IIngestionService, IngestionService>();
builder.Services.AddScoped<IProvenanceService, Neo4jProvenanceService>();
builder.Services.AddScoped<IReasoningService, ReasoningService>();
builder.Services.AddScoped<IAtomQueryService, AtomQueryService>();
```

**Acceptance Criteria**:
- ✅ All business services registered as Scoped
- ✅ Services share DbContext per request
- ✅ No captive dependencies

---

## Phase 3: API Request/Response Models Refactoring (Week 2)

### 3.1 Create DTOs Folder Structure

**Objective**: Organize API request/response DTOs into logical namespaces (separate from EF Core entities)

**Note**: EF Core entities in `Hartonomous.Data.Entities` are scaffolded from SQL Server 2025 schema. These DTOs are for API contracts only.

**Structure**:
```text
src/Hartonomous.Api/DTOs/
├─ Common/
│  ├─ PaginatedResponse.cs
│  ├─ ErrorResponse.cs
│  └─ SuccessResponse.cs
├─ Ingestion/
│  ├─ FileUploadRequest.cs
│  ├─ UrlIngestionRequest.cs
│  ├─ DatabaseIngestionRequest.cs
│  ├─ GitIngestionRequest.cs
│  └─ IngestionJobResponse.cs
├─ Atoms/
│  ├─ AtomDetailResponse.cs
│  ├─ AtomListResponse.cs
│  ├─ AtomLineageResponse.cs
│  ├─ AtomCompositionResponse.cs
│  └─ AtomSearchRequest.cs
├─ Provenance/
│  ├─ LineageRequest.cs
│  ├─ LineageResponse.cs
│  ├─ SessionPathResponse.cs
│  └─ ErrorClusterResponse.cs
├─ Reasoning/
│  ├─ ChainOfThoughtRequest.cs
│  ├─ ChainOfThoughtResponse.cs
│  ├─ TreeOfThoughtRequest.cs
│  └─ TreeOfThoughtResponse.cs
├─ Streaming/
│  ├─ StreamSessionRequest.cs
│  ├─ TelemetryBatchRequest.cs
│  ├─ VideoFrameRequest.cs
│  └─ AudioBufferRequest.cs
└─ Audit/
   ├─ AuditLogRequest.cs
   ├─ AuditLogResponse.cs
   └─ ComplianceReportResponse.cs
```

**Tasks**:
1. Move all API request/response DTOs out of controller files
2. Update namespaces: `Hartonomous.Api.DTOs.{Area}`
3. Update controller usings
4. Keep EF Core entities in `Hartonomous.Data.Entities` (DB-first scaffolded)
5. Use AutoMapper or manual mapping between entities and DTOs

**Acceptance Criteria**:
- ✅ No API DTOs in controller files
- ✅ Clean separation: EF entities (data layer) vs DTOs (API layer)
- ✅ Clean namespace organization
- ✅ All controllers compile

---

### 3.2 Add Validation Attributes

**Objective**: Input validation at DTO level with Problem Details format

**Tasks**:

1. **Add DataAnnotations**
   ```csharp
   public class FileUploadRequest
   {
       [Required]
       [Range(1, int.MaxValue)]
       public int TenantId { get; set; }
       
       [Required]
       [MaxLength(255)]
       public string FileName { get; set; }
       
       [Range(1, 1_000_000_000)] // 1GB max
       public long FileSizeBytes { get; set; }
   }
   ```

2. **Configure Model Validation with Problem Details**
   ```csharp
   builder.Services.AddControllers()
       .ConfigureApiBehaviorOptions(options =>
       {
           // Automatic 400 Bad Request with Problem Details for validation errors
           options.InvalidModelStateResponseFactory = context =>
           {
               var problemDetails = new ValidationProblemDetails(context.ModelState)
               {
                   Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                   Title = "One or more validation errors occurred.",
                   Status = StatusCodes.Status400BadRequest,
                   Detail = "See the errors property for details.",
                   Instance = context.HttpContext.Request.Path
               };
               
               problemDetails.Extensions.Add("traceId", context.HttpContext.TraceIdentifier);
               
               return new BadRequestObjectResult(problemDetails)
               {
                   ContentTypes = { "application/problem+json" }
               };
           };
       });
   ```

**Response Format** (Automatic):
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "detail": "See the errors property for details.",
  "instance": "/api/ingestion/file",
  "traceId": "00-a1b2c3d4e5f6-01-01",
  "errors": {
    "FileName": ["The FileName field is required."],
    "FileSizeBytes": ["The field FileSizeBytes must be between 1 and 1000000000."]
  }
}
```

**Acceptance Criteria**:
- ✅ Invalid requests return 400 with Problem Details format
- ✅ All DTOs have validation attributes
- ✅ Consistent RFC 7807 error format
- ✅ Trace IDs included for debugging

---

### 3.3 Standardized Response Models

**Objective**: Consistent API response structure with Problem Details for errors

**Common Patterns**:

1. **Success Response** (Direct data, no wrapper)
   ```csharp
   // Microsoft pattern: Return data directly, not wrapped
   [HttpGet("{id}")]
   public async Task<ActionResult<AtomDetailResponse>> GetAtom(string id)
   {
       var atom = await _service.GetAtomAsync(id, this.GetTenantId());
       return Ok(atom); // ← Direct data, no ApiResponse<T> wrapper
   }
   ```

2. **Paginated Response** (Standard pattern)
   ```csharp
   public class PaginatedResponse<T>
   {
       public IEnumerable<T> Items { get; set; }
       public int Page { get; set; }
       public int PageSize { get; set; }
       public int TotalItems { get; set; }
       public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
       public bool HasNextPage => Page < TotalPages;
       public bool HasPreviousPage => Page > 1;
   }
   
   // Usage with extension method
   [HttpGet]
   public async Task<ActionResult<PaginatedResponse<AtomDto>>> GetAtoms(
       [FromQuery] int page = 1,
       [FromQuery] int pageSize = 50)
   {
       var tenantId = this.GetTenantId();
       var (items, total) = await _service.GetAtomsAsync(tenantId, page, pageSize);
       
       return this.Paginated(items, total, page, pageSize); // ← Extension method
   }
   ```

3. **Error Response** (Problem Details - Automatic via Middleware)
   ```csharp
   // NO custom ErrorResponse class needed!
   // Middleware automatically converts exceptions to Problem Details:
   
   // Service throws exception
   throw new InvalidFileFormatException("Unsupported file format");
   
   // Middleware converts to:
   {
     "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
     "title": "Bad Request",
     "status": 400,
     "detail": "Unsupported file format",
     "traceId": "00-a1b2c3d4e5f6-01-01",
     "timestamp": "2025-11-19T12:34:56Z"
   }
   ```

**Acceptance Criteria**:
- ✅ Success responses return data directly (no wrapper)
- ✅ Errors use Problem Details (RFC 7807) automatically
- ✅ Pagination uses standard PaginatedResponse<T>
- ✅ Trace IDs included in all responses
- ✅ No manual error response formatting needed

---

## Phase 4: Master/Detail Atom Navigation (Week 2-3)

### 4.1 Atom Detail Endpoint

**Objective**: Retrieve comprehensive atom information with related atoms

**Endpoint**: `GET /api/atoms/{atomHash}`

**Response Structure**:
```csharp
public class AtomDetailResponse
{
    // Core Atom Data
    public string AtomHash { get; set; }
    public byte[] AtomicValue { get; set; }
    public string ContentType { get; set; }
    public string Modality { get; set; }
    public string Subtype { get; set; }
    public string CanonicalText { get; set; }
    public string Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TenantId { get; set; }
    
    // Composition (Parent/Children)
    public AtomSummary Parent { get; set; } // if this atom is part of another
    public List<AtomSummary> Components { get; set; } // child atoms
    
    // Provenance
    public SourceInfo Source { get; set; }
    public List<LineagePath> Lineage { get; set; } // derivation history
    public List<AtomInfluence> Influences { get; set; } // atoms that influenced this
    
    // Usage
    public AtomUsageStats Usage { get; set; }
    public List<InferenceReference> Inferences { get; set; } // inferences using this atom
    
    // Spatial/Embedding
    public double[] Embedding { get; set; } // 1536D vector
    public SpatialPosition Position { get; set; } // 3D projected position
    public List<AtomSummary> SpatiallyNear { get; set; } // nearby atoms in embedding space
    
    // Relations (master/detail navigation)
    public NavigationLinks Links { get; set; }
}

public class NavigationLinks
{
    public string Self { get; set; } // /api/atoms/{hash}
    public string Parent { get; set; } // /api/atoms/{parentHash}
    public string Components { get; set; } // /api/atoms/{hash}/components
    public string Lineage { get; set; } // /api/atoms/{hash}/lineage
    public string Inferences { get; set; } // /api/atoms/{hash}/inferences
    public string SimilarAtoms { get; set; } // /api/atoms/{hash}/similar
}
```

**Stored Procedure** (SQL Server 2025):
```sql
CREATE PROCEDURE dbo.sp_GetAtomDetail
    @AtomHash BINARY(32),
    @TenantId INT,
    @IncludeComponents BIT = 1,
    @IncludeLineage BIT = 1,
    @IncludeInferences BIT = 0,
    @IncludeSpatiallyNear BIT = 0,
    @MaxComponents INT = 50,
    @MaxLineageDepth INT = 5
AS
BEGIN
    -- Main atom data
    SELECT * FROM TensorAtoms WHERE ContentHash = @AtomHash AND TenantId = @TenantId;
    
    -- Parent atom (if this is a component)
    IF @IncludeComponents = 1
    BEGIN
        SELECT TOP 1 a.*
        FROM TensorAtoms a
        INNER JOIN AtomCompositions c ON a.ContentHash = c.ParentAtomHash
        WHERE c.ComponentAtomHash = @AtomHash AND a.TenantId = @TenantId;
    END
    
    -- Components (child atoms)
    IF @IncludeComponents = 1
    BEGIN
        SELECT TOP (@MaxComponents) a.*, c.SequenceIndex, c.Position
        FROM TensorAtoms a
        INNER JOIN AtomCompositions c ON a.ContentHash = c.ComponentAtomHash
        WHERE c.ParentAtomHash = @AtomHash AND a.TenantId = @TenantId
        ORDER BY c.SequenceIndex;
    END
    
    -- Lineage (provenance chain)
    IF @IncludeLineage = 1
    BEGIN
        EXEC dbo.sp_GetAtomLineage @AtomHash, @TenantId, @MaxLineageDepth;
    END
    
    -- Recent inferences using this atom
    IF @IncludeInferences = 1
    BEGIN
        SELECT TOP 10 i.InferenceId, i.Timestamp, i.ResultHash
        FROM Inferences i
        WHERE i.TenantId = @TenantId
          AND EXISTS (
              SELECT 1 FROM InferenceAtoms ia
              WHERE ia.InferenceId = i.InferenceId AND ia.AtomHash = @AtomHash
          )
        ORDER BY i.Timestamp DESC;
    END
    
    -- Spatially near atoms (in embedding space)
    IF @IncludeSpatiallyNear = 1
    BEGIN
        EXEC dbo.sp_FindSpatiallyNearAtoms @AtomHash, @TenantId, @TopK = 10;
    END
END
```

**Acceptance Criteria**:
- ✅ Returns complete atom details
- ✅ Navigation links support drilling down
- ✅ Efficient query (indexed lookups)
- ✅ Respects tenant isolation

---

### 4.2 Atom Components Endpoint (Collection Navigation)

**Endpoint**: `GET /api/atoms/{atomHash}/components?page=1&pageSize=50`

**Response**:
```csharp
public class AtomComponentsResponse
{
    public string ParentAtomHash { get; set; }
    public string ParentCanonicalText { get; set; }
    public PaginatedResponse<ComponentAtom> Components { get; set; }
}

public class ComponentAtom
{
    public string AtomHash { get; set; }
    public string CanonicalText { get; set; }
    public string Modality { get; set; }
    public int SequenceIndex { get; set; }
    public SpatialPosition Position { get; set; }
    public string DetailLink { get; set; } // /api/atoms/{hash}
}
```

**Use Case**: Click on a document atom → see all page atoms, click page → see paragraph atoms

**Acceptance Criteria**:
- ✅ Supports pagination
- ✅ Ordered by sequence index
- ✅ Links to component details

---

### 4.3 Atom Lineage Endpoint (Provenance Chain)

**Endpoint**: `GET /api/atoms/{atomHash}/lineage?maxDepth=5`

**Response**:
```csharp
public class AtomLineageResponse
{
    public string AtomHash { get; set; }
    public List<LineageNode> Ancestors { get; set; } // who created this
    public List<LineageNode> Descendants { get; set; } // what this created
}

public class LineageNode
{
    public string AtomHash { get; set; }
    public string CanonicalText { get; set; }
    public string RelationType { get; set; } // "derived_from", "composed_of", "influenced_by"
    public int Depth { get; set; }
    public DateTime CreatedAt { get; set; }
    public string DetailLink { get; set; }
}
```

**Use Case**: Track how an atom was derived (e.g., "This embedding came from this image, which came from this video frame")

**Acceptance Criteria**:
- ✅ Traverses provenance graph
- ✅ Limits depth to prevent infinite loops
- ✅ Shows relationship types

---

### 4.4 Atom Search/Browse Endpoints

**Endpoints**:

1. **Search**: `POST /api/atoms/search`
   ```csharp
   public class AtomSearchRequest
   {
       public string Query { get; set; } // text or semantic query
       public List<string> Modalities { get; set; } // filter by text, image, audio
       public List<string> Subtypes { get; set; }
       public DateRange CreatedAt { get; set; }
       public int Page { get; set; } = 1;
       public int PageSize { get; set; } = 50;
   }
   ```

2. **Browse by Modality**: `GET /api/atoms/by-modality/{modality}?page=1&pageSize=50`
   - List all text atoms, image atoms, etc.

3. **Browse by Source**: `GET /api/atoms/by-source/{sourceId}?page=1&pageSize=50`
   - All atoms from a particular ingestion job

4. **Similar Atoms**: `GET /api/atoms/{atomHash}/similar?topK=20`
   - Semantic similarity search (embedding-based)

**Acceptance Criteria**:
- ✅ Full-text search on CanonicalText
- ✅ Semantic search using embeddings
- ✅ Faceted filtering (modality, subtype)
- ✅ Efficient pagination

---

### 4.5 Atom Inference Usage

**Endpoint**: `GET /api/atoms/{atomHash}/inferences?page=1&pageSize=20`

**Response**:
```csharp
public class AtomInferenceUsageResponse
{
    public string AtomHash { get; set; }
    public PaginatedResponse<InferenceReference> Inferences { get; set; }
}

public class InferenceReference
{
    public Guid InferenceId { get; set; }
    public DateTime Timestamp { get; set; }
    public string ResultAtomHash { get; set; } // what was generated
    public string ResultCanonicalText { get; set; }
    public string Role { get; set; } // "input", "context", "output"
    public string DetailLink { get; set; } // /api/inferences/{id}
}
```

**Use Case**: "Show me all inferences that used this atom"

**Acceptance Criteria**:
- ✅ Shows all inferences using the atom
- ✅ Indicates role (input vs context)
- ✅ Links to inference details

---

## Phase 5: Worker Services (Week 3-4)

### 5.1 CES Consumer Worker (CRITICAL)

**Project**: `Hartonomous.Workers.CesConsumer`

**Purpose**: Atomize data from Service Broker queues

**Pattern**: Use IServiceScopeFactory to create scopes for scoped services (DbContext, business services)

**Queue Polling** (Correct Pattern):
```csharp
public class CesConsumerWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory; // ← Inject scope factory
    private readonly ILogger<CesConsumerWorker> _logger;
    private readonly string _connectionString;
    
    public CesConsumerWorker(
        IServiceScopeFactory scopeFactory, // ← NOT DbContext directly!
        IConfiguration configuration,
        ILogger<CesConsumerWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _connectionString = configuration.GetConnectionString("HartonomousDb");
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CES Consumer Worker starting");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync(stoppingToken);
                
                using var transaction = connection.BeginTransaction();
                using var command = new SqlCommand(@"
                    WAITFOR (
                        RECEIVE TOP(1) conversation_handle, message_body
                        FROM dbo.IngestionQueue
                    ), TIMEOUT 5000", connection, transaction);
                
                using var reader = await command.ExecuteReaderAsync(stoppingToken);
                if (await reader.ReadAsync(stoppingToken))
                {
                    var messageBody = reader.GetString(1);
                    var request = JsonSerializer.Deserialize<IngestionMessage>(messageBody);
                    
                    // Create scope for scoped services
                    using var scope = _scopeFactory.CreateScope(); // ← CRITICAL
                    await ProcessIngestionAsync(scope.ServiceProvider, request, stoppingToken);
                }
                
                transaction.Commit();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing CES message");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
    
    private async Task ProcessIngestionAsync(
        IServiceProvider serviceProvider, // ← Resolve from scope
        IngestionMessage request,
        CancellationToken stoppingToken)
    {
        // Resolve scoped services from scope, NOT constructor
        var ingestionService = serviceProvider.GetRequiredService<IIngestionService>();
        var telemetry = serviceProvider.GetRequiredService<TelemetryClient>();
        
        using var operation = telemetry.StartOperation<RequestTelemetry>("CES.ProcessIngestion");
        
        try
        {
            var result = await ingestionService.IngestFileAsync(
                request.FileData,
                request.FileName,
                request.TenantId);
            
            telemetry.TrackMetric("CES.ItemsProcessed", result.ItemsProcessed);
            operation.Telemetry.Success = true;
        }
        catch (Exception ex)
        {
            operation.Telemetry.Success = false;
            throw;
        }
    }
}
```

**Tasks**:
1. Create worker project
2. Implement Service Broker RECEIVE loop
3. Deserialize messages
4. Call atomizers
5. Bulk insert atoms
6. Handle errors and retries

**Acceptance Criteria**:
- ✅ Polls IngestionQueue continuously
- ✅ Uses IServiceScopeFactory to create scopes (NOT DbContext in constructor)
- ✅ Resolves scoped services from scope.ServiceProvider
- ✅ Atomizes data in background
- ✅ Tracks telemetry to Application Insights
- ✅ Handles failures gracefully with retry
- ✅ Logs progress with correlation

---

### 5.2 Neo4j Sync Worker

**Project**: `Hartonomous.Workers.Neo4jSync`

**Purpose**: Sync provenance graph to Neo4j

**Pattern**: Use IServiceScopeFactory for scoped provenance service

**Queue Polling** (Correct Pattern):
```csharp
public class Neo4jSyncWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory; // ← Scope factory
    private readonly ILogger<Neo4jSyncWorker> _logger;
    private readonly string _connectionString;
    
    public Neo4jSyncWorker(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<Neo4jSyncWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _connectionString = configuration.GetConnectionString("HartonomousDb");
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Neo4j Sync Worker starting");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync(stoppingToken);
                
                using var transaction = connection.BeginTransaction();
                using var command = new SqlCommand(@"
                    WAITFOR (
                        RECEIVE TOP(1) message_body
                        FROM dbo.Neo4jSyncQueue
                    ), TIMEOUT 5000", connection, transaction);
                
                using var reader = await command.ExecuteReaderAsync(stoppingToken);
                if (await reader.ReadAsync(stoppingToken))
                {
                    var messageBody = reader.GetString(0);
                    var syncMessage = JsonSerializer.Deserialize<Neo4jSyncMessage>(messageBody);
                    
                    // Create scope for scoped services
                    using var scope = _scopeFactory.CreateScope();
                    await SyncToNeo4jAsync(scope.ServiceProvider, syncMessage, stoppingToken);
                }
                
                transaction.Commit();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing to Neo4j");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
    
    private async Task SyncToNeo4jAsync(
        IServiceProvider serviceProvider,
        Neo4jSyncMessage message,
        CancellationToken stoppingToken)
    {
        var provenanceService = serviceProvider.GetRequiredService<IProvenanceService>();
        var telemetry = serviceProvider.GetRequiredService<TelemetryClient>();
        
        // Track to Neo4j
        using var operation = telemetry.StartOperation<DependencyTelemetry>("Neo4j.SyncAtoms");
        
        try
        {
            await provenanceService.TrackIngestionAsync(
                message.Source,
                message.Atoms,
                message.TenantId);
            
            operation.Telemetry.Success = true;
        }
        catch (Exception ex)
        {
            operation.Telemetry.Success = false;
            throw;
        }
    }
}
```

**Sync Operations**:
- Create atom nodes
- Create derivation relationships
- Create composition relationships
- Update node properties

**Acceptance Criteria**:
- ✅ Uses IServiceScopeFactory pattern for scoped services
- ✅ Syncs new atoms to Neo4j
- ✅ Creates relationships (derivation, composition)
- ✅ Tracks Neo4j operations to Application Insights
- ✅ Idempotent (handles duplicates with MERGE)
- ✅ Recovers from Neo4j downtime with retry

---

### 5.3 Embedding Generator Worker (Optional)

**Project**: `Hartonomous.Workers.EmbeddingGenerator`

**Purpose**: Generate embeddings for atoms

**Process**:
1. Poll atoms without embeddings
2. Call embedding model (ONNX Runtime or Azure OpenAI)
3. Update TensorAtoms.Embedding
4. Trigger spatial projection

**Acceptance Criteria**:
- ✅ Generates embeddings in batches
- ✅ Configurable embedding model
- ✅ GPU acceleration (if available)

---

### 5.4 Deployment Configuration

**SystemD Service Files** (Linux):

```ini
# /etc/systemd/system/hartonomous-ces-consumer.service
[Unit]
Description=Hartonomous CES Consumer Worker
After=network.target

[Service]
Type=notify
User=hartonomous
WorkingDirectory=/srv/www/hartonomous/ces-consumer
ExecStart=/usr/bin/dotnet Hartonomous.Workers.CesConsumer.dll
Restart=always
RestartSec=10
Environment=DOTNET_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

**Acceptance Criteria**:
- ✅ Workers run as SystemD services
- ✅ Auto-restart on failure
- ✅ Managed identity authentication

---

## Phase 6: Production Readiness (Week 4)

### 6.1 Application Insights & Telemetry (Already in Phase 0)

**Status**: ✅ Configured in Phase 0.1 (Middleware Pipeline)

**What's Already Configured**:
- Application Insights telemetry
- OpenTelemetry with Azure Monitor
- Correlation ID tracking
- Request/response logging middleware
- Automatic exception tracking

**Additional Telemetry Enhancements**:

1. **Custom Metrics in Services**
   ```csharp
   public class IngestionService : IIngestionService
   {
       private readonly TelemetryClient _telemetry;
       private readonly ILogger<IngestionService> _logger;
       
       public async Task<IngestionResult> IngestFileAsync(byte[] fileData, string fileName, int tenantId)
       {
           using var operation = _telemetry.StartOperation<RequestTelemetry>("IngestFile");
           operation.Telemetry.Properties["FileName"] = fileName;
           operation.Telemetry.Properties["TenantId"] = tenantId.ToString();
           
           try
           {
               var atoms = await ProcessFileAsync(fileData, fileType);
               
               // Custom metrics
               _telemetry.TrackMetric("Atoms.Ingested", atoms.Count);
               _telemetry.TrackEvent("FileIngestionCompleted", new Dictionary<string, string>
               {
                   ["FileName"] = fileName,
                   ["FileType"] = fileType.ToString(),
                   ["AtomCount"] = atoms.Count.ToString()
               });
               
               operation.Telemetry.Success = true;
               return new IngestionResult { Success = true, ItemsProcessed = atoms.Count };
           }
           catch (Exception ex)
           {
               operation.Telemetry.Success = false;
               _logger.LogError(ex, "Ingestion failed for {FileName}", fileName);
               throw; // Let global handler convert to Problem Details
           }
       }
   }
   ```

2. **Dependency Tracking (Automatic)**
   - ✅ HTTP calls tracked automatically
   - ✅ SQL Server queries tracked automatically
   - ✅ EF Core operations tracked automatically
   - ⚠️ Neo4j requires custom tracking:
   
   ```csharp
   public class Neo4jProvenanceService : IProvenanceService
   {
       private readonly TelemetryClient _telemetry;
       
       public async Task TrackIngestionAsync(string source, IEnumerable<Atom> atoms, int tenantId)
       {
           var dependency = new DependencyTelemetry
           {
               Name = "Neo4j.CreateProvenanceGraph",
               Type = "Neo4j",
               Target = _neo4jConfig.Endpoint
           };
           
           var sw = Stopwatch.StartNew();
           try
           {
               // Execute Neo4j operations
               await CreateProvenanceNodesAsync(atoms);
               
               dependency.Success = true;
           }
           catch (Exception ex)
           {
               dependency.Success = false;
               throw;
           }
           finally
           {
               dependency.Duration = sw.Elapsed;
               _telemetry.TrackDependency(dependency);
           }
       }
   }
   ```

**Acceptance Criteria**:
- ✅ All HTTP requests logged to Application Insights
- ✅ SQL queries tracked as dependencies
- ✅ Neo4j operations tracked as dependencies
- ✅ Custom metrics for business events
- ✅ Distributed tracing with correlation IDs
- ✅ Exception telemetry automatic

---

### 6.2 Switch to Production Services

**Task**: Update `Program.cs` to use real services

```csharp
// Remove mock services
// builder.Services.AddHartonomousMockServices();

// Add production infrastructure
builder.Services.AddHartonomousInfrastructure(builder.Configuration);

// Verify health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<HartonomousDbContext>()
    .AddCheck<Neo4jHealthCheck>("neo4j")
    .AddAzureKeyVault(new Uri(builder.Configuration["KeyVaultUri"]), new DefaultAzureCredential());
```

**Verification**:
- Test all endpoints with real SQL Server
- Test provenance with Neo4j
- Validate performance under load
- Run health checks

**Acceptance Criteria**:
- ✅ All endpoints use production implementations
- ✅ Mock services removed from Program.cs
- ✅ Health checks pass (/health endpoint)
- ✅ No mock data in production environment

---

### 6.3 API Versioning

**Objective**: Support v1, v2 API versions

**Tasks**:

1. **Add Versioning**
   ```csharp
   builder.Services.AddApiVersioning(options =>
   {
       options.DefaultApiVersion = new ApiVersion(1, 0);
       options.AssumeDefaultVersionWhenUnspecified = true;
       options.ReportApiVersions = true;
   });
   ```

2. **Update Controllers**
   ```csharp
   [ApiController]
   [Route("api/v{version:apiVersion}/ingestion")]
   [ApiVersion("1.0")]
   public class DataIngestionController : ControllerBase
   ```

**Acceptance Criteria**:
- ✅ Supports /api/v1/ and /api/v2/
- ✅ Version header support
- ✅ Deprecated versions return warnings

---

### 6.4 Rate Limiting

**Objective**: Prevent abuse and enforce quotas

**Tasks**:

1. **Add Rate Limiting Middleware**
   ```csharp
   builder.Services.AddRateLimiter(options =>
   {
       options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
       {
           var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
           
           return RateLimitPartition.GetFixedWindowLimiter(userId, _ => new FixedWindowRateLimiterOptions
           {
               PermitLimit = 100,
               Window = TimeSpan.FromMinutes(1)
           });
       });
   });
   ```

2. **Per-Endpoint Limits**
   ```csharp
   [HttpPost("file")]
   [EnableRateLimiting("file-upload")]
   public async Task<IActionResult> IngestFile(...)
   ```

**Acceptance Criteria**:
- ✅ Rate limits enforced per user
- ✅ 429 Too Many Requests returned
- ✅ Retry-After header included

---

### 6.5 CORS Configuration

**Objective**: Support web clients from specific origins

**Tasks**:

1. **Configure CORS**
   ```csharp
   builder.Services.AddCors(options =>
   {
       options.AddPolicy("AllowWebApp", policy =>
       {
           policy.WithOrigins("https://hartonomous.com", "https://app.hartonomous.com")
                 .AllowAnyMethod()
                 .AllowAnyHeader()
                 .AllowCredentials();
       });
   });
   
   app.UseCors("AllowWebApp");
   ```

**Acceptance Criteria**:
- ✅ Web app can call API
- ✅ Other origins blocked
- ✅ Preflight requests handled

---

## Phase 7: Testing & Documentation (Week 5)

### 7.1 Middleware Testing

**Objective**: Test middleware pipeline with TestServer

**Test Patterns**:

1. **Test Problem Details Conversion**
   ```csharp
   [Fact]
   public async Task ExceptionHandler_ConvertsExceptionToProblemDetails()
   {
       // Arrange
       var factory = new WebApplicationFactory<Program>();
       var client = factory.CreateClient();
       
       // Act - trigger endpoint that throws
       var response = await client.GetAsync("/api/test/throw-exception");
       
       // Assert
       Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
       Assert.Equal("application/problem+json", response.Content.Headers.ContentType.MediaType);
       
       var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
       Assert.NotNull(problemDetails);
       Assert.Equal(500, problemDetails.Status);
       Assert.Contains("traceId", problemDetails.Extensions.Keys);
   }
   ```

2. **Test Correlation ID Middleware**
   ```csharp
   [Fact]
   public async Task CorrelationIdMiddleware_AddsHeaderToResponse()
   {
       // Arrange
       var factory = new WebApplicationFactory<Program>();
       var client = factory.CreateClient();
       
       // Act
       var response = await client.GetAsync("/api/health");
       
       // Assert
       Assert.True(response.Headers.Contains("X-Correlation-ID"));
       var correlationId = response.Headers.GetValues("X-Correlation-ID").First();
       Assert.NotEmpty(correlationId);
   }
   ```

3. **Test Request Logging Middleware**
   ```csharp
   [Fact]
   public async Task RequestLoggingMiddleware_LogsRequestAndResponse()
   {
       // Arrange
       var loggerProvider = new FakeLoggerProvider();
       var factory = new WebApplicationFactory<Program>()
           .WithWebHostBuilder(builder =>
           {
               builder.ConfigureLogging(logging =>
               {
                   logging.AddProvider(loggerProvider);
               });
           });
       var client = factory.CreateClient();
       
       // Act
       await client.GetAsync("/api/health");
       
       // Assert
       var logs = loggerProvider.GetLogs();
       Assert.Contains(logs, log => log.Contains("Request: GET /api/health"));
       Assert.Contains(logs, log => log.Contains("Response: 200"));
   }
   ```

**Acceptance Criteria**:
- ✅ Exception handler converts to Problem Details
- ✅ Correlation IDs present in all responses
- ✅ Request/response logging works
- ✅ Middleware pipeline order validated

---

### 7.2 Service Layer Testing (No Repository Mocks)

**Objective**: Test services with in-memory DbContext (Microsoft pattern)

**Pattern**: Use in-memory database, NOT repository mocks

**Test Example**:
```csharp
public class IngestionServiceTests : IDisposable
{
    private readonly HartonomousDbContext _context;
    private readonly IngestionService _service;
    
    public IngestionServiceTests()
    {
        // Use in-memory database (no repository mocks)
        var options = new DbContextOptionsBuilder<HartonomousDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new HartonomousDbContext(options);
        
        // Service uses DbContext directly
        _service = new IngestionService(
            _context, // ← Direct DbContext injection
            Mock.Of<IFileTypeDetector>(),
            new[] { Mock.Of<IAtomizer<byte[]>>() },
            Mock.Of<IProvenanceService>(),
            Mock.Of<ILogger<IngestionService>>());
    }
    
    [Fact]
    public async Task IngestFileAsync_ThrowsException_ForEmptyFile()
    {
        // Arrange
        var emptyFile = Array.Empty<byte>();
        
        // Act & Assert - exceptions, not Result<T>
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.IngestFileAsync(emptyFile, "test.txt", tenantId: 1));
    }
    
    [Fact]
    public async Task IngestFileAsync_SavesAtomsToDatabase()
    {
        // Arrange
        var fileData = Encoding.UTF8.GetBytes("test content");
        
        // Act
        var result = await _service.IngestFileAsync(fileData, "test.txt", tenantId: 1);
        
        // Assert - verify data in DbContext
        Assert.True(result.Success);
        Assert.True(_context.TensorAtoms.Any());
        var atom = _context.TensorAtoms.First();
        Assert.Equal(1, atom.TenantId);
    }
    
    public void Dispose()
    {
        _context.Dispose();
    }
}
```

**Acceptance Criteria**:
- ✅ Services tested with in-memory DbContext
- ✅ No repository mocking (DbContext IS the repository)
- ✅ Exception-based error flow tested
- ✅ Business logic validated

---

### 7.3 Integration Testing with TestServer

**Objective**: Test complete request pipeline (middleware + controllers + services + DbContext)

**Test Example**:
```csharp
public class DataIngestionIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    
    public DataIngestionIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace DbContext with in-memory
                services.RemoveAll<HartonomousDbContext>();
                services.AddDbContext<HartonomousDbContext>(options =>
                    options.UseInMemoryDatabase("IntegrationTests"));
                
                // Keep middleware, keep services, replace infrastructure
            });
        });
    }
    
    [Fact]
    public async Task POST_IngestFile_ReturnsBadRequest_ForInvalidFile()
    {
        // Arrange
        var client = _factory.CreateClient();
        var content = new MultipartFormDataContent();
        
        // Act
        var response = await client.PostAsync("/api/ingestion/file", content);
        
        // Assert - Problem Details format
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problemDetails);
        Assert.Equal(400, problemDetails.Status);
        Assert.Contains("traceId", problemDetails.Extensions.Keys);
    }
}
```

**Acceptance Criteria**:
- ✅ Complete pipeline tested (middleware → controller → service → DbContext)
- ✅ Problem Details responses validated
- ✅ Authentication/authorization tested
- ✅ Correlation IDs verified

---

## Phase 7 (Continued)

### 7.1 Integration Tests

**Objective**: Test all endpoints with real authentication

**Test Project**: `Hartonomous.Api.IntegrationTests`

**Sample Test**:
```csharp
[Fact]
public async Task IngestFile_WithValidToken_ReturnsSuccess()
{
    // Arrange
    var token = await GetValidTokenAsync();
    var file = CreateTestFile("test.txt", "Hello world");
    
    // Act
    var response = await _client.PostAsync("/api/ingestion/file", file, token);
    
    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var result = await response.Content.ReadAsAsync<IngestionJobResponse>();
    Assert.Equal("processing", result.Status);
}
```

**Acceptance Criteria**:
- ✅ Tests for all controllers
- ✅ Authentication tests
- ✅ Authorization tests
- ✅ Error handling tests

---

### 7.2 OpenAPI/Swagger Documentation

**Objective**: Comprehensive API documentation

**Tasks**:

1. **Enhance Swagger Configuration**
   ```csharp
   builder.Services.AddSwaggerGen(options =>
   {
       options.SwaggerDoc("v1", new OpenApiInfo
       {
           Title = "Hartonomous API",
           Version = "v1",
           Description = "Production atomization and inference API"
       });
       
       // Add XML comments
       var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
       var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
       options.IncludeXmlComments(xmlPath);
       
       // Add OAuth2 flow for Azure AD
       options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
       {
           Type = SecuritySchemeType.OAuth2,
           Flows = new OpenApiOAuthFlows
           {
               Implicit = new OpenApiOAuthFlow
               {
                   AuthorizationUrl = new Uri("https://login.microsoftonline.com/{tenant}/oauth2/v2.0/authorize"),
                   TokenUrl = new Uri("https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token"),
                   Scopes = new Dictionary<string, string>
                   {
                       { "api://hartonomous/access_as_user", "Access Hartonomous API" }
                   }
               }
           }
       });
   });
   ```

2. **XML Documentation on Endpoints**
   ```csharp
   /// <summary>
   /// Ingest a file and atomize its contents.
   /// </summary>
   /// <param name="file">File to ingest (max 1GB)</param>
   /// <param name="tenantId">Tenant ID for multi-tenant isolation</param>
   /// <returns>Ingestion job details</returns>
   /// <response code="200">Ingestion job started successfully</response>
   /// <response code="400">Invalid file or parameters</response>
   /// <response code="401">Authentication required</response>
   /// <response code="403">Insufficient permissions</response>
   [HttpPost("file")]
   [ProducesResponseType(typeof(IngestionJobResponse), 200)]
   [ProducesResponseType(typeof(ErrorResponse), 400)]
   ```

**Acceptance Criteria**:
- ✅ All endpoints documented
- ✅ Request/response examples
- ✅ Swagger UI includes OAuth2 login

---

## Implementation Checklist

### Week 1: Foundation, Security & Service Layer
- [ ] **Phase 0.1**: Configure middleware pipeline (Problem Details, App Insights, correlation)
- [ ] **Phase 0.2**: Create extension methods and filters (no base controller)
- [ ] **Phase 0.3**: Configure service lifetimes (Scoped DbContext, services)
- [ ] **Phase 1.1**: Add Entra ID authentication to API
- [ ] **Phase 1.1**: Configure authorization policies
- [ ] **Phase 1.1**: Add [Authorize] attributes to controllers
- [ ] **Phase 1.2**: Configure External ID for external users
- [ ] **Phase 1.3**: Implement API key authentication (optional)
- [ ] **Phase 1.4**: Load Azure App Configuration
- [ ] **Phase 1.5**: Load Azure Key Vault secrets
- [ ] **Phase 1.6**: Create appsettings hierarchy
- [ ] **Phase 2.1**: Extract service interfaces (IIngestionService, IProvenanceService, etc.)
- [ ] **Phase 2.1**: Implement services with direct DbContext usage
- [ ] **Phase 2.1**: Refactor controllers to thin HTTP layer
- [ ] **Phase 2.2**: Register services with correct lifetimes
- [ ] Test authentication flows
- [ ] Test exception handling → Problem Details conversion
- [ ] Verify correlation IDs in responses

### Week 2: API DTOs & Navigation
- [ ] **Phase 3.1**: Create DTOs folder structure (API contracts, separate from EF entities)
- [ ] **Phase 3.1**: Move all API DTOs out of controllers into namespaces
- [ ] **Phase 3.2**: Add validation attributes with Problem Details format
- [ ] **Phase 3.3**: Implement PaginatedResponse<T> (no wrappers for simple responses)
- [ ] **Phase 3.3**: Remove custom ErrorResponse (use Problem Details)
- [ ] Re-scaffold EF Core entities if database schema changes (`scaffold-entities.ps1`)
- [ ] **Phase 4.1**: Implement atom detail endpoint with navigation links
- [ ] **Phase 4.2**: Implement atom components endpoint with pagination
- [ ] **Phase 4.3**: Implement atom lineage endpoint (provenance chain)
- [ ] **Phase 4.4**: Implement atom search/browse endpoints
- [ ] **Phase 4.5**: Implement atom inference usage endpoint
- [ ] Create stored procedures for master/detail navigation
- [ ] Test master/detail navigation flows
- [ ] Verify all responses follow Microsoft patterns (direct data, Problem Details errors)

### Week 3: Worker Services
- [ ] Create CesConsumer worker project
- [ ] Implement Service Broker polling
- [ ] Create Neo4jSync worker project
- [ ] Implement Neo4j sync logic
- [ ] Create embedding generator worker (optional)
- [ ] Write SystemD service files
- [ ] Deploy workers to Arc servers
- [ ] Test background processing
- [ ] Monitor worker health

### Week 4: Production Readiness
- [ ] **Phase 6.1**: Add custom telemetry in services (TelemetryClient)
- [ ] **Phase 6.1**: Implement Neo4j dependency tracking
- [ ] **Phase 6.1**: Add custom metrics for business events
- [ ] **Phase 6.2**: Switch to production services (remove mocks)
- [ ] **Phase 6.2**: Verify health checks pass
- [ ] **Phase 6.3**: Add API versioning (v1, v2 support)
- [ ] **Phase 6.4**: Configure rate limiting per user
- [ ] **Phase 6.5**: Configure CORS for web app origins
- [ ] Test all endpoints with production SQL Server
- [ ] Test provenance with production Neo4j
- [ ] Load testing (validate performance)
- [ ] End-to-end smoke tests in production environment
- [ ] Verify distributed tracing works across services
- [ ] Validate Problem Details format in all error scenarios

### Week 5: Testing & Documentation
- [ ] Write integration tests
- [ ] Test authentication/authorization
- [ ] Enhance Swagger documentation
- [ ] Add XML comments
- [ ] Create API usage guide
- [ ] Performance benchmarks
- [ ] Security audit
- [ ] Production readiness review

---

## Success Criteria

### Architecture & Patterns
✅ Middleware pipeline configured (correct order)  
✅ Problem Details (RFC 7807) for all errors  
✅ No try/catch in controllers (global handler)  
✅ Services use direct DbContext (no repository layer)  
✅ Service interfaces for business logic only  
✅ Extension methods for shared functionality (no base controller)  
✅ Exceptions for error flow (not Result<T>)  
✅ Correct DI lifetimes (Scoped DbContext and services)  
✅ Composition over inheritance pattern followed  

### Security (.NET 10 API)
✅ All endpoints require authentication  
✅ Role-based authorization enforced  
✅ No secrets in source control  
✅ Azure AD integration working  
✅ External ID for public users  
✅ Tenant isolation enforced  

### Configuration
✅ App Configuration loaded  
✅ Key Vault secrets loaded  
✅ Local dev works without Azure  
✅ Production uses managed identity  

### Data Access
✅ Master/detail navigation works  
✅ Atom detail shows full context  
✅ Lineage traversal functional  
✅ Search and browse efficient  
✅ DbContext used directly (no repository wrapper)  

### Background Processing
✅ Workers process queues  
✅ Workers use IServiceScopeFactory pattern  
✅ Atomization happens automatically  
✅ Neo4j stays in sync  
✅ Embeddings generated  

### Observability
✅ Application Insights integrated  
✅ Correlation IDs in all responses  
✅ Request/response logging middleware  
✅ Distributed tracing works  
✅ Custom metrics captured  
✅ Neo4j dependency tracking  
✅ Health checks pass  

### Production Quality
✅ No mock services in production  
✅ API versioning implemented  
✅ Rate limiting enforced  
✅ CORS configured  
✅ Comprehensive tests pass  
✅ Swagger documentation complete  
✅ All responses follow Microsoft patterns  

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Azure AD misconfiguration | 🔴 Critical | Test in dev tenant first, use existing Admin app as template |
| Service Broker message loss | 🔴 Critical | Implement RECEIVE with transactions, retry logic |
| Captive dependencies (Singleton capturing Scoped) | 🔴 Critical | Enable scope validation in Development, use IServiceScopeFactory in workers |
| Middleware ordering errors | 🔴 Critical | Follow documented order, test auth/error handling flows |
| Worker service crashes | 🟡 Medium | SystemD auto-restart, health monitoring |
| Performance degradation | 🟡 Medium | Load testing, query optimization, caching |
| Breaking API changes | 🟡 Medium | API versioning, deprecation warnings |
| Over-engineering with unnecessary patterns | 🟡 Medium | Follow validation report, avoid repository layer, Result<T>, base controllers |

---

## Timeline

**Total Duration**: 5 weeks (with buffer)

```
Week 1: [████████████████████] Security & Config
Week 2: [████████████████████] DTOs & Navigation  
Week 3: [████████████████████] Worker Services
Week 4: [████████████████████] Production Readiness
Week 5: [████████████████████] Testing & Docs
```

**Go-Live Target**: End of Week 4  
**Hardening**: Week 5  

---

## Next Steps

1. **Review this plan** with stakeholders
2. **Prioritize phases** based on business needs
3. **Allocate development resources**
4. **Set up tracking** (GitHub Projects, Azure Boards)
5. **Begin Phase 1** (Security & Configuration)

---

**Document Version**: 1.0  
**Last Updated**: 2025-11-19  
**Status**: Draft for Review
