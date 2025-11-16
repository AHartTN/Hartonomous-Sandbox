using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Hartonomous.Core.Interfaces;
using Hartonomous.Infrastructure.Repositories;
using Hartonomous.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// AZURE KEY VAULT CONFIGURATION
// =====================================================
var keyVaultUri = builder.Configuration["KeyVault:VaultUri"];

if (!string.IsNullOrEmpty(keyVaultUri))
{
    var credential = new DefaultAzureCredential();
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), credential);
}

// =====================================================
// AZURE APP CONFIGURATION
// =====================================================
var appConfigConnectionString = builder.Configuration["AzureAppConfiguration:ConnectionString"];

if (!string.IsNullOrEmpty(appConfigConnectionString))
{
    builder.Configuration.AddAzureAppConfiguration(options =>
    {
        options.Connect(appConfigConnectionString)
               .UseFeatureFlags(); // Enable feature flags
    });
}

// =====================================================
// ENTRA ID AUTHENTICATION
// =====================================================
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization(options =>
{
    // Role-based policies
    options.AddPolicy("RequireAdminRole", policy =>
        policy.RequireRole("Admin"));
    
    options.AddPolicy("RequireAnalystRole", policy =>
        policy.RequireRole("Admin", "Analyst"));
    
    options.AddPolicy("RequireUserRole", policy =>
        policy.RequireRole("Admin", "Analyst", "User"));
});

// =====================================================
// APPLICATION SERVICES
// =====================================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("oauth2", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.OAuth2,
        Flows = new Microsoft.OpenApi.Models.OpenApiOAuthFlows
        {
            Implicit = new Microsoft.OpenApi.Models.OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri($"https://login.microsoftonline.com/{builder.Configuration["AzureAd:TenantId"]}/oauth2/v2.0/authorize"),
                TokenUrl = new Uri($"https://login.microsoftonline.com/{builder.Configuration["AzureAd:TenantId"]}/oauth2/v2.0/token"),
                Scopes = new Dictionary<string, string>
                {
                    { "api://hartonomous/access_as_user", "Access Hartonomous API" }
                }
            }
        }
    });
    
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "oauth2"
                }
            },
            new[] { "api://hartonomous/access_as_user" }
        }
    });
});

builder.Services.AddHttpClient();

// Hartonomous services
builder.Services.AddScoped<IAtomRepository, AtomRepository>();
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();

// Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// CORS (if needed for Blazor UI)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://hart-server:7000", "https://localhost:7000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// =====================================================
// HTTP REQUEST PIPELINE
// =====================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.OAuthClientId(builder.Configuration["AzureAd:ClientId"]);
        options.OAuthUsePkce();
    });
}

app.UseHttpsRedirection();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
