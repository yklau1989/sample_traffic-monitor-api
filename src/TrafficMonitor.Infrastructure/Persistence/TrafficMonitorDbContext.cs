using Microsoft.EntityFrameworkCore;
using TrafficMonitor.Domain.Entities;

namespace TrafficMonitor.Infrastructure.Persistence;

public class TrafficMonitorDbContext : DbContext
{
    public DbSet<TrafficEvent> TrafficEvents => Set<TrafficEvent>();

    public TrafficMonitorDbContext(DbContextOptions<TrafficMonitorDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TrafficMonitorDbContext).Assembly);
    }
}
