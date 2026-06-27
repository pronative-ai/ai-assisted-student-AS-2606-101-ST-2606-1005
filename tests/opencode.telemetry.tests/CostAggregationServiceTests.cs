using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Opencode.Telemetry.Application;
using Opencode.Telemetry.Contracts;
using Opencode.Telemetry.Domain;

namespace Opencode.Telemetry.Tests;

public class CostAggregationServiceTests
{
    private readonly Mock<ITelemetryRepository> _repositoryMock;
    private readonly CostAggregationService _service;

    public CostAggregationServiceTests()
    {
        _repositoryMock = new Mock<ITelemetryRepository>();
        var loggerMock = new Mock<ILogger<CostAggregationService>>();
        _service = new CostAggregationService(_repositoryMock.Object, loggerMock.Object);
    }

    [Fact]
    public async Task ComputeCostUsageAsync_ValidWindow_ReturnsDelta()
    {
        var points = new List<MetricPointDocument>
        {
            new() { ObservationTimestampUtc = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc), ValueUsd = 10.25, RawIdentityFingerprint = "a" },
            new() { ObservationTimestampUtc = new DateTime(2026, 1, 1, 11, 0, 0, DateTimeKind.Utc), ValueUsd = 13.75, RawIdentityFingerprint = "b" }
        };

        _repositoryMock.Setup(r => r.QueryMetricPointsAsync("opencode.cost.usage", It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(points);

        var request = new CostAggregationRequest
        {
            Start = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            End = new DateTime(2026, 1, 1, 11, 0, 0, DateTimeKind.Utc)
        };

        var result = await _service.ComputeCostUsageAsync(request);

        result.UsageUsd.Should().Be(3.50);
        result.PointCountConsidered.Should().Be(2);
        result.Currency.Should().Be("USD");
        result.MetricName.Should().Be("opencode.cost.usage");
        result.AggregationMethod.Should().Be("window_delta_from_cumulative_counter");
    }

    [Fact]
    public async Task ComputeCostUsageAsync_SinglePoint_ReturnsZero()
    {
        var points = new List<MetricPointDocument>
        {
            new() { ObservationTimestampUtc = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc), ValueUsd = 10.25, RawIdentityFingerprint = "a" }
        };

        _repositoryMock.Setup(r => r.QueryMetricPointsAsync("opencode.cost.usage", It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(points);

        var request = new CostAggregationRequest
        {
            Start = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc),
            End = new DateTime(2026, 1, 1, 11, 0, 0, DateTimeKind.Utc)
        };

        var result = await _service.ComputeCostUsageAsync(request);

        result.UsageUsd.Should().Be(0);
        result.PointCountConsidered.Should().Be(1);
    }

    [Fact]
    public async Task ComputeCostUsageAsync_NoPoints_ReturnsZero()
    {
        _repositoryMock.Setup(r => r.QueryMetricPointsAsync("opencode.cost.usage", It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MetricPointDocument>());

        var request = new CostAggregationRequest
        {
            Start = new DateTime(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc),
            End = new DateTime(2026, 1, 1, 11, 0, 0, DateTimeKind.Utc)
        };

        var result = await _service.ComputeCostUsageAsync(request);

        result.UsageUsd.Should().Be(0);
        result.PointCountConsidered.Should().Be(0);
    }

    [Fact]
    public async Task ComputeCostUsageAsync_CounterReset_ReturnsZero()
    {
        var points = new List<MetricPointDocument>
        {
            new() { ObservationTimestampUtc = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc), ValueUsd = 13.75, RawIdentityFingerprint = "a" },
            new() { ObservationTimestampUtc = new DateTime(2026, 1, 1, 11, 0, 0, DateTimeKind.Utc), ValueUsd = 10.25, RawIdentityFingerprint = "b" }
        };

        _repositoryMock.Setup(r => r.QueryMetricPointsAsync("opencode.cost.usage", It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(points);

        var request = new CostAggregationRequest
        {
            Start = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            End = new DateTime(2026, 1, 1, 11, 0, 0, DateTimeKind.Utc)
        };

        var result = await _service.ComputeCostUsageAsync(request);

        result.UsageUsd.Should().Be(0);
        result.PointCountConsidered.Should().Be(2);
    }

    [Fact]
    public async Task ComputeCostUsageAsync_DuplicatePoints_Deduplicates()
    {
        var points = new List<MetricPointDocument>
        {
            new() { ObservationTimestampUtc = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc), ValueUsd = 10.25, RawIdentityFingerprint = "a" },
            new() { ObservationTimestampUtc = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc), ValueUsd = 10.25, RawIdentityFingerprint = "a" },
            new() { ObservationTimestampUtc = new DateTime(2026, 1, 1, 11, 0, 0, DateTimeKind.Utc), ValueUsd = 13.75, RawIdentityFingerprint = "b" }
        };

        _repositoryMock.Setup(r => r.QueryMetricPointsAsync("opencode.cost.usage", It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(points);

        var request = new CostAggregationRequest
        {
            Start = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            End = new DateTime(2026, 1, 1, 11, 0, 0, DateTimeKind.Utc)
        };

        var result = await _service.ComputeCostUsageAsync(request);

        result.UsageUsd.Should().Be(3.50);
        result.PointCountConsidered.Should().Be(2);
    }
}
