namespace TrafficMonitor.Application.Abstractions.Persistence;

using TrafficMonitor.Domain.Entities;

public interface ITrafficEventRepository
{
    Task AddAsync(TrafficEvent trafficEvent, CancellationToken cancellationToken);

    Task<TrafficEvent?> FindByEventIdAsync(Guid eventId, CancellationToken cancellationToken);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
