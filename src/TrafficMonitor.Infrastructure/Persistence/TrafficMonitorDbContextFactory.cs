using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TrafficMonitor.Infrastructure.Persistence;

public class TrafficMonitorDbContextFactory : IDesignTimeDbContextFactory<TrafficMonitorDbContext>
{
    public TrafficMonitorDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings:Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is not configured.");

        var optionsBuilder = new DbContextOptionsBuilder<TrafficMonitorDbContext>();

        optionsBuilder.UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention();

        return new TrafficMonitorDbContext(optionsBuilder.Options);
    }
}
