using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Opencode.Telemetry.Application;
using Opencode.Telemetry.Domain;

namespace Opencode.Telemetry.Infrastructure;

public class CosmosTelemetryRepository : ITelemetryRepository
{
    private readonly Container _container;
    private readonly ILogger<CosmosTelemetryRepository> _logger;

    public CosmosTelemetryRepository(CosmosClient cosmosClient, string databaseName, string containerName, ILogger<CosmosTelemetryRepository> logger)
    {
        _container = cosmosClient.GetContainer(databaseName, containerName);
        _logger = logger;
    }

    public async Task PersistMetricPointAsync(MetricPointDocument document, CancellationToken cancellationToken = default)
    {
        try
        {
            await _container.UpsertItemAsync(document, new PartitionKey(document.StudentContextKey), cancellationToken: cancellationToken);
            _logger.LogInformation("Persisted metric point {Id} signal={Signal} value={Value} ts={Timestamp}",
                document.Id, document.SignalName, document.ValueUsd, document.ObservationTimestampUtc);
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Cosmos DB write failed for metric point {Id}", document.Id);
            throw;
        }
    }

    public async Task PersistLogEventAsync(LogEventDocument document, CancellationToken cancellationToken = default)
    {
        try
        {
            await _container.UpsertItemAsync(document, new PartitionKey(document.StudentContextKey), cancellationToken: cancellationToken);
            _logger.LogInformation("Persisted log event {Id} event={Event} ts={Timestamp}",
                document.Id, document.EventName, document.TimestampUtc);
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Cosmos DB write failed for log event {Id}", document.Id);
            throw;
        }
    }

    public async Task<IReadOnlyList<MetricPointDocument>> QueryMetricPointsAsync(
        string signalName,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.documentType = 'metric_point' AND c.signalName = @signalName AND c.observationTimestampUtc >= @start AND c.observationTimestampUtc <= @end")
                .WithParameter("@signalName", signalName)
                .WithParameter("@start", startUtc.ToString("O"))
                .WithParameter("@end", endUtc.ToString("O"));

            var results = new List<MetricPointDocument>();
            using var iterator = _container.GetItemQueryIterator<MetricPointDocument>(query);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                results.AddRange(response);
            }

            _logger.LogInformation("Queried {Count} metric points for signal={Signal} range=[{Start},{End}]",
                results.Count, signalName, startUtc, endUtc);
            return results;
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Cosmos DB query failed for signal={Signal} range=[{Start},{End}]",
                signalName, startUtc, endUtc);
            throw;
        }
    }
}
