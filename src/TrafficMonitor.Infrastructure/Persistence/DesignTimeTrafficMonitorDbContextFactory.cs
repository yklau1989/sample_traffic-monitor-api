namespace TrafficMonitor.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TrafficMonitor.Infrastructure.Configuration;

public sealed class DesignTimeTrafficMonitorDbContextFactory : IDesignTimeDbContextFactory<TrafficMonitorDbContext>
{
    public TrafficMonitorDbContext CreateDbContext(string[] args)
    {
        var contentRootPath = Directory.GetCurrentDirectory();
        var environmentValues = PostgresConnectionStringResolver.LoadEnvironmentValues(contentRootPath);
        var connectionString = PostgresConnectionStringResolver.Resolve(environmentValues);
        var optionsBuilder = new DbContextOptionsBuilder<TrafficMonitorDbContext>();

        optionsBuilder.UseNpgsql(connectionString);

        return new TrafficMonitorDbContext(optionsBuilder.Options);
    }
}
