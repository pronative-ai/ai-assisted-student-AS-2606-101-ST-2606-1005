using Opencode.Telemetry.Contracts;

namespace Opencode.Telemetry.Application;

public interface ICostAggregationService
{
    Task<CostAggregationResponse> ComputeCostUsageAsync(
        CostAggregationRequest request,
        CancellationToken cancellationToken = default);
}
