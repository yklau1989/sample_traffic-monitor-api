using TrafficMonitor.Domain.Entities;
using TrafficMonitor.Domain.Enums;
using TrafficMonitor.Domain.ValueObjects;

namespace TrafficMonitor.Tests.Domain;

public class TrafficEventTests
{
    [Fact]
    public void Constructor_WithValidValues_StoresAllFields()
    {
        var eventId = Guid.NewGuid();
        var occurredAt = new DateTime(2026, 4, 18, 9, 0, 0, DateTimeKind.Utc);
        var ingestedAt = new DateTime(2026, 4, 18, 9, 1, 0, DateTimeKind.Utc);
        var detections = new[]
        {
            new Detection("car", 0.91, new BoundingBox(0.1, 0.2, 0.3, 0.4)),
            new Detection("truck", 0.78, new BoundingBox(0.4, 0.5, 0.2, 0.1))
        };

        var trafficEvent = new TrafficEvent(
            eventId,
            EventType.Accident,
            Severity.High,
            "  camera-01  ",
            occurredAt,
            ingestedAt,
            detections);

        Assert.Equal(0, trafficEvent.Id);
        Assert.Equal(eventId, trafficEvent.EventId);
        Assert.Equal(EventType.Accident, trafficEvent.EventType);
        Assert.Equal(Severity.High, trafficEvent.Severity);
        Assert.Equal("camera-01", trafficEvent.CameraId);
        Assert.Equal(occurredAt, trafficEvent.OccurredAt);
        Assert.Equal(ingestedAt, trafficEvent.IngestedAt);
        Assert.Equal(detections, trafficEvent.Detections);
    }

    [Fact]
    public void Constructor_WithEmptyGuid_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new TrafficEvent(
            Guid.Empty,
            EventType.Debris,
            Severity.Low,
            "camera-01",
            new DateTime(2026, 4, 18, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 18, 9, 1, 0, DateTimeKind.Utc),
            []));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidCameraId_ThrowsArgumentException(string cameraId)
    {
        Assert.Throws<ArgumentException>(() => new TrafficEvent(
            Guid.NewGuid(),
            EventType.Debris,
            Severity.Low,
            cameraId,
            new DateTime(2026, 4, 18, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 18, 9, 1, 0, DateTimeKind.Utc),
            []));
    }

    [Fact]
    public void Constructor_WithCameraIdLongerThan64Characters_ThrowsArgumentException()
    {
        var cameraId = new string('c', 65);

        Assert.Throws<ArgumentException>(() => new TrafficEvent(
            Guid.NewGuid(),
            EventType.Debris,
            Severity.Low,
            cameraId,
            new DateTime(2026, 4, 18, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 18, 9, 1, 0, DateTimeKind.Utc),
            []));
    }

    [Fact]
    public void Constructor_WithNonUtcOccurredAt_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new TrafficEvent(
            Guid.NewGuid(),
            EventType.Debris,
            Severity.Low,
            "camera-01",
            new DateTime(2026, 4, 18, 9, 0, 0, DateTimeKind.Local),
            new DateTime(2026, 4, 18, 9, 1, 0, DateTimeKind.Utc),
            []));
    }

    [Fact]
    public void Constructor_WithNonUtcIngestedAt_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new TrafficEvent(
            Guid.NewGuid(),
            EventType.Debris,
            Severity.Low,
            "camera-01",
            new DateTime(2026, 4, 18, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 18, 9, 1, 0, DateTimeKind.Local),
            []));
    }

    [Fact]
    public void Constructor_WithIngestedAtEarlierThanOccurredAt_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new TrafficEvent(
            Guid.NewGuid(),
            EventType.Debris,
            Severity.Low,
            "camera-01",
            new DateTime(2026, 4, 18, 9, 1, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 18, 9, 0, 0, DateTimeKind.Utc),
            []));
    }

    [Fact]
    public void Constructor_WithEmptyDetectionsList_AcceptsEmptyCollection()
    {
        var trafficEvent = new TrafficEvent(
            Guid.NewGuid(),
            EventType.Congestion,
            Severity.Medium,
            "camera-02",
            new DateTime(2026, 4, 18, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 18, 9, 0, 1, DateTimeKind.Utc),
            []);

        Assert.Empty(trafficEvent.Detections);
    }
}
