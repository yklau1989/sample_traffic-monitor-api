using TrafficMonitor.Domain.Enums;

namespace TrafficMonitor.Application.Queries.ListTrafficEvents;

public sealed record ListTrafficEventsQuery(
    EventType? EventType = null,
    Severity? Severity = null,
    DateTime? From = null,
    DateTime? To = null,
    string? CameraId = null,
    string Sort = "occurredAt",
    int Page = 1,
    int PageSize = 50);
