using Microsoft.AspNetCore.Mvc;
using Opencode.Telemetry.Application;
using Opencode.Telemetry.Contracts;

namespace Opencode.Telemetry.Api.Controllers;

[ApiController]
[Route("api/opencode")]
public class CostAggregationController : ControllerBase
{
    private readonly ICostAggregationService _aggregationService;
    private readonly ILogger<CostAggregationController> _logger;

    public CostAggregationController(ICostAggregationService aggregationService, ILogger<CostAggregationController> logger)
    {
        _aggregationService = aggregationService;
        _logger = logger;
    }

    [HttpGet("cost-usage")]
    public async Task<IActionResult> GetCostUsage(
        [FromQuery] DateTime? start,
        [FromQuery] DateTime? end,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received cost aggregation request: start={Start}, end={End}", start, end);

        if (start == null || end == null)
        {
            return BadRequest(new { error = "Both 'start' and 'end' query parameters are required." });
        }

        if (start.Value.Kind == DateTimeKind.Unspecified)
            start = DateTime.SpecifyKind(start.Value, DateTimeKind.Utc);
        if (end.Value.Kind == DateTimeKind.Unspecified)
            end = DateTime.SpecifyKind(end.Value, DateTimeKind.Utc);

        if (start.Value >= end.Value)
        {
            return BadRequest(new { error = "'start' must be earlier than 'end'." });
        }

        var request = new CostAggregationRequest
        {
            Start = start.Value.ToUniversalTime(),
            End = end.Value.ToUniversalTime()
        };

        var response = await _aggregationService.ComputeCostUsageAsync(request, cancellationToken);

        _logger.LogInformation("Cost aggregation result: usage={Usage}, points={Points}",
            response.UsageUsd, response.PointCountConsidered);

        return Ok(response);
    }
}
