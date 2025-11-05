using Hartonomous.Admin;
using Hartonomous.Admin.Hubs;
using Hartonomous.Admin.Models;
using Hartonomous.Admin.Operations;
using Hartonomous.Admin.Services;
using Hartonomous.Infrastructure;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSignalR();
builder.Services.AddHartonomousInfrastructure(builder.Configuration);
builder.Services.AddHartonomousHealthChecks(builder.Configuration);

// ============================================================================
// OPENTELEMETRY CONFIGURATION - Pipeline Observability
// ============================================================================
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("Hartonomous.Admin")
        .AddAttributes(new[] {
            new KeyValuePair<string, object>("environment", builder.Environment.EnvironmentName),
            new KeyValuePair<string, object>("version", "1.0.0")
        }))
    .WithTracing(tracing => tracing
        .AddSource("Hartonomous.Pipelines")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
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
        .AddMeter("Hartonomous.Pipelines")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter()); // Exposes /metrics endpoint

builder.Services.Configure<AdminTelemetryOptions>(builder.Configuration.GetSection(AdminTelemetryOptions.SectionName));

builder.Services.AddSingleton<AdminTelemetryCache>();
builder.Services.AddSingleton<AdminOperationCoordinator>();
builder.Services.AddScoped<AdminOperationService>();
builder.Services.AddHostedService<AdminOperationWorker>();
builder.Services.AddHostedService<TelemetryBackgroundService>();

builder.Services.AddAntiforgery();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapHub<TelemetryHub>("/hubs/telemetry");
app.MapHealthChecks("/health");
app.MapPrometheusScrapingEndpoint();

app.Run();
