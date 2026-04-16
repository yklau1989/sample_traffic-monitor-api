namespace TrafficMonitor.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using TrafficMonitor.Domain.Entities;

public sealed class TrafficMonitorDbContext(DbContextOptions<TrafficMonitorDbContext> options) : DbContext(options)
{
    public DbSet<TrafficEvent> TrafficEvents => Set<TrafficEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TrafficMonitorDbContext).Assembly);
    }
}
