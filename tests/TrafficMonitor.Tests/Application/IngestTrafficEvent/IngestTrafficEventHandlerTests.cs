using System.ComponentModel.DataAnnotations;
using TrafficMonitor.Application.Commands.IngestTrafficEvent;
using TrafficMonitor.Application.Repositories;
using TrafficMonitor.Domain.Entities;
using TrafficMonitor.Domain.Enums;
using TrafficMonitor.Domain.ValueObjects;

namespace TrafficMonitor.Tests.Application.IngestTrafficEvent;

public class IngestTrafficEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithNewEvent_InsertsEventAndReturnsNonDuplicateResult()
    {
        var repository = new FakeTrafficEventRepository();
        var handler = new IngestTrafficEventHandler(repository);
        var command = CreateCommand();

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(command.Input.EventId, result.EventId);
        Assert.False(result.WasDuplicate);
        Assert.Single(repository.Store);
        Assert.Equal(1, repository.SaveCallCount);

        var storedTrafficEvent = repository.Store[0];
        Assert.Equal(command.Input.EventId, storedTrafficEvent.EventId);
        Assert.Equal(command.Input.EventType, storedTrafficEvent.EventType);
        Assert.Equal(command.Input.Severity, storedTrafficEvent.Severity);
        Assert.Equal("camera-01", storedTrafficEvent.CameraId);
        Assert.Equal(command.Input.OccurredAt, storedTrafficEvent.OccurredAt);
        Assert.Equal(DateTimeKind.Utc, storedTrafficEvent.IngestedAt.Kind);
        Assert.Equal(2, storedTrafficEvent.Detections.Count);
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateEvent_ReturnsDuplicateResultWithoutInserting()
    {
        var existingEvent = CreateTrafficEvent();
        var repository = new FakeTrafficEventRepository(existingEvent);
        var handler = new IngestTrafficEventHandler(repository);
        var command = new IngestTrafficEventCommand(CreateInput(existingEvent.EventId, "camera-01", DateTime.UtcNow.AddMinutes(-1)));

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(existingEvent.EventId, result.EventId);
        Assert.True(result.WasDuplicate);
        Assert.Single(repository.Store);
        Assert.Equal(0, repository.SaveCallCount);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyEventId_ThrowsArgumentException()
    {
        var handler = new IngestTrafficEventHandler(new FakeTrafficEventRepository());
        var command = CreateCommand(eventId: Guid.Empty);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(command, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_WithNonUtcOccurredAt_ThrowsArgumentException()
    {
        var handler = new IngestTrafficEventHandler(new FakeTrafficEventRepository());
        var command = CreateCommand(occurredAt: new DateTime(2026, 4, 18, 9, 0, 0, DateTimeKind.Local));

        await Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(command, CancellationToken.None));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_WithMissingCameraId_ThrowsException(string? cameraId)
    {
        var handler = new IngestTrafficEventHandler(new FakeTrafficEventRepository());
        var command = new IngestTrafficEventCommand(
            CreateInput(Guid.NewGuid(), cameraId, DateTime.UtcNow.AddMinutes(-1)));

        await Assert.ThrowsAnyAsync<Exception>(() => handler.HandleAsync(command, CancellationToken.None));
    }

    private static IngestTrafficEventCommand CreateCommand(
        Guid? eventId = null,
        string? cameraId = null,
        DateTime? occurredAt = null)
    {
        return new IngestTrafficEventCommand(
            CreateInput(
                eventId ?? Guid.NewGuid(),
                cameraId ?? "camera-01",
                occurredAt ?? DateTime.UtcNow.AddMinutes(-1)));
    }

    private static TrafficEventInput CreateInput(Guid eventId, string? cameraId, DateTime occurredAt)
    {
        return new TrafficEventInput(
            eventId,
            EventType.Accident,
            Severity.High,
            cameraId,
            occurredAt,
            [
                new DetectionInput("car", 0.91, new BoundingBoxInput(0.1, 0.2, 0.3, 0.4)),
                new DetectionInput("truck", 0.78, new BoundingBoxInput(0.4, 0.5, 0.2, 0.1))
            ]);
    }

    private static TrafficEvent CreateTrafficEvent()
    {
        return new TrafficEvent(
            Guid.NewGuid(),
            EventType.Accident,
            Severity.High,
            "camera-01",
            DateTime.UtcNow.AddMinutes(-2),
            DateTime.UtcNow.AddMinutes(-1),
            [
                new Detection("car", 0.91, new BoundingBox(0.1, 0.2, 0.3, 0.4))
            ]);
    }

    private sealed class FakeTrafficEventRepository : ITrafficEventRepository
    {
        private readonly List<TrafficEvent> _store = new();

        public FakeTrafficEventRepository(TrafficEvent? existingEvent = null)
        {
            if (existingEvent is not null)
            {
                _store.Add(existingEvent);
            }
        }

        public int SaveCallCount { get; private set; }

        public IReadOnlyList<TrafficEvent> Store => _store;

        public Task<TrafficEvent?> FindByEventIdAsync(Guid eventId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_store.FirstOrDefault(trafficEvent => trafficEvent.EventId == eventId));
        }

        public Task AddAsync(TrafficEvent trafficEvent, CancellationToken cancellationToken)
        {
            _store.Add(trafficEvent);
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCallCount++;
            return Task.FromResult(1);
        }
    }
}
