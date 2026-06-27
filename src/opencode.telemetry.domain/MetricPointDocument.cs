namespace Opencode.Telemetry.Domain;

public class MetricPointDocument
{
    public string Id { get; set; } = string.Empty;
    public string DocumentType { get; set; } = "metric_point";
    public string StudentContextKey { get; set; } = string.Empty;
    public string SignalName { get; set; } = string.Empty;
    public MetricType MetricType { get; set; } = MetricType.Unspecified;
    public AggregationTemporality AggregationTemporality { get; set; } = AggregationTemporality.Unspecified;
    public double ValueUsd { get; set; }
    public DateTime ObservationTimestampUtc { get; set; }
    public Dictionary<string, string> ResourceAttributes { get; set; } = new();
    public Dictionary<string, string> ScopeAttributes { get; set; } = new();
    public string? SourceTraceId { get; set; }
    public string? SourceSpanId { get; set; }
    public DateTime IngestedAtUtc { get; set; }
    public string SchemaVersion { get; set; } = "1.0";
    public string RawIdentityFingerprint { get; set; } = string.Empty;
}
