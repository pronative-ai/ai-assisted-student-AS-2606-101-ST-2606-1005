namespace Opencode.Telemetry.Contracts;

public class MetricPointDocumentDto
{
    public string SignalName { get; set; } = string.Empty;
    public string MetricType { get; set; } = string.Empty;
    public double ValueUsd { get; set; }
    public DateTime ObservationTimestampUtc { get; set; }
    public string AggregationTemporality { get; set; } = string.Empty;
    public Dictionary<string, string> ResourceAttributes { get; set; } = new();
    public Dictionary<string, string> ScopeAttributes { get; set; } = new();
}
