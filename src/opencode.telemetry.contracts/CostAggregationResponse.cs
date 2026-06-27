namespace Opencode.Telemetry.Contracts;

public class CostAggregationResponse
{
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public double UsageUsd { get; set; }
    public string Currency { get; set; } = "USD";
    public string MetricName { get; set; } = "opencode.cost.usage";
    public string AggregationMethod { get; set; } = "window_delta_from_cumulative_counter";
    public int PointCountConsidered { get; set; }
}
