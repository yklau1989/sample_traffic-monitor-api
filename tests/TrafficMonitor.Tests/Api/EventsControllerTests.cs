using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TrafficMonitor.Application.Commands.IngestTrafficEvent;
using TrafficMonitor.Application.Queries.ListTrafficEvents;
using TrafficMonitor.Application.Repositories;
using TrafficMonitor.Domain.Entities;
using TrafficMonitor.Domain.Enums;

namespace TrafficMonitor.Tests.Api;

public sealed class EventsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
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

    private sealed class InMemoryTrafficEventRepository : ITrafficEventRepository
    {
        private readonly List<TrafficEvent> _events = [];

        public Task<TrafficEvent?> FindByEventIdAsync(Guid eventId, CancellationToken cancellationToken)
        {
            var found = _events.FirstOrDefault(e => e.EventId == eventId);
            return Task.FromResult(found);
        }

        public Task AddAsync(TrafficEvent trafficEvent, CancellationToken cancellationToken)
        {
            _events.Add(trafficEvent);
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_events.Count);
        }

        public Task<PagedResult<EventListItemDto>> ListAsync(
            ListTrafficEventsQuery query,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
