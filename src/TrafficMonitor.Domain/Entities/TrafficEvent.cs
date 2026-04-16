namespace TrafficMonitor.Domain.Entities;

using TrafficMonitor.Domain.Enums;
using TrafficMonitor.Domain.ValueObjects;

public sealed class TrafficEvent
{
    private readonly List<Detection> _detections = [];

    private TrafficEvent()
    {
    }

    public TrafficEvent(
        Guid eventId,
        string cameraId,
        EventType eventType,
        Severity severity,
        DateTimeOffset occurredAt,
        IEnumerable<Detection>? detections = null)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Event ID cannot be empty.", nameof(eventId));
        }

        if (string.IsNullOrWhiteSpace(cameraId))
        {
            throw new ArgumentException("Camera ID is required.", nameof(cameraId));
        }

        if (occurredAt.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("OccurredAt must be in UTC.", nameof(occurredAt));
        }

        EventId = eventId;
        CameraId = cameraId.Trim();
        EventType = eventType;
        Severity = severity;
        OccurredAt = occurredAt;

        if (detections is not null)
        {
            _detections.AddRange(detections);
        }
    }

    public int Id { get; private set; }

    public Guid EventId { get; private set; }

    public string CameraId { get; private set; } = string.Empty;

    public EventType EventType { get; private set; }

    public Severity Severity { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public IReadOnlyCollection<Detection> Detections => _detections;
}
