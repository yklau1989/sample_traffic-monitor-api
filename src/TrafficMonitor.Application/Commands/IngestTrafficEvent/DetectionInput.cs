using System.ComponentModel.DataAnnotations;

namespace TrafficMonitor.Application.Commands.IngestTrafficEvent;

public sealed record DetectionInput(
    [Required]
    [StringLength(128, MinimumLength = 1)]
    string? Label,
    [Range(0.0, 1.0)]
    double Confidence,
    [Required]
    BoundingBoxInput? BoundingBox);
