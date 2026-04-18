using System.ComponentModel.DataAnnotations;
using TrafficMonitor.Domain.Enums;

namespace TrafficMonitor.Application.Commands.IngestTrafficEvent;

public sealed record TrafficEventInput(
    Guid EventId,
    EventType EventType,
    Severity Severity,
    [Required]
    [StringLength(64, MinimumLength = 1)]
    string? CameraId,
    DateTime OccurredAt,
    [Required]
    IReadOnlyCollection<DetectionInput>? Detections);
