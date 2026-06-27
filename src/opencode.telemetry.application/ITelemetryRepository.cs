using Opencode.Telemetry.Domain;

namespace Opencode.Telemetry.Application;

public interface ITelemetryRepository
{
    Task PersistMetricPointAsync(MetricPointDocument document, CancellationToken cancellationToken = default);
    Task PersistLogEventAsync(LogEventDocument document, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MetricPointDocument>> QueryMetricPointsAsync(
        string signalName,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default);
}
