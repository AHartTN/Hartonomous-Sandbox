using System.Diagnostics;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Hartonomous.Api.Middleware;
using Hartonomous.Api.OpenApi;
using Hartonomous.Core.Configuration;
using Hartonomous.Infrastructure.Configurations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// ===== PHASE 4: Configure strongly-typed settings with validation =====
builder.Services.AddOptions<AzureOptions>()
    .Bind(builder.Configuration.GetSection(AzureOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<AzureAppConfigurationOptions>()
    .Bind(builder.Configuration.GetSection(AzureAppConfigurationOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<KeyVaultOptions>()
    .Bind(builder.Configuration.GetSection(KeyVaultOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<StorageOptions>()
    .Bind(builder.Configuration.GetSection(StorageOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<ApplicationInsightsOptions>()
    .Bind(builder.Configuration.GetSection(ApplicationInsightsOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<Neo4jOptions>()
    .Bind(builder.Configuration.GetSection(Neo4jOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<AuthenticationOptions>()
    .Bind(builder.Configuration.GetSection(AuthenticationOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<AzureAdOptions>()
    .Bind(builder.Configuration.GetSection(AzureAdOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<ExternalIdOptions>()
    .Bind(builder.Configuration.GetSection(ExternalIdOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<DatabaseOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ===== PHASE 1.4: Azure App Configuration (if enabled) =====
// NOTE: This runs before DI container is built, so we read from IConfiguration directly
var appConfigEnabled = builder.Configuration.GetValue<bool>("AzureAppConfiguration:Enabled", true);
var appConfigEndpoint = builder.Configuration["AzureAppConfiguration:Endpoint"];
var useManagedIdentity = builder.Configuration.GetValue<bool>("Azure:UseManagedIdentity", true);

if (appConfigEnabled && !string.IsNullOrEmpty(appConfigEndpoint))
{
    builder.Configuration.AddAzureAppConfiguration(options =>
    {
        options.Connect(new Uri(appConfigEndpoint), 
            useManagedIdentity 
                ? new DefaultAzureCredential() 
                : new DefaultAzureCredential(new DefaultAzureCredentialOptions { ExcludeManagedIdentityCredential = true }));
        
        // Load configuration with label filter (environment-specific)
        options.Select("*", builder.Environment.EnvironmentName);
        options.Select("*", null); // Load default values too
        
        // Configure refresh for dynamic configuration
        options.ConfigureRefresh(refresh =>
        {
            refresh.Register("Sentinel", refreshAll: true)
                   .SetRefreshInterval(TimeSpan.FromMinutes(5));
        });
    });
}

// ===== PHASE 1.5: Azure Key Vault (if enabled) =====
// NOTE: This runs before DI container is built, so we read from IConfiguration directly
var keyVaultEnabled = builder.Configuration.GetValue<bool>("KeyVault:Enabled", true);
var keyVaultUri = builder.Configuration["KeyVault:VaultUri"];

if (keyVaultEnabled && !string.IsNullOrEmpty(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        useManagedIdentity
            ? new DefaultAzureCredential()
            : new DefaultAzureCredential(new DefaultAzureCredentialOptions { ExcludeManagedIdentityCredential = true }));
}

// ===== PHASE 0.1: Configure Problem Details =====
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        // Add trace ID for debugging
        ctx.ProblemDetails.Extensions["traceId"] =
            Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier;

        // Add timestamp
        ctx.ProblemDetails.Extensions["timestamp"] = DateTime.UtcNow;

        // Add environment name (helps identify dev vs prod issues)
        ctx.ProblemDetails.Extensions["environment"] =
            builder.Environment.EnvironmentName;
    };
});

// ===== PHASE 1.1: Configure Entra ID Authentication =====
// NOTE: This runs before DI container is built, so we read from IConfiguration directly
var disableAuth = builder.Configuration.GetValue<bool>("Authentication:DisableAuth", false);

if (!disableAuth)
{
    builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "AzureAd");
    
    // Configure authorization policies
    builder.Services.AddAuthorization(options =>
    {
        // Default policy: require authenticated user
        options.FallbackPolicy = options.DefaultPolicy;
        
        // Custom policies
        options.AddPolicy("DataIngestion", policy =>
            policy.RequireClaim("roles", "DataIngestion.Write", "Admin"));
        
        options.AddPolicy("DataRead", policy =>
            policy.RequireClaim("roles", "DataIngestion.Read", "DataIngestion.Write", "Admin"));
        
        options.AddPolicy("Admin", policy =>
            policy.RequireClaim("roles", "Admin"));
        
        // API Documentation Access (Premium/Developer/Admin)
        options.AddPolicy("ApiDocumentationAccess", policy =>
            policy.RequireAssertion(context =>
                context.User.HasClaim(c => c.Type == "roles" && 
                    (c.Value == "Admin" || 
                     c.Value == "Developer" || 
                     c.Value == "PremiumSubscriber"))));
    });
}
else
{
    // Development mode: no authentication
    builder.Services.AddAuthorization(options =>
    {
        // Allow unrestricted access in development
        options.AddPolicy("ApiDocumentationAccess", policy =>
            policy.RequireAssertion(_ => true));
    });
}

// ===== Configure Controllers with Model Validation =====
builder.Services.AddControllers(options =>
{
    // Additional controller options can go here
})
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

// ===== PHASE 6.4: Configure API Versioning =====
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"));
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'V";
    options.SubstituteApiVersionInUrl = true;
});

// ===== PHASE 7.4: Configure OpenAPI with comprehensive documentation =====
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<ApiInfoTransformer>();
    options.AddDocumentTransformer<SecuritySchemeTransformer>();
    options.AddDocumentTransformer<ServerConfigurationTransformer>();
    options.AddDocumentTransformer<TagDescriptionTransformer>();
});

// ===== SignalR for Real-Time Streaming =====
builder.Services.AddSignalR();

// ===== PHASE 0.3: Register Services with Correct Lifetimes =====

// PHASE 1.2: Application Insights (if configured)
// NOTE: This runs before DI container is built, so we read from IConfiguration directly
var appInsightsConnection = builder.Configuration["ApplicationInsights:ConnectionString"];

if (!string.IsNullOrEmpty(appInsightsConnection))
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = appInsightsConnection;
    });
}

// Register DbContext and business services (Scoped lifetime)
builder.Services.AddBusinessServices();

// Register mock services for marketing/demo environment
// TODO: Switch to AddHartonomousInfrastructure() for production
builder.Services.AddHartonomousMockServices();

// Register data ingestion services (atomizers, file detection, bulk insert)
builder.Services.AddIngestionServices();

// Add health checks (basic DbContext check)
// NOTE: Full health checks (Neo4j, Key Vault, App Config) are in AddHartonomousInfrastructure()
builder.Services.AddHealthChecks()
    .AddDbContextCheck<Hartonomous.Data.Entities.HartonomousDbContext>("database");

// ===== PHASE 6.5: Configure Rate Limiting =====
builder.Services.AddRateLimiter(options =>
{
    // Global rate limit: 100 requests per minute per user
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));

    // Ingestion endpoint: more restrictive (20 requests per minute)
    options.AddPolicy("ingestion", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5
            }));

    // Query endpoints: moderate (50 requests per minute)
    options.AddPolicy("query", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 50,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            }));

    // Rejection response
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// ===== PHASE 6.6: Configure CORS =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("ProductionCors", builder =>
    {
        builder
            .WithOrigins(
                "https://hartonomous.com",
                "https://www.hartonomous.com",
                "https://app.hartonomous.com",
                "https://admin.hartonomous.com")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });

    options.AddPolicy("DevelopmentCors", builder =>
    {
        builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// ===== PHASE 0.3: Scope Validation (Development Only) =====
if (builder.Environment.IsDevelopment())
{
    // Validate no captive dependencies (singleton capturing scoped)
    builder.Services.AddOptions<ServiceProviderOptions>()
        .Configure(options => options.ValidateScopes = true);
}

// ===== Register OpenAPI Document Transformers =====
builder.Services.AddTransient<ApiInfoTransformer>();
builder.Services.AddTransient<SecuritySchemeTransformer>();
builder.Services.AddTransient<ServerConfigurationTransformer>();
builder.Services.AddTransient<TagDescriptionTransformer>();

var app = builder.Build();

// ===== PHASE 0.1: Configure Middleware Pipeline (Order is CRITICAL for security) =====

// 1. Exception handling (FIRST - catches all exceptions)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(); // Converts exceptions → Problem Details
}
else
{
    app.UseDeveloperExceptionPage(); // Detailed errors in development
}

// 2. Status code pages (404, 401, etc. → Problem Details)
app.UseStatusCodePages();

// 3. OpenAPI endpoint (production-ready, secured for tooling/discovery)
var enableSwagger = app.Configuration.GetValue<bool>("Swagger:EnableUI", app.Environment.IsDevelopment());

app.MapOpenApi()
    .RequireAuthorization("ApiDocumentationAccess");

// 3a. Swagger UI (interactive documentation - development + premium production access)
if (enableSwagger)
{
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Hartonomous API V1");
        
        // OAuth2 Configuration for Entra ID
        var clientId = app.Configuration["AzureAd:ClientId"];
        if (!string.IsNullOrEmpty(clientId))
        {
            options.OAuthClientId(clientId);
            options.OAuthUsePkce();
            options.OAuthScopes($"api://{clientId}/user_impersonation");
        }
        
        // UI Customization
        options.DocumentTitle = "Hartonomous API Documentation";
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        options.DefaultModelsExpandDepth(2);
        options.DisplayRequestDuration();
        options.EnableTryItOutByDefault();
        options.EnableDeepLinking();
        options.EnableFilter();
        
        // Production settings
        if (!app.Environment.IsDevelopment())
        {
            options.SupportedSubmitMethods(
                Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Get,
                Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Post,
                Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Put,
                Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Delete);
        }
    });
}

// 4. HTTPS redirection
app.UseHttpsRedirection();

// 5. Correlation ID middleware (adds X-Correlation-ID header)
app.UseMiddleware<CorrelationIdMiddleware>();

// 6. Request logging middleware (logs all requests/responses)
app.UseMiddleware<RequestLoggingMiddleware>();

// 7. CORS (PHASE 6.6)
if (app.Environment.IsDevelopment())
{
    app.UseCors("DevelopmentCors");
}
else
{
    app.UseCors("ProductionCors");
}

// 8. Rate limiting (PHASE 6.5)
app.UseRateLimiter();

// 9. Authentication (PHASE 1.1)
if (!disableAuth)
{
    app.UseAuthentication();
}

// 10. Authorization (must be AFTER authentication)
app.UseAuthorization();

// 10. Routing
app.UseRouting();

// Map endpoints
app.MapControllers();

// Map SignalR hubs
app.MapHub<Hartonomous.Infrastructure.Services.IngestionHub>("/hubs/ingestion");

// Map health check endpoints
app.MapHealthChecks("/health"); // Overall health
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready") || check.Tags.Contains("database")
}); // Readiness probe (database connectivity)
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // Always returns healthy (process is running)
}); // Liveness probe (process health)

app.Run();

// Make the implicit Program class public for integration tests
public partial class Program { }
