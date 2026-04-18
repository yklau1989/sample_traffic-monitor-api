using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TrafficMonitor.Application.Commands.IngestTrafficEvent;
using TrafficMonitor.Application.Exceptions;
using TrafficMonitor.Application.Queries.ListTrafficEvents;
using TrafficMonitor.Application.Repositories;
using TrafficMonitor.Domain.Entities;
using TrafficMonitor.Domain.Enums;
using TrafficMonitor.Domain.ValueObjects;

namespace TrafficMonitor.Tests.Api;

public sealed class EventsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly DateTime BaseOccurredAt = new(2026, 4, 18, 0, 0, 0, DateTimeKind.Utc);

    private readonly WebApplicationFactory<Program> _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };

    static EventsControllerTests()
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__Postgres",
            "Host=localhost;Database=traffic_monitor_tests;Username=postgres;Password=postgres");
    }

    public EventsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostAsync_NewEvent_Returns201WithLocationHeaderAsync()
    {
        var stub = new InMemoryTrafficEventRepository();
        using var factory = CreateFactoryWithStub(stub);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var input = ValidInput();
        using var response = await client.PostAsJsonAsync("/api/events", input, JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/events/{input.EventId}", response.Headers.Location?.ToString());

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(input.EventId.ToString(), body.GetProperty("eventId").GetString());
    }

    [Fact]
    public async Task PostAsync_DuplicateEvent_Returns200WithLocationHeaderAsync()
    {
        var stub = new InMemoryTrafficEventRepository();
        using var factory = CreateFactoryWithStub(stub);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var input = ValidInput();
        using var first = await client.PostAsJsonAsync("/api/events", input, JsonOptions);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        using var second = await client.PostAsJsonAsync("/api/events", input, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal($"/api/events/{input.EventId}", second.Headers.Location?.ToString());
    }

    [Fact]
    public async Task PostAsync_MissingCameraId_Returns400WithErrorsAsync()
    {
        var stub = new InMemoryTrafficEventRepository();
        using var factory = CreateFactoryWithStub(stub);
        using var client = factory.CreateClient();

        var input = ValidInput() with { CameraId = null };
        using var response = await client.PostAsJsonAsync("/api/events", input, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("errors", out var errors));

        var hasKey = errors.EnumerateObject().Any(p =>
            p.Name.Equals("CameraId", StringComparison.OrdinalIgnoreCase));
        Assert.True(hasKey);
    }

    [Fact]
    public async Task PostAsync_EmptyGuid_Returns422Async()
    {
        var stub = new InMemoryTrafficEventRepository();
        using var factory = CreateFactoryWithStub(stub);
        using var client = factory.CreateClient();

        var input = ValidInput(Guid.Empty);
        using var response = await client.PostAsJsonAsync("/api/events", input, JsonOptions);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task ListAsync_WhenEventsExist_ReturnsPagedEnvelopeAsync()
    {
        var stub = new InMemoryTrafficEventRepository(
            [
                CreateTrafficEvent(
                    eventId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    occurredAt: BaseOccurredAt.AddMinutes(2)),
                CreateTrafficEvent(
                    eventId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    occurredAt: BaseOccurredAt.AddMinutes(1))
            ]);
        using var factory = CreateFactoryWithStub(stub);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<EventListItemDto>>(JsonOptions);

        Assert.NotNull(body);
        Assert.Equal(2, body!.Total);
        Assert.Equal(1, body.Page);
        Assert.Equal(50, body.PageSize);
        Assert.Equal(2, body.Items.Count);
        Assert.Equal(
            [
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Guid.Parse("11111111-1111-1111-1111-111111111111")
            ],
            body.Items.Select(item => item.EventId));
    }

    [Fact]
    public async Task ListAsync_WhenFilteringBySeverity_ReturnsMatchingEventsAsync()
    {
        var stub = new InMemoryTrafficEventRepository(
            [
                CreateTrafficEvent(severity: Severity.High, occurredAt: BaseOccurredAt.AddMinutes(1)),
                CreateTrafficEvent(severity: Severity.Low, occurredAt: BaseOccurredAt.AddMinutes(2)),
                CreateTrafficEvent(severity: Severity.High, occurredAt: BaseOccurredAt.AddMinutes(3))
            ]);
        using var factory = CreateFactoryWithStub(stub);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/events?severity=high");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedResult<EventListItemDto>>(JsonOptions);

        Assert.NotNull(body);
        Assert.Equal(2, body!.Total);
        Assert.Equal(2, body.Items.Count);
        Assert.All(body.Items, item => Assert.Equal("High", item.Severity));
    }

    [Fact]
    public async Task ListAsync_WhenSortFieldIsUnknown_Returns400ProblemDetailsAsync()
    {
        var stub = new InMemoryTrafficEventRepository(
            [
                CreateTrafficEvent()
            ]);
        using var factory = CreateFactoryWithStub(stub);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/events?sort=foo");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);

        Assert.NotNull(body);
        Assert.Equal("Invalid sort field", body!.Title);
        Assert.Equal(400, body.Status);
        Assert.Equal("Unknown sort field: foo", body.Detail);
    }

    [Fact]
    public async Task ListAsync_WhenFromTimestampIsNotUtc_Returns400ProblemDetailsAsync()
    {
        var stub = new InMemoryTrafficEventRepository(
            [
                CreateTrafficEvent()
            ]);
        using var factory = CreateFactoryWithStub(stub);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/events?from=2026-01-01T00:00:00");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonOptions);

        Assert.NotNull(body);
        Assert.Equal("Invalid timestamp", body!.Title);
        Assert.Equal(400, body.Status);
        Assert.Equal("The from query parameter must be a UTC timestamp.", body.Detail);
    }

    private WebApplicationFactory<Program> CreateFactoryWithStub(InMemoryTrafficEventRepository stub)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.Single(d => d.ServiceType == typeof(ITrafficEventRepository));
                services.Remove(descriptor);
                services.AddSingleton<ITrafficEventRepository>(stub);
            });
        });
    }

    private static TrafficEventInput ValidInput(Guid? eventId = null)
    {
        return new TrafficEventInput(
            eventId ?? Guid.NewGuid(),
            EventType.Debris,
            Severity.Low,
            "cam-01",
            DateTime.UtcNow,
            [new DetectionInput("debris", 0.9, new BoundingBoxInput(0, 0, 100, 100))]);
    }

    private static TrafficEvent CreateTrafficEvent(
        Guid? eventId = null,
        EventType eventType = EventType.Accident,
        Severity severity = Severity.High,
        string cameraId = "camera-01",
        DateTime? occurredAt = null,
        IReadOnlyCollection<Detection>? detections = null)
    {
        var actualOccurredAt = occurredAt ?? BaseOccurredAt;

        return new TrafficEvent(
            eventId ?? Guid.NewGuid(),
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

    private sealed class InMemoryTrafficEventRepository : ITrafficEventRepository
    {
        private readonly List<TrafficEvent> _events;

        public InMemoryTrafficEventRepository(IEnumerable<TrafficEvent>? seed = null)
        {
            _events = seed?.ToList() ?? [];
        }

        public Task<TrafficEvent?> FindByEventIdAsync(Guid eventId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var found = _events.FirstOrDefault(e => e.EventId == eventId);
            return Task.FromResult(found);
        }

        public Task AddAsync(TrafficEvent trafficEvent, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _events.Add(trafficEvent);
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_events.Count);
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

            IEnumerable<TrafficEvent> filteredEvents = _events;

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
