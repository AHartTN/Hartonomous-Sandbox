using System.Linq;
using System.Threading.RateLimiting;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Hartonomous.Api.Authorization;
using Hartonomous.Api.Services;
using Hartonomous.Infrastructure;
using Hartonomous.Shared.Contracts.Errors;
using Hartonomous.Shared.Contracts.Responses;
using Hartonomous.Core.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using Neo4j.Driver;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

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
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(otlp =>
        {
            // OTLP endpoint from configuration (e.g., Application Insights, Jaeger, Grafana Tempo)
            var endpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];
            if (!string.IsNullOrEmpty(endpoint))
            {
                otlp.Endpoint = new Uri(endpoint);
            }
            // Defaults to http://localhost:4317 if not configured
        }))
    .WithMetrics(metrics => metrics
        .AddMeter("Hartonomous.Api")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation());

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

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    // Fixed window limiter for general API endpoints
    options.AddFixedWindowLimiter("api", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 10;
    });

    // Stricter limiter for inference endpoints (more resource-intensive)
    options.AddTokenBucketLimiter("inference", opt =>
    {
        opt.TokenLimit = 20;
        opt.TokensPerPeriod = 5;
        opt.ReplenishmentPeriod = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 5;
        opt.AutoReplenishment = true;
    });

    // Sliding window for authenticated users (per-user limiting)
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
builder.Services.AddHostedService<Hartonomous.Infrastructure.Services.Jobs.InferenceJobWorker>();
builder.Services.AddScoped<Hartonomous.Infrastructure.Services.Jobs.InferenceJobProcessor>();

var app = builder.Build();

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

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireRateLimiting("api");
app.MapHealthChecks("/health").AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.Run();
