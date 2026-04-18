using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TrafficMonitor.Application.Exceptions;
using TrafficMonitor.Application.Queries.ListTrafficEvents;
using TrafficMonitor.Application.Repositories;
using TrafficMonitor.Domain.Entities;
using TrafficMonitor.Domain.ValueObjects;
using TrafficMonitor.Infrastructure.Persistence;

namespace TrafficMonitor.Infrastructure.Repositories;

public class TrafficEventRepository : ITrafficEventRepository
{
    private readonly TrafficMonitorDbContext _dbContext;

    public TrafficEventRepository(TrafficMonitorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TrafficEvent?> FindByEventIdAsync(Guid eventId, CancellationToken cancellationToken) =>
        _dbContext.TrafficEvents.FirstOrDefaultAsync(trafficEvent => trafficEvent.EventId == eventId, cancellationToken);

    public async Task<PagedResult<EventListItemDto>> ListAsync(
        ListTrafficEventsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var page = query.Page > 0 ? query.Page : 1;
        var pageSize = query.PageSize switch
        {
            <= 0 => 50,
            > 200 => 200,
            _ => query.PageSize
        };

        var filteredQuery = ApplyFilters(_dbContext.TrafficEvents.AsNoTracking(), query);
        var orderedQuery = ApplySorting(filteredQuery, query.Sort);
        var total = await filteredQuery.CountAsync(cancellationToken);

        var trafficEvents = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Detections are stored through a JSONB value converter, so DetectionSummary must be composed in memory.
        var items = trafficEvents
            .Select(MapToEventListItemDto)
            .ToArray();

        return new PagedResult<EventListItemDto>(items, total, page, pageSize);
    }

    public async Task AddAsync(TrafficEvent trafficEvent, CancellationToken cancellationToken)
    {
        await _dbContext.TrafficEvents.AddAsync(trafficEvent, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        _dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<TrafficEvent> ApplyFilters(
        IQueryable<TrafficEvent> query,
        ListTrafficEventsQuery request)
    {
        if (request.EventType is not null)
        {
            query = query.Where(trafficEvent => trafficEvent.EventType == request.EventType.Value);
        }

        if (request.Severity is not null)
        {
            query = query.Where(trafficEvent => trafficEvent.Severity == request.Severity.Value);
        }

        if (request.From is not null)
        {
            query = query.Where(trafficEvent => trafficEvent.OccurredAt >= request.From.Value);
        }

        if (request.To is not null)
        {
            query = query.Where(trafficEvent => trafficEvent.OccurredAt <= request.To.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.CameraId))
        {
            var cameraId = request.CameraId.Trim();
            query = query.Where(trafficEvent => trafficEvent.CameraId == cameraId);
        }

        return query;
    }

    private static IQueryable<TrafficEvent> ApplySorting(
        IQueryable<TrafficEvent> query,
        string sort)
    {
        var normalizedSort = string.IsNullOrWhiteSpace(sort) ? "occurredAt" : sort.Trim();

        return normalizedSort switch
        {
            "occurredAt" => query
                .OrderBy(trafficEvent => trafficEvent.OccurredAt)
                .ThenBy(trafficEvent => trafficEvent.EventId),
            "-occurredAt" => query
                .OrderByDescending(trafficEvent => trafficEvent.OccurredAt)
                .ThenByDescending(trafficEvent => trafficEvent.EventId),
            _ => throw new InvalidSortFieldException(normalizedSort)
        };
    }

    private static EventListItemDto MapToEventListItemDto(TrafficEvent trafficEvent)
    {
        return new EventListItemDto(
            trafficEvent.EventId,
            trafficEvent.EventType.ToString(),
            trafficEvent.Severity.ToString(),
            trafficEvent.CameraId,
            trafficEvent.OccurredAt,
            trafficEvent.IngestedAt,
            BuildDetectionSummary(trafficEvent.Detections));
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
