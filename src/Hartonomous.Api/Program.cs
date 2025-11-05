using System.Linq;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Hartonomous.Api.Services;
using Hartonomous.Infrastructure;
using Hartonomous.Shared.Contracts.Errors;
using Hartonomous.Shared.Contracts.Responses;
using Hartonomous.Core.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using Neo4j.Driver;

var builder = WebApplication.CreateBuilder(args);

// Azure AD Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("DataScientist", policy => policy.RequireRole("Admin", "DataScientist"));
    options.AddPolicy("User", policy => policy.RequireRole("Admin", "DataScientist", "User"));
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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
