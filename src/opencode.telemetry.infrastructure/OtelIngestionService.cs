using System.Security.Cryptography;
using System.Text;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Opencode.Telemetry.Application;
using Opencode.Telemetry.Domain;
using OtlpCommon = OpenTelemetry.Proto.Common.V1;
using OtlpMetrics = OpenTelemetry.Proto.Metrics.V1;
using OtlpLogs = OpenTelemetry.Proto.Logs.V1;
using OtlpCollectorMetrics = OpenTelemetry.Proto.Collector.Metrics.V1;
using OtlpCollectorLogs = OpenTelemetry.Proto.Collector.Logs.V1;

namespace Opencode.Telemetry.Infrastructure;

public class OtelIngestionService : IOtelIngestionService
{
    private readonly ITelemetryRepository _repository;
    private readonly ILogger<OtelIngestionService> _logger;
    private readonly string _studentContextKey;

    public OtelIngestionService(
        ITelemetryRepository repository,
        ILogger<OtelIngestionService> logger,
        string studentContextKey = "default-student")
    {
        _repository = repository;
        _logger = logger;
        _studentContextKey = studentContextKey;
    }

    public async Task<IngestionResult> IngestMetricsAsync(byte[] otlpProtobufPayload, CancellationToken cancellationToken = default)
    {
        var result = new IngestionResult { Success = true };

        try
        {
            var request = OtlpCollectorMetrics.ExportMetricsServiceRequest.Parser.ParseFrom(otlpProtobufPayload);

            foreach (var resourceMetric in request.ResourceMetrics)
            {
                var resourceAttrs = ExtractAttributes(resourceMetric.Resource?.Attributes);

                foreach (var scopeMetric in resourceMetric.ScopeMetrics)
                {
                    var scopeAttrs = ExtractAttributes(scopeMetric.Scope?.Attributes);

                    foreach (var metric in scopeMetric.Metrics)
                    {
                        if (!IsInScopeMetric(metric.Name))
                        {
                            result.UnsupportedSignalsSkipped++;
                            _logger.LogDebug("Skipped unsupported metric signal: {Name}", metric.Name);
                            continue;
                        }

                        var dataPoints = ExtractDataPoints(metric);
                        foreach (var (timestampNano, value) in dataPoints)
                        {
                            var doc = new MetricPointDocument
                            {
                                Id = GenerateDeterministicId("metric", metric.Name, (long)timestampNano, value),
                                StudentContextKey = _studentContextKey,
                                SignalName = metric.Name,
                                MetricType = Domain.MetricType.Sum,
                                AggregationTemporality = Domain.AggregationTemporality.Cumulative,
                                ValueUsd = value,
                                ObservationTimestampUtc = FromUnixTimeNano(timestampNano),
                                ResourceAttributes = resourceAttrs,
                                ScopeAttributes = scopeAttrs,
                                IngestedAtUtc = DateTime.UtcNow,
                                RawIdentityFingerprint = $"{metric.Name}|{timestampNano}|{value}"
                            };

                            await _repository.PersistMetricPointAsync(doc, cancellationToken);
                            result.MetricsPersisted++;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest OTLP metrics payload");
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    public async Task<IngestionResult> IngestLogsAsync(byte[] otlpProtobufPayload, CancellationToken cancellationToken = default)
    {
        var result = new IngestionResult { Success = true };

        try
        {
            var request = OtlpCollectorLogs.ExportLogsServiceRequest.Parser.ParseFrom(otlpProtobufPayload);

            foreach (var resourceLog in request.ResourceLogs)
            {
                var resourceAttrs = ExtractAttributes(resourceLog.Resource?.Attributes);

                foreach (var scopeLog in resourceLog.ScopeLogs)
                {
                    var scopeAttrs = ExtractAttributes(scopeLog.Scope?.Attributes);

                    foreach (var logRecord in scopeLog.LogRecords)
                    {
                        var eventName = logRecord.EventName;
                        if (string.IsNullOrEmpty(eventName))
                            eventName = logRecord.Body?.StringValue ?? "unknown";

                        if (!IsInScopeLogEvent(eventName))
                        {
                            result.UnsupportedSignalsSkipped++;
                            _logger.LogDebug("Skipped unsupported log event: {Name}", eventName);
                            continue;
                        }

                        var doc = new LogEventDocument
                        {
                            Id = GenerateDeterministicId("log", eventName, (long)logRecord.TimeUnixNano, 0),
                            StudentContextKey = _studentContextKey,
                            EventName = eventName,
                            TimestampUtc = FromUnixTimeNano(logRecord.TimeUnixNano),
                            SeverityText = logRecord.SeverityText,
                            SeverityNumber = logRecord.SeverityNumber != 0 ? (int)logRecord.SeverityNumber : null,
                            Body = logRecord.Body?.StringValue,
                            ResourceAttributes = resourceAttrs,
                            ScopeAttributes = scopeAttrs,
                            SourceTraceId = ByteStringToHex(logRecord.TraceId),
                            SourceSpanId = ByteStringToHex(logRecord.SpanId),
                            IngestedAtUtc = DateTime.UtcNow,
                            SchemaVersion = "1.0",
                            RawIdentityFingerprint = $"{eventName}|{logRecord.TimeUnixNano}|{logRecord.SeverityText ?? "none"}|{logRecord.SeverityNumber}"
                        };

                        await _repository.PersistLogEventAsync(doc, cancellationToken);
                        result.LogsPersisted++;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest OTLP logs payload");
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    private static bool IsInScopeMetric(string metricName) => metricName switch
    {
        "opencode.cost.usage" => true,
        _ => false
    };

    private static bool IsInScopeLogEvent(string eventName) => eventName switch
    {
        "api_request" or "api_error" => true,
        _ => false
    };

    private static Dictionary<string, string> ExtractAttributes(Google.Protobuf.Collections.RepeatedField<OtlpCommon.KeyValue>? attributes)
    {
        var result = new Dictionary<string, string>();
        if (attributes == null) return result;

        foreach (var kv in attributes)
        {
            result[kv.Key] = kv.Value?.StringValue ?? string.Empty;
        }
        return result;
    }

    private static List<(ulong TimestampNano, double Value)> ExtractDataPoints(OtlpMetrics.Metric metric)
    {
        var points = new List<(ulong, double)>();

        if (metric.DataCase == OtlpMetrics.Metric.DataOneofCase.Sum && metric.Sum?.DataPoints != null)
        {
            foreach (var dp in metric.Sum.DataPoints)
            {
                if (dp.ValueCase == OtlpMetrics.NumberDataPoint.ValueOneofCase.AsDouble)
                    points.Add((dp.TimeUnixNano, dp.AsDouble));
            }
        }
        else if (metric.DataCase == OtlpMetrics.Metric.DataOneofCase.Gauge && metric.Gauge?.DataPoints != null)
        {
            foreach (var dp in metric.Gauge.DataPoints)
            {
                if (dp.ValueCase == OtlpMetrics.NumberDataPoint.ValueOneofCase.AsDouble)
                    points.Add((dp.TimeUnixNano, dp.AsDouble));
            }
        }

        return points;
    }

    private static DateTime FromUnixTimeNano(ulong nanoseconds)
    {
        var ticks = nanoseconds / 100;
        return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks((long)ticks);
    }

    private static string? ByteStringToHex(ByteString? bytes)
    {
        if (bytes == null || bytes.IsEmpty) return null;
        return BitConverter.ToString(bytes.ToByteArray()).Replace("-", "").ToLowerInvariant();
    }

    private static string GenerateDeterministicId(string prefix, string name, long timestampNano, double value)
    {
        var input = $"{prefix}|{name}|{timestampNano}|{value}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return $"{prefix}_{Convert.ToHexString(hash)[..16].ToLowerInvariant()}";
    }
}
