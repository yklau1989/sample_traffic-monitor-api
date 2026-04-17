using TrafficMonitor.Domain.Enums;
using TrafficMonitor.Domain.ValueObjects;

namespace TrafficMonitor.Domain.Entities;

public class TrafficEvent
{
    private readonly List<Detection> _detections;

    public int Id { get; private set; }

    public Guid EventId { get; private set; }

    public EventType EventType { get; private set; }

    public Severity Severity { get; private set; }

    public string CameraId { get; private set; } = string.Empty;

    public DateTime OccurredAt { get; private set; }

    public DateTime IngestedAt { get; private set; }

    public IReadOnlyCollection<Detection> Detections => _detections;

    private TrafficEvent()
    {
        _detections = new();
    }

    public TrafficEvent(
        Guid eventId,
        EventType eventType,
        Severity severity,
        string cameraId,
        DateTime occurredAt,
        DateTime ingestedAt,
        IEnumerable<Detection> detections)
        : this()
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("EventId cannot be empty.", nameof(eventId));
        }

        if (string.IsNullOrWhiteSpace(cameraId))
        {
            throw new ArgumentException("CameraId is required.", nameof(cameraId));
        }

        var trimmedCameraId = cameraId.Trim();

        if (trimmedCameraId.Length > 64)
        {
            throw new ArgumentException("CameraId cannot exceed 64 characters.", nameof(cameraId));
        }

        if (occurredAt.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("OccurredAt must be UTC.", nameof(occurredAt));
        }

        if (ingestedAt.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("IngestedAt must be UTC.", nameof(ingestedAt));
        }

        if (ingestedAt < occurredAt)
        {
            throw new ArgumentException("IngestedAt cannot be earlier than OccurredAt.", nameof(ingestedAt));
        }

        if (detections is null)
        {
            throw new ArgumentException("Detections collection is required.", nameof(detections));
        }

        Id = default;
        EventId = eventId;
        EventType = eventType;
        Severity = severity;
        CameraId = trimmedCameraId;
        OccurredAt = occurredAt;
        IngestedAt = ingestedAt;
        _detections.AddRange(detections);
    }
}
