namespace TrafficMonitor.Application.Queries.ListTrafficEvents;

public sealed record EventListItemDto(
    Guid EventId,
    string EventType,
    string Severity,
    string CameraId,
    DateTime OccurredAt,
    DateTime IngestedAt,
    string DetectionSummary);
