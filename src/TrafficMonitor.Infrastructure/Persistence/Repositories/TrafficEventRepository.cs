namespace TrafficMonitor.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using TrafficMonitor.Application.Abstractions.Persistence;
using TrafficMonitor.Domain.Entities;

public sealed class TrafficEventRepository(TrafficMonitorDbContext dbContext) : ITrafficEventRepository
{
    public async Task AddAsync(TrafficEvent trafficEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(trafficEvent);

        await dbContext.TrafficEvents.AddAsync(trafficEvent, cancellationToken);
    }

    public Task<TrafficEvent?> FindByEventIdAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return dbContext.TrafficEvents
            .SingleOrDefaultAsync(trafficEvent => trafficEvent.EventId == eventId, cancellationToken);
    }

    public async Task<IReadOnlyList<TrafficEvent>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.TrafficEvents
            .AsNoTracking()
            .OrderByDescending(trafficEvent => trafficEvent.OccurredAt)
            .ToListAsync(cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
