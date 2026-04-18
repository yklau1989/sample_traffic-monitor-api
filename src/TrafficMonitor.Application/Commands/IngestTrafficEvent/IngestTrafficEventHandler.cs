using System.ComponentModel.DataAnnotations;
using TrafficMonitor.Application.Repositories;
using TrafficMonitor.Domain.Entities;
using TrafficMonitor.Domain.ValueObjects;

namespace TrafficMonitor.Application.Commands.IngestTrafficEvent;

public sealed class IngestTrafficEventHandler
{
    private readonly ITrafficEventRepository _trafficEventRepository;

    public IngestTrafficEventHandler(ITrafficEventRepository trafficEventRepository)
    {
        _trafficEventRepository = trafficEventRepository;
    }

    public async Task<IngestTrafficEventResult> HandleAsync(
        IngestTrafficEventCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var input = command.Input ?? throw new ArgumentNullException(nameof(command.Input));
        var existingTrafficEvent = await _trafficEventRepository.FindByEventIdAsync(
            input.EventId,
            cancellationToken);

        if (existingTrafficEvent is not null)
        {
            return new IngestTrafficEventResult(existingTrafficEvent.EventId, true);
        }

        ValidateInput(input);

        var cameraId = input.CameraId ?? throw new ArgumentException("CameraId is required.", nameof(input.CameraId));
        var detections = input.Detections ?? throw new ArgumentException("Detections are required.", nameof(input.Detections));

        var trafficEvent = new TrafficEvent(
            input.EventId,
            input.EventType,
            input.Severity,
            cameraId,
            input.OccurredAt,
            DateTime.UtcNow,
            MapDetections(detections));

        await _trafficEventRepository.AddAsync(trafficEvent, cancellationToken);
        await _trafficEventRepository.SaveChangesAsync(cancellationToken);

        return new IngestTrafficEventResult(trafficEvent.EventId, false);
    }

    private static void ValidateInput(TrafficEventInput input)
    {
        Validator.ValidateObject(input, new ValidationContext(input), validateAllProperties: true);

        if (input.EventId == Guid.Empty)
        {
            throw new ArgumentException("EventId cannot be empty.", nameof(input.EventId));
        }

        if (input.OccurredAt.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("OccurredAt must be UTC.", nameof(input.OccurredAt));
        }

        if (string.IsNullOrWhiteSpace(input.CameraId))
        {
            throw new ArgumentException("CameraId is required.", nameof(input.CameraId));
        }

        foreach (var detection in input.Detections ?? [])
        {
            Validator.ValidateObject(detection, new ValidationContext(detection), validateAllProperties: true);
        }
    }

    private static IReadOnlyCollection<Detection> MapDetections(IReadOnlyCollection<DetectionInput> detectionInputs)
    {
        return detectionInputs
            .Select(
                detectionInput =>
                {
                    var label = detectionInput.Label
                        ?? throw new ArgumentException("Label is required.", nameof(detectionInput.Label));
                    var boundingBox = detectionInput.BoundingBox
                        ?? throw new ArgumentException("BoundingBox is required.", nameof(detectionInput.BoundingBox));

                    return new Detection(
                        label,
                        detectionInput.Confidence,
                        new BoundingBox(
                            boundingBox.X,
                            boundingBox.Y,
                            boundingBox.Width,
                            boundingBox.Height));
                })
            .ToArray();
    }
}
