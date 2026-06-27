namespace Opencode.Telemetry.Application;

public interface IOtelIngestionService
{
    Task<IngestionResult> IngestMetricsAsync(byte[] otlpProtobufPayload, CancellationToken cancellationToken = default);
    Task<IngestionResult> IngestLogsAsync(byte[] otlpProtobufPayload, CancellationToken cancellationToken = default);
}

public class IngestionResult
{
    public bool Success { get; set; }
    public int MetricsPersisted { get; set; }
    public int LogsPersisted { get; set; }
    public int UnsupportedSignalsSkipped { get; set; }
    public string? ErrorMessage { get; set; }
}
