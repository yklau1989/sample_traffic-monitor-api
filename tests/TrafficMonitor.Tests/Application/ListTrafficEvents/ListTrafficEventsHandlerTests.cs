using System.Globalization;
using TrafficMonitor.Application.Exceptions;
using TrafficMonitor.Application.Queries.ListTrafficEvents;
using TrafficMonitor.Application.Repositories;
using TrafficMonitor.Domain.Entities;
using TrafficMonitor.Domain.Enums;
using TrafficMonitor.Domain.ValueObjects;

namespace TrafficMonitor.Tests.Application.ListTrafficEvents;

public class ListTrafficEventsHandlerTests
{
    private static readonly DateTime BaseOccurredAt = new(2026, 4, 18, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task HandleAsync_WhenNoEventsMatch_ReturnsEmptyPagedResult()
    {
        var repository = new FakeTrafficEventRepository(
            [
                CreateTrafficEvent(eventType: EventType.Accident)
            ]);
        var handler = new ListTrafficEventsHandler(repository);
        var query = new ListTrafficEventsQuery(EventType: EventType.Pedestrian);

        var result = await handler.HandleAsync(query, CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.Total);
        Assert.Equal(1, result.Page);
        Assert.Equal(50, result.PageSize);
    }

    [Fact]
    public async Task HandleAsync_WhenFilteringByEventType_ReturnsOnlyMatchingEvents()
    {
        var repository = new FakeTrafficEventRepository(
            [
                CreateTrafficEvent(eventType: EventType.Accident),
                CreateTrafficEvent(eventType: EventType.Debris, occurredAt: BaseOccurredAt.AddMinutes(1))
            ]);
        var handler = new ListTrafficEventsHandler(repository);

        var result = await handler.HandleAsync(
            new ListTrafficEventsQuery(EventType: EventType.Accident),
            CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal("Accident", item.EventType);
        Assert.Contains("detections", item.DetectionSummary);
    }

    [Fact]
    public async Task HandleAsync_WhenFilteringBySeverity_ReturnsOnlyMatchingEvents()
    {
        var repository = new FakeTrafficEventRepository(
            [
                CreateTrafficEvent(severity: Severity.Critical),
                CreateTrafficEvent(severity: Severity.Low, occurredAt: BaseOccurredAt.AddMinutes(1))
            ]);
        var handler = new ListTrafficEventsHandler(repository);

        var result = await handler.HandleAsync(
            new ListTrafficEventsQuery(Severity: Severity.Critical),
            CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal("Critical", item.Severity);
    }

    [Fact]
    public async Task HandleAsync_WhenFilteringByCameraId_ReturnsOnlyMatchingEvents()
    {
        var repository = new FakeTrafficEventRepository(
            [
                CreateTrafficEvent(cameraId: "camera-a"),
                CreateTrafficEvent(cameraId: "camera-b", occurredAt: BaseOccurredAt.AddMinutes(1))
            ]);
        var handler = new ListTrafficEventsHandler(repository);

        var result = await handler.HandleAsync(
            new ListTrafficEventsQuery(CameraId: "camera-b"),
            CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal("camera-b", item.CameraId);
    }

    [Fact]
    public async Task HandleAsync_WhenFilteringByOccurredAtRange_ReturnsOnlyEventsWithinRange()
    {
        var repository = new FakeTrafficEventRepository(
            [
                CreateTrafficEvent(occurredAt: BaseOccurredAt.AddMinutes(-10)),
                CreateTrafficEvent(occurredAt: BaseOccurredAt.AddMinutes(5)),
                CreateTrafficEvent(occurredAt: BaseOccurredAt.AddMinutes(20))
            ]);
        var handler = new ListTrafficEventsHandler(repository);

        var result = await handler.HandleAsync(
            new ListTrafficEventsQuery(
                From: BaseOccurredAt,
                To: BaseOccurredAt.AddMinutes(10)),
            CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(BaseOccurredAt.AddMinutes(5), item.OccurredAt);
    }

    [Fact]
    public async Task HandleAsync_WhenCombiningFilters_UsesAndSemantics()
    {
        var repository = new FakeTrafficEventRepository(
            [
                CreateTrafficEvent(
                    eventType: EventType.Accident,
                    severity: Severity.High,
                    cameraId: "camera-a",
                    occurredAt: BaseOccurredAt.AddMinutes(2)),
                CreateTrafficEvent(
                    eventType: EventType.Accident,
                    severity: Severity.Low,
                    cameraId: "camera-a",
                    occurredAt: BaseOccurredAt.AddMinutes(3)),
                CreateTrafficEvent(
                    eventType: EventType.Debris,
                    severity: Severity.High,
                    cameraId: "camera-a",
                    occurredAt: BaseOccurredAt.AddMinutes(4))
            ]);
        var handler = new ListTrafficEventsHandler(repository);

        var result = await handler.HandleAsync(
            new ListTrafficEventsQuery(
                EventType: EventType.Accident,
                Severity: Severity.High,
                From: BaseOccurredAt,
                To: BaseOccurredAt.AddMinutes(2),
                CameraId: "camera-a"),
            CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal("Accident", item.EventType);
        Assert.Equal("High", item.Severity);
        Assert.Equal("camera-a", item.CameraId);
    }

    [Fact]
    public async Task HandleAsync_WhenSortingAscending_OrdersByOccurredAt()
    {
        var repository = new FakeTrafficEventRepository(
            [
                CreateTrafficEvent(occurredAt: BaseOccurredAt.AddMinutes(10)),
                CreateTrafficEvent(occurredAt: BaseOccurredAt.AddMinutes(2)),
                CreateTrafficEvent(occurredAt: BaseOccurredAt.AddMinutes(5))
            ]);
        var handler = new ListTrafficEventsHandler(repository);

        var result = await handler.HandleAsync(
            new ListTrafficEventsQuery(Sort: "occurredAt"),
            CancellationToken.None);

        Assert.Equal(
            [
                BaseOccurredAt.AddMinutes(2),
                BaseOccurredAt.AddMinutes(5),
                BaseOccurredAt.AddMinutes(10)
            ],
            result.Items.Select(item => item.OccurredAt));
    }

    [Fact]
    public async Task HandleAsync_WhenSortingDescending_OrdersByOccurredAt()
    {
        var repository = new FakeTrafficEventRepository(
            [
                CreateTrafficEvent(occurredAt: BaseOccurredAt.AddMinutes(10)),
                CreateTrafficEvent(occurredAt: BaseOccurredAt.AddMinutes(2)),
                CreateTrafficEvent(occurredAt: BaseOccurredAt.AddMinutes(5))
            ]);
        var handler = new ListTrafficEventsHandler(repository);

        var result = await handler.HandleAsync(
            new ListTrafficEventsQuery(Sort: "-occurredAt"),
            CancellationToken.None);

        Assert.Equal(
            [
                BaseOccurredAt.AddMinutes(10),
                BaseOccurredAt.AddMinutes(5),
                BaseOccurredAt.AddMinutes(2)
            ],
            result.Items.Select(item => item.OccurredAt));
    }

    [Fact]
    public async Task HandleAsync_WhenPageRequested_ReturnsRequestedSlice()
    {
        var repository = new FakeTrafficEventRepository(
            [
                CreateTrafficEvent(occurredAt: BaseOccurredAt.AddMinutes(1)),
                CreateTrafficEvent(occurredAt: BaseOccurredAt.AddMinutes(2)),
                CreateTrafficEvent(occurredAt: BaseOccurredAt.AddMinutes(3))
            ]);
        var handler = new ListTrafficEventsHandler(repository);

        var result = await handler.HandleAsync(
            new ListTrafficEventsQuery(Page: 2, PageSize: 2),
            CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(BaseOccurredAt.AddMinutes(3), item.OccurredAt);
        Assert.Equal(3, result.Total);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
    }

    [Fact]
    public async Task HandleAsync_WhenPageIsBeyondAvailableItems_ReturnsEmptyPage()
    {
        var repository = new FakeTrafficEventRepository(
            [
                CreateTrafficEvent(occurredAt: BaseOccurredAt.AddMinutes(1)),
                CreateTrafficEvent(occurredAt: BaseOccurredAt.AddMinutes(2))
            ]);
        var handler = new ListTrafficEventsHandler(repository);

        var result = await handler.HandleAsync(
            new ListTrafficEventsQuery(Page: 3, PageSize: 2),
            CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(2, result.Total);
        Assert.Equal(3, result.Page);
        Assert.Equal(2, result.PageSize);
    }

    [Fact]
    public async Task HandleAsync_WhenPageSizeExceedsCap_CapsAt200()
    {
        var repository = new FakeTrafficEventRepository(
            Enumerable.Range(0, 250)
                .Select(index => CreateTrafficEvent(occurredAt: BaseOccurredAt.AddMinutes(index)))
                .ToArray());
        var handler = new ListTrafficEventsHandler(repository);

        var result = await handler.HandleAsync(
            new ListTrafficEventsQuery(PageSize: 500),
            CancellationToken.None);

        Assert.Equal(250, result.Total);
        Assert.Equal(200, result.PageSize);
        Assert.Equal(200, result.Items.Count);
    }

    [Fact]
    public async Task HandleAsync_WhenSortFieldIsUnknown_ThrowsInvalidSortFieldException()
    {
        var handler = new ListTrafficEventsHandler(
            new FakeTrafficEventRepository(
                [
                    CreateTrafficEvent()
                ]));

        await Assert.ThrowsAsync<InvalidSortFieldException>(
            () => handler.HandleAsync(new ListTrafficEventsQuery(Sort: "cameraId"), CancellationToken.None));
    }

    private static TrafficEvent CreateTrafficEvent(
        EventType eventType = EventType.Accident,
        Severity severity = Severity.High,
        string cameraId = "camera-01",
        DateTime? occurredAt = null,
        IReadOnlyCollection<Detection>? detections = null)
    {
        var actualOccurredAt = occurredAt ?? BaseOccurredAt;

        return new TrafficEvent(
            Guid.NewGuid(),
            eventType,
            severity,
            cameraId,
            actualOccurredAt,
            actualOccurredAt.AddMinutes(1),
            detections ?? CreateDetections());
    }

    private static IReadOnlyCollection<Detection> CreateDetections()
    {
        return
        [
            new Detection("car", 0.91, new BoundingBox(0.1, 0.2, 0.3, 0.4)),
            new Detection("truck", 0.78, new BoundingBox(0.4, 0.5, 0.2, 0.1))
        ];
    }

    private sealed class FakeTrafficEventRepository : ITrafficEventRepository
    {
        private readonly IReadOnlyList<TrafficEvent> _store;

        public FakeTrafficEventRepository(IReadOnlyList<TrafficEvent> store)
        {
            _store = store;
        }

        public Task<TrafficEvent?> FindByEventIdAsync(Guid eventId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(_store.FirstOrDefault(trafficEvent => trafficEvent.EventId == eventId));
        }

        public Task<PagedResult<EventListItemDto>> ListAsync(
            ListTrafficEventsQuery query,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(query);
            cancellationToken.ThrowIfCancellationRequested();

            var page = query.Page > 0 ? query.Page : 1;
            var pageSize = query.PageSize switch
            {
                <= 0 => 50,
                > 200 => 200,
                _ => query.PageSize
            };

            IEnumerable<TrafficEvent> filteredEvents = _store;

            if (query.EventType is not null)
            {
                filteredEvents = filteredEvents.Where(trafficEvent => trafficEvent.EventType == query.EventType.Value);
            }

            if (query.Severity is not null)
            {
                filteredEvents = filteredEvents.Where(trafficEvent => trafficEvent.Severity == query.Severity.Value);
            }

            if (query.From is not null)
            {
                filteredEvents = filteredEvents.Where(trafficEvent => trafficEvent.OccurredAt >= query.From.Value);
            }

            if (query.To is not null)
            {
                filteredEvents = filteredEvents.Where(trafficEvent => trafficEvent.OccurredAt <= query.To.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.CameraId))
            {
                var cameraId = query.CameraId.Trim();
                filteredEvents = filteredEvents.Where(trafficEvent => trafficEvent.CameraId == cameraId);
            }

            var total = filteredEvents.Count();
            var normalizedSort = string.IsNullOrWhiteSpace(query.Sort) ? "occurredAt" : query.Sort.Trim();

            var orderedEvents = normalizedSort switch
            {
                "occurredAt" => filteredEvents
                    .OrderBy(trafficEvent => trafficEvent.OccurredAt)
                    .ThenBy(trafficEvent => trafficEvent.EventId),
                "-occurredAt" => filteredEvents
                    .OrderByDescending(trafficEvent => trafficEvent.OccurredAt)
                    .ThenByDescending(trafficEvent => trafficEvent.EventId),
                _ => throw new InvalidSortFieldException(normalizedSort)
            };

            var items = orderedEvents
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(
                    trafficEvent => new EventListItemDto(
                        trafficEvent.EventId,
                        trafficEvent.EventType.ToString(),
                        trafficEvent.Severity.ToString(),
                        trafficEvent.CameraId,
                        trafficEvent.OccurredAt,
                        trafficEvent.IngestedAt,
                        BuildDetectionSummary(trafficEvent.Detections)))
                .ToArray();

            return Task.FromResult(new PagedResult<EventListItemDto>(items, total, page, pageSize));
        }

        public Task AddAsync(TrafficEvent trafficEvent, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new NotSupportedException();
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new NotSupportedException();
        }

        private static string BuildDetectionSummary(IReadOnlyCollection<Detection> detections)
        {
            if (detections.Count == 0)
            {
                return "0 detections";
            }

            var preview = detections
                .Take(3)
                .Select(detection => $"{detection.Label} {detection.Confidence.ToString("0.##", CultureInfo.InvariantCulture)}");
            var remainder = detections.Count > 3 ? $" +{detections.Count - 3} more" : string.Empty;
            var noun = detections.Count == 1 ? "detection" : "detections";

            return $"{detections.Count} {noun}: {string.Join(", ", preview)}{remainder}";
        }
    }
}
