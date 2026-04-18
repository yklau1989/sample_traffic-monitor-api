using Microsoft.EntityFrameworkCore;
using TrafficMonitor.Application.Repositories;
using TrafficMonitor.Domain.Entities;
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

    public async Task AddAsync(TrafficEvent trafficEvent, CancellationToken cancellationToken)
    {
        await _dbContext.TrafficEvents.AddAsync(trafficEvent, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
