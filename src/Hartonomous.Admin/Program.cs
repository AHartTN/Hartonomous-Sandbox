using Hartonomous.Admin;
using Hartonomous.Admin.Hubs;
using Hartonomous.Admin.Models;
using Hartonomous.Admin.Operations;
using Hartonomous.Admin.Services;
using Hartonomous.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSignalR();
builder.Services.AddHartonomousInfrastructure(builder.Configuration);
builder.Services.AddHartonomousHealthChecks(builder.Configuration);
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

app.Run();
