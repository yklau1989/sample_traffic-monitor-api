using System.ComponentModel.DataAnnotations;

namespace TrafficMonitor.Application.Commands.IngestTrafficEvent;

public sealed record DetectionInput(
    [property: Required]
    [property: StringLength(128, MinimumLength = 1)]
    string? Label,
    [property: Range(0.0, 1.0)]
    double Confidence,
    [property: Required]
    BoundingBoxInput? BoundingBox);
