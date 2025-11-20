using Hartonomous.Admin.Components;
using Azure.Identity;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.FeatureManagement;
using Azure.Monitor.OpenTelemetry.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Azure App Configuration (optional - only if configured)
var appConfigEndpoint = builder.Configuration["AzureAppConfigurationEndpoint"];
if (!string.IsNullOrEmpty(appConfigEndpoint))
{
    builder.Configuration.AddAzureAppConfiguration(options =>
    {
        options.Connect(new Uri(appConfigEndpoint), new DefaultAzureCredential())
               .UseFeatureFlags();
    });
}

// Azure Key Vault (optional - only if configured)
var keyVaultUri = builder.Configuration["KeyVaultUri"];
if (!string.IsNullOrEmpty(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential());
}

// Microsoft Entra ID Authentication (optional - only if configured)
var azureAdSection = builder.Configuration.GetSection("AzureAd");
if (!string.IsNullOrEmpty(azureAdSection["TenantId"]))
{
    builder.Services.AddAuthentication("MicrosoftIdentityWebApp")
        .AddMicrosoftIdentityWebApp(azureAdSection);

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    });
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

// Azure Monitor (optional - only if configured)
var appInsightsConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
if (!string.IsNullOrEmpty(appInsightsConnectionString))
{
    builder.Services.AddOpenTelemetry()
        .UseAzureMonitor(options =>
        {
            options.ConnectionString = appInsightsConnectionString;
        });
}

// Feature Management
builder.Services.AddFeatureManagement();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

// Only use authentication if configured
if (!string.IsNullOrEmpty(azureAdSection["TenantId"]))
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapControllers();

app.Run();

public partial class Program { } // For testing
