using Opencode.Telemetry.Contracts;
using Opencode.Telemetry.Domain;
using Microsoft.Extensions.Logging;

namespace Opencode.Telemetry.Application;

public class CostAggregationService : ICostAggregationService
{
    private readonly ITelemetryRepository _repository;
    private readonly ILogger<CostAggregationService> _logger;

    public CostAggregationService(ITelemetryRepository repository, ILogger<CostAggregationService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<CostAggregationResponse> ComputeCostUsageAsync(
        CostAggregationRequest request,
        CancellationToken cancellationToken = default)
    {
        var startUtc = request.Start!.Value;
        var endUtc = request.End!.Value;

        var points = await _repository.QueryMetricPointsAsync(
            "opencode.cost.usage", startUtc, endUtc, cancellationToken);

        var orderedPoints = points
            .OrderBy(p => p.ObservationTimestampUtc)
            .ThenBy(p => p.IngestedAtUtc)
            .ToList();

        var deduplicated = DeduplicatePoints(orderedPoints);
        var pointCount = deduplicated.Count;
        double usageUsd;

        if (pointCount < 2)
        {
            usageUsd = 0;
            _logger.LogInformation(
                "Insufficient points for delta: {Count} point(s) in window [{Start}] to [{End}]",
                pointCount, startUtc, endUtc);
        }
        else
        {
            var firstValue = deduplicated[0].ValueUsd;
            var lastValue = deduplicated[^1].ValueUsd;
            var delta = lastValue - firstValue;

            if (delta < 0)
            {
                _logger.LogWarning(
                    "Counter reset detected: first={First} last={Last} in window [{Start}] to [{End}]. Returning 0.",
                    firstValue, lastValue, startUtc, endUtc);
                usageUsd = 0;
            }
            else
            {
                usageUsd = delta;
            }
        }

        return new CostAggregationResponse
        {
            StartUtc = startUtc,
            EndUtc = endUtc,
            UsageUsd = Math.Round(usageUsd, 6),
            PointCountConsidered = pointCount
        };
    }

    private static List<MetricPointDocument> DeduplicatePoints(List<MetricPointDocument> points)
    {
        var seen = new HashSet<string>();
        var result = new List<MetricPointDocument>();

        foreach (var point in points)
        {
            var key = $"{point.ObservationTimestampUtc:O}_{point.ValueUsd}_{point.RawIdentityFingerprint}";
            if (seen.Add(key))
            {
                result.Add(point);
            }
        }

        return result;
    }
}
