using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Opencode.Telemetry.Application;

namespace Opencode.Telemetry.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        string cosmosConnectionString,
        string databaseName,
        string containerName,
        string studentContextKey)
    {
        services.AddSingleton(sp =>
        {
            var clientOptions = new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Gateway,
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            };
            return new CosmosClient(cosmosConnectionString, clientOptions);
        });

        services.AddSingleton<ITelemetryRepository>(sp =>
        {
            var cosmosClient = sp.GetRequiredService<CosmosClient>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CosmosTelemetryRepository>>();
            return new CosmosTelemetryRepository(cosmosClient, databaseName, containerName, logger);
        });

        services.AddSingleton<IOtelIngestionService>(sp =>
        {
            var repository = sp.GetRequiredService<ITelemetryRepository>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<OtelIngestionService>>();
            return new OtelIngestionService(repository, logger, studentContextKey);
        });

        return services;
    }
}
