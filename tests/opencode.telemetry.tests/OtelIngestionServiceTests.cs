using FluentAssertions;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Moq;
using Opencode.Telemetry.Application;
using Opencode.Telemetry.Domain;
using OtlpCommon = OpenTelemetry.Proto.Common.V1;
using OtlpMetrics = OpenTelemetry.Proto.Metrics.V1;
using OtlpLogs = OpenTelemetry.Proto.Logs.V1;
using OtlpCollectorMetrics = OpenTelemetry.Proto.Collector.Metrics.V1;
using OtlpCollectorLogs = OpenTelemetry.Proto.Collector.Logs.V1;
using Opencode.Telemetry.Infrastructure;

namespace Opencode.Telemetry.Tests;

public class OtelIngestionServiceTests
{
    private readonly Mock<ITelemetryRepository> _repositoryMock;
    private readonly OtelIngestionService _service;

    public OtelIngestionServiceTests()
    {
        _repositoryMock = new Mock<ITelemetryRepository>();
        var loggerMock = new Mock<ILogger<OtelIngestionService>>();
        _service = new OtelIngestionService(_repositoryMock.Object, loggerMock.Object, "test-student");
    }

    [Fact]
    public async Task IngestMetricsAsync_SupportedMetric_PersistsDocument()
    {
        var request = new OtlpCollectorMetrics.ExportMetricsServiceRequest();
        request.ResourceMetrics.Add(new OtlpMetrics.ResourceMetrics
        {
            Resource = new OpenTelemetry.Proto.Resource.V1.Resource(),
            ScopeMetrics =
            {
                new OtlpMetrics.ScopeMetrics
                {
                    Metrics =
                    {
                        new OtlpMetrics.Metric
                        {
                            Name = "opencode.cost.usage",
                            Sum = new OtlpMetrics.Sum
                            {
                                AggregationTemporality = OtlpMetrics.AggregationTemporality.Cumulative,
                                DataPoints =
                                {
                                    new OtlpMetrics.NumberDataPoint
                                    {
                                        TimeUnixNano = 1704067200000000000,
                                        AsDouble = 42.5
                                    }
                                }
                            }
                        }
                    }
                }
            }
        });

        var payload = request.ToByteArray();
        var result = await _service.IngestMetricsAsync(payload);

        result.Success.Should().BeTrue();
        result.MetricsPersisted.Should().Be(1);
        result.UnsupportedSignalsSkipped.Should().Be(0);
        _repositoryMock.Verify(r => r.PersistMetricPointAsync(It.Is<MetricPointDocument>(d =>
            d.SignalName == "opencode.cost.usage" &&
            d.ValueUsd == 42.5), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IngestMetricsAsync_UnsupportedMetric_Skipped()
    {
        var request = new OtlpCollectorMetrics.ExportMetricsServiceRequest();
        request.ResourceMetrics.Add(new OtlpMetrics.ResourceMetrics
        {
            ScopeMetrics =
            {
                new OtlpMetrics.ScopeMetrics
                {
                    Metrics =
                    {
                        new OtlpMetrics.Metric
                        {
                            Name = "unsupported.metric",
                            Sum = new OtlpMetrics.Sum
                            {
                                DataPoints =
                                {
                                    new OtlpMetrics.NumberDataPoint
                                    {
                                        TimeUnixNano = 1704067200000000000,
                                        AsDouble = 99.9
                                    }
                                }
                            }
                        }
                    }
                }
            }
        });

        var payload = request.ToByteArray();
        var result = await _service.IngestMetricsAsync(payload);

        result.Success.Should().BeTrue();
        result.MetricsPersisted.Should().Be(0);
        result.UnsupportedSignalsSkipped.Should().Be(1);
        _repositoryMock.Verify(r => r.PersistMetricPointAsync(It.IsAny<MetricPointDocument>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IngestLogsAsync_SupportedLogs_PersistsDocuments()
    {
        var request = new OtlpCollectorLogs.ExportLogsServiceRequest();
        request.ResourceLogs.Add(new OtlpLogs.ResourceLogs
        {
            ScopeLogs =
            {
                new OtlpLogs.ScopeLogs
                {
                    LogRecords =
                    {
                        new OtlpLogs.LogRecord
                        {
                            TimeUnixNano = 1704067200000000000,
                            EventName = "api_request",
                            SeverityText = "info",
                            Body = new OtlpCommon.AnyValue { StringValue = "api_request" }
                        },
                        new OtlpLogs.LogRecord
                        {
                            TimeUnixNano = 1704067300000000000,
                            EventName = "api_error",
                            SeverityText = "error",
                            SeverityNumber = OtlpLogs.SeverityNumber.Error,
                            Body = new OtlpCommon.AnyValue { StringValue = "api_error" }
                        }
                    }
                }
            }
        });

        var payload = request.ToByteArray();
        var result = await _service.IngestLogsAsync(payload);

        result.Success.Should().BeTrue();
        result.LogsPersisted.Should().Be(2);
        result.UnsupportedSignalsSkipped.Should().Be(0);
        _repositoryMock.Verify(r => r.PersistLogEventAsync(It.Is<LogEventDocument>(d => d.EventName == "api_request"), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.PersistLogEventAsync(It.Is<LogEventDocument>(d => d.EventName == "api_error"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IngestLogsAsync_UnsupportedLog_Skipped()
    {
        var request = new OtlpCollectorLogs.ExportLogsServiceRequest();
        request.ResourceLogs.Add(new OtlpLogs.ResourceLogs
        {
            ScopeLogs =
            {
                new OtlpLogs.ScopeLogs
                {
                    LogRecords =
                    {
                        new OtlpLogs.LogRecord
                        {
                            TimeUnixNano = 1704067200000000000,
                            EventName = "unsupported_event",
                            Body = new OtlpCommon.AnyValue { StringValue = "unsupported_event" }
                        }
                    }
                }
            }
        });

        var payload = request.ToByteArray();
        var result = await _service.IngestLogsAsync(payload);

        result.Success.Should().BeTrue();
        result.LogsPersisted.Should().Be(0);
        result.UnsupportedSignalsSkipped.Should().Be(1);
    }
}
