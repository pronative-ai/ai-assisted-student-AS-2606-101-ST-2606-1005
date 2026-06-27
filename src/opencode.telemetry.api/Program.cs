using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Opencode.Telemetry.Application;
using Opencode.Telemetry.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var cosmosConnectionString = builder.Configuration.GetValue<string>("CosmosDb:ConnectionString")
    ?? Environment.GetEnvironmentVariable("COSMOS_CONNECTION_STRING")
    ?? "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

var databaseName = builder.Configuration.GetValue<string>("CosmosDb:DatabaseName") ?? "opencode-telemetry";
var containerName = builder.Configuration.GetValue<string>("CosmosDb:ContainerName") ?? "telemetry";
var studentContextKey = builder.Configuration.GetValue<string>("StudentContextKey") ?? "default-student";

builder.Services.AddInfrastructureServices(cosmosConnectionString, databaseName, containerName, studentContextKey);
builder.Services.AddScoped<ICostAggregationService, CostAggregationService>();

var appInsightsConnectionString = builder.Configuration.GetValue<string>("ApplicationInsights:ConnectionString")
    ?? Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");

if (!string.IsNullOrEmpty(appInsightsConnectionString))
{
    builder.Services.AddOpenTelemetry().UseAzureMonitor(options =>
    {
        options.ConnectionString = appInsightsConnectionString;
    });
}

builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/healthz");

app.Run();
