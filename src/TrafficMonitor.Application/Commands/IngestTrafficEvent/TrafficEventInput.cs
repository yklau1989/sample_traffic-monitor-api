using System.ComponentModel.DataAnnotations;
using TrafficMonitor.Domain.Enums;

namespace TrafficMonitor.Application.Commands.IngestTrafficEvent;

public sealed record TrafficEventInput(
    Guid EventId,
    EventType EventType,
    Severity Severity,
    [property: Required]
    [property: StringLength(64, MinimumLength = 1)]
    string? CameraId,
    DateTime OccurredAt,
    [property: Required]
    IReadOnlyCollection<DetectionInput>? Detections);
