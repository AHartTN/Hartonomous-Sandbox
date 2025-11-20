using System.Diagnostics;
using Hartonomous.Api.Middleware;
using Hartonomous.Infrastructure.Configurations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// ===== PHASE 1.4: Azure App Configuration (if enabled) =====
var appConfigEnabled = builder.Configuration.GetValue<bool>("AzureAppConfiguration:Enabled", true);
var appConfigEndpoint = builder.Configuration["AzureAppConfiguration:Endpoint"];

if (appConfigEnabled && !string.IsNullOrEmpty(appConfigEndpoint))
{
    builder.Configuration.AddAzureAppConfiguration(options =>
    {
        options.Connect(new Uri(appConfigEndpoint), 
            builder.Configuration.GetValue<bool>("Azure:UseManagedIdentity") 
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
var keyVaultEnabled = builder.Configuration.GetValue<bool>("KeyVault:Enabled", true);
var keyVaultUri = builder.Configuration["KeyVault:VaultUri"];

if (keyVaultEnabled && !string.IsNullOrEmpty(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        builder.Configuration.GetValue<bool>("Azure:UseManagedIdentity")
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
var disableAuth = builder.Configuration.GetValue<bool>("Authentication:DisableAuth");

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
    });
}
else
{
    // Development mode: no authentication
    builder.Services.AddAuthorization();
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

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// ===== PHASE 0.3: Register Services with Correct Lifetimes =====

// PHASE 1.2: Application Insights (if configured)
var appInsightsConnection = builder.Configuration["ApplicationInsights:ConnectionString"];
if (!string.IsNullOrEmpty(appInsightsConnection))
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = appInsightsConnection;
    });
}

// Register DbContext and business services (Scoped lifetime)
builder.Services.AddBusinessServices(builder.Configuration);

// Register mock services for marketing/demo environment
// TODO: Switch to AddHartonomousInfrastructure() for production
builder.Services.AddHartonomousMockServices();

// Register data ingestion services (atomizers, file detection, bulk insert)
builder.Services.AddIngestionServices(builder.Configuration);

// Add health checks
builder.Services.AddHealthChecks();

// ===== PHASE 0.3: Scope Validation (Development Only) =====
if (builder.Environment.IsDevelopment())
{
    // Validate no captive dependencies (singleton capturing scoped)
    builder.Services.AddOptions<ServiceProviderOptions>()
        .Configure(options => options.ValidateScopes = true);
}

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

// 3. Configure the HTTP request pipeline (OpenAPI in development)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// 4. HTTPS redirection
app.UseHttpsRedirection();

// 5. Correlation ID middleware (adds X-Correlation-ID header)
app.UseMiddleware<CorrelationIdMiddleware>();

// 6. Request logging middleware (logs all requests/responses)
app.UseMiddleware<RequestLoggingMiddleware>();

// 7. CORS (not configured yet, placeholder for Phase 6)
// app.UseCors();

// 8. Authentication (PHASE 1.1)
if (!disableAuth)
{
    app.UseAuthentication();
}

// 9. Authorization (must be AFTER authentication)
app.UseAuthorization();

// 10. Routing
app.UseRouting();

// Map endpoints
app.MapControllers();

// Map health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live");

app.Run();

// Make the implicit Program class public for integration tests
public partial class Program { }
