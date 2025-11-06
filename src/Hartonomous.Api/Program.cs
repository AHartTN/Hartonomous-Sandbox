using System.Linq;
using System.Threading.RateLimiting;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Hartonomous.Api.Authorization;
using Hartonomous.Api.RateLimiting;
using Hartonomous.Api.Services;
using Hartonomous.Infrastructure;
using Hartonomous.Infrastructure.RateLimiting;
using Hartonomous.Shared.Contracts.Errors;
using Hartonomous.Shared.Contracts.Responses;
using Hartonomous.Core.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.FeatureManagement;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using Neo4j.Driver;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Graceful shutdown configuration
var shutdownTimeoutSeconds = builder.Configuration.GetValue<int?>("GracefulShutdown:ShutdownTimeoutSeconds") ?? 30;
builder.Host.ConfigureHostOptions(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(shutdownTimeoutSeconds);
});

// Azure App Configuration integration
var appConfigEndpoint = builder.Configuration["Endpoints:AppConfiguration"] 
    ?? "https://appconfig-hartonomous.azconfig.io";
builder.Configuration.AddAzureAppConfiguration(options =>
{
    options.Connect(new Uri(appConfigEndpoint), new DefaultAzureCredential())
        // Configure Key Vault integration for secret references
        .ConfigureKeyVault(kv =>
        {
            kv.SetCredential(new DefaultAzureCredential());
        })
        // Enable configuration refresh (optional)
        .ConfigureRefresh(refresh =>
        {
            refresh.RegisterAll().SetRefreshInterval(TimeSpan.FromMinutes(5));
        });
});

// Add Azure App Configuration middleware for refresh support
builder.Services.AddAzureAppConfiguration();

// Feature Management - integrated with Azure App Configuration
builder.Services.AddFeatureManagement();

// OpenTelemetry Configuration
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("Hartonomous.Api")
        .AddAttributes(new[] {
            new KeyValuePair<string, object>("environment", builder.Environment.EnvironmentName),
            new KeyValuePair<string, object>("version", "1.0.0")
        }))
    .WithTracing(tracing => tracing
        .AddSource("Hartonomous.Api")
        .AddSource("Hartonomous.Pipelines") // From DependencyInjection.cs
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(otlp =>
        {
            // OTLP endpoint from configuration (e.g., Aspire Dashboard, Application Insights, Jaeger, Grafana Tempo)
            var endpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];
            if (!string.IsNullOrEmpty(endpoint))
            {
                otlp.Endpoint = new Uri(endpoint);
            }
            // Defaults to http://localhost:4317 (Aspire Dashboard) if not configured
        }))
    .WithMetrics(metrics => metrics
        .AddMeter("Hartonomous.Api")
        .AddMeter("Hartonomous.Pipelines") // From DependencyInjection.cs
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(otlp =>
        {
            var endpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];
            if (!string.IsNullOrEmpty(endpoint))
            {
                otlp.Endpoint = new Uri(endpoint);
            }
        }));

// Azure AD Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

// Authorization with custom handlers
builder.Services.AddHttpContextAccessor(); // Required for TenantResourceAuthorizationHandler

builder.Services.AddAuthorization(options =>
{
    // Legacy role-based policies (for backward compatibility)
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("DataScientist", policy => policy.RequireRole("Admin", "DataScientist"));
    options.AddPolicy("User", policy => policy.RequireRole("Admin", "DataScientist", "User"));

    // New role hierarchy policies
    options.AddPolicy("RequireAdmin", policy =>
        policy.Requirements.Add(new RoleHierarchyRequirement("Admin")));
    options.AddPolicy("RequireDataScientist", policy =>
        policy.Requirements.Add(new RoleHierarchyRequirement("DataScientist")));
    options.AddPolicy("RequireUser", policy =>
        policy.Requirements.Add(new RoleHierarchyRequirement("User")));

    // Tenant isolation policies
    options.AddPolicy("TenantIsolation", policy =>
        policy.Requirements.Add(new TenantResourceRequirement()));
    options.AddPolicy("TenantAtomAccess", policy =>
        policy.Requirements.Add(new TenantResourceRequirement("Atom")));
    options.AddPolicy("TenantEmbeddingAccess", policy =>
        policy.Requirements.Add(new TenantResourceRequirement("Embedding")));
    options.AddPolicy("TenantInferenceAccess", policy =>
        policy.Requirements.Add(new TenantResourceRequirement("InferenceJob")));
});

// Register authorization handlers
builder.Services.AddSingleton<IAuthorizationHandler, RoleHierarchyHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, TenantResourceAuthorizationHandler>();

// Register tenant rate limiting policy
builder.Services.AddSingleton<TenantRateLimitPolicy>();

// Rate Limiting with configuration-based options
var rateLimitOptions = new RateLimitOptions();
builder.Configuration.GetSection(RateLimitOptions.SectionName).Bind(rateLimitOptions);

builder.Services.AddRateLimiter(options =>
{
    // Authenticated users - sliding window
    options.AddSlidingWindowLimiter(RateLimitPolicies.Authenticated, opt =>
    {
        opt.PermitLimit = rateLimitOptions.AuthenticatedPermitLimit;
        opt.Window = TimeSpan.FromSeconds(rateLimitOptions.AuthenticatedWindowSeconds);
        opt.SegmentsPerWindow = rateLimitOptions.SegmentsPerWindow;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = rateLimitOptions.QueueLimit;
    });

    // Anonymous users - fixed window (stricter)
    options.AddFixedWindowLimiter(RateLimitPolicies.Anonymous, opt =>
    {
        opt.PermitLimit = rateLimitOptions.AnonymousPermitLimit;
        opt.Window = TimeSpan.FromSeconds(rateLimitOptions.AnonymousWindowSeconds);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = rateLimitOptions.QueueLimit;
    });

    // Premium tier - sliding window with higher limits
    options.AddSlidingWindowLimiter(RateLimitPolicies.Premium, opt =>
    {
        opt.PermitLimit = rateLimitOptions.PremiumPermitLimit;
        opt.Window = TimeSpan.FromSeconds(rateLimitOptions.PremiumWindowSeconds);
        opt.SegmentsPerWindow = rateLimitOptions.SegmentsPerWindow;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = rateLimitOptions.QueueLimit;
    });

    // Inference operations - concurrency limiter + token bucket
    options.AddConcurrencyLimiter(RateLimitPolicies.Inference, opt =>
    {
        opt.PermitLimit = rateLimitOptions.InferenceConcurrentLimit;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = rateLimitOptions.QueueLimit;
    });

    // Generation operations - concurrency limiter (most resource-intensive)
    options.AddConcurrencyLimiter(RateLimitPolicies.Generation, opt =>
    {
        opt.PermitLimit = rateLimitOptions.GenerationConcurrentLimit;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = rateLimitOptions.QueueLimit;
    });

    // Embedding operations - fixed window
    options.AddFixedWindowLimiter(RateLimitPolicies.Embedding, opt =>
    {
        opt.PermitLimit = rateLimitOptions.EmbeddingPermitLimit;
        opt.Window = TimeSpan.FromSeconds(rateLimitOptions.EmbeddingWindowSeconds);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = rateLimitOptions.QueueLimit;
    });

    // Graph operations - fixed window
    options.AddFixedWindowLimiter(RateLimitPolicies.Graph, opt =>
    {
        opt.PermitLimit = rateLimitOptions.GraphPermitLimit;
        opt.Window = TimeSpan.FromSeconds(rateLimitOptions.GraphWindowSeconds);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = rateLimitOptions.QueueLimit;
    });

    // Global rate limiter per IP address
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, System.Net.IPAddress>(httpContext =>
    {
        var remoteIp = httpContext.Connection.RemoteIpAddress;

        if (remoteIp != null && !System.Net.IPAddress.IsLoopback(remoteIp))
        {
            return RateLimitPartition.GetSlidingWindowLimiter(remoteIp, _ =>
                new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = rateLimitOptions.GlobalPermitLimit,
                    Window = TimeSpan.FromSeconds(rateLimitOptions.GlobalWindowSeconds),
                    SegmentsPerWindow = rateLimitOptions.SegmentsPerWindow,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = rateLimitOptions.QueueLimit
                });
        }

        return RateLimitPartition.GetNoLimiter(System.Net.IPAddress.Loopback);
    });

    // Legacy policies for backward compatibility
    options.AddFixedWindowLimiter("api", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 10;
    });

    options.AddTokenBucketLimiter("inference", opt =>
    {
        opt.TokenLimit = 20;
        opt.TokensPerPeriod = 5;
        opt.ReplenishmentPeriod = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 5;
        opt.AutoReplenishment = true;
    });

    options.AddPolicy("per-user", context =>
    {
        var username = context.User.Identity?.Name ?? "anonymous";
        return RateLimitPartition.GetSlidingWindowLimiter(username, _ =>
            new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 200,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 4,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            });
    });

    // Tenant-aware rate limiting (tier-based)
    options.AddPolicy("tenant-tier", context =>
    {
        var policy = context.RequestServices.GetRequiredService<TenantRateLimitPolicy>();
        return policy.GetPartition(context);
    });

    // Tenant-aware sliding window (for API endpoints)
    options.AddPolicy("tenant-api", context =>
    {
        var policy = context.RequestServices.GetRequiredService<TenantRateLimitPolicy>();
        return policy.GetSlidingWindowPartition(context);
    });

    // Tenant-aware token bucket (for inference/generation)
    options.AddPolicy("tenant-inference", context =>
    {
        var policy = context.RequestServices.GetRequiredService<TenantRateLimitPolicy>();
        return policy.GetTokenBucketPartition(context);
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers["Retry-After"] =
                ((int)retryAfter.TotalSeconds).ToString();
        }
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(
            ApiResponse<object>.Failure(
                [ErrorDetailFactory.RateLimitExceeded("Rate limit exceeded. Please try again later.")],
                context.HttpContext.TraceIdentifier),
            cancellationToken);
    };
});

// Azure Storage Services
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var accountName = config["AzureStorage:AccountName"];
    var blobEndpoint = config["AzureStorage:BlobEndpoint"];
    var credential = new DefaultAzureCredential();
    return new BlobServiceClient(new Uri(blobEndpoint!), credential);
});

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var queueEndpoint = config["AzureStorage:QueueEndpoint"];
    var credential = new DefaultAzureCredential();
    return new QueueServiceClient(new Uri(queueEndpoint!), credential);
});

// Neo4j Driver
builder.Services.AddSingleton<IDriver>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var uri = config["Neo4j:Uri"] ?? "bolt://localhost:7687";
    var username = config["Neo4j:Username"] ?? "neo4j";
    var password = config["Neo4j:Password"] ?? "neo4jneo4j";
    return GraphDatabase.Driver(uri, AuthTokens.Basic(username, password));
});

builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressInferBindingSourcesForParameters = true;
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .SelectMany(entry => entry.Value!.Errors.Select(error =>
                ErrorDetailFactory.InvalidFieldValue(entry.Key, string.IsNullOrWhiteSpace(error.ErrorMessage)
                    ? "The supplied value is invalid."
                    : error.ErrorMessage)))
            .ToArray();

        var response = ApiResponse<object>.Failure(errors, context.HttpContext.TraceIdentifier);
        return new BadRequestObjectResult(response);
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hartonomous API",
        Version = "v1",
        Description = "REST API surface for Hartonomous multi-modal atomic substrate platform."
    });

    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri($"{builder.Configuration["AzureAd:Instance"]}{builder.Configuration["AzureAd:TenantId"]}/oauth2/v2.0/authorize"),
                TokenUrl = new Uri($"{builder.Configuration["AzureAd:Instance"]}{builder.Configuration["AzureAd:TenantId"]}/oauth2/v2.0/token"),
                Scopes = new Dictionary<string, string>
                {
                    { $"api://{builder.Configuration["AzureAd:ClientId"]}/access_as_user", "Access API as user" }
                }
            }
        }
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" }
            },
            new[] { $"api://{builder.Configuration["AzureAd:ClientId"]}/access_as_user" }
        }
    });
});

builder.Services.AddHartonomousInfrastructure(builder.Configuration);
builder.Services.AddHartonomousHealthChecks(builder.Configuration);
builder.Services.AddScoped<IModelIngestionService, ApiModelIngestionService>();

// Legacy inference job processor (still supported)
builder.Services.AddHostedService<Hartonomous.Infrastructure.Services.Jobs.InferenceJobWorker>();
builder.Services.AddScoped<Hartonomous.Infrastructure.Services.Jobs.InferenceJobProcessor>();

// Background job infrastructure
builder.Services.AddScoped<Hartonomous.Infrastructure.Jobs.IJobService, Hartonomous.Infrastructure.Jobs.JobService>();
builder.Services.AddSingleton<Hartonomous.Infrastructure.Jobs.JobExecutor>();

// Register job processors
builder.Services.AddScoped<Hartonomous.Infrastructure.Jobs.Processors.CleanupJobProcessor>();
builder.Services.AddScoped<Hartonomous.Infrastructure.Jobs.Processors.IndexMaintenanceJobProcessor>();
builder.Services.AddScoped<Hartonomous.Infrastructure.Jobs.Processors.AnalyticsJobProcessor>();
builder.Services.AddScoped<Hartonomous.Infrastructure.Caching.CacheWarmingJobProcessor>();

// Background job worker
builder.Services.AddHostedService<Hartonomous.Infrastructure.Jobs.BackgroundJobWorker>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Hartonomous.Infrastructure.Jobs.BackgroundJobWorker>>();
    return new Hartonomous.Infrastructure.Jobs.BackgroundJobWorker(sp, logger, maxConcurrency: 5);
});

var app = builder.Build();

// Register job processors with executor
using (var scope = app.Services.CreateScope())
{
    var executor = scope.ServiceProvider.GetRequiredService<Hartonomous.Infrastructure.Jobs.JobExecutor>();
    
    executor.RegisterProcessor<
        Hartonomous.Infrastructure.Jobs.Processors.CleanupJobPayload,
        Hartonomous.Infrastructure.Jobs.Processors.CleanupJobProcessor>("Cleanup");
    
    executor.RegisterProcessor<
        Hartonomous.Infrastructure.Jobs.Processors.IndexMaintenancePayload,
        Hartonomous.Infrastructure.Jobs.Processors.IndexMaintenanceJobProcessor>("IndexMaintenance");
    
    executor.RegisterProcessor<
        Hartonomous.Infrastructure.Jobs.Processors.AnalyticsJobPayload,
        Hartonomous.Infrastructure.Jobs.Processors.AnalyticsJobProcessor>("Analytics");
    
    executor.RegisterProcessor<
        Hartonomous.Infrastructure.Caching.CacheWarmingPayload,
        Hartonomous.Infrastructure.Caching.CacheWarmingJobProcessor>("CacheWarming");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.OAuthClientId(builder.Configuration["AzureAd:ClientId"]);
        options.OAuthUsePkce();
        options.OAuthScopeSeparator(" ");
    });
}

app.UseHttpsRedirection();

// Enable Azure App Configuration refresh middleware
app.UseAzureAppConfiguration();

// Correlation tracking middleware (W3C Trace Context)
// Must be before UseRateLimiter and UseAuthentication
app.UseMiddleware<Hartonomous.Infrastructure.Middleware.CorrelationMiddleware>();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireRateLimiting("api");

// Health check endpoints for Kubernetes probes
// Startup probe: Checks if application has completed initialization
app.MapHealthChecks("/health/startup", new HealthCheckOptions
{
    Predicate = _ => false, // No specific checks - just validates the app started
    AllowCachingResponses = false
}).AllowAnonymous();

// Readiness probe: Checks if application can handle traffic
// All checks tagged with "ready" must pass
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    AllowCachingResponses = false,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                data = e.Value.Data,
                duration = e.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        });
        await context.Response.WriteAsync(result);
    }
}).AllowAnonymous();

// Liveness probe: Checks if application is still running (not deadlocked)
// Returns healthy if the app can process requests (no specific checks)
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false, // No checks - just validates request pipeline works
    AllowCachingResponses = false
}).AllowAnonymous();

// General health check endpoint with all checks
app.MapHealthChecks("/health", new HealthCheckOptions
{
    AllowCachingResponses = false,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                tags = e.Value.Tags,
                data = e.Value.Data,
                duration = e.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        });
        await context.Response.WriteAsync(result);
    }
}).AllowAnonymous();

app.Run();
