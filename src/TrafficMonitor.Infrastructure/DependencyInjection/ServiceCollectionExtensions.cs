namespace TrafficMonitor.Infrastructure.DependencyInjection;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TrafficMonitor.Application.Abstractions.Persistence;
using TrafficMonitor.Infrastructure.Configuration;
using TrafficMonitor.Infrastructure.Persistence;
using TrafficMonitor.Infrastructure.Persistence.Repositories;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string contentRootPath)
    {
        var connectionString = PostgresConnectionStringResolver.Resolve(configuration, contentRootPath);

        services.AddDbContext<TrafficMonitorDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ITrafficEventRepository, TrafficEventRepository>();

        return services;
    }
}
