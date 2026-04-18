using TrafficMonitor.Application.Queries.ListTrafficEvents;
using TrafficMonitor.Domain.Entities;

namespace TrafficMonitor.Application.Repositories;

public interface ITrafficEventRepository
{
    Task<TrafficEvent?> FindByEventIdAsync(Guid eventId, CancellationToken cancellationToken);

    Task<PagedResult<EventListItemDto>> ListAsync(
        ListTrafficEventsQuery query,
        CancellationToken cancellationToken);

    Task AddAsync(TrafficEvent trafficEvent, CancellationToken cancellationToken);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
