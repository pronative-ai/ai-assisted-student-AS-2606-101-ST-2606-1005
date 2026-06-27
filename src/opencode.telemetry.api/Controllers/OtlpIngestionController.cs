using Microsoft.AspNetCore.Mvc;
using Opencode.Telemetry.Application;

namespace Opencode.Telemetry.Api.Controllers;

[ApiController]
[Route("v1")]
public class OtlpIngestionController : ControllerBase
{
    private readonly IOtelIngestionService _ingestionService;
    private readonly ILogger<OtlpIngestionController> _logger;

    public OtlpIngestionController(IOtelIngestionService ingestionService, ILogger<OtlpIngestionController> logger)
    {
        _ingestionService = ingestionService;
        _logger = logger;
    }

    [HttpPost("metrics")]
    [Consumes("application/x-protobuf")]
    public async Task<IActionResult> ExportMetrics(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received OTLP metrics ingestion request");

        using var memoryStream = new MemoryStream();
        await Request.Body.CopyToAsync(memoryStream, cancellationToken);
        var payload = memoryStream.ToArray();

        var result = await _ingestionService.IngestMetricsAsync(payload, cancellationToken);

        if (!result.Success)
        {
            _logger.LogError("Metrics ingestion failed: {Error}", result.ErrorMessage);
            return Ok(new { partial_success = new { rejected_data_points = result.MetricsPersisted, error_message = result.ErrorMessage } });
        }

        _logger.LogInformation("Metrics ingestion succeeded: {Count} points persisted, {Skipped} skipped",
            result.MetricsPersisted, result.UnsupportedSignalsSkipped);

        return Ok(new
        {
            partial_success = new
            {
                rejected_data_points = 0L,
                error_message = ""
            }
        });
    }

    [HttpPost("logs")]
    [Consumes("application/x-protobuf")]
    public async Task<IActionResult> ExportLogs(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received OTLP logs ingestion request");

        using var memoryStream = new MemoryStream();
        await Request.Body.CopyToAsync(memoryStream, cancellationToken);
        var payload = memoryStream.ToArray();

        var result = await _ingestionService.IngestLogsAsync(payload, cancellationToken);

        if (!result.Success)
        {
            _logger.LogError("Logs ingestion failed: {Error}", result.ErrorMessage);
            return Ok(new { partial_success = new { rejected_log_records = result.LogsPersisted, error_message = result.ErrorMessage } });
        }

        _logger.LogInformation("Logs ingestion succeeded: {Count} records persisted, {Skipped} skipped",
            result.LogsPersisted, result.UnsupportedSignalsSkipped);

        return Ok(new
        {
            partial_success = new
            {
                rejected_log_records = 0L,
                error_message = ""
            }
        });
    }
}
